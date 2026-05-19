import { apiGet, apiPost, apiDelete, websiteId } from '../apiClient.js';

export const navigationTools = [
    {
        name: 'get_menu',
        description: 'Retrieve the full site menu structure as a flat array. Each item has: id, parent (0 = top level), droppable (true = section header that can have children), text (display label), and optionally pageId and pageName for leaf items.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => apiGet(`/menu/${websiteId()}`)
    },
    {
        name: 'add_menu_item',
        description: 'Add a new menu item. To add a leaf page item under an existing section, provide parentId (the section\'s id), itemTitle, and pageId. To add a new top-level section header, provide parentId: 0 and itemTitle only. Returns the new item and updated total.',
        inputSchema: {
            type: 'object',
            properties: {
                parentId: { type: 'number', description: 'ID of the parent item. Use 0 for top-level sections.' },
                itemTitle: { type: 'string', description: 'Display text for the menu item.' },
                pageId: { type: 'number', description: 'Page ID to link to. Omit for section headers.' },
                pageName: { type: 'string', description: 'Page slug/name for reference. Omit for section headers.' }
            },
            required: ['parentId', 'itemTitle']
        },
        handler: async ({ parentId, itemTitle, pageId, pageName }) =>
            apiPost(`/menu/${websiteId()}/item`, { parentId, itemTitle, pageId, pageName })
    },
    {
        name: 'update_menu_item',
        description: 'Update an existing menu item\'s display text, linked page, or parent. Provide the item\'s id and any fields to change.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'ID of the menu item to update.' },
                itemTitle: { type: 'string', description: 'New display text.' },
                pageId: { type: 'number', description: 'New page ID to link to.' },
                pageName: { type: 'string', description: 'New page slug/name.' },
                parentId: { type: 'number', description: 'New parent ID to move item under.' }
            },
            required: ['id']
        },
        handler: async ({ id, itemTitle, pageId, pageName, parentId }) =>
            apiPost(`/menu/${websiteId()}/item/${id}`, { itemTitle, pageId, pageName, parentId })
    },
    {
        name: 'delete_menu_item',
        description: 'Remove a menu item by id. Also removes all child items if the item is a section header. Returns the IDs removed.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'ID of the menu item to delete.' }
            },
            required: ['id']
        },
        handler: async ({ id }) => apiDelete(`/menu/${websiteId()}/item/${id}`)
    }
];
