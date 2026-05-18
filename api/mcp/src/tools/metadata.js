import { apiGet } from '../apiClient.js';

export const metadataTools = [
    {
        name: 'get_topics',
        description: 'List all topics available on the site.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet('/topic')
    },
    {
        name: 'get_keywords',
        description: 'List all keywords available on the site.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet('/keyword')
    },
    {
        name: 'get_layouts',
        description: 'List all page layouts available on the site.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet('/layout')
    }
];
