// stdio transport. Launched by Claude Code via .mcp.json.
// All tool logic lives in dispatch.js — this file only wires up the stream.

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { CallToolRequestSchema, ListToolsRequestSchema } from '@modelcontextprotocol/sdk/types.js';
import { setWebsiteId } from './apiClient.js';
import { listTools, callTool } from './dispatch.js';

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

const server = new Server(
    { name: 'webcms-mcp', version: '0.1.0' },
    { capabilities: { tools: {} } }
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({ tools: listTools() }));

server.setRequestHandler(CallToolRequestSchema, async (request) =>
    callTool(request.params.name, request.params.arguments)
);

const transport = new StdioServerTransport();
await server.connect(transport);
