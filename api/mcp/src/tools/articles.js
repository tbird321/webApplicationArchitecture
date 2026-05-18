import { apiGet, apiGetText, apiPost, websiteId, websiteIdAsNumber } from '../apiClient.js';

export const articleTools = [
    {
        name: 'search_articles',
        description: 'Search articles by name, keywords, or topics.',
        inputSchema: {
            type: 'object',
            properties: {
                name: { type: 'string' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } },
                description: { type: 'string' }
            }
        },
        handler: async (args) => apiPost('/article/search', {
            Name: args.name,
            Keywords: args.keywords || [],
            Topics: args.topics || [],
            Description: args.description,
            WebsiteId: websiteId()
        })
    },
    {
        name: 'get_article',
        description: 'Retrieve article metadata by ID. Note: HTML content lives in S3 and is fetched separately by the frontend; this returns only the DB-tracked metadata.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiGet(`/article/${args.id}`)
    },
    {
        name: 'create_article',
        description: 'Create or update an article (upsert). The articleId field is the S3 slug. To create new, omit id; to update, pass id.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number' },
                articleId: { type: 'string', description: 'Stable slug used as S3 filename. Generate a UUID if creating.' },
                name: { type: 'string' },
                description: { type: 'string' },
                articlePath: { type: 'string', description: 'S3 filename, typically `${articleId}.html`.' },
                memeImagePath: { type: 'string' },
                sequence_no: { type: 'number' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['articleId', 'name', 'articlePath']
        },
        handler: async (args) => apiPost('/article', {
            id: args.id ?? null,
            articleId: args.articleId,
            name: args.name,
            description: args.description || '',
            articlePath: args.articlePath,
            memeImagePath: args.memeImagePath || '',
            sequence_no: args.sequence_no || 1,
            keywords: args.keywords || [],
            topics: args.topics || [],
            websiteId: websiteIdAsNumber(),
            status: 'draft'
        })
    },
    {
        name: 'update_article',
        description: 'Update an article record. Pass the existing id.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number' },
                name: { type: 'string' },
                description: { type: 'string' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['id']
        },
        handler: async (args) => apiPost('/article', {
            ...args,
            websiteId: websiteIdAsNumber()
        })
    },
    {
        name: 'get_article_content',
        description: 'Retrieve the full HTML content of an article from S3.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number', description: 'Article DB id.' } },
            required: ['id']
        },
        handler: async (args) => apiGetText(`/article/${args.id}/content`)
    },
    {
        name: 'set_article_content',
        description: 'Write HTML content for an article to S3. If the article has no path yet one is auto-generated. Returns the articlePath.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'Article DB id.' },
                htmlContent: { type: 'string', description: 'Full HTML content to store.' }
            },
            required: ['id', 'htmlContent']
        },
        handler: async (args) => apiPost(`/article/${args.id}/content`, { htmlContent: args.htmlContent })
    },
    {
        name: 'publish_article',
        description: 'Set an article status to published.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/article/${args.id}/publish`, {})
    },
    {
        name: 'unpublish_article',
        description: 'Set an article status to draft.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/article/${args.id}/unpublish`, {})
    }
];
