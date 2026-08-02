// The single dispatch path shared by every transport.
//
// WHY THIS EXISTS
// ---------------
// There are two entry points: src/index.js (stdio, run locally by Claude Code) and
// src/lambda.js (HTTP, deployed as McpFunction). They previously each carried their own copy
// of "look up the tool, validate the arguments, call the handler, format the result".
//
// Anything living in the shared tool modules stayed correct in both automatically. Anything
// living in the duplicated dispatch drifted silently — when argument validation was added it
// went into index.js only, so the deployed server kept accepting typo'd parameters, and
// nothing failed loudly enough to notice.
//
// So dispatch lives here exactly once. An entry point is now responsible only for what is
// genuinely transport-specific: connecting a stream, checking an API key, selecting the site,
// and shaping the response envelope.

import { validateArgs } from './validate.js';

import { pageTools } from './tools/pages.js';
import { articleTools } from './tools/articles.js';
import { navigationTools } from './tools/navigation.js';
import { collectionTools } from './tools/collections.js';
import { metadataTools } from './tools/metadata.js';
import { compositeTools } from './tools/composite.js';
import { sourceFetchTools } from './tools/sourceFetch.js';
import { cacheTools } from './tools/cache.js';

const allTools = [
    ...compositeTools,
    ...sourceFetchTools,
    ...pageTools,
    ...articleTools,
    ...navigationTools,
    ...collectionTools,
    ...metadataTools,
    ...cacheTools
];

const toolMap = new Map(allTools.map(t => [t.name, t]));

/** The tool list, in the shape both the MCP SDK and the raw JSON-RPC envelope expect. */
export function listTools() {
    return allTools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema }));
}

export function hasTool(name) {
    return toolMap.has(name);
}

export function toolNames() {
    return [...toolMap.keys()].sort();
}

/** Render a handler's return value as MCP text content. */
function toContent(result) {
    return [{
        type: 'text',
        text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
    }];
}

/**
 * Run one tool call: look it up, validate and coerce its arguments, invoke it, and normalise
 * both success and failure into MCP `{ content, isError }`.
 *
 * Never throws — a transport should not have to decide how to render an error, and the two
 * transports previously disagreed about it (one threw, one returned isError).
 */
export async function callTool(name, rawArgs) {
    const tool = toolMap.get(name);
    if (!tool) {
        return {
            content: toContent(`Unknown tool: ${name}. Available tools: ${toolNames().join(', ')}`),
            isError: true
        };
    }

    let args;
    try {
        // Validate before the handler runs, so a bad call fails fast with a readable message
        // instead of writing a null into the database.
        args = validateArgs(tool.name, tool.inputSchema, rawArgs);
    } catch (e) {
        return { content: toContent(e.message), isError: true };
    }

    try {
        const result = await tool.handler(args);
        return { content: toContent(result), isError: false };
    } catch (e) {
        // Name the failing tool — an opaque upstream 500 with no context is what made these
        // problems hard to diagnose in the first place.
        const detail = e && e.message ? e.message : String(e);
        return { content: toContent(`${tool.name} failed: ${detail}`), isError: true };
    }
}
