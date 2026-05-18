import { apiGet, apiPost, websiteId, websiteIdAsNumber } from '../apiClient.js';

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
    }
];
