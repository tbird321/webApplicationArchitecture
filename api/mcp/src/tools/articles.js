import { apiGet, apiGetText, apiPost, apiDelete, websiteId, websiteIdAsNumber } from '../apiClient.js';
import { readModifyWrite, assertSameSite, describeChange } from '../safety.js';

export const articleTools = [
    {
        name: 'search_articles',
        description: 'Search articles by name, keywords, or topics on the current site. Returns matching article records. Use this to find an article id before reading or updating it.',
        inputSchema: {
            type: 'object',
            properties: {
                name: { type: 'string', description: 'Substring match on article name.' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } },
                description: { type: 'string' }
            }
        },
        handler: async (args) => apiPost('/article/search', {
            Name: args.name,
            Keywords: args.keywords || [],
            Topics: args.topics || [],
            Description: args.description,
            WebsiteId: websiteId()
        })
    },
    {
        name: 'get_article',
        description: 'Retrieve article metadata by ID (name, slug, path, status, keywords, topics). Does NOT return the HTML body — use get_article_content for that.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiGet(`/article/${args.id}`)
    },
    {
        name: 'create_article',
        description:
            'Create a new article record (metadata only, no HTML body). ' +
            'PREFER create_page_with_article for new content — it creates the page, the article, links, publishes and uploads in the correct order for you. ' +
            'Use this tool only when you need a standalone article record. After creating, call set_article_content to upload the HTML.',
        inputSchema: {
            type: 'object',
            properties: {
                articleId: { type: 'string', description: 'Stable slug, also used as the S3 filename stem. Usually the page slug.' },
                name: { type: 'string', description: 'Display name of the article.' },
                description: { type: 'string' },
                articlePath: { type: 'string', description: 'S3 filename, normally `${articleId}.html`.' },
                memeImagePath: { type: 'string' },
                sequence_no: { type: 'number', description: 'Display order within a page. Default 5.' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['articleId', 'name', 'articlePath']
        },
        handler: async (args) => apiPost('/article', {
            id: null,
            articleId: args.articleId,
            name: args.name,
            description: args.description || '',
            articlePath: args.articlePath,
            memeImagePath: args.memeImagePath || '',
            sequence_no: args.sequence_no ?? 5,
            keywords: args.keywords || [],
            topics: args.topics || [],
            websiteId: websiteIdAsNumber(),
            status: 'draft'
        })
    },
    {
        name: 'update_article',
        description:
            'Change one or more fields on an existing article. Safe to call with only the fields you want to change — ' +
            'everything you omit is read from the stored record and preserved, including the S3 path, slug and published status. ' +
            'Refuses if the article belongs to a different website than this server is configured for.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'Article DB id.' },
                name: { type: 'string' },
                description: { type: 'string' },
                articleId: { type: 'string', description: 'Stable slug. Changing this changes the S3 filename stem — rarely what you want.' },
                articlePath: { type: 'string', description: 'S3 filename. Changing this orphans the existing HTML unless you re-upload it.' },
                memeImagePath: { type: 'string' },
                sequence_no: { type: 'number' },
                keywords: { type: 'array', items: { type: 'string' } },
                topics: { type: 'array', items: { type: 'string' } }
            },
            required: ['id']
        },
        handler: async (args) => {
            const { id, ...patch } = args;

            const { merged, changed } = await readModifyWrite(`/article/${id}`, patch, {
                kind: 'article',
                id,
                // Columns the upsert always writes. Defaults only apply when the stored
                // record genuinely lacks the field, never to overwrite a real value.
                defaults: {
                    description: '',
                    memeImagePath: '',
                    sequence_no: 5,
                    keywords: [],
                    topics: [],
                    status: 'draft'
                }
            });

            // websiteId is pinned to the record's own owner (already verified to match the
            // configured site) rather than re-read from env at write time.
            const result = await apiPost('/article', { ...merged, id, websiteId: websiteIdAsNumber() });

            return {
                article: result,
                summary: describeChange('article', id, changed)
            };
        }
    },
    {
        name: 'get_article_content',
        description: 'Retrieve the full HTML body of an article from S3.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number', description: 'Article DB id.' } },
            required: ['id']
        },
        handler: async (args) => apiGetText(`/article/${args.id}/content`)
    },
    {
        name: 'set_article_content',
        description:
            'Write the HTML body of an article to S3. This write is what triggers the static re-render of the page. ' +
            'IMPORTANT: the render only picks up pages that already exist, are already linked to this article, and are already PUBLISHED. ' +
            'If the page is still a draft or unlinked, the upload succeeds but the public URL keeps 404ing until something else triggers a render. ' +
            'For brand-new content use create_page_with_article, which sequences this correctly.',
        inputSchema: {
            type: 'object',
            properties: {
                id: { type: 'number', description: 'Article DB id.' },
                htmlContent: { type: 'string', description: 'Full HTML body to store.' }
            },
            required: ['id', 'htmlContent']
        },
        handler: async (args) => {
            // Guard the cross-site case: uploading content for a foreign-site article would
            // write into the wrong bucket prefix.
            const article = await apiGet(`/article/${args.id}`);
            if (!article || article.id == null) {
                throw new Error(`Refused: article ${args.id} not found. Nothing was uploaded.`);
            }
            assertSameSite(article, 'article', args.id);

            const result = await apiPost(`/article/${args.id}/content`, { htmlContent: args.htmlContent });

            const published = String(article.status || '').toLowerCase() === 'published';
            return {
                result,
                summary: published
                    ? `Uploaded ${args.htmlContent.length} bytes to article ${args.id}. A re-render was triggered; allow a few seconds, plus up to 5 minutes for CloudFront.`
                    : `Uploaded ${args.htmlContent.length} bytes to article ${args.id}. NOTE: this article is '${article.status}', not published — the render trigger skips unpublished content, so the public URL may 404 until you publish it and the site re-renders.`
            };
        }
    },
    {
        name: 'publish_article',
        description: 'Set an article status to published. A page also needs publish_page before its URL goes live.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/article/${args.id}/publish`, {})
    },
    {
        name: 'unpublish_article',
        description: 'Set an article status back to draft, removing it from the public site on the next render.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number' } },
            required: ['id']
        },
        handler: async (args) => apiPost(`/article/${args.id}/unpublish`, {})
    },
    {
        name: 'delete_empty_article',
        description:
            'Delete an article record by id (this also unpublishes it). ' +
            'Detach it from any page FIRST via update_page with an articles array that omits this id — otherwise the delete can leave a stale page→article row or fail on a foreign key.',
        inputSchema: {
            type: 'object',
            properties: { id: { type: 'number', description: 'Article DB id to delete.' } },
            required: ['id']
        },
        handler: async (args) => {
            const id = args.id;

            const article = await apiGet(`/article/${id}`);
            if (!article || article.id == null || article.id === 0) {
                throw new Error(`Refused: article ${id} not found.`);
            }
            assertSameSite(article, 'article', id);

            return apiDelete(`/article/${id}`);
        }
    }
];
