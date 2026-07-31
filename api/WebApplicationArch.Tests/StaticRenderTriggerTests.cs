using System;
using WebApplicationArch;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// The S3 trigger's key parser decides, for every object written anywhere in the content
/// bucket, whether it is an article and which site it belongs to. Get it wrong in one
/// direction and edits stop publishing; wrong in the other and unrelated uploads kick off
/// renders. Neither surfaces as an error, so it is tested directly.
/// </summary>
public class StaticRenderTriggerTests
{
    [Theory]
    // The shape the admin actually writes (FileProcessing.saveFileData).
    [InlineData("public/websites/ldsfaithincrisis.com/articles/Home_Hook.html", "ldsfaithincrisis.com", "Home_Hook.html")]
    [InlineData("public/websites/cesletter.info/articles/bom-dna.html", "cesletter.info", "bom-dna.html")]
    [InlineData("public/websites/ldsapologetics.com/articles/Temple-And-Masonry.html", "ldsapologetics.com", "Temple-And-Masonry.html")]
    // A GUID-named article, which is what createPageWithDefaultArticle generates.
    [InlineData("public/websites/ldsdoctrines.com/articles/7f3a1c2e-9b44-4d1a-8e55-0a1b2c3d4e5f.html", "ldsdoctrines.com", "7f3a1c2e-9b44-4d1a-8e55-0a1b2c3d4e5f.html")]
    // Nested paths under articles/ stay intact.
    [InlineData("public/websites/cesletter.info/articles/sub/folder/x.html", "cesletter.info", "sub/folder/x.html")]
    public void Parses_RealArticleKeys(string key, string expectedDomain, string expectedPath)
    {
        Assert.True(ApiStaticRenderFunctions.TryParseArticleKey(key, out var domain, out var path), $"should have parsed '{key}'");
        Assert.Equal(expectedDomain, domain);
        Assert.Equal(expectedPath, path);
    }

    [Theory]
    // S3 percent-encodes event keys, and encodes spaces as '+'.
    [InlineData("public/websites/cesletter.info/articles/My+Article.html", "cesletter.info", "My Article.html")]
    [InlineData("public/websites/cesletter.info/articles/My%20Article.html", "cesletter.info", "My Article.html")]
    [InlineData("public/websites/cesletter.info/articles/Joseph%27s%20Polygamy.html", "cesletter.info", "Joseph's Polygamy.html")]
    public void Decodes_EncodedKeys(string key, string expectedDomain, string expectedPath)
    {
        Assert.True(ApiStaticRenderFunctions.TryParseArticleKey(key, out var domain, out var path));
        Assert.Equal(expectedDomain, domain);
        Assert.Equal(expectedPath, path);
    }

