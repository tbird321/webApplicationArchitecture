using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// The site's appearance is entirely CSS driven, and the public pages are now static
/// files that never load the SPA bundle. That means every structural rule the SPA
/// relies on has to be duplicated into StaticLayoutCss and inlined at render time.
///
/// These tests exist to catch DRIFT: someone adds a layout to PageContainer.css, the
/// editor picks it in the admin UI, and the static page silently renders as a plain
/// vertical stack because the rule was never mirrored. Nothing else would notice.
/// </summary>
public class StaticLayoutCssTests
{
    /// <summary>
    /// Walk up from the test binary to the repo root. Anchored on having BOTH an api\
    /// and a react\ directory -- a .sln is not a usable marker here because there is one
    /// in api\ as well as at the root.
    /// </summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null &&
               !(Directory.Exists(Path.Combine(dir.FullName, "api")) &&
                 Directory.Exists(Path.Combine(dir.FullName, "react"))))
            dir = dir.Parent;
        Assert.True(dir != null, "Could not locate the repository root from " + AppContext.BaseDirectory);
        return dir!.FullName;
    }

    private static string SpaStylesheetPath() => Path.Combine(
        RepoRoot(), "react", "baseProject", "src", "sitecontent", "content", "PageContainer.css");

    private static HashSet<string> LayoutClassesIn(string css) =>
        Regex.Matches(css, @"\.(layout-[a-z0-9_\-]+)", RegexOptions.IgnoreCase)
             .Select(m => m.Groups[1].Value.ToLowerInvariant())
             .ToHashSet();

    [Fact]
    public void LayoutCss_CoversEveryLayoutInTheSpaStylesheet()
    {
        var spaPath = SpaStylesheetPath();
        Assert.True(File.Exists(spaPath), $"SPA stylesheet not found at {spaPath}");

        var inSpa = LayoutClassesIn(File.ReadAllText(spaPath));
        var inStatic = LayoutClassesIn(StaticLayoutCss.Css);

        Assert.True(inSpa.Count > 0, "Parsed no .layout-* classes out of PageContainer.css -- the parser or the file changed shape.");

        var missing = inSpa.Except(inStatic).OrderBy(x => x).ToList();
        Assert.True(missing.Count == 0,
            "These layouts exist in PageContainer.css but are MISSING from StaticLayoutCss, so any page using them " +
            "renders as an unstyled vertical stack:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void LayoutCss_KeepsArticleContentsPadding()
    {
        // Applies to EVERY page, not just multi-article ones. Losing it makes body text
        // run edge to edge on desktop.
        Assert.Contains(".articleContents", StaticLayoutCss.Css);
        Assert.Contains("padding-left:15%", StaticLayoutCss.Css.Replace(" ", ""));
    }

    [Fact]
    public void LayoutCss_ChildSelectorsAreElementAgnostic()
    {
        // PageContainer.css targets "> div:nth-child(...)" because the SPA wraps each
        // article in a <div>. The static renderer emits <article>. If a rule were copied
        // across verbatim with the "div" left in, it would silently match nothing.
        var offenders = Regex.Matches(StaticLayoutCss.Css, @"\.layout-[a-z0-9_\-]+\s*>\s*div\b", RegexOptions.IgnoreCase)
                             .Select(m => m.Value).ToList();
        Assert.True(offenders.Count == 0,
            "These selectors name a 'div' child, but the static renderer emits <article> -- they will never match:\n  "
            + string.Join("\n  ", offenders));
    }

    [Fact]
    public void LayoutCss_IsLoadableFromTheEmbeddedResource()
    {
        // If the EmbeddedResource entry is dropped from the csproj, StaticLayoutCss throws
        // rather than rendering unstyled pages. Prove the resource is actually there.
        var css = StaticLayoutCss.Css;
        Assert.False(string.IsNullOrWhiteSpace(css));
        Assert.Contains(".articleContents", css);
    }

    [Fact]
    public void LayoutCss_MatchesTheSharedSourceFile()
    {
        // The embedded copy and the file publish-static-pages.ps1 reads must be the same
        // bytes, or the save-hook renderer and the bulk backfill produce different pages.
        var shared = Path.Combine(RepoRoot(), "infra", "static", "page-layout.css");
        Assert.True(File.Exists(shared), $"Shared stylesheet missing at {shared}");
        Assert.Equal(
            File.ReadAllText(shared).Replace("\r\n", "\n").Trim(),
            StaticLayoutCss.Css.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    public void LayoutCss_StylesTheStaticMenuMarkup()
    {
        // The static renderer emits <nav class="menuContents"><ul>..., NOT the SPA's
        // <div class="nested-dynamic-menu">. Styling the wrong class name leaves the
        // navigation as an unstyled bulleted list.
        Assert.Contains(".menuContents", StaticLayoutCss.Css);
        Assert.Contains("list-style:none", StaticLayoutCss.Css.Replace(" ", ""));
    }

    /// <summary>
    /// The GA4 measurement id baked into each static page must match what the SPA uses for
    /// that site (react/baseProject/configs/{prefix}-Prod.json -> analyticsTag). If they
    /// diverge, traffic for that site silently stops being recorded the moment its pages go
    /// static -- with no error anywhere, and precisely when you most want the numbers.
    /// </summary>
    [Fact]
    public void Analytics_MatchesTheReactConfigs()
    {
        var configDir = Path.Combine(RepoRoot(), "react", "baseProject", "configs");
        Assert.True(Directory.Exists(configDir), $"Config directory not found at {configDir}");

        var problems = new List<string>();

        foreach (var configPath in Directory.GetFiles(configDir, "*-Prod.json"))
        {
            var json = File.ReadAllText(configPath);

            var siteName = Regex.Match(json, @"""siteName""\s*:\s*""([^""]*)""").Groups[1].Value;
            var websiteId = Regex.Match(json, @"""websiteId""\s*:\s*(\d+)").Groups[1].Value;
            var tag = Regex.Match(json, @"""analyticsTag""\s*:\s*""([^""]*)""").Groups[1].Value;

            if (string.IsNullOrEmpty(websiteId)) continue;   // not a site config we can map

            var meta = StaticPageRenderer.GetSiteMeta(int.Parse(websiteId), siteName);

            if (meta.Analytics != tag)
            {
                problems.Add(
                    $"{Path.GetFileName(configPath)} (websiteId {websiteId}, {siteName}): " +
                    $"config has '{tag}' but StaticPageRenderer has '{meta.Analytics}'");
            }
        }

        Assert.True(problems.Count == 0,
            "Google Analytics ids disagree between the SPA config and the static renderer. " +
            "Static pages would report to the wrong property, or to none at all:\n  " +
            string.Join("\n  ", problems));
    }

    /// <summary>
    /// The on-save publish gate. Without it, editing an article on a page named "Home"
    /// writes to the public bucket ROOT index.html -- straight over the live SPA shell
    /// of a site that may not have been migrated yet, unattended and with no backup.
    /// Default-off is the whole point, so it is tested explicitly.
    /// </summary>
    public class PublishGateTests : IDisposable
    {
        private readonly string? _original =
            Environment.GetEnvironmentVariable("STATIC_PUBLISH_SITES");

        private static void Set(string? v) =>
            Environment.SetEnvironmentVariable("STATIC_PUBLISH_SITES", v);

        public void Dispose() => Set(_original);

        [Fact]
        public void Unset_PublishesNothing()
        {
            Set(null);
            foreach (var id in new[] { 1, 2, 4, 5, 6, 8 })
                Assert.False(StaticPageRenderer.IsPublishEnabled(id),
                    $"website {id} must NOT publish when STATIC_PUBLISH_SITES is unset");
        }

        [Fact]
        public void Empty_PublishesNothing()
        {
            Set("   ");
            Assert.False(StaticPageRenderer.IsPublishEnabled(1));
        }

        [Fact]
        public void SingleSite_PublishesOnlyThatSite()
        {
            Set("1");
            Assert.True(StaticPageRenderer.IsPublishEnabled(1));
            foreach (var other in new[] { 2, 4, 5, 6, 8 })
                Assert.False(StaticPageRenderer.IsPublishEnabled(other),
                    $"website {other} must not publish when only 1 is enabled");
        }

        [Fact]
        public void MultipleSites_RespectsListAndWhitespace()
        {
            Set(" 1 , 8 ");
            Assert.True(StaticPageRenderer.IsPublishEnabled(1));
            Assert.True(StaticPageRenderer.IsPublishEnabled(8));
            Assert.False(StaticPageRenderer.IsPublishEnabled(5));
        }

        [Fact]
        public void All_PublishesEveryMigratedSite()
        {
            Set("all");
            foreach (var id in new[] { 1, 2, 5, 6, 8 })
                Assert.True(StaticPageRenderer.IsPublishEnabled(id), $"website {id} should publish under 'all'");
        }

        [Theory]
        [InlineData("all")]
        [InlineData("ALL")]
        [InlineData("1,2,4,5,6,8")]
        [InlineData("4")]
        public void ExcludedSite_NeverPublishes_WhateverTheSetting(string setting)
        {
            // "all" is the normal setting now that every applicable site is migrated, so this
            // deny list is the ONLY thing standing between a content edit and a destroyed
            // site. reflectiverealizations (4) is hand-built static HTML, not the SPA:
            // publishing it would write its 2KB CMS "Home" over the live 8.5KB homepage.
            // Naming it explicitly, including in an explicit list, is the point.
            Set(setting);
            Assert.False(StaticPageRenderer.IsPublishEnabled(4),
                $"website 4 must stay excluded when STATIC_PUBLISH_SITES='{setting}'");
        }

        [Fact]
        public void Garbage_FailsClosed()
        {
            // An unparseable value must never be read as "publish everywhere".
            Set("yes,true,ldsapologetics");
            foreach (var id in new[] { 1, 2, 4, 5, 6, 8 })
                Assert.False(StaticPageRenderer.IsPublishEnabled(id));
        }
    }

    /// <summary>
    /// The GA4 measurement id lives in three places that must agree: the React config
    /// (what the SPA sends), StaticPageRenderer.SiteMetaById (what the save hook bakes
    /// into a page), and the publish-static-pages.ps1 table (what the backfill bakes in).
    ///
    /// Drift here is silent -- pages render fine, analytics just goes to the wrong
    /// property or nowhere. ldsfaithincrisis shipped "G-J6H714HFSM1" for months: the
    /// ldsapologetics id with a "1" appended, an 11-character suffix that cannot be a
    /// valid GA4 id.
    /// </summary>
    public class AnalyticsTests
    {
        private static string RepoRootPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null &&
                   !(Directory.Exists(Path.Combine(dir.FullName, "api")) &&
                     Directory.Exists(Path.Combine(dir.FullName, "react"))))
                dir = dir.Parent;
            Assert.True(dir != null, "Could not locate the repository root");
            return dir!.FullName;
        }

        /// <summary>site key -> analyticsTag, read from react/baseProject/configs/*-Prod.json.</summary>
        private static Dictionary<string, string> ReactConfigTags()
        {
            var dir = Path.Combine(RepoRootPath(), "react", "baseProject", "configs");
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.GetFiles(dir, "*-Prod.json"))
            {
                var prefix = Path.GetFileName(f).Replace("-Prod.json", "");
                var m = Regex.Match(File.ReadAllText(f), @"""analyticsTag""\s*:\s*""([^""]*)""");
                if (m.Success) result[prefix] = m.Groups[1].Value;
            }
            return result;
        }

        [Fact]
        public void Analytics_IdsAreWellFormed()
        {
            // A GA4 measurement id is "G-" followed by a 10-character alphanumeric suffix.
            foreach (var id in new[] { 1, 2, 4, 5, 6, 8 })
            {
                var tag = StaticPageRenderer.GetSiteMeta(id, "x").Analytics;
                if (string.IsNullOrEmpty(tag)) continue;   // deliberately unset is allowed
                Assert.True(Regex.IsMatch(tag, "^G-[A-Z0-9]{10}$"),
                    $"website {id} has analytics id '{tag}', which is not a valid GA4 measurement id " +
                    "(expected G- followed by exactly 10 alphanumeric characters)");
            }
        }

        [Fact]
        public void Analytics_MatchesTheReactConfigs()
        {
            var react = ReactConfigTags();

            // configPrefix differs from the site key for ldsfaithincrisis (faithInCrisis).
            var byWebsiteId = new Dictionary<int, string>
            {
                { 1, "faithInCrisis" },
                { 2, "ldsdoctrines" },
                { 5, "ldsapologetics" },
                { 6, "ldsdiscussions" },
                { 8, "cesletter" },
            };

            foreach (var kv in byWebsiteId)
            {
                if (!react.TryGetValue(kv.Value, out var reactTag)) continue;   // no config set yet
                var rendererTag = StaticPageRenderer.GetSiteMeta(kv.Key, "x").Analytics;
                Assert.True(reactTag == rendererTag,
                    $"website {kv.Key}: the React config ({kv.Value}-Prod.json) says '{reactTag}' but " +
                    $"StaticPageRenderer.SiteMetaById says '{rendererTag}'. Static pages would report to " +
                    "a different GA property than the SPA does.");
            }
        }

        [Fact]
        public void Analytics_NoTwoSitesShareAnId()
        {
            // Two sites reporting into one property silently merges their traffic.
            var seen = new Dictionary<string, int>();
            foreach (var id in new[] { 1, 2, 4, 5, 6, 8 })
            {
                var tag = StaticPageRenderer.GetSiteMeta(id, "x").Analytics;
                if (string.IsNullOrEmpty(tag)) continue;
                Assert.False(seen.ContainsKey(tag),
                    $"websites {seen.GetValueOrDefault(tag)} and {id} both use analytics id '{tag}'");
                seen[tag] = id;
            }
        }
    }

    [Theory]
    [InlineData("Standard", "layout-standard")]
    [InlineData("2, 1 Grid", "layout-2-1")]
    [InlineData("2 Grid", "layout-2")]
    [InlineData("1, 2 Grid", "layout-1-2")]
    public void Slug_LayoutClass_MatchesTheSpaTransform(string layout, string expected)
    {
        // These four are the layout values actually in use across the six sites.
        // LayoutClass mirrors CollectionContainer.jsx's processLayout().
        Assert.Equal(expected, StaticPageRenderer.LayoutClass(layout));
    }

    [Fact]
    public void LayoutClass_EveryLayoutInUse_HasAMatchingRule()
    {
        // End-to-end version of the above: the class the renderer generates for each
        // layout actually in use must have a rule in the stylesheet that ships.
        var inUse = new[] { "Standard", "2, 1 Grid", "2 Grid", "1, 2 Grid" };
        var inStatic = LayoutClassesIn(StaticLayoutCss.Css);

        foreach (var layout in inUse)
        {
            var cls = StaticPageRenderer.LayoutClass(layout).ToLowerInvariant();
            Assert.True(inStatic.Contains(cls),
                $"Layout '{layout}' generates class '{cls}', which has no rule in StaticLayoutCss.");
        }
    }
}
