#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';

import { pageTools } from './tools/pages.js';
import { articleTools } from './tools/articles.js';
import { navigationTools } from './tools/navigation.js';
import { collectionTools } from './tools/collections.js';
import { metadataTools } from './tools/metadata.js';

const REQUIRED_ENV = ['MCP_API_KEY', 'LAMBDA_API_BASE_URL', 'WEBSITE_ID'];
const missing = REQUIRED_ENV.filter((k) => !process.env[k]);
if (missing.length > 0) {
    console.error(`webcms-mcp: missing required env vars: ${missing.join(', ')}`);
    console.error('Copy mcp/.env.example to mcp/.env and fill in the values, or set them in your MCP client config.');
    process.exit(1);
}

const allTools = [
    ...pageTools,
    ...articleTools,
    ...navigationTools,
    ...collectionTools,
    ...metadataTools
];
const toolMap = new Map(allTools.map((t) => [t.name, t]));

const server = new Server(
    { name: 'webcms-mcp', version: '0.1.0' },
    { capabilities: { tools: {} } }
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: allTools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema }))
}));

server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const tool = toolMap.get(request.params.name);
    if (!tool) {
        return {
            content: [{ type: 'text', text: `Unknown tool: ${request.params.name}` }],
            isError: true
        };
    }
    try {
        const result = await tool.handler(request.params.arguments || {});
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    } catch (err) {
        return {
            content: [{ type: 'text', text: `Tool ${tool.name} failed: ${err.message}` }],
            isError: true
        };
    }
});

const transport = new StdioServerTransport();
await server.connect(transport);
console.error('webcms-mcp: connected via stdio');
