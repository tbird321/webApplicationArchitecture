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
        /// Files that sit directly under "public/websites/{domain}/" and are BAKED INTO EVERY
        /// PAGE at render time. Writing one of these must re-render the whole site, or the
        /// change is invisible on every page that already exists and nothing reports a problem:
        /// a new menu item shows up nowhere, a header edit shows on nothing, a site-meta title
        /// change reaches no page.
        ///
        ///   sitemenu.json  -- the nav (StaticPageRenderer.BuildNav)
        ///   header.html    -- the site header, inlined above the article body
        ///   site-meta.json -- public title and analytics tag (ResolveSiteMetaAsync)
        ///
        /// This is an explicit ALLOWLIST, not "anything in the site folder", because the
        /// article notification's filter is prefix=public/websites/ suffix=.html -- so every
        /// stray .html dropped in a site folder reaches this Lambda. Matching loosely would
        /// turn an unrelated upload into a 471-page render.
        ///
        /// To add one: put the filename here. Then check that S3 actually DELIVERS it -- the
        /// notification filters allow one prefix and one suffix each, so a new extension needs
        /// its own entry in scripts/configure-render-trigger.ps1. (.html and site-meta.json are
        /// already covered by existing entries; sitemenu.json has its own.)
        /// </summary>
        private static readonly HashSet<string> SiteWideAssets =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "sitemenu.json",   // the nav
                "header.html",     // the site header banner
                "site-meta.json",  // public title + GA measurement id
            };

        /// <summary>
        /// Recognise "public/websites/{domain}/{site-wide asset}".
        ///
        /// Deliberately a separate parser from TryParseArticleKey, which must keep REJECTING
        /// these keys -- a site-asset write is a whole-site render, not a one-page render. The
        /// exact-four-segments rule is what keeps the two mutually exclusive: an article is
        /// always public/websites/{domain}/articles/... which is five or more.
        ///
        /// Reports WHICH asset matched, so the render log names the file that caused it rather
        /// than repeating the whole key.
        /// </summary>
        public static bool TryParseSiteAssetKey(string key, out string domain, out string asset)
        {
            domain = null;
            asset = null;
            if (string.IsNullOrWhiteSpace(key)) return false;

            var decoded = Uri.UnescapeDataString(key.Replace("+", " "));
            var parts = decoded.Split('/');

            // public / websites / {domain} / {asset}  -- exactly four segments.
            if (parts.Length != 4) return false;
            if (!string.Equals(parts[0], "public", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[1], "websites", StringComparison.OrdinalIgnoreCase)) return false;
            if (!SiteWideAssets.Contains(parts[3])) return false;

            domain = parts[2];
            asset = parts[3];
            return !string.IsNullOrWhiteSpace(domain);
        }

        /// <summary>
        /// Recognise "public/assets/{domain}/themes/theme.css" -- the ThemeBuilder output, which
        /// is inlined into every page just like the files above.
        ///
        /// It needs its own parser because it lives under a different prefix entirely
        /// (public/assets/, not public/websites/), which also means its own S3 notification.
        /// </summary>
        public static bool TryParseThemeKey(string key, out string domain, out string asset)
        {
            domain = null;
            asset = null;
            if (string.IsNullOrWhiteSpace(key)) return false;

            var decoded = Uri.UnescapeDataString(key.Replace("+", " "));
            var parts = decoded.Split('/');

            // public / assets / {domain} / themes / theme.css  -- exactly five segments.
            if (parts.Length != 5) return false;
            if (!string.Equals(parts[0], "public", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[1], "assets", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[3], "themes", StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(parts[4], "theme.css", StringComparison.OrdinalIgnoreCase)) return false;

            domain = parts[2];
            asset = parts[4];
            return !string.IsNullOrWhiteSpace(domain);
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

            // One admin drag-and-drop can write sitemenu.json several times, and a single batch
            // may carry a menu AND a header write for the same site. A whole-site render is
            // expensive, so collapse repeats: render each site at most once per invocation,
            // however many site-wide assets changed.
            var siteRendered = new HashSet<int>();

            foreach (var record in evnt.Records)
            {
                var key = record?.S3?.Object?.Key;
                try
                {
                    bool isSiteWide = TryParseSiteAssetKey(key, out var domain, out var asset)
                                      || TryParseThemeKey(key, out domain, out asset);
                    string articlePath = null;

                    if (!isSiteWide && !TryParseArticleKey(key, out domain, out articlePath))
                    {
                        context.Logger.Log($"Static render trigger: skipping unrecognised key '{key}'.");
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

                    if (isSiteWide)
                    {
                        if (!siteRendered.Add(websiteId))
                        {
                            context.Logger.Log($"Static render trigger: {domain} already re-rendered in this batch ('{asset}' skipped).");
                            continue;
                        }
                        await RenderAllPagesForSite(site, websiteId, environment, contentBucket, context, $"'{asset}'");
                    }
                    else
                    {
                        await RenderPagesForArticlePath(site, websiteId, articlePath, environment, contentBucket, context);
                    }
                }
                catch (Exception ex)
                {
                    // Never fail the whole batch for one bad record.
                    context.Logger.LogError($"Static render trigger: '{key}' failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Re-render EVERY served page on the site. Driven by a write to any file that is baked
        /// into every page (menu, header, site-meta, theme) -- see SiteWideAssets -- because a
        /// change to one is otherwise invisible on every page that already exists.
        ///
        /// Rendered with bounded concurrency: ldsdoctrines is ~471 pages and each render does
        /// several S3 reads, so sequential rendering would run for many minutes and time the
        /// Lambda out. The work is I/O bound, so a modest fan-out is the whole fix.
        ///
        /// The sitewide files are loaded ONCE here and shared by every page. Loading them per
        /// page (which is what the single-page RenderAsync overload does, correctly, for a
        /// one-off) meant five redundant S3 reads per page -- ~2,355 of them on ldsdoctrines --
        /// plus rebuilding the identical nav string for every page.
        ///
        /// This is also why StaticRenderOnUploadFunction is 1024 MB / 900 s in serverless.template
        /// (it was 512 MB / 120 s, sized for a single-article render). The template is JSON and
        /// cannot carry a comment, so the reason lives here: the ceiling exists for THIS path.
        /// Lowering either value again will start timing out whole-site renders on the big
        /// sites, and the failure is partial -- some pages get the new nav and some do not.
        /// </summary>
        private async Task RenderAllPagesForSite(
            WebsiteModel site, int websiteId, string environment, string contentBucket, ILambdaContext context,
            string reason = "site-wide asset")
        {
            if (string.IsNullOrWhiteSpace(site.name)) return;

            var conn = await ConnectionInfoAsync(environment);
            var pageDao = new PageDAO(conn);
            var allPages = await pageDao.SearchPages(
                new List<string>(), new List<string>(), null, null, websiteId.ToString());

            var served = (allPages ?? new List<PageModel>())
                .Where(p => !string.IsNullOrWhiteSpace(p.name)
                            && (string.IsNullOrEmpty(p.status) || p.status == "published"))
                .ToList();

            if (served.Count == 0)
            {
                context.Logger.Log($"Static render trigger: {reason} changed on {site.name} but no served pages to render.");
                return;
            }

            context.Logger.Log($"Static render trigger: {reason} changed on {site.name} -- re-rendering {served.Count} page(s).");

            var renderer = new StaticPageRenderer(contentBucket, "us-west-2");
            var assets = await renderer.LoadSiteAssetsAsync(websiteId, site.name);

            using var gate = new System.Threading.SemaphoreSlim(SiteRenderConcurrency);
            int ok = 0;

            var tasks = served.Select(async pg =>
            {
                await gate.WaitAsync();
                try
                {
                    await renderer.RenderAsync(pg, site.name, upload: true, assets);
                    System.Threading.Interlocked.Increment(ref ok);
                }
                catch (Exception ex)
                {
                    // One bad page must not abandon the other 470.
                    context.Logger.LogError($"Static render trigger: page '{pg.name}' failed: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);
            context.Logger.Log($"Static render trigger: {reason} re-render touched {ok}/{served.Count} page(s) on {site.name}.");
        }

        /// <summary>
        /// How many pages to render at once on a whole-site render. Tuned against the Lambda's
        /// memory: too high and the S3 client's connection pool and GC start costing more than
        /// the parallelism buys.
        /// </summary>
        private const int SiteRenderConcurrency = 8;

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
