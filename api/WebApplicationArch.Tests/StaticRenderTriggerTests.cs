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

    // ------------------------------------------------------------ site-wide asset parsing
    //
    // sitemenu.json, header.html and site-meta.json are baked into EVERY page at render time,
    // so writing one has to re-render the whole site. Misparse in one direction and those edits
    // silently stop propagating; in the other, a stray upload kicks off a 471-page render.
    //
    // header.html is the reason the allowlist is explicit: the article notification filters on
    // suffix ".html", so every loose .html in a site folder already reaches this Lambda.

    [Theory]
    [InlineData("public/websites/cesletter.info/sitemenu.json", "cesletter.info", "sitemenu.json")]
    [InlineData("public/websites/ldsdoctrines.com/sitemenu.json", "ldsdoctrines.com", "sitemenu.json")]
    [InlineData("public/websites/ldsapologetics.com/header.html", "ldsapologetics.com", "header.html")]
    [InlineData("public/websites/ldsfaithincrisis.com/site-meta.json", "ldsfaithincrisis.com", "site-meta.json")]
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
    [InlineData("public/websites/cesletter.info/old-header.html")]
    [InlineData("public/websites/cesletter.info/header.html.bak")]
    // Nested deeper, or shallower, than the real location.
    [InlineData("public/websites/cesletter.info/articles/sitemenu.json")]
    [InlineData("public/websites/cesletter.info/sub/header.html")]
    [InlineData("public/websites/sitemenu.json")]
    // Near-misses on the filename.
    [InlineData("public/websites/cesletter.info/sitemenu.json.bak")]
    [InlineData("public/websites/cesletter.info/my-sitemenu.json")]
    // Malformed / unrelated.
    [InlineData("public/assets/cesletter.info/sitemenu.json")]
    [InlineData("sitemenu.json")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_NonSiteAssetKeys(string key)
    {
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey(key, out _, out _), $"should NOT have parsed '{key}'");
    }

    [Theory]
    [InlineData("public/websites/cesletter.info/sitemenu.json")]
    [InlineData("public/websites/cesletter.info/header.html")]
    [InlineData("public/websites/cesletter.info/site-meta.json")]
    public void SiteAssetAndArticleParsers_AreMutuallyExclusive(string siteAsset)
    {
        // The handler tries the site-asset parser first and falls through to the article one.
        // If a key ever satisfied both, a header edit would also be treated as an article write.
        Assert.True(ApiStaticRenderFunctions.TryParseSiteAssetKey(siteAsset, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseArticleKey(siteAsset, out _, out _));

        const string article = "public/websites/cesletter.info/articles/bom-dna.html";
        Assert.True(ApiStaticRenderFunctions.TryParseArticleKey(article, out _, out _));
        Assert.False(ApiStaticRenderFunctions.TryParseSiteAssetKey(article, out _, out _));
    }
}
