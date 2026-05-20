import { setWebsiteId } from './apiClient.js';
import { pageTools } from './tools/pages.js';
import { articleTools } from './tools/articles.js';
import { navigationTools } from './tools/navigation.js';
import { collectionTools } from './tools/collections.js';
import { metadataTools } from './tools/metadata.js';
import { compositeTools } from './tools/composite.js';
import { sourceFetchTools } from './tools/sourceFetch.js';

const allTools = [...compositeTools, ...sourceFetchTools, ...pageTools, ...articleTools, ...navigationTools, ...collectionTools, ...metadataTools];
const toolMap = new Map(allTools.map(t => [t.name, t]));

const reply = (statusCode, body) => ({
    statusCode,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
});

export const handler = async (event) => {
    const key = event.headers?.['x-api-key'] ?? event.headers?.['X-Api-Key'];
    if (!key || key !== process.env.ENDPOINT_KEY) {
        return reply(401, { error: 'Unauthorized' });
    }

    const websiteId = event.queryStringParameters?.websiteId;
    if (!websiteId) {
        return reply(400, { error: 'websiteId query parameter is required' });
    }
    setWebsiteId(websiteId);

    let req;
    try {
        req = JSON.parse(event.body || '{}');
    } catch {
        return reply(400, { jsonrpc: '2.0', id: null, error: { code: -32700, message: 'Parse error' } });
    }

    const { method, params, id } = req;

    if (method === 'initialize') {
        return reply(200, {
            jsonrpc: '2.0', id,
            result: {
                protocolVersion: '2024-11-05',
                capabilities: { tools: {} },
                serverInfo: { name: 'webcms-mcp', version: '0.1.0' }
            }
        });
    }

    if (method === 'tools/list') {
        return reply(200, {
            jsonrpc: '2.0', id,
            result: { tools: allTools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema })) }
        });
    }

    if (method === 'tools/call') {
        const tool = toolMap.get(params?.name);
        if (!tool) {
            return reply(200, {
                jsonrpc: '2.0', id,
                result: { content: [{ type: 'text', text: `Unknown tool: ${params?.name}` }], isError: true }
            });
        }
        try {
            const result = await tool.handler(params.arguments || {});
            return reply(200, {
                jsonrpc: '2.0', id,
                result: { content: [{ type: 'text', text: typeof result === 'string' ? result : JSON.stringify(result, null, 2) }] }
            });
        } catch (e) {
            return reply(200, {
                jsonrpc: '2.0', id,
                result: { content: [{ type: 'text', text: `Tool ${tool.name} failed: ${e.message}` }], isError: true }
            });
        }
    }

    return reply(200, {
        jsonrpc: '2.0', id,
        error: { code: -32601, message: `Method not found: ${method}` }
    });
};
