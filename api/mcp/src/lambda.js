// HTTP transport, deployed as McpFunction behind API Gateway (POST /mcp?websiteId=N).
// All tool logic lives in dispatch.js — this file only handles auth, site selection,
// and the JSON-RPC envelope.

import { setWebsiteId } from './apiClient.js';
import { listTools, callTool } from './dispatch.js';

const reply = (statusCode, body) => ({
    statusCode,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body)
});

// A JSON-RPC *notification* has no `id` and MUST NOT receive a response body.
// Returning an error object for one (as this handler used to do for
// notifications/initialized) breaks the handshake with strict MCP clients.
const accepted = () => ({ statusCode: 202, headers: {}, body: '' });

// Protocol versions this server can speak. We echo the client's version when we
// know it, and otherwise fall back to the newest we support, rather than
// hard-coding one and hoping the client tolerates it.
const SUPPORTED_PROTOCOL_VERSIONS = ['2025-06-18', '2025-03-26', '2024-11-05'];
const DEFAULT_PROTOCOL_VERSION = '2025-06-18';

export const handler = async (event) => {
    // Header lookup is case-insensitive: API Gateway REST preserves the case the client
    // sent, and different MCP clients send x-api-key / X-API-Key / X-Api-Key.
    const rawHeaders = event.headers || {};
    const headerLookup = {};
    for (const [k, v] of Object.entries(rawHeaders)) headerLookup[k.toLowerCase()] = v;

    const key = headerLookup['x-api-key'];
    if (!key || key !== process.env.ENDPOINT_KEY) {
        return reply(401, { error: 'Unauthorized' });
    }

    // websiteId selects the site this request acts on. It is NOT required for the handshake
    // or for listing tools — only for calls that actually touch site data. Rejecting it up
    // front would break the connection entirely instead of failing one call with a message.
    const websiteId = event.queryStringParameters?.websiteId;
    setWebsiteId(websiteId || '');

    let req;
    try {
        req = JSON.parse(event.body || '{}');
    } catch {
        return reply(400, { jsonrpc: '2.0', id: null, error: { code: -32700, message: 'Parse error' } });
    }

    const { method, params, id } = req;
    const isNotification = id === undefined || id === null;

    // Notifications (initialized, cancelled, progress, ...) are fire-and-forget.
    if (isNotification && String(method || '').startsWith('notifications/')) {
        return accepted();
    }

    if (method === 'initialize') {
        const requested = params?.protocolVersion;
        const protocolVersion = SUPPORTED_PROTOCOL_VERSIONS.includes(requested)
            ? requested
            : DEFAULT_PROTOCOL_VERSION;

        return reply(200, {
            jsonrpc: '2.0', id,
            result: {
                protocolVersion,
                capabilities: { tools: { listChanged: false } },
                serverInfo: { name: 'webcms-mcp', version: '0.1.0' }
            }
        });
    }

    // Liveness check used by several clients during and after the handshake.
    if (method === 'ping') {
        return reply(200, { jsonrpc: '2.0', id, result: {} });
    }

    if (method === 'tools/list') {
        return reply(200, { jsonrpc: '2.0', id, result: { tools: listTools() } });
    }

    if (method === 'tools/call') {
        if (!websiteId) {
            return reply(200, {
                jsonrpc: '2.0', id,
                result: {
                    content: [{
                        type: 'text',
                        text: 'No site selected: this MCP endpoint needs a `websiteId` query parameter on its URL ' +
                              '(for example .../mcp?websiteId=5). Tool calls cannot run without it.'
                    }],
                    isError: true
                }
            });
        }

        const result = await callTool(params?.name, params?.arguments);
        return reply(200, { jsonrpc: '2.0', id, result });
    }

    // Any other notification: accept silently rather than erroring.
    if (isNotification) return accepted();

    return reply(200, {
        jsonrpc: '2.0', id,
        error: { code: -32601, message: `Method not found: ${method}` }
    });
};
