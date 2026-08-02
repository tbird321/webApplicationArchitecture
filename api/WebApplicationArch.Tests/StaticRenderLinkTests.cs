using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// The admin editor stripped the leading slash off in-body links for a long time, turning an
/// authored "/some-page/" into "some-page/" or "../../some-page/". Those resolve underneath the
/// current article and 404, and nothing surfaces it -- the nav is rebuilt from sitemenu.json on
/// every render, so only article bodies rot.
///
/// The render now repairs them. The risk in a repair like this is over-reach: rewriting an
/// external URL would break outbound links across every site at once, which is far worse than
/// the bug being fixed. The "leaves alone" cases below are the important half of this file.
/// </summary>
public class StaticRenderLinkTests
{
    [Theory]
    // The exact damage observed on ldsapologetics Imputed-Righteousness after an admin edit.
    [InlineData("<a href=\"sola-fide-false/\">x</a>", "<a href=\"/sola-fide-false/\">x</a>")]
    // The form TinyMCE actually produces when the editing URL is deeper than the target.
    [InlineData("<a href=\"../../sola-fide-false/\">x</a>", "<a href=\"/sola-fide-false/\">x</a>")]
    [InlineData("<a href=\"./james-2-faith-works/\">x</a>", "<a href=\"/james-2-faith-works/\">x</a>")]
    // No trailing slash, and attributes on either side of href.
    [InlineData("<a class=\"c\" href=\"horses\" title=\"t\">x</a>", "<a class=\"c\" href=\"/horses\" title=\"t\">x</a>")]
    // Single-quoted attributes keep their quoting style.
    [InlineData("<a href='carnal-christian/'>x</a>", "<a href='/carnal-christian/'>x</a>")]
    // Casing and whitespace around the '=' are both legal HTML.
    [InlineData("<A HREF = \"born-again/\">x</A>", "<A HREF = \"/born-again/\">x</A>")]
    public void Roots_StrippedInternalLinks(string input, string expected)
        => Assert.Equal(expected, StaticPageRenderer.NormalizeInternalLinks(input));

    [Theory]
    // Every scripture link on every site is absolute and external. Breaking these would be
    // catastrophic and silent, so they are pinned hard.
    [InlineData("<a href=\"https://www.churchofjesuschrist.org/study/scriptures/nt/rom/2?lang=eng&amp;id=13#13\">x</a>")]
    [InlineData("<a href=\"http://example.org/path/\">x</a>")]
    // Protocol-relative is external.
    [InlineData("<a href=\"//cdn.example.org/asset.js\">x</a>")]
    // Non-http schemes must survive untouched.
    [InlineData("<a href=\"mailto:someone@example.org\">x</a>")]
    [InlineData("<a href=\"tel:+15555551212\">x</a>")]
    [InlineData("<a href=\"data:text/plain;base64,SGVsbG8=\">x</a>")]
    [InlineData("<a href=\"javascript:void(0)\">x</a>")]
    // Already correct -- must be a no-op, not a double slash.
    [InlineData("<a href=\"/sola-fide-false/\">x</a>")]
    [InlineData("<a href=\"/\">home</a>")]
    // Fragments and query-only links target the current page.
    [InlineData("<a href=\"#the-verdict\">x</a>")]
    [InlineData("<a href=\"?page=Found-Spirit\">x</a>")]
    // Degenerate values are left as-is rather than being invented into a path.
    [InlineData("<a href=\"\">x</a>")]
    [InlineData("<a name=\"anchor\">x</a>")]
    // Not an anchor tag at all.
    [InlineData("<link href=\"theme.css\" rel=\"stylesheet\">")]
    [InlineData("<p>no links here at all</p>")]
    public void Leaves_EverythingElseAlone(string input)
        => Assert.Equal(input, StaticPageRenderer.NormalizeInternalLinks(input));

    [Fact]
    public void Repairs_OnlyTheBrokenLink_InMixedContent()
    {
        // The realistic case: a "See also" paragraph where the internal links were mangled by the
        // editor and the scripture link beside them was not.
        const string input =
            "<p>See <a href=\"sola-fide-false/\" style=\"color:#377dff\">Sola Fide</a> and " +
            "<a href=\"https://www.churchofjesuschrist.org/study/scriptures/nt/james/2?lang=eng&amp;id=24#24\" " +
            "target=\"_blank\" rel=\"noopener\" style=\"color:#377dff\">James 2:24</a>.</p>";

        const string expected =
            "<p>See <a href=\"/sola-fide-false/\" style=\"color:#377dff\">Sola Fide</a> and " +
            "<a href=\"https://www.churchofjesuschrist.org/study/scriptures/nt/james/2?lang=eng&amp;id=24#24\" " +
            "target=\"_blank\" rel=\"noopener\" style=\"color:#377dff\">James 2:24</a>.</p>";

        Assert.Equal(expected, StaticPageRenderer.NormalizeInternalLinks(input));
    }

    [Fact]
    public void Is_Idempotent()
    {
        const string input = "<a href=\"sola-fide-false/\">x</a> <a href=\"https://example.org/\">y</a>";
        var once = StaticPageRenderer.NormalizeInternalLinks(input);
        var twice = StaticPageRenderer.NormalizeInternalLinks(once);
        Assert.Equal(once, twice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Handles_NullAndEmpty(string input)
        => Assert.Equal(input, StaticPageRenderer.NormalizeInternalLinks(input));
}
