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
        description: 'Update a page (name, description, layout, keywords, topics). Pass the existing id.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number' },
                name: { type: 'string' },
                description: { type: 'string' },
                layout: { type: 'string' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['id']
        },
        handler: async (args) => apiPost('/page', {
            ...args,
            websiteId: websiteIdAsNumber()
        })
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
