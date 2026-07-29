using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySQLConnector.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApplicationArch.content
{
    // Per-site presentation metadata that isn't in the DB (public title + GA tag).
    // Kept in sync with scripts/publish-static-pages.ps1 so the batch backfill and the
    // on-save render produce identical output.
    public class SiteMeta
    {
        public string Domain { get; set; }
        public string Title { get; set; }
        public string Analytics { get; set; }
        public SiteMeta(string domain, string title, string analytics)
        {
            Domain = domain; Title = title; Analytics = analytics;
        }
    }

    /// <summary>
    /// Renders a CMS page to a complete, crawlable static HTML document and (optionally)
    /// uploads it to the site's public bucket at a clean path ({slug}/index.html).
    ///
    /// The public sites run as a client-rendered React SPA, so every URL returns the same
    /// near-empty shell and crawlers see no content. This produces a real document -- unique
    /// title, meta description, canonical, Open Graph/Twitter, JSON-LD, and the article body
    /// inlined -- with no JavaScript required to read it.
    ///
    /// This is the single source of render truth: the SetArticleContent save hook, the
    /// RegenerateAllStaticPages backfill endpoint, and (by mirrored logic) the PowerShell
    /// preview script all go through the same document template.
    /// </summary>
    public class StaticPageRenderer
    {
        private readonly string _contentBucket;
        private readonly string _region;

        private static readonly Dictionary<int, SiteMeta> SiteMetaById = new Dictionary<int, SiteMeta>
        {
            // The GA4 measurement ids must match react/baseProject/configs/{site}-Prod.json
            // (analyticsTag) -- that is where the SPA reads them from, and it is the source
            // of truth. A blank here silently drops analytics for that site the moment its
            // pages go static. StaticLayoutCssTests.Analytics_MatchesTheReactConfigs guards it.
            { 1, new SiteMeta("ldsfaithincrisis.com", "LDS Faith in Crisis", "G-3X09PX2WS1") },
            { 2, new SiteMeta("ldsdoctrines.com", "LDS Doctrines", "G-PY9E2KT5DC") },
            { 4, new SiteMeta("reflectiverealizations.com", "Reflective Realizations", "") },
            { 5, new SiteMeta("ldsapologetics.com", "LDS Apologetics", "G-J6H714HFSM") },
            { 6, new SiteMeta("ldsdiscussions.info", "LDS Discussions", "G-G8VH9TBRNR") },
            { 8, new SiteMeta("cesletter.info", "CES Letter", "G-Z4XDMTTGRN") },
        };

        public StaticPageRenderer(string contentBucket, string region)
        {
            _contentBucket = contentBucket;
            _region = region;
        }

        public static SiteMeta GetSiteMeta(int websiteId, string fallbackDomain)
            => SiteMetaById.TryGetValue(websiteId, out var m) ? m : new SiteMeta(fallbackDomain, fallbackDomain, "");

        /// <summary>
        /// Domain for a website id, without a database round trip. Used by endpoints that only
        /// need to know which site they are acting on -- an unknown id is a real answer here
        /// (the site is not in the static registry), so it fails loudly rather than guessing.
        /// </summary>
        public static bool TryGetSiteDomain(int websiteId, out string domain)
        {
            if (SiteMetaById.TryGetValue(websiteId, out var m) && !string.IsNullOrWhiteSpace(m.Domain))
            {
                domain = m.Domain;
                return true;
            }
            domain = null;
            return false;
        }

        /// <summary>Website ids this renderer knows how to serve. For error messages.</summary>
        public static IEnumerable<int> KnownSiteIds => SiteMetaById.Keys;

        /// <summary>
        /// Merge the site's own metadata file over the compiled defaults.
        ///
        /// s3://{content}/public/websites/{domain}/site-meta.json
        ///     { "title": "Example Site", "analytics": "G-XXXXXXXXXX" }
        ///
        /// Only title and analytics are read; the domain is always the site name we were
        /// called with, because that is what the bucket and canonical URLs are built from and
        /// letting a data file redirect those would be a way to publish one site over another.
        ///
        /// An absent file, empty file, or malformed JSON all fall through to the compiled
        /// table -- a metadata file is an override, never a prerequisite for rendering.
        /// </summary>
        public static async Task<SiteMeta> ResolveSiteMetaAsync(
            AmazonS3Storage content, string basePath, int websiteId, string siteName)
        {
            var fallback = GetSiteMeta(websiteId, siteName);
            string json = await TryRead(content, "site-meta.json", basePath);
            if (string.IsNullOrWhiteSpace(json)) return fallback;

            try
            {
                var o = JObject.Parse(json);
                string title = (string)o["title"];
                // An explicit "" means "this site deliberately has no analytics", which is
                // different from the key being absent -- so only null falls back.
                var analyticsToken = o["analytics"];
                string analytics = analyticsToken == null ? fallback.Analytics : (string)analyticsToken;

                return new SiteMeta(
                    fallback.Domain,
                    string.IsNullOrWhiteSpace(title) ? fallback.Title : title,
                    analytics ?? "");
            }
            catch (JsonException)
            {
                // A broken metadata file must not stop a page from publishing.
                return fallback;
            }
        }

        /// <summary>
        /// Website ids that must NEVER be published to, whatever STATIC_PUBLISH_SITES says.
        ///
        /// This is a DENY list and it outranks everything, because the failure it prevents is
        /// destructive and silent: rendering a site's CMS "Home" writes to the bucket ROOT
        /// index.html, straight over whatever is live there, with no backup.
        ///
        /// 4 = reflectiverealizations.com. It is not a React SPA at all -- it is a hand-built
        /// static site (its own fonts and stylesheets, a hand-authored homepage with client
        /// reviews) that is already fully crawlable. Its CMS record holds a single 2KB article,
        /// so publishing it would replace an 8.5KB designed page with that fragment. It is
        /// excluded from the tooling in scripts/sites.json for the same reason; this is the
        /// server-side half of that, so switching the env var to "all" cannot destroy it.
        /// </summary>
        private static readonly HashSet<int> NeverPublish = new HashSet<int> { 4 };

        /// <summary>
        /// Which sites the render trigger is allowed to publish to, from the
        /// STATIC_PUBLISH_SITES env var: a comma-separated list of website ids, or "all".
        ///
        /// DEFAULT IS EMPTY, MEANING PUBLISH NOTHING. That mattered most during the migration,
        /// when publishing to a site that had not been cut over would have written over its
        /// live SPA shell. Now that every applicable site is migrated the normal setting is
        /// "all", and the per-site list is kept only as an escape hatch -- maintaining it by
        /// hand is itself a hazard, since a site left off is migrated-but-frozen with nothing
        /// reporting a problem.
        /// </summary>
        public static bool IsPublishEnabled(int websiteId)
        {
            if (NeverPublish.Contains(websiteId)) return false;

            var raw = Environment.GetEnvironmentVariable("STATIC_PUBLISH_SITES");
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (raw.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) return true;
            return raw.Split(',')
                      .Select(s => s.Trim())
                      .Any(s => int.TryParse(s, out var id) && id == websiteId);
        }

        // ---------------------------------------------------------------- pure helpers

        // Public path segment: lowercase, keep hyphens, strip anything unsafe.
        public static string Slug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var s = name.Trim().ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", "-");
            s = Regex.Replace(s, @"[^a-z0-9\-]", "");
            s = Regex.Replace(s, @"-{2,}", "-").Trim('-');
            return s;
        }

        // Mirrors the SPA's processLayout(): "2, 1 Grid" -> "layout-2-1".
        public static string LayoutClass(string layout)
        {
            if (string.IsNullOrWhiteSpace(layout)) return "";
            var s = Regex.Replace(layout, @"\s+", "");
            s = s.Replace(",", "-").Replace("/", "_");
            s = Regex.Replace(s, "(?i)Grid", "").ToLowerInvariant();
            return "layout-" + s;
        }

        private static string Enc(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string FirstH1(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var m = Regex.Match(html, @"(?is)<h1[^>]*>(.*?)</h1>");
            if (!m.Success) return "";
            return Regex.Replace(m.Groups[1].Value, @"(?s)<[^>]+>", "").Trim();
        }

        private static string MetaDescription(string pageDesc, string html)
        {
            var d = pageDesc;
            if (string.IsNullOrWhiteSpace(d))
            {
                var m = Regex.Match(html ?? "", @"(?is)<p[^>]*>(.*?)</p>");
                if (m.Success) d = Regex.Replace(m.Groups[1].Value, @"(?s)<[^>]+>", "");
            }
            d = Regex.Replace(d ?? "", @"\s+", " ").Trim();
            if (d.Length > 160) d = d.Substring(0, 157).TrimEnd() + "...";
            return d;
        }

        // Builds a <nav> from sitemenu.json. Defensive: handles the flat
        // react-dnd-treeview shape (id/parent/text with pageName possibly under .data),
        // and returns "" on any parse trouble (nav is enhancement, not SEO-critical).
        private static string BuildNav(string menuJson, string domain)
        {
            if (string.IsNullOrWhiteSpace(menuJson)) return "";
            JArray items;
            try { items = JArray.Parse(menuJson); }
            catch { return ""; }
            if (items == null || items.Count == 0) return "";

            string PageName(JToken it) =>
                (it["pageName"] ?? it["data"]?["pageName"])?.ToString();

            // A menu item that points at a page must be a LINK, at any level. Home lives
            // at the site root, not /home/ -- same rule as PublicPath and the CloudFront
            // function.
            string Href(string pageName) =>
                string.Equals(pageName, "Home", StringComparison.OrdinalIgnoreCase)
                    ? $"https://www.{domain}/"
                    : $"https://www.{domain}/{Slug(pageName)}/";

            var sb = new StringBuilder();
            sb.AppendLine("<nav class=\"menuContents\" aria-label=\"Site navigation\"><ul>");
            var top = items.Where(i => (i["parent"]?.ToString() ?? "0") == "0").ToList();
            foreach (var section in top)
            {
                var label = Enc(section["text"]?.ToString() ?? "");
                var sectionPage = PageName(section);

                // Top-level items are frequently pages in their own right rather than mere
                // group headings -- ldsfaithincrisis's whole menu is two of them. Rendering
                // those as a bare <span> makes the entire navigation unclickable, so link
                // whenever the item has a page and fall back to a heading only when it does
                // not.
                if (!string.IsNullOrEmpty(sectionPage))
                    sb.AppendLine($"<li><a href=\"{Href(sectionPage)}\">{label}</a>");
                else
                    sb.AppendLine($"<li><span class=\"nav-section\">{label}</span>");

                var sid = section["id"]?.ToString();
                var children = items.Where(i => i["parent"]?.ToString() == sid && !string.IsNullOrEmpty(PageName(i))).ToList();
                if (children.Count > 0)
                {
                    sb.AppendLine("<ul>");
                    foreach (var child in children)
                    {
                        var ctext = Enc(child["text"]?.ToString() ?? "");
                        sb.AppendLine($"<li><a href=\"{Href(PageName(child))}\">{ctext}</a></li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul></nav>");
            return sb.ToString();
        }

        /// <summary>Assemble the full static document. Pure (no I/O) so it is trivially testable.</summary>
        public string BuildDocument(PageModel page, SiteMeta site, string bodyHtml, string headerHtml, string navHtml, string themeCss, string baseStyles = "")
        {
            bool isHome = string.Equals(page.name, "Home", StringComparison.OrdinalIgnoreCase);
            string slug = isHome ? "" : Slug(page.name);
            string canonical = isHome ? $"https://www.{site.Domain}/" : $"https://www.{site.Domain}/{slug}/";

            // Title precedence: the visible <h1> is best, then the PAGE name, and only
            // then an article name.
            //
            // Page name before article name matters: page names are human-meaningful and
            // already match the URL ("About-Us" -> "About Us"), whereas article names are
            // internal CMS labels. Getting this backwards titled the About Us page
            // "Alexis-Bio" and the podcast page "PodCastList" -- and <title> is the single
            // highest-impact on-page SEO element, which is the whole point of going static.
            // Only pages with no <h1> are affected; content that follows the house style
            // (see CLAUDE.md) always has one and is unchanged.
            string h1 = FirstH1(bodyHtml);
            string pageTitle = (page.name ?? "").Replace("-", " ").Trim();
            string title = !string.IsNullOrWhiteSpace(h1) ? h1
                         : !string.IsNullOrWhiteSpace(pageTitle) ? pageTitle
                         : (page.articles?.FirstOrDefault()?.name ?? "");
            string desc = MetaDescription(page.description, bodyHtml);

            string encTitle = Enc(title);
            string encDesc = Enc(desc);
            string encSite = Enc(site.Title);

            string ogImage = "";
            var firstMeme = page.articles?.FirstOrDefault(a => !string.IsNullOrEmpty(a.memeImagePath))?.memeImagePath;
            if (!string.IsNullOrEmpty(firstMeme))
                ogImage = $"https://www-websitecontent.s3.us-west-2.amazonaws.com/public/{firstMeme}";

            string jsonLd = JsonConvert.SerializeObject(new
            {
                context = "https://schema.org",
                type = "Article",
                headline = title,
                description = desc,
                inLanguage = "en",
                mainEntityOfPage = canonical,
                url = canonical,
                publisher = new { type = "Organization", name = site.Title, url = $"https://www.{site.Domain}/" }
            }).Replace("\"context\":", "\"@context\":").Replace("\"type\":", "\"@type\":");

            // This JSON sits inside a <script> element, where a literal '<' in a title or
            // description would end the block early -- a page title containing "</script>"
            // silently corrupts the whole document. Newtonsoft leaves those characters raw.
            // Escaping them is also what PowerShell's ConvertTo-Json does, so doing it here
            // keeps the two renderers byte-identical instead of merely equivalent.
            jsonLd = jsonLd.Replace("<", "\\u003c")
                           .Replace(">", "\\u003e")
                           .Replace("&", "\\u0026")
                           .Replace("'", "\\u0027");

            string ga = string.IsNullOrEmpty(site.Analytics) ? "" :
                $"<script async src=\"https://www.googletagmanager.com/gtag/js?id={site.Analytics}\"></script>\n" +
                $"<script>window.dataLayer=window.dataLayer||[];function gtag(){{dataLayer.push(arguments);}}gtag('js',new Date());gtag('config','{site.Analytics}');</script>";

            // Cascade order matters, least specific first:
            //   1. page-layout.css  structural grids, padding, menu   (from the SPA bundle)
            //   2. BaseStyles.css   per-site brand: body font/colour  (from the SPA bundle)
            //   3. theme.css        ThemeBuilder output, may be absent
            // Each later sheet must be able to override the earlier one. All three are
            // inlined so a page is self-contained.
            //
            // Consequence worth knowing: changing BaseStyles.css does NOT propagate to
            // already-rendered pages until they are re-rendered. Use RegenerateAllStaticPages.
            string layoutTag = $"<style>{StaticLayoutCss.Css}</style>";
            string baseTag = string.IsNullOrWhiteSpace(baseStyles) ? "" : $"<style>{baseStyles}</style>";
            string themeTag = string.IsNullOrWhiteSpace(themeCss) ? "" : $"<style>{themeCss}</style>";
            string ogImageTag = string.IsNullOrEmpty(ogImage) ? "" : $"<meta property=\"og:image\" content=\"{ogImage}\">";
            string layoutClass = LayoutClass(page.layout);

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            // The home page titles as the site itself. "Home | LDS Faith in Crisis" wastes
            // the most valuable characters in the SERP on a word nobody searches for.
            sb.AppendLine(isHome && string.IsNullOrWhiteSpace(h1)
                ? $"<title>{encSite}</title>"
                : $"<title>{encTitle} | {encSite}</title>");
            sb.AppendLine($"<meta name=\"description\" content=\"{encDesc}\">");
            sb.AppendLine($"<link rel=\"canonical\" href=\"{canonical}\">");
            sb.AppendLine("<meta name=\"robots\" content=\"index, follow\">");
            sb.AppendLine("<meta property=\"og:type\" content=\"article\">");
            sb.AppendLine($"<meta property=\"og:title\" content=\"{encTitle}\">");
            sb.AppendLine($"<meta property=\"og:description\" content=\"{encDesc}\">");
            sb.AppendLine($"<meta property=\"og:url\" content=\"{canonical}\">");
            sb.AppendLine($"<meta property=\"og:site_name\" content=\"{encSite}\">");
            if (!string.IsNullOrEmpty(ogImageTag)) sb.AppendLine(ogImageTag);
            sb.AppendLine("<meta name=\"twitter:card\" content=\"summary_large_image\">");
            sb.AppendLine($"<meta name=\"twitter:title\" content=\"{encTitle}\">");
            sb.AppendLine($"<meta name=\"twitter:description\" content=\"{encDesc}\">");
            sb.AppendLine(layoutTag);
            if (!string.IsNullOrEmpty(baseTag)) sb.AppendLine(baseTag);
            if (!string.IsNullOrEmpty(themeTag)) sb.AppendLine(themeTag);
            sb.AppendLine($"<script type=\"application/ld+json\">{jsonLd}</script>");
            if (!string.IsNullOrEmpty(ga)) sb.AppendLine(ga);
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"pageContents\">");
            sb.AppendLine($"<header class=\"headerContents\">{headerHtml}</header>");
            if (!string.IsNullOrEmpty(navHtml)) sb.AppendLine(navHtml);
            sb.AppendLine($"<main class=\"articleContents {layoutClass}\">");
            sb.AppendLine(bodyHtml);
            sb.AppendLine("</main>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- IO

        private static async Task<string> TryRead(AmazonS3Storage s3, string filename, string path)
        {
            try
            {
                using var stream = s3.DownloadFile(filename, path);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch { return ""; }
        }

        /// <summary>
        /// Read the page's article HTML + site assets from the content bucket, assemble the
        /// document, and (when upload=true) write it to the public bucket at {slug}/index.html.
        /// Returns the generated HTML (so callers can preview without uploading), or null if
        /// the page has no article content to publish.
        /// </summary>
        public async Task<string> RenderAsync(PageModel page, int websiteId, string siteName, bool upload)
        {
            var content = new AmazonS3Storage(_contentBucket, _region);
            string basePath = $"public/websites/{siteName}";

            // Per-site presentation metadata (public title, GA tag) comes from a file in the
            // content bucket, falling back to the compiled table below.
            //
            // The file is what makes a new site provisionable WITHOUT a code change: the
            // compiled dictionary meant every site added required editing C# and redeploying
            // the Lambda, which is fine for a handful of sites you own and impossible for
            // self-serve creation. The table is kept only so the sites that predate the file
            // keep working; once each has a site-meta.json it can go.
            var site = await ResolveSiteMetaAsync(content, basePath, websiteId, siteName);

            // The PUBLIC site bucket holds BaseStyles.css, which the SPA build deploys to its
            // root and compiles into the bundle.
            var pubAssets = new AmazonS3Storage($"www.{site.Domain}", _region);

            // Header comes from the CONTENT bucket only -- the single place the SPA looks
            // (PageGallery -> FileProcessing.fileExists("websites/{site}", headerFileName)).
            // A site with nothing there genuinely has no header, and the static page must
            // agree. Do NOT fall back to a header.html at the public bucket root: those are
            // leftovers from old builds, and falling back put a banner on cesletter.info
            // that neither its SPA nor its live site ever showed.
            string headerHtml = await TryRead(content, "header.html", basePath);

            string menuJson = await TryRead(content, "sitemenu.json", basePath);

            // BaseStyles.css is the site's REAL stylesheet -- body font/colour/line-height and
            // the .headerContents/.menuContents layout. It is compiled into the SPA bundle, so
            // a static page never sees it, and three of the six sites (ldsapologetics,
            // ldsdoctrines, cesletter) have no theme.css at all, making this their ONLY
            // site-wide CSS. Read it from the public bucket root, where the SPA build puts it.
            string baseStyles = await TryRead(pubAssets, "BaseStyles.css", "");

            // theme.css is the ThemeBuilder output. Genuinely absent for several sites.
            string themeCss = await TryRead(content, "theme.css", $"public/assets/{siteName}/themes");

            var body = new StringBuilder();
            foreach (var a in (page.articles ?? new List<ArticleModel>()).OrderBy(x => x.sequence_no))
            {
                if (string.IsNullOrEmpty(a.articlePath)) continue;
                string html = await TryRead(content, a.articlePath, $"{basePath}/articles");
                if (!string.IsNullOrWhiteSpace(html)) body.Append("<article>").Append(html).Append("</article>\n");
            }
            if (body.Length == 0) return null;

            string nav = BuildNav(menuJson, site.Domain);
            string doc = BuildDocument(page, site, body.ToString(), headerHtml, nav, themeCss, baseStyles);

            if (upload)
            {
                bool isHome = string.Equals(page.name, "Home", StringComparison.OrdinalIgnoreCase);
                string slug = isHome ? "" : Slug(page.name);
                var pub = new AmazonS3Storage($"www.{site.Domain}", _region);
                var bytes = Encoding.UTF8.GetBytes(doc);
                using var ms = new MemoryStream(bytes);
                // key = {slug}/index.html (or index.html for home).
                await pub.UploadFile(ms, "index.html", slug, "text/html; charset=utf-8", "public, max-age=300");
            }
            return doc;
        }

        /// <summary>The public path a page publishes to (for logging / sitemap).</summary>
        public static string PublicPath(PageModel page)
        {
            if (string.Equals(page.name, "Home", StringComparison.OrdinalIgnoreCase)) return "/";
            return "/" + Slug(page.name) + "/";
        }
    }
}
