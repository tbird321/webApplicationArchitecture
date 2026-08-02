import { apiGet, apiPost, apiDelete, websiteId, websiteIdAsNumber } from '../apiClient.js';
import { readModifyWrite, describeChange } from '../safety.js';

export const collectionTools = [
    {
        name: 'search_collections',
        description: 'Search collections by name.',
        inputSchema: {
            type: 'object',
            properties: { name: { type: 'string' } }
        },
        handler: async (args) => apiPost('/collection/search', {
            Name: args.name,
            WebsiteId: websiteId()
        })
    },
    {
        name: 'get_collection',
        description: 'Retrieve a collection by ID with its associated pages and their sequence.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiGet(`/collection/${args.id}`)
    },
    {
        name: 'upsert_collection',
        description:
            'Create a collection (omit id) or update an existing one (pass id). ' +
            'On update, fields you omit are read from the stored record and preserved rather than blanked.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'Omit to create a new collection; pass to update an existing one.' },
                name: { type: 'string' },
                description: { type: 'string' },
                type: { type: 'string', enum: ['standard', 'gallery'] }
            },
            required: ['name']
        },
        handler: async (args) => {
            // Create path — nothing to preserve.
            if (args.id == null) {
                return apiPost('/collection', {
                    id: null,
                    name: args.name,
                    description: args.description || '',
                    type: args.type || 'standard',
                    websiteId: websiteIdAsNumber()
                });
            }

            // Update path — merge over the stored record so omitted fields survive.
            const { id, ...patch } = args;
            const { merged, changed } = await readModifyWrite(`/collection/${id}`, patch, {
                kind: 'collection',
                id,
                defaults: { description: '', type: 'standard' }
            });

            // `pages` belongs to the association endpoints, not the collection upsert.
            const { pages, ...record } = merged;

            const result = await apiPost('/collection', { ...record, id, websiteId: websiteIdAsNumber() });
            return { collection: result, summary: describeChange('collection', id, changed) };
        }
    },
    {
        name: 'add_page_to_collection',
        description: 'Associate one or more pages with a collection at the specified sequence positions.',
        inputSchema: {
            type: 'object',
            properties: {
                collectionId: { type: 'number' },
                pages: {
                    type: 'array',
                    items: {
                        type: 'object',
                        properties: {
                            id: { type: 'number' },
                            sequence: { type: 'number' }
                        },
                        required: ['id']
                    }
                }
            },
            required: ['collectionId', 'pages']
        },
        handler: async (args) => apiPost(`/collection/${args.collectionId}/page`, {
            collection: { id: args.collectionId },
            pages: args.pages
        })
    },
    {
        name: 'remove_page_from_collection',
        description: 'Remove a page from a collection.',
        inputSchema: {
            type: 'object',
            properties: {
                collectionId: { type: 'number' },
                pageId: { type: 'number' }
            },
            required: ['collectionId', 'pageId']
        },
        handler: async (args) => apiDelete(`/collection/${args.collectionId}/page/${args.pageId}`)
    }
];
