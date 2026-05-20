import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { setWebsiteId } from './apiClient.js';
import { pageTools } from './tools/pages.js';
import { articleTools } from './tools/articles.js';
import { navigationTools } from './tools/navigation.js';
import { collectionTools } from './tools/collections.js';
import { metadataTools } from './tools/metadata.js';
import { compositeTools } from './tools/composite.js';

setWebsiteId(process.env.WEBSITE_ID || '');

const allTools = [...compositeTools, ...pageTools, ...articleTools, ...navigationTools, ...collectionTools, ...metadataTools];
const toolMap = new Map(allTools.map(t => [t.name, t]));

const server = new Server(
    { name: 'webcms-mcp', version: '0.1.0' },
    { capabilities: { tools: {} } }
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: allTools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema }))
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const tool = toolMap.get(request.params.name);
    if (!tool) throw new Error(`Unknown tool: ${request.params.name}`);
    const result = await tool.handler(request.params.arguments || {});
    return {
        content: [{ type: 'text', text: typeof result === 'string' ? result : JSON.stringify(result, null, 2) }]
    };
});

const transport = new StdioServerTransport();
await server.connect(transport);
