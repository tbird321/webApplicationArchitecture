using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using MySQLConnector;
using MySQLConnector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplicationArch.content;

namespace WebApplicationArch
{
    /// <summary>
    /// Re-renders static pages when an article's HTML changes in the content bucket.
    ///
    /// WHY THIS IS DRIVEN BY S3 RATHER THAN THE API
    /// --------------------------------------------
    /// The React admin does NOT save article content through this API. It writes straight to
    /// s3://www-websitecontent/public/websites/{domain}/articles/{path} using Amplify Storage
    /// with Cognito identity-pool credentials (see FileProcessing.saveFileData -> Storage.put).
    /// The MCP tools and the PowerShell scripts write there too.
    ///
    /// So a save hook on SetArticleContent -- which is what this originally was -- never fires
    /// for a normal edit: the article updates, the public page silently does not, and the site
    /// looks fine while going stale. Hanging the render off the BUCKET catches every writer,
    /// present and future, without asking each one to remember to call something.
    ///
    /// The trigger is an S3 ObjectCreated notification on the articles/ prefix. See
    /// scripts/configure-render-trigger.ps1 -- the notification lives on the bucket, which is
    /// not managed by this stack, so it is configured separately from the deploy.
    ///
    /// NO RECURSION: this reads from the content bucket and writes to www.{domain}. The
    /// notification is scoped to the content bucket's articles/ prefix, which nothing here
    /// writes to.
    /// </summary>
    public class ApiStaticRenderFunctions : ApiBaseFunctions
    {
        /// <summary>
        /// Split "public/websites/{domain}/articles/{articlePath}" into its parts.
        /// Returns false for any key that is not an article under a website.
        /// </summary>
        public static bool TryParseArticleKey(string key, out string domain, out string articlePath)
        {
            domain = null;
            articlePath = null;
            if (string.IsNullOrWhiteSpace(key)) return false;

            // S3 event keys are URL-encoded, and spaces arrive as '+'.
            var decoded = Uri.UnescapeDataString(key.Replace("+", " "));
            var parts = decoded.Split('/');

            // public / websites / {domain} / articles / {rest...}
            if (parts.Length < 5) return false;
            if (!string.Equals(parts[0], "public", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[1], "websites", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[3], "articles", StringComparison.OrdinalIgnoreCase)) return false;

            domain = parts[2];
            articlePath = string.Join("/", parts.Skip(4));

            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(articlePath)) return false;

            // Directory placeholders and the "articles_$folder$" marker objects are not content.
            if (articlePath.EndsWith("/")) return false;
            if (articlePath.Contains("$folder$")) return false;

            return true;
        }

        /// <summary>
        /// S3 ObjectCreated handler. One invocation may carry several records; a failure on one
        /// must not abandon the rest, so each is handled independently.
        /// </summary>
        public async Task RenderOnArticleUpload(S3Event evnt, ILambdaContext context)
        {
            if (evnt?.Records == null || evnt.Records.Count == 0)
            {
                context.Logger.Log("Static render trigger: no records.");
                return;
            }

            string environment = Environment.GetEnvironmentVariable("Environment") ?? "Prod";
            string contentBucket = Environment.GetEnvironmentVariable("CONTENT_BUCKET") ?? "www-websitecontent";

            // Resolve the website list once for the whole batch.
            List<WebsiteModel> websites;
            try
            {
                var conn = await ConnectionInfoAsync(environment);
                websites = await new WebsiteDAO(conn).GetWebsites() ?? new List<WebsiteModel>();
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Static render trigger: could not load websites: {ex.Message}");
                return;
            }

            foreach (var record in evnt.Records)
            {
                var key = record?.S3?.Object?.Key;
                try
                {
                    if (!TryParseArticleKey(key, out var domain, out var articlePath))
                    {
                        context.Logger.Log($"Static render trigger: skipping non-article key '{key}'.");
                        continue;
                    }

                    var site = websites.FirstOrDefault(w =>
                        string.Equals(w.name, domain, StringComparison.OrdinalIgnoreCase));
                    if (site == null || !site.id.HasValue)
                    {
                        context.Logger.LogWarning($"Static render trigger: no website row matches '{domain}' (key '{key}').");
                        continue;
                    }
                    int websiteId = site.id.Value;

                    // Per-site opt-in. A site that has not been cut over must not have pages
                    // written into its live bucket -- a page named "Home" renders to the bucket
                    // ROOT, i.e. straight over its SPA shell. See StaticPageRenderer.IsPublishEnabled.
                    if (!StaticPageRenderer.IsPublishEnabled(websiteId))
                    {
                        context.Logger.Log($"Static render trigger: website {websiteId} ({domain}) is not in STATIC_PUBLISH_SITES -- skipping.");
                        continue;
                    }

                    await RenderPagesForArticlePath(site, websiteId, articlePath, environment, contentBucket, context);
                }
                catch (Exception ex)
                {
                    // Never fail the whole batch for one bad record.
                    context.Logger.LogError($"Static render trigger: '{key}' failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Find every served page on this site whose articles include the given articlePath, and
        /// re-render each. SearchPages populates page.articles (including articlePath) via
        /// get_page_articles_view, so the parent lookup is one query filtered in memory.
        /// </summary>
        private async Task RenderPagesForArticlePath(
            WebsiteModel site, int websiteId, string articlePath, string environment, string contentBucket, ILambdaContext context)
        {
            if (string.IsNullOrWhiteSpace(site.name)) return;

            var conn = await ConnectionInfoAsync(environment);
            var pageDao = new PageDAO(conn);
            var allPages = await pageDao.SearchPages(
                new List<string>(), new List<string>(), null, null, websiteId.ToString());

            var affected = (allPages ?? new List<PageModel>())
                .Where(p => !string.IsNullOrWhiteSpace(p.name)
                            && (string.IsNullOrEmpty(p.status) || p.status == "published")
                            && p.articles != null
                            && p.articles.Any(a => string.Equals(a.articlePath, articlePath, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (affected.Count == 0)
            {
                context.Logger.Log($"Static render trigger: no served page references '{articlePath}' on {site.name}.");
                return;
            }

            var renderer = new StaticPageRenderer(contentBucket, "us-west-2");
            int ok = 0;
            foreach (var pg in affected)
            {
                try
                {
                    await renderer.RenderAsync(pg, websiteId, site.name, upload: true);
                    ok++;
                    context.Logger.Log($"Static render trigger: re-rendered {StaticPageRenderer.PublicPath(pg)} on {site.name}.");
                }
                catch (Exception ex)
                {
                    context.Logger.LogError($"Static render trigger: page '{pg.name}' failed: {ex.Message}");
                }
            }
            context.Logger.Log($"Static render trigger: '{articlePath}' touched {ok}/{affected.Count} page(s) on {site.name}.");
        }
    }
}
