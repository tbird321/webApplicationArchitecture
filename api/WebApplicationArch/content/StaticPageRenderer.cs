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
    /// Everything a render needs that is the SAME for every page on a site: the resolved
    /// metadata, the header, the built nav, and the two stylesheets -- plus the S3 handles
    /// they were read through.
    ///
    /// WHY THIS EXISTS
    /// ---------------
    /// A sitewide change (menu, header, site-meta, theme) re-renders EVERY page, and the
    /// naive loop re-read all five of these files once per page: ~2,355 S3 GETs of five
    /// identical objects on a 471-page site, plus re-parsing sitemenu.json and rebuilding
    /// the same &lt;nav&gt; string 471 times. Loading them once per site makes a whole-site
    /// render cost roughly one read and one write per page, which is the floor.
    ///
    /// Safe to share across concurrent renders: AmazonS3Storage wraps AmazonS3Client, which
    /// is thread-safe, and every field here is set once at load and only read afterwards.
    /// </summary>
    public sealed class SiteAssets
    {
        public SiteMeta Site { get; set; }
        public string HeaderHtml { get; set; }
        /// <summary>Already built by BuildNav -- the JSON is parsed once, not once per page.</summary>
        public string NavHtml { get; set; }
        public string BaseStyles { get; set; }
        public string ThemeCss { get; set; }
        /// <summary>Content bucket handle, reused for each page's article reads.</summary>
        public AmazonS3Storage Content { get; set; }
        /// <summary>Public site bucket handle, reused for each page's upload.</summary>
        public AmazonS3Storage Public { get; set; }
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
            // Replaced the site's old UA-69739113-1 property, which was Universal Analytics and
            // stopped collecting in July 2023. The GA4 property also issues a Google tag id
            // (GT-57VBS2J); the renderer's gtag snippet keys on the measurement id below, so the
            // GT- form is not used here.
            { 9, new SiteMeta("ldsgospeldoctrine.info", "LDS Gospel Doctrine", "G-0QJ9ZJ0LC6") },
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

        /// <summary>
        /// Turn markup into plain text: strip tags, then resolve HTML entities.
        ///
        /// The decode is the part that is easy to miss and expensive to get wrong. Article HTML
        /// follows the house style in CLAUDE.md, which uses &amp;mdash;, &amp;ldquo; and &amp;rdquo;
        /// liberally. Stripping tags leaves those entities as LITERAL TEXT, and Enc() then escapes
        /// their ampersand -- so an &lt;h1&gt; reading "A &amp;mdash; B" became the title
        /// "A &amp;amp;mdash; B", which search engines and browser tabs render as the visible
        /// characters "A &amp;mdash; B". Decoding here means Enc() escapes a real em dash, which
        /// needs no escaping at all.
        ///
        /// Decode must happen BEFORE truncation too, or "&amp;mdash;" burns 7 characters of the
        /// 160-character description budget instead of 1.
        /// </summary>
        private static string PlainText(string markup)
        {
            if (string.IsNullOrEmpty(markup)) return "";
            var stripped = Regex.Replace(markup, @"(?s)<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(stripped);
        }

        /// <summary>
        /// Repairs in-body links whose leading slash was stripped.
        ///
        /// The admin editor (TinyMCE) ran for a long time on its default convert_urls /
        /// relative_urls settings, which rewrite an authored "/some-page/" into a path relative
        /// to the page being edited -- "some-page/", or "../../some-page/". Rendered pages live
        /// at /{slug}/, so a relative href resolves *underneath* the current article and 404s.
        /// Nothing surfaces the breakage: the nav is rebuilt from sitemenu.json on every render
        /// with absolute URLs, so only in-body links rot, and they rot silently.
        ///
        /// The editor default is now overridden, but articles saved before that still carry the
        /// damage and any future editor could reintroduce it, so the render normalises too.
        ///
        /// Left strictly alone -- these must never be rewritten:
        ///   * anything carrying a URI scheme (http:, https:, mailto:, tel:, data:, javascript:)
        ///   * protocol-relative "//host/path", which is external
        ///   * fragment-only "#section" and query-only "?x=1"
        ///   * hrefs already rooted at "/"
        /// Only a scheme-less, non-rooted path is touched. Leading "./" and "../" segments are
        /// dropped before rooting, because "../../some-page/" was authored as "/some-page/".
        /// </summary>
        public static string NormalizeInternalLinks(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            if (html.IndexOf("href", StringComparison.OrdinalIgnoreCase) < 0) return html;

            return Regex.Replace(
                html,
                @"(?is)(<a\b[^>]*?\bhref\s*=\s*)(""([^""]*)""|'([^']*)')",
                m =>
                {
                    bool single = m.Groups[4].Success;
                    string href = single ? m.Groups[4].Value : m.Groups[3].Value;
                    string rooted = RootInternalHref(href);
                    if (string.Equals(rooted, href, StringComparison.Ordinal)) return m.Value;
                    string quote = single ? "'" : "\"";
                    return m.Groups[1].Value + quote + rooted + quote;
                });
        }

        /// <summary>
        /// Returns the href unchanged unless it is a scheme-less, non-rooted path, in which case
        /// it is rooted at "/". See <see cref="NormalizeInternalLinks"/> for why.
        /// </summary>
        private static string RootInternalHref(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return href;

            var h = href.Trim();

            // A leading '/' covers both site-rooted "/page/" and protocol-relative "//host/path".
            // Both are already correct as authored; neither may be touched.
            if (h[0] == '/' || h[0] == '#' || h[0] == '?') return href;

            // Any URI scheme at all -- http:, https:, mailto:, tel:, data:, javascript:.
            if (Regex.IsMatch(h, @"^[a-zA-Z][a-zA-Z0-9+.\-]*:")) return href;

            // Whatever "./" or "../" prefix the editor introduced is not part of the author's
            // intent; the target is a top-level page slug.
            var trimmed = Regex.Replace(h, @"^(?:\.{1,2}/)+", "");
            if (trimmed.Length == 0) return href;

            return "/" + trimmed;
        }

        private static string FirstH1(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var m = Regex.Match(html, @"(?is)<h1[^>]*>(.*?)</h1>");
            if (!m.Success) return "";
            return PlainText(m.Groups[1].Value).Trim();
        }

        private static string MetaDescription(string pageDesc, string html)
        {
            var d = pageDesc;
            if (string.IsNullOrWhiteSpace(d))
            {
                var m = Regex.Match(html ?? "", @"(?is)<p[^>]*>(.*?)</p>");
                if (m.Success) d = m.Groups[1].Value;
            }
            // Applied to the CMS description as well as the extracted paragraph: descriptions are
            // authored alongside the HTML and pick up the same entities.
            d = PlainText(d ?? "");
            d = Regex.Replace(d, @"\s+", " ").Trim();
            if (d.Length > 160)
            {
                // Truncate on a word boundary -- cutting mid-word produced descriptions ending
                // "...f..." in live search results.
                var cut = d.Substring(0, 157);
                var lastSpace = cut.LastIndexOf(' ');
                if (lastSpace > 100) cut = cut.Substring(0, lastSpace);
                d = cut.TrimEnd(' ', ',', ';', ':', '-', '—') + "...";
            }
            return d;
        }

        /// <summary>
        /// How many levels of nav are rendered. Top-level sections are depth 0, so 5 allows
        /// Section > Group > Subgroup > Sub-subgroup > Page.
        ///
        /// A cap exists because sitemenu.json is written by four clients (the tree editor, the
        /// in-page editor, the MCP tools, and hand edits) and none of them owns the file. A
        /// malformed parent chain must degrade to a shallower menu, never to a renderer that
        /// walks forever -- this runs inside a whole-site render that already has 471 pages
        /// and a 900 s ceiling to fit into.
        ///
        /// Kept in sync with MAX_MENU_DEPTH in api/mcp/src/tools/navigation.js, which refuses
        /// to CREATE what this would refuse to render.
        /// </summary>
        public const int MaxNavDepth = 5;

        // Builds a <nav> from sitemenu.json. Defensive: handles the flat
        // react-dnd-treeview shape (id/parent/text with pageName possibly under .data),
        // and returns "" on any parse trouble (nav is enhancement, not SEO-critical).
        //
        // Nests to MaxNavDepth. This was two hard-coded loops until 2026-08-12, which silently
        // dropped every grandchild -- the data model has always carried arbitrary depth, since
        // an item names its parent and nothing bounds that chain.
        public static string BuildNav(string menuJson, string domain)
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

            // Index children by parent id once. Document order within a parent is preserved,
            // which is what the tree editor's drag-to-reorder actually writes -- sibling order
            // IS array order, there is no sort key.
            var byParent = new Dictionary<string, List<JToken>>(StringComparer.Ordinal);
            foreach (var it in items)
            {
                if (it == null) continue;
                var p = it["parent"]?.ToString() ?? "0";
                if (!byParent.TryGetValue(p, out var bucket))
                    byParent[p] = bucket = new List<JToken>();
                bucket.Add(it);
            }

            // Every id renders at most once. This is what makes a malformed file safe: an item
            // that is its own parent, or a duplicated id, is skipped on the second visit rather
            // than recursed into. The depth cap is the second line of defence.
            var rendered = new HashSet<string>(StringComparer.Ordinal);

            string RenderLevel(string parentId, int depth)
            {
                if (depth >= MaxNavDepth) return "";
                if (!byParent.TryGetValue(parentId, out var children)) return "";

                var level = new StringBuilder();
                foreach (var item in children)
                {
                    var id = item["id"]?.ToString();
                    if (string.IsNullOrEmpty(id) || !rendered.Add(id)) continue;

                    var label = Enc(item["text"]?.ToString() ?? "");
                    var pageName = PageName(item);
                    var childHtml = RenderLevel(id, depth + 1);

                    // Below the top level an item earns its place by going somewhere: it links
                    // to a page, or it heads a group of items that do. A pageless, childless
                    // entry at depth 1+ is an editing leftover.
                    //
                    // Top-level items are exempt: a site may deliberately carry an empty
                    // section, and dropping those would change menus that render correctly
                    // today.
                    if (depth > 0 && string.IsNullOrEmpty(pageName) && childHtml.Length == 0)
                        continue;

                    // Items are frequently pages in their own right rather than mere group
                    // headings -- ldsfaithincrisis's whole menu is two of them. Rendering those
                    // as a bare <span> makes the navigation unclickable, so link whenever the
                    // item has a page and fall back to a heading only when it does not. This
                    // holds at every depth, including a section that has children AND a page.
                    var anchor = !string.IsNullOrEmpty(pageName)
                        ? $"<a href=\"{Href(pageName)}\">{label}</a>"
                        : $"<span class=\"nav-section\">{label}</span>";

                    if (childHtml.Length == 0)
                    {
                        level.AppendLine($"<li>{anchor}</li>");
                    }
                    else
                    {
                        level.AppendLine($"<li>{anchor}");
                        level.AppendLine("<ul>");
                        level.Append(childHtml);
                        level.AppendLine("</ul>");
                        level.AppendLine("</li>");
                    }
                }
                return level.ToString();
            }

            var top = RenderLevel("0", 0);
            if (top.Length == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine("<nav class=\"menuContents\" aria-label=\"Site navigation\"><ul>");
            sb.Append(top);
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
        /// Read the five files that are identical for every page on the site, and build the
        /// nav once. Call this ONCE per whole-site render and hand the result to the
        /// RenderAsync overload below; see SiteAssets for why.
        /// </summary>
        public async Task<SiteAssets> LoadSiteAssetsAsync(int websiteId, string siteName)
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
            // root and compiles into the bundle. It is also where rendered pages are uploaded.
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

            return new SiteAssets
            {
                Site = site,
                HeaderHtml = headerHtml,
                NavHtml = BuildNav(menuJson, site.Domain),
                BaseStyles = baseStyles,
                ThemeCss = themeCss,
                Content = content,
                Public = pubAssets
            };
        }

        /// <summary>
        /// Read the page's article HTML + site assets from the content bucket, assemble the
        /// document, and (when upload=true) write it to the public bucket at {slug}/index.html.
        /// Returns the generated HTML (so callers can preview without uploading), or null if
        /// the page has no article content to publish.
        ///
        /// Loads the sitewide assets itself, so it stands alone for a ONE-page render. A loop
        /// over many pages should load them once and use the overload below instead.
        /// </summary>
        public async Task<string> RenderAsync(PageModel page, int websiteId, string siteName, bool upload)
        {
            var assets = await LoadSiteAssetsAsync(websiteId, siteName);
            return await RenderAsync(page, siteName, upload, assets);
        }

        /// <summary>
        /// Render one page against already-loaded sitewide assets. Identical output to the
        /// overload above -- the only difference is who pays for the five shared reads.
        /// </summary>
        public async Task<string> RenderAsync(PageModel page, string siteName, bool upload, SiteAssets assets)
        {
            if (assets == null) throw new ArgumentNullException(nameof(assets));

            var site = assets.Site;
            string basePath = $"public/websites/{siteName}";

            var body = new StringBuilder();
            foreach (var a in (page.articles ?? new List<ArticleModel>()).OrderBy(x => x.sequence_no))
            {
                if (string.IsNullOrEmpty(a.articlePath)) continue;
                string html = await TryRead(assets.Content, a.articlePath, $"{basePath}/articles");
                if (!string.IsNullOrWhiteSpace(html))
                    body.Append("<article>").Append(NormalizeInternalLinks(html)).Append("</article>\n");
            }
            if (body.Length == 0) return null;

            string doc = BuildDocument(page, site, body.ToString(), assets.HeaderHtml, assets.NavHtml, assets.ThemeCss, assets.BaseStyles);

            if (upload)
            {
                bool isHome = string.Equals(page.name, "Home", StringComparison.OrdinalIgnoreCase);
                string slug = isHome ? "" : Slug(page.name);
                var bytes = Encoding.UTF8.GetBytes(doc);
                using var ms = new MemoryStream(bytes);
                // key = {slug}/index.html (or index.html for home).
                await assets.Public.UploadFile(ms, "index.html", slug, "text/html; charset=utf-8", "public, max-age=300");
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
