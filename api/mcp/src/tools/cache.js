import { apiPost, websiteId } from '../apiClient.js';

export const cacheTools = [
    {
        name: 'invalidate_cache',
        description:
            'Force CloudFront to re-fetch pages for the current site, instead of waiting for the ' +
            'cache to expire on its own. Static pages are cached for 5 minutes, so a single edit ' +
            'does NOT need this — use it after a BATCH of changes you want live immediately, or ' +
            'for files with a longer cache (sitemap.xml, robots.txt, BaseStyles.css, images). ' +
            'With no paths it clears the whole site, which is also the cheapest option: CloudFront ' +
            'bills "/*" as a single path. Takes 1-3 minutes to complete. ' +
            'Only works for sites migrated to static hosting.',
        inputSchema: {
            type: 'object',
            properties: {
                paths: {
                    type: 'array',
                    items: { type: 'string' },
                    description:
                        'Optional. Paths or full URLs to clear, e.g. ["/about-us/", "/sitemap.xml"]. ' +
                        'Omit to clear the entire site ("/*"), which is the usual choice. ' +
                        'Max 50 entries; anything larger should be a whole-site clear.'
                }
            }
        },
        handler: async (args) => {
            const paths = Array.isArray(args.paths) ? args.paths : [];
            return apiPost(`/cache/invalidate?websiteId=${websiteId()}`, { paths });
        }
    }
];
