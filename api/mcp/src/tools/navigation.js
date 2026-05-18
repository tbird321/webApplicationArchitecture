const NOT_IMPLEMENTED = {
    error: 'Menu tools are not yet wired up.',
    reason: 'The site menu is stored as a JSON file in S3 (`websites/{siteName}/{menuConfigFileName}`) and is read/written directly by the React admin. There is currently no Lambda endpoint exposing menu CRUD. To enable these tools, either add /menu endpoints to the backend OR give this MCP server S3 credentials via @aws-sdk/client-s3.',
    deferredUntil: 'A future task explicitly enables menu management via the API.'
};

export const navigationTools = [
    {
        name: 'get_menu',
        description: 'Retrieve site menu structure as tree. NOT YET IMPLEMENTED — returns an explanation of what is required.',
        inputSchema: { type: 'object', properties: {} },
        handler: async () => NOT_IMPLEMENTED
    },
    {
        name: 'update_menu_item',
        description: 'Rename a menu item or change its linked page. NOT YET IMPLEMENTED.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'string' },
                itemTitle: { type: 'string' },
                pageId: { type: 'number' }
            }
        },
        handler: async () => NOT_IMPLEMENTED
    },
    {
        name: 'add_menu_item',
        description: 'Add a new menu item, optionally as a child of an existing item. NOT YET IMPLEMENTED.',
        inputSchema: {
            type: 'object',
            properties: {
                parentId: { type: 'string' },
                itemTitle: { type: 'string' },
                pageId: { type: 'number' }
            }
        },
        handler: async () => NOT_IMPLEMENTED
    },
    {
        name: 'delete_menu_item',
        description: 'Remove a menu item. NOT YET IMPLEMENTED.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'string' } },
            required: ['id']
        },
        handler: async () => NOT_IMPLEMENTED
    }
];
