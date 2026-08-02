import { apiGet, apiPost, apiDelete, websiteId, websiteIdAsNumber } from '../apiClient.js';
import { assertSameSite, describeChange } from '../safety.js';

export const pageTools = [
    {
        name: 'search_pages',
        description: 'Search pages by name, keywords, or topics. Returns matching pages with their status (draft|published).',
        inputSchema: {
            type: 'object',
            properties: {
                name: { type: 'string', description: 'Substring match on page name.' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } },
                description: { type: 'string' }
            }
        },
        handler: async (args) => apiPost('/page/search', {
            Name: args.name,
            Keywords: args.keywords || [],
            Topics: args.topics || [],
            Description: args.description,
            WebsiteId: websiteId()
        })
    },
    {
        name: 'get_page',
        description: 'Retrieve a page by ID including its articles, keywords, topics, and layout.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number', description: 'Page ID.' } },
            required: ['id']
        },
        handler: async (args) => apiGet(`/page/${args.id}/${websiteId()}`)
    },
    {
        name: 'create_page',
        description: 'Create a new page. Defaults to draft status so it does not appear on the public site until published.',
        inputSchema: {
            type: 'object',
            properties: {
                name: { type: 'string' },
                description: { type: 'string' },
                layout: { type: 'string' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['name']
        },
        handler: async (args) => apiPost('/page', {
            id: null,
            name: args.name,
            description: args.description || '',
            layout: args.layout || '',
            keywords: args.keywords || [],
            topics: args.topics || [],
            articles: [],
            websiteId: websiteIdAsNumber(),
            status: 'draft'
        })
    },
    {
        name: 'update_page',
        description: 'Update a page (name, description, layout, keywords, topics, articles). Pass the existing id. To link articles to the page, pass articles as an array of objects with id (article DB id) and sequence_no (display order, e.g. 5).',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number' },
                name: { type: 'string' },
                description: { type: 'string' },
                layout: { type: 'string' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } },
                articles: {
                    type: 'array',
                    description: 'Articles to associate with this page.',
                    items: {
                        type: 'object',
                        properties: {
                            id: { type: 'number', description: 'Article DB id.' },
                            sequence_no: { type: 'number', description: 'Display order within the page (e.g. 5).' }
                        },
                        required: ['id', 'sequence_no']
                    }
                }
            },
            required: ['id']
        },
        handler: async (args) => {
            // Fetch the current page so we can merge rather than overwrite
            const current = await apiGet(`/page/${args.id}/${websiteId()}`);
            if (!current || current.id == null) {
                throw new Error(`Refused: page ${args.id} not found on website ${websiteId()}. Nothing was written.`);
            }
            assertSameSite(current, 'page', args.id);
            const base = current;

            // articles may arrive as a JSON string (or double-encoded string) from the MCP harness — parse until we get an array
            let articleRefs = args.articles;
            while (typeof articleRefs === 'string') {
                try { articleRefs = JSON.parse(articleRefs); } catch (_) { articleRefs = undefined; break; }
            }

            // Saving a page RE-UPSERTS every article attached to it, and the article upsert
            // writes every column. The article list nested inside a page read is a partial
            // projection — it omits websiteId and status — so handing it straight back would
            // rewrite each article with websiteId 0 and status 'draft', orphaning it from the
            // site and unpublishing it.
            //
            // So whichever source the list comes from, always re-read each article in full by
            // id and carry only the sequence number from the caller.
            const refs = Array.isArray(articleRefs)
                ? articleRefs
                : (articleRefs !== undefined ? [] : (base.articles ?? []));

            const articles = await Promise.all(refs.map(async (ref) => {
                const full = await apiGet(`/article/${ref.id}`).catch(() => null);
                if (!full || full.id == null) {
                    throw new Error(
                        `Refused: could not re-read article ${ref.id} while updating page ${args.id}. ` +
                        `Saving the page would rewrite that article from an incomplete record. Nothing was written.`
                    );
                }
                return { ...full, sequence_no: ref.sequence_no ?? full.sequence_no ?? 5 };
            }));

            // UpsertPage writes `status ?? "draft"` unconditionally, so an update must carry the
            // page's current status or it silently unpublishes a live page. If the read did not
            // return a status we must NOT guess "draft" — that is the destructive direction.
            // Fall back to "published", then re-assert it explicitly below, so the worst case is
            // a redundant publish rather than a page disappearing from the site.
            const statusKnown = typeof base.status === 'string' && base.status.length > 0;
            const statusToWrite = statusKnown ? base.status : 'published';

            const result = await apiPost('/page', {
                id: args.id,
                name: args.name ?? base.name ?? '',
                description: args.description ?? base.description ?? '',
                layout: args.layout ?? base.layout ?? '',
                keywords: args.keywords ?? base.keywords ?? [],
                topics: args.topics ?? base.topics ?? [],
                articles,
                style: base.style ?? '',
                layoutid: base.layoutid ?? null,
                websiteId: websiteIdAsNumber(),
                status: statusToWrite
            });

            // Belt and braces: re-assert the status we intended, so that even if the upsert
            // coerced it we end up where we started rather than silently off the site.
            if (statusToWrite === 'published') {
                try { await apiPost(`/page/${args.id}/publish`, {}); } catch (_) { /* non-fatal */ }
            }

            const changed = ['name', 'description', 'layout', 'keywords', 'topics']
                .filter(k => args[k] !== undefined && JSON.stringify(args[k]) !== JSON.stringify(base[k]));
            if (articleRefs !== undefined) changed.push('articles');

            const note = statusKnown
                ? `Status (${statusToWrite}) and layout id were preserved.`
                : `The stored record returned no status, so the page was kept PUBLISHED rather than risk unpublishing it.`;

            return {
                page: result,
                summary: describeChange('page', args.id, changed, note)
            };
        }
    },
    {
        name: 'publish_page',
        description: 'Set a page status to published so it becomes visible on the public site.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/page/${args.id}/publish`, {})
    },
    {
        name: 'unpublish_page',
        description: 'Set a page status to draft so it no longer appears on the public site.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/page/${args.id}/unpublish`, {})
    },
    {
        name: 'regenerate_sitemap',
        description: 'Rebuild sitemap.xml for the current site (from every served page in the CMS) and upload it to the site\'s S3 root. Run after publishing a batch of new content. No parameters — uses the current WEBSITE_ID.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiPost(`/sitemap/regenerate?websiteId=${websiteId()}`, {})
    },
    {
        name: 'delete_empty_page',
        description: 'Safely delete an EMPTY page. Refuses unless: (1) page has zero linked articles, and (2) no menu item references it. Deleting subsumes unpublishing, so published pages may also be deleted. Intended for orphan cleanup — will not delete pages with linked content. To delete a populated page, detach articles first via update_page, then call this.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number', description: 'Page ID to delete.' } },
            required: ['id']
        },
        handler: async (args) => {
            const id = args.id;

            // Guard 1: page must exist and have no linked articles.
            // Status is intentionally NOT checked — delete is a strict superset of unpublish.
            const page = await apiGet(`/page/${id}/${websiteId()}`);
            if (!page || page.id == null) {
                throw new Error(`delete_empty_page refused: page ${id} not found.`);
            }
            if (Array.isArray(page.articles) && page.articles.length > 0) {
                throw new Error(`delete_empty_page refused: page ${id} has ${page.articles.length} linked article(s). Detach or delete them first.`);
            }

            // Guard 2: no menu item may reference this page
            let menu;
            try {
                menu = await apiGet(`/menu/${websiteId()}`);
            } catch (e) {
                throw new Error(`delete_empty_page refused: could not fetch menu to verify references (${e.message}). Aborting for safety.`);
            }
            const referencing = (Array.isArray(menu) ? menu : []).filter(m => m && m.pageId === id);
            if (referencing.length > 0) {
                const items = referencing.map(m => `id=${m.id} ("${m.text}")`).join(', ');
                throw new Error(`delete_empty_page refused: page ${id} is referenced by ${referencing.length} menu item(s): ${items}. Remove menu items first via delete_menu_item.`);
            }

            // All guards passed — proceed with the delete. The Lambda re-checks server-side.
            return apiDelete(`/page/${id}?websiteId=${websiteId()}`);
        }
    }
];
