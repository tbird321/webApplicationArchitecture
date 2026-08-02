import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { setWebsiteId } from './apiClient.js';
import { validateArgs } from './validate.js';
import { pageTools } from './tools/pages.js';
import { articleTools } from './tools/articles.js';
import { navigationTools } from './tools/navigation.js';
import { collectionTools } from './tools/collections.js';
import { metadataTools } from './tools/metadata.js';
import { compositeTools } from './tools/composite.js';
import { sourceFetchTools } from './tools/sourceFetch.js';
import { cacheTools } from './tools/cache.js';

const CONFIGURED_SITE = process.env.WEBSITE_ID || '';
setWebsiteId(CONFIGURED_SITE);

// Fail loudly at startup rather than producing confusing 500s on the first write.
// (When this server becomes multi-tenant these checks move to a per-request identity;
// site scoping is deliberately funnelled through apiClient/safety so that swap stays local.)
const missing = [
    !process.env.LAMBDA_API_BASE_URL && 'LAMBDA_API_BASE_URL',
    !process.env.MCP_API_KEY && 'MCP_API_KEY',
    !CONFIGURED_SITE && 'WEBSITE_ID'
].filter(Boolean);
if (missing.length > 0) {
    console.error(
        `[webcms-mcp] Missing required environment variable(s): ${missing.join(', ')}. ` +
        `Every tool call will fail until these are set. See CLAUDE.md "MCP Server Setup".`
    );
}

const allTools = [...compositeTools, ...sourceFetchTools, ...pageTools, ...articleTools, ...navigationTools, ...collectionTools, ...metadataTools, ...cacheTools];
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
    if (!tool) {
        const names = [...toolMap.keys()].sort().join(', ');
        throw new Error(`Unknown tool: ${request.params.name}. Available tools: ${names}`);
    }

    // Coerce and validate centrally so handlers can trust their arguments, and so a bad
    // call fails fast with a readable message instead of writing a null into the database.
    const args = validateArgs(tool.name, tool.inputSchema, request.params.arguments);

    try {
        const result = await tool.handler(args);
        return {
            content: [{ type: 'text', text: typeof result === 'string' ? result : JSON.stringify(result, null, 2) }]
        };
    } catch (e) {
        // Surface the failing tool and its arguments — an opaque upstream 500 with no
        // context is what made these problems hard to diagnose in the first place.
        const detail = e && e.message ? e.message : String(e);
        throw new Error(`${tool.name} failed: ${detail}`);
    }
});

const transport = new StdioServerTransport();
await server.connect(transport);
