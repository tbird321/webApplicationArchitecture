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
}
