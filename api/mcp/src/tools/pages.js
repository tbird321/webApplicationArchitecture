import { apiGet, apiPost, apiDelete, websiteId, websiteIdAsNumber } from '../apiClient.js';

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
            const base = current || {};

            // articles may arrive as a JSON string (or double-encoded string) from the MCP harness — parse until we get an array
            let articleRefs = args.articles;
            while (typeof articleRefs === 'string') {
                try { articleRefs = JSON.parse(articleRefs); } catch (_) { articleRefs = undefined; break; }
            }

            // If article refs were provided, fetch full article details so that
            // UpsertArticle does not overwrite existing article data with nulls.
            let articles;
            if (Array.isArray(articleRefs) && articleRefs.length > 0) {
                articles = await Promise.all(articleRefs.map(async (ref) => {
                    try {
                        const full = await apiGet(`/article/${ref.id}`);
                        if (full) {
                            return { ...full, sequence_no: ref.sequence_no ?? full.sequence_no };
                        }
                    } catch (_) { /* fall through to ref */ }
                    return ref;
                }));
            } else if (articleRefs !== undefined) {
                articles = articleRefs;
            } else {
                articles = base.articles ?? [];
            }

            return apiPost('/page', {
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
                status: base.status ?? 'draft'
            });
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