    [Theory]
    // Site assets, themes and images live in the same bucket -- they must NOT trigger a render.
    [InlineData("public/assets/cesletter.info/themes/theme.css")]
    [InlineData("public/assets/ldsdoctrines.com/images/Apostasy.jpg")]
    [InlineData("public/websites/cesletter.info/header.html")]
    [InlineData("public/websites/cesletter.info/sitemenu.json")]
    // Console-created folder markers and directory placeholders are not content.
    [InlineData("public/websites/ldsfaithincrisis.com/articles/articles_$folder$")]
    [InlineData("public/websites/ldsfaithincrisis.com/articles/")]
    // Malformed / unrelated keys.
    [InlineData("public/websites/cesletter.info")]
    [InlineData("articles/x.html")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_NonArticleKeys(string key)
    {
        Assert.False(ApiStaticRenderFunctions.TryParseArticleKey(key, out _, out _), $"should NOT have parsed '{key}'");
    }

    [Fact]
    public void Rejects_TheStaticOutputItself()
    {
        // Guard against a future misconfiguration pointing the notification at the public
        // bucket: rendering a page would then trigger another render, forever.
        Assert.False(ApiStaticRenderFunctions.TryParseArticleKey("index.html", out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseArticleKey("about-us/index.html", out _, out _));
    }

    // ---------------------------------------------------------------- sitewide key parsing
    //
    // The menu, header, site-meta and theme are all baked into every page's HTML at render
    // time, so a write to any of them has to re-render the WHOLE site. Misparse in one
    // direction and those edits silently stop propagating; in the other, an unrelated upload
    // kicks off a 471-page render.
    //
    // header.html is the reason the allowlist is explicit: the article notification filters on
    // suffix ".html", so every loose .html in a site folder already reaches this Lambda.

    [Theory]
    // The nav.
    [InlineData("public/websites/cesletter.info/sitemenu.json", "cesletter.info", "sitemenu.json")]
    [InlineData("public/websites/ldsdoctrines.com/sitemenu.json", "ldsdoctrines.com", "sitemenu.json")]
    [InlineData("public/websites/ldsfaithincrisis.com/sitemenu.json", "ldsfaithincrisis.com", "sitemenu.json")]
    // The header banner -- baked in exactly like the nav, so a header edit is a whole-site
    // render too. This was previously asserted NOT to parse, which meant header changes
    // reached no already-rendered page.
    [InlineData("public/websites/ldsapologetics.com/header.html", "ldsapologetics.com", "header.html")]
    [InlineData("public/websites/ldsdoctrines.com/header.html", "ldsdoctrines.com", "header.html")]
    // Public title + GA measurement id, both written into every page's <head>.
    [InlineData("public/websites/ldsfaithincrisis.com/site-meta.json", "ldsfaithincrisis.com", "site-meta.json")]
    [InlineData("public/websites/cesletter.info/site-meta.json", "cesletter.info", "site-meta.json")]
    // Casing of the filename is not guaranteed across writers.
    [InlineData("public/websites/cesletter.info/SiteMenu.json", "cesletter.info", "SiteMenu.json")]
    [InlineData("public/websites/cesletter.info/Header.HTML", "cesletter.info", "Header.HTML")]
    public void Parses_SiteAssetKeys(string key, string expectedDomain, string expectedAsset)
    {
        Assert.True(ApiStaticRenderFunctions.TryParseSiteAssetKey(key, out var domain, out var asset), $"should have parsed '{key}'");
        Assert.Equal(expectedDomain, domain);
        Assert.Equal(expectedAsset, asset);
    }

    [Theory]
    // Articles are a per-page render, never a whole-site one.
    [InlineData("public/websites/cesletter.info/articles/bom-dna.html")]
    // A stray .html in the site folder reaches this Lambda via the article filter. It must NOT
    // be mistaken for a site-wide asset and trigger a full re-render.
    [InlineData("public/websites/cesletter.info/scratch.html")]
    [InlineData("public/websites/cesletter.info/header.html.bak")]
    // An asset nested deeper, or shallower, than the real location.
    [InlineData("public/websites/cesletter.info/articles/sitemenu.json")]
    [InlineData("public/websites/cesletter.info/articles/header.html")]
    [InlineData("public/websites/cesletter.info/sub/header.html")]
    [InlineData("public/websites/sitemenu.json")]
    // Near-misses on the filename.
    [InlineData("public/websites/cesletter.info/sitemenu.json.bak")]
    [InlineData("public/websites/cesletter.info/my-sitemenu.json")]
    [InlineData("public/websites/cesletter.info/header.htm")]
    [InlineData("public/websites/cesletter.info/old-header.html")]
    // Wrong prefix -- theme.css lives here and has its own parser, but nothing else does.
    [InlineData("public/assets/cesletter.info/sitemenu.json")]
    // Malformed / unrelated.
    [InlineData("sitemenu.json")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_NonSiteAssetKeys(string key)
    {
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey(key, out _, out _), $"should NOT have parsed '{key}'");
    }

    [Theory]
    [InlineData("public/assets/cesletter.info/themes/theme.css", "cesletter.info")]
    [InlineData("public/assets/ldsdoctrines.com/themes/theme.css", "ldsdoctrines.com")]
    [InlineData("public/assets/cesletter.info/themes/Theme.CSS", "cesletter.info")]
    public void Parses_ThemeKeys(string key, string expectedDomain)
    {
        Assert.True(ApiStaticRenderFunctions.TryParseThemeKey(key, out var domain, out var asset), $"should have parsed '{key}'");
        Assert.Equal(expectedDomain, domain);
        // The render log names the file that caused the re-render, so the parser has to report it.
        Assert.Equal("theme.css", asset, ignoreCase: true);
    }

    [Theory]
    // Right file, wrong place.
    [InlineData("public/websites/cesletter.info/themes/theme.css")]
    [InlineData("public/assets/cesletter.info/theme.css")]
    [InlineData("public/assets/cesletter.info/themes/nested/theme.css")]
    // Other stylesheets in the same folder are not inlined and must not trigger anything.
    [InlineData("public/assets/cesletter.info/themes/theme.css.map")]
    [InlineData("public/assets/cesletter.info/themes/custom.css")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_NonThemeKeys(string key)
    {
        Assert.False(ApiStaticRenderFunctions.TryParseThemeKey(key, out _, out _), $"should NOT have parsed '{key}'");
    }

    [Fact]
    public void SiteWideAndArticleParsers_AreMutuallyExclusive()
    {
        // The handler tries the sitewide parsers first and falls through to the article one. If
        // a key ever satisfied both, a sitewide write would also be treated as an article write
        // -- and, worse, header.html is reached by the SAME .html notification as articles.
        const string article = "public/websites/cesletter.info/articles/bom-dna.html";
        string[] siteWide =
        {
            "public/websites/cesletter.info/sitemenu.json",
            "public/websites/cesletter.info/header.html",
            "public/websites/cesletter.info/site-meta.json"
        };

        foreach (var key in siteWide)
        {
            Assert.True(ApiStaticRenderFunctions.TryParseSiteAssetKey(key, out _, out _), key);
            Assert.False(ApiStaticRenderFunctions.TryParseArticleKey(key, out _, out _), key);
            Assert.False(ApiStaticRenderFunctions.TryParseThemeKey(key, out _, out _), key);
        }

        Assert.True(ApiStaticRenderFunctions.TryParseArticleKey(article, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey(article, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseThemeKey(article, out _, out _));

        const string theme = "public/assets/cesletter.info/themes/theme.css";
        Assert.True(ApiStaticRenderFunctions.TryParseThemeKey(theme, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey(theme, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseArticleKey(theme, out _, out _));
    }

    [Fact]
    public void ThemeParser_RejectsTheStaticOutputItself()
    {
        // Same guard as Rejects_TheStaticOutputItself, for the theme notification: nothing the
        // renderer writes to the public bucket may ever look like a trigger.
        Assert.False(ApiStaticRenderFunctions.TryParseThemeKey("index.html", out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseThemeKey("about-us/index.html", out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey("index.html", out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey("about-us/index.html", out _, out _));
    }
}
