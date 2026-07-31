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
        // ORDER IS LOAD-BEARING: the HTML upload is LAST, and must stay last.
        //
        // Writing article HTML to S3 is what fires the static re-render trigger
        // (ApiStaticRenderFunctions.RenderOnArticleUpload). That handler looks for every
        // SERVED page whose articles include the uploaded path -- so at the moment of the
        // write the page must already exist, already be linked to the article, and already
        // be published. Upload any earlier and the trigger fires against a page that does
        // not exist yet, finds nothing, logs "no served page references ...", and skips.
        //
        // That is exactly what this tool used to do, and the symptom was brutal to diagnose:
        // the tool reported success, regenerate_sitemap listed the new URL, and the live page
        // 404'd until someone happened to save the article a second time. Nothing errored.
        handler: (args) => acquireLock(async () => {
            const wid = websiteIdAsNumber();
            const layout = args.layout || 'Standard';

            // 1. Create article record (metadata only -- content comes last, see above)
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

            // 2. Create page record
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

            // 3. Link article to page — spread the full article object so no fields are null.
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

            // 4. Publish both. Must precede the upload: the render trigger only considers
            // pages whose status is published (or empty), so uploading first would render
            // nothing even with the link in place.
            await apiPost(`/article/${article.id}/publish`, {});
            await apiPost(`/page/${page.id}/publish`, {});

            // 5. Upload HTML content to S3 -- LAST, so this write is the thing that triggers
            // the render, with the page created, linked and published.
            await apiPost(`/article/${article.id}/content`, { htmlContent: args.htmlContent });

            return {
                pageId: page.id,
                articleId: article.id,
                pageName: args.pageName,
                articlePath: `${args.pageName}.html`,
                message: `Page "${args.pageName}" created and published (pageId:${page.id}, articleId:${article.id}). The static page renders within a few seconds; allow up to 5 minutes for CloudFront. Add to menu with add_menu_item — note that a menu change re-renders the whole site.`
            };
        })
    }
];
