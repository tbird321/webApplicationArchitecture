import fetch from 'node-fetch';
import { acquireFetchLock } from '../semaphore.js';

// Polite delay between sequential fetches — agents queue and wait their turn.
// 4 seconds gives the source site breathing room and avoids triggering rate limits.
const FETCH_DELAY_MS = 4000;

async function fetchSourcePage(url) {
    return acquireFetchLock(async () => {
        // Wait before each request so back-to-back agent fetches are spaced out
        await new Promise(r => setTimeout(r, FETCH_DELAY_MS));

        const res = await fetch(url, {
            headers: {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
                'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8',
                'Accept-Language': 'en-US,en;q=0.9',
                'Cache-Control': 'no-cache'
            }
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}: ${res.statusText}`);

        const html = await res.text();

        // Strip scripts, styles, and tags — return readable text only
        return html
            .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
            .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
            .replace(/<[^>]+>/g, ' ')
            .replace(/&nbsp;/g, ' ')
            .replace(/&amp;/g, '&')
            .replace(/&lt;/g, '<')
            .replace(/&gt;/g, '>')
            .replace(/\s+/g, ' ')
            .trim()
            .substring(0, 60000);
    });
}

export const sourceFetchTools = [
    {
        name: 'fetch_source_page',
        description: 'Fetch a page from ldsdiscussions.com with polite rate limiting. Parallel agents queue through a semaphore with a 4-second delay between requests so the source site is never hit concurrently. The agent calling this may wait if another agent is already fetching. Returns extracted text content (scripts/styles stripped). USE THIS instead of WebFetch for any ldsdiscussions.com URL.',
        inputSchema: {
            type: 'object',
            properties: {
                url: {
                    type: 'string',
                    description: 'The ldsdiscussions.com URL to fetch (e.g. https://www.ldsdiscussions.com/translation)'
                }
            },
            required: ['url']
        },
        handler: (args) => fetchSourcePage(args.url)
    }
];
