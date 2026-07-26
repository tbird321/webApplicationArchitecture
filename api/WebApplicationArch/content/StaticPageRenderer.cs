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
            { 1, new SiteMeta("ldsfaithincrisis.com", "LDS Faith in Crisis", "") },
            { 2, new SiteMeta("ldsdoctrines.com", "LDS Doctrines", "") },
            { 4, new SiteMeta("reflectiverealizations.com", "Reflective Realizations", "") },
            { 5, new SiteMeta("ldsapologetics.com", "LDS Apologetics", "G-J6H714HFSM") },
            { 6, new SiteMeta("ldsdiscussions.info", "LDS Discussions", "") },
            { 8, new SiteMeta("cesletter.info", "CES Letter", "") },
        };

        public StaticPageRenderer(string contentBucket, string region)
        {
            _contentBucket = contentBucket;
            _region = region;
        }

        public static SiteMeta GetSiteMeta(int websiteId, string fallbackDomain)
            => SiteMetaById.TryGetValue(websiteId, out var m) ? m : new SiteMeta(fallbackDomain, fallbackDomain, "");

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

            var sb = new StringBuilder();
            sb.AppendLine("<nav class=\"menuContents\" aria-label=\"Site navigation\"><ul>");
            var top = items.Where(i => (i["parent"]?.ToString() ?? "0") == "0").ToList();
            foreach (var section in top)
            {
                var label = Enc(section["text"]?.ToString() ?? "");
                sb.AppendLine($"<li><span class=\"nav-section\">{label}</span>");
                var sid = section["id"]?.ToString();
                var children = items.Where(i => i["parent"]?.ToString() == sid && !string.IsNullOrEmpty(PageName(i))).ToList();
                if (children.Count > 0)
                {
                    sb.AppendLine("<ul>");
                    foreach (var child in children)
                    {
                        var slug = Slug(PageName(child));
                        var ctext = Enc(child["text"]?.ToString() ?? "");
                        sb.AppendLine($"<li><a href=\"https://www.{domain}/{slug}/\">{ctext}</a></li>");
                    }
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul></nav>");
            return sb.ToString();
        }

        /// <summary>Assemble the full static document. Pure (no I/O) so it is trivially testable.</summary>
        public string BuildDocument(PageModel page, SiteMeta site, string bodyHtml, string headerHtml, string navHtml, string themeCss)
        {
            bool isHome = string.Equals(page.name, "Home", StringComparison.OrdinalIgnoreCase);
            string slug = isHome ? "" : Slug(page.name);
            string canonical = isHome ? $"https://www.{site.Domain}/" : $"https://www.{site.Domain}/{slug}/";

            string h1 = FirstH1(bodyHtml);
            string title = !string.IsNullOrEmpty(h1)
                ? h1
                : (page.articles?.FirstOrDefault()?.name ?? (page.name ?? "").Replace("-", " "));
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

            string ga = string.IsNullOrEmpty(site.Analytics) ? "" :
                $"<script async src=\"https://www.googletagmanager.com/gtag/js?id={site.Analytics}\"></script>\n" +
                $"<script>window.dataLayer=window.dataLayer||[];function gtag(){{dataLayer.push(arguments);}}gtag('js',new Date());gtag('config','{site.Analytics}');</script>";

            string themeTag = string.IsNullOrWhiteSpace(themeCss) ? "" : $"<style>{themeCss}</style>";
            string ogImageTag = string.IsNullOrEmpty(ogImage) ? "" : $"<meta property=\"og:image\" content=\"{ogImage}\">";
            string layoutClass = LayoutClass(page.layout);

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine($"<title>{encTitle} | {encSite}</title>");
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
            var site = GetSiteMeta(websiteId, siteName);
            var content = new AmazonS3Storage(_contentBucket, _region);
            string basePath = $"public/websites/{siteName}";

            string headerHtml = await TryRead(content, "header.html", basePath);
            string menuJson = await TryRead(content, "sitemenu.json", basePath);
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
            string doc = BuildDocument(page, site, body.ToString(), headerHtml, nav, themeCss);

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
