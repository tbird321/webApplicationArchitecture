import { apiPost, websiteId, websiteIdAsNumber } from '../apiClient.js';
import { acquireLock } from '../semaphore.js';

export const compositeTools = [
    {
        name: 'create_page_with_article',
        description: 'Atomically create a page, create an article, upload its HTML content, link them together, and publish both. Use this instead of calling create_article / create_page / update_page / publish individually — it holds a semaphore so parallel agents cannot interleave their DB writes.',
        inputSchema: {
            type: 'object',
            properties: {
                pageName:        { type: 'string',  description: 'URL-safe page name (used as the page slug).' },
                pageDescription: { type: 'string',  description: 'Short description of the page.' },
                articleName:     { type: 'string',  description: 'Display name of the article.' },
                htmlContent:     { type: 'string',  description: 'Full HTML content for the article.' },
                layout:          { type: 'string',  description: 'Page layout name. Default: Standard.' },
                keywords:        { type: 'array', items: { type: 'string' } },
                topics:          { type: 'array', items: { type: 'string' } }
            },
            required: ['pageName', 'articleName', 'htmlContent']
        },
        handler: (args) => acquireLock(async () => {
            const wid = websiteIdAsNumber();
            const layout = args.layout || 'Standard';

            // 1. Create article record
            const article = await apiPost('/article', {
                id: null,
                articleId: args.pageName,
                name: args.articleName,
                description: args.pageDescription || '',
                articlePath: `${args.pageName}.html`,
                memeImagePath: '',
                sequence_no: 5,
                keywords: args.keywords || [],
                topics: args.topics || [],
                websiteId: wid,
                status: 'draft'
            });

            // 2. Upload HTML content to S3
            await apiPost(`/article/${article.id}/content`, { htmlContent: args.htmlContent });

            // 3. Create page record
            const page = await apiPost('/page', {
                id: null,
                name: args.pageName,
                description: args.pageDescription || '',
                layout,
                keywords: args.keywords || [],
                topics: args.topics || [],
                articles: [],
                websiteId: wid,
                status: 'draft'
            });

            // 4. Link article to page — spread the full article object so no fields are null.
            // Passing a minimal article object (omitting description, memeImagePath, etc.) causes
            // MySQL Connector/NET 8.2.0 to throw NullReferenceException when inferring the MySQL
            // type for a null AddWithValue call inside UpsertArticle.
            await apiPost('/page', {
                id: page.id,
                name: args.pageName,
                description: args.pageDescription || '',
                layout,
                keywords: args.keywords || [],
                topics: args.topics || [],
                articles: [{ ...article, sequence_no: 5 }],
                style: '',
                layoutid: page.layoutid ?? null,
                websiteId: wid,
                status: 'draft'
            });

            // 5. Publish both
            await apiPost(`/article/${article.id}/publish`, {});
            await apiPost(`/page/${page.id}/publish`, {});

            return {
                pageId: page.id,
                articleId: article.id,
                pageName: args.pageName,
                articlePath: `${args.pageName}.html`,
                message: `Page "${args.pageName}" created and published (pageId:${page.id}, articleId:${article.id}). Add to menu with add_menu_item.`
            };
        })
    }
];
