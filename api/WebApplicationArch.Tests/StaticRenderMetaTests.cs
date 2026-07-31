using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySQLConnector.Models;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// The &lt;title&gt; and meta description are the only things most people ever see of a page --
/// they are the search result. They are also assembled from article HTML written in the house
/// style (CLAUDE.md), which is full of &amp;mdash; / &amp;ldquo; / &amp;rdquo; entities. Stripping
/// tags without resolving those entities double-escapes them, and the live site showed titles
/// containing a visible "&amp;mdash;" for months. Guarded here because nothing else would catch it.
/// </summary>
public class StaticRenderMetaTests
{
    private static readonly StaticPageRenderer Renderer = new StaticPageRenderer("www-websitecontent", "us-west-2");
    private static readonly SiteMeta Site =
        new SiteMeta("ldsapologetics.com", "LDS Apologetics", "G-TEST");

    private static string Render(string pageName, string pageDesc, string bodyHtml) =>
        Renderer.BuildDocument(
            new PageModel { name = pageName, description = pageDesc, articles = new List<ArticleModel>() },
            Site, bodyHtml, "", "", "");

    private static string Tag(string html, string pattern) =>
        Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value;

    private static string Title(string html) => Tag(html, @"<title>(.*?)</title>");
    private static string Desc(string html) => Tag(html, @"<meta name=""description"" content=""([^""]*)""");

    [Theory]
    [InlineData("&mdash;", "—")]
    [InlineData("&ldquo;", "“")]
    [InlineData("&rdquo;", "”")]
    [InlineData("&ndash;", "–")]
    [InlineData("&amp;", "&amp;")]   // a real ampersand must still end up escaped exactly once
    public void Title_ResolvesEntities_WithoutDoubleEscaping(string entity, string expectedInOutput)
    {
        var html = Render("Some-Page", "d", $"<h1>Alpha {entity} Beta</h1>");
        var title = Title(html);

        Assert.Contains(expectedInOutput, title);
        // The bug: "&mdash;" surviving tag-stripping and then being escaped to "&amp;mdash;".
        Assert.DoesNotContain("&amp;mdash;", title);
        Assert.DoesNotContain("&amp;ldquo;", title);
        Assert.DoesNotContain("&amp;rdquo;", title);
        Assert.DoesNotContain("&amp;ndash;", title);
    }

    [Fact]
    public void Description_ResolvesEntities_FromTheCmsDescription()
    {
        var html = Render("Some-Page", "Joseph Smith &mdash; the &ldquo;translation&rdquo; question", "<p>body</p>");
        var desc = Desc(html);

        Assert.Contains("—", desc);
        Assert.DoesNotContain("&amp;mdash;", desc);
        Assert.DoesNotContain("&amp;ldquo;", desc);
    }

    [Fact]
    public void Description_ResolvesEntities_FromTheFallbackParagraph()
    {
        // No CMS description -- the renderer falls back to the first <p> of the article.
        var html = Render("Some-Page", "", "<h1>T</h1><p>The <em>endowment</em> &mdash; in form, not substance.</p>");
        var desc = Desc(html);

        Assert.Contains("—", desc);
        Assert.DoesNotContain("<em>", desc);        // tags stripped
        Assert.DoesNotContain("&amp;mdash;", desc);
    }

    [Fact]
    public void Description_TruncatesOnAWordBoundary()
    {
        var longDesc = string.Join(" ", new string('x', 12), new string('y', 12));
        longDesc = string.Concat(System.Linq.Enumerable.Repeat(longDesc + " ", 20)); // well over 160
        var desc = Desc(Render("Some-Page", longDesc, "<p>b</p>"));

        Assert.True(desc.Length <= 160, $"description was {desc.Length} chars");
        Assert.EndsWith("...", desc);
        // The live symptom was a description ending mid-word, e.g. "...f...".
        var beforeEllipsis = desc.Substring(0, desc.Length - 3);
        Assert.False(beforeEllipsis.EndsWith(" "), "should not leave a trailing space before the ellipsis");
        Assert.DoesNotContain("xy", beforeEllipsis.Substring(beforeEllipsis.Length - 1)); // sanity
    }

    [Fact]
    public void Description_CountsDecodedCharactersAgainstTheBudget()
    {
        // 40 em dashes as entities = 280 raw chars but only 40 real ones, so this must NOT truncate.
        var d = string.Concat(System.Linq.Enumerable.Repeat("&mdash;", 40));
        var desc = Desc(Render("Some-Page", d, "<p>b</p>"));

        Assert.DoesNotContain("...", desc);
        Assert.Equal(40, desc.Length);
    }

    [Fact]
    public void Title_PrefersH1_ThenPageName()
    {
        Assert.StartsWith("Real Heading", Title(Render("Some-Page", "d", "<h1>Real Heading</h1>")));
        // No h1 -> page name with hyphens humanised.
        Assert.StartsWith("Some Page", Title(Render("Some-Page", "d", "<p>no heading</p>")));
    }
}
