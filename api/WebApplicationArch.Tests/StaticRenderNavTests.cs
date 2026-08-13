using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// The nav is baked into EVERY page of EVERY site, so BuildNav is the single highest-blast-radius
/// pure function in the renderer -- and until 2026-08-12 it was two hard-coded loops that silently
/// dropped every grandchild in sitemenu.json.
///
/// The data model has always carried arbitrary depth: an item names its parent, and nothing bounds
/// that chain. These tests pin the rendering half to that reality, and pin the failure modes a
/// hand-edited or half-written menu file can produce -- a cycle, a duplicate id, an absent parent.
/// </summary>
public class StaticRenderNavTests
{
    private const string Domain = "example.com";

    private static string Nav(string json) => StaticPageRenderer.BuildNav(json, Domain);

    /// <summary>
    /// Menu depth of a marker in the emitted markup, in the same vocabulary the renderer uses:
    /// a top-level section is 0. Counted from the enclosing &lt;ul&gt; elements, less the one
    /// &lt;nav&gt; wraps around the whole menu.
    /// </summary>
    private static int DepthOf(string html, string marker)
    {
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"'{marker}' is not in the rendered nav:\n{html}");
        var before = html.Substring(0, idx);
        return Regex.Matches(before, "<ul>").Count - Regex.Matches(before, "</ul>").Count - 1;
    }

    // ---------------------------------------------------------------- existing behaviour

    [Fact]
    public void TwoLevelMenu_StillRendersSectionAndChild()
    {
        // The shape every live site uses today. This must not change.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""Doctrine""},
            {""id"":2,""parent"":1,""text"":""Grace"",""pageId"":10,""pageName"":""Grace""}
        ]");

        Assert.Contains("<span class=\"nav-section\">Doctrine</span>", nav);
        Assert.Contains("<a href=\"https://www.example.com/grace/\">Grace</a>", nav);
    }

    [Fact]
    public void HomeLinksToTheSiteRoot_NotToASlug()
    {
        // Same rule as PublicPath and the CloudFront function. /home/ does not exist.
        var nav = Nav(@"[{""id"":1,""parent"":0,""text"":""Home"",""pageId"":1,""pageName"":""Home""}]");
        Assert.Contains("href=\"https://www.example.com/\"", nav);
        Assert.DoesNotContain("/home/", nav);
    }

    [Fact]
    public void TopLevelEmptySection_IsStillRendered()
    {
        // Empty top-level sections are deliberate on some sites. Dropping them would change
        // menus that render correctly today, so the "must go somewhere" rule starts at depth 1.
        var nav = Nav(@"[{""id"":1,""parent"":0,""droppable"":true,""text"":""Coming Soon""}]");
        Assert.Contains("Coming Soon", nav);
    }

    [Fact]
    public void ItemWithPageNameUnderData_IsStillALink()
    {
        // react-dnd-treeview stores custom fields under `data` in some menus, top level in others.
        var nav = Nav(@"[{""id"":1,""parent"":0,""text"":""Deep"",""data"":{""pageName"":""Nested-Field""}}]");
        Assert.Contains("href=\"https://www.example.com/nested-field/\"", nav);
    }

    // ---------------------------------------------------------------- the new depth

    [Fact]
    public void ThreeLevelMenu_RendersGrandchildren()
    {
        // The regression this whole change exists for: item 3 was silently dropped.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""Topics""},
            {""id"":2,""parent"":1,""droppable"":true,""text"":""Book of Mormon""},
            {""id"":3,""parent"":2,""text"":""Nephi"",""pageId"":10,""pageName"":""Nephi""}
        ]");

        Assert.Contains("Book of Mormon", nav);
        Assert.Contains("<a href=\"https://www.example.com/nephi/\">Nephi</a>", nav);

        Assert.Equal(0, DepthOf(nav, ">Topics<"));
        Assert.Equal(1, DepthOf(nav, ">Book of Mormon<"));
        Assert.Equal(2, DepthOf(nav, ">Nephi</a>"));
    }

    [Fact]
    public void MidLevelSectionWithoutAPage_IsKeptWhenItHasChildren()
    {
        // The old child filter required a pageName, so a depth-1 GROUP HEADING was dropped --
        // taking every page beneath it with it. Three-level menus are mostly these.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""Topics""},
            {""id"":2,""parent"":1,""droppable"":true,""text"":""Group Heading""},
            {""id"":3,""parent"":2,""text"":""Leaf"",""pageId"":10,""pageName"":""Leaf""}
        ]");

        Assert.Contains("<span class=\"nav-section\">Group Heading</span>", nav);
        Assert.Contains(">Leaf</a>", nav);
    }

    [Fact]
    public void SectionWithBothAPageAndChildren_LinksAndStillNests()
    {
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""text"":""Section"",""pageId"":5,""pageName"":""Section-Landing""},
            {""id"":2,""parent"":1,""text"":""Child"",""pageId"":6,""pageName"":""Child-Page""}
        ]");

        Assert.Contains("<a href=\"https://www.example.com/section-landing/\">Section</a>", nav);
        Assert.Contains("<a href=\"https://www.example.com/child-page/\">Child</a>", nav);
    }

    [Fact]
    public void PagelessChildlessItem_BelowTopLevel_IsDropped()
    {
        // An editing leftover. At depth 1+ it renders as an inert label inside a dropdown.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""Section""},
            {""id"":2,""parent"":1,""droppable"":true,""text"":""Leftover Empty Group""},
            {""id"":3,""parent"":1,""text"":""Real"",""pageId"":10,""pageName"":""Real""}
        ]");

        Assert.DoesNotContain("Leftover Empty Group", nav);
        Assert.Contains(">Real</a>", nav);
    }

    [Fact]
    public void SiblingOrderIsDocumentOrder()
    {
        // Sibling order IS array order -- there is no sort key, and drag-to-reorder in the tree
        // editor writes exactly that. Reordering here would silently reorder every site's nav.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""S""},
            {""id"":2,""parent"":1,""text"":""Zebra"",""pageId"":1,""pageName"":""Zebra""},
            {""id"":3,""parent"":1,""text"":""Apple"",""pageId"":2,""pageName"":""Apple""}
        ]");

        Assert.True(nav.IndexOf("Zebra", StringComparison.Ordinal) < nav.IndexOf("Apple", StringComparison.Ordinal),
            "Siblings must render in array order, not alphabetically:\n" + nav);
    }

    // ---------------------------------------------------------------- malformed input

    [Fact]
    public void DepthBeyondTheCap_IsTruncatedNotRendered()
    {
        var items = Enumerable.Range(0, 8).Select(i =>
            $@"{{""id"":{i + 1},""parent"":{i},""text"":""L{i}"",""pageId"":{i + 1},""pageName"":""L{i}""}}");
        var nav = Nav("[" + string.Join(",", items) + "]");

        for (int i = 0; i < StaticPageRenderer.MaxNavDepth; i++)
            Assert.Contains($">L{i}</a>", nav);

        for (int i = StaticPageRenderer.MaxNavDepth; i < 8; i++)
            Assert.DoesNotContain($">L{i}</a>", nav);
    }

    [Fact]
    public void SelfParentingItem_DoesNotRecurseForever()
    {
        // Would be an infinite recursion inside a 900 s whole-site render, i.e. a site that
        // stops updating with no obvious cause.
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""Root""},
            {""id"":2,""parent"":2,""text"":""Self"",""pageId"":9,""pageName"":""Self""},
            {""id"":3,""parent"":1,""text"":""Fine"",""pageId"":8,""pageName"":""Fine""}
        ]");

        Assert.Contains(">Fine</a>", nav);       // the healthy part of the menu still renders
        Assert.DoesNotContain(">Self</a>", nav); // unreachable from the root, so it is not nav
    }

    [Fact]
    public void MutualParentCycle_DoesNotRecurseForever()
    {
        var nav = Nav(@"[
            {""id"":1,""parent"":2,""text"":""A"",""pageId"":1,""pageName"":""A""},
            {""id"":2,""parent"":1,""text"":""B"",""pageId"":2,""pageName"":""B""},
            {""id"":3,""parent"":0,""text"":""C"",""pageId"":3,""pageName"":""C""}
        ]");

        Assert.Contains(">C</a>", nav);
    }

    [Fact]
    public void DuplicateId_RendersOnce()
    {
        var nav = Nav(@"[
            {""id"":1,""parent"":0,""droppable"":true,""text"":""S""},
            {""id"":2,""parent"":1,""text"":""Dup"",""pageId"":1,""pageName"":""Dup""},
            {""id"":2,""parent"":1,""text"":""Dup"",""pageId"":1,""pageName"":""Dup""}
        ]");

        Assert.Single(Regex.Matches(nav, ">Dup</a>"));
    }

    [Fact]
    public void ItemWithNoParentProperty_IsTopLevel()
    {
        // `it["parent"]?.ToString() ?? "0"` -- an item written without the field is a section.
        var nav = Nav(@"[{""id"":1,""text"":""NoParentField"",""pageId"":3,""pageName"":""Orphan""}]");
        Assert.Contains("NoParentField", nav);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("not json at all")]
    [InlineData("{\"not\":\"an array\"}")]
    public void UnusableMenu_ReturnsEmptyRatherThanThrowing(string json)
    {
        // The nav is an enhancement; a broken menu file must not fail the page render.
        Assert.Equal("", Nav(json));
    }

    [Fact]
    public void LabelsAreHtmlEncoded()
    {
        var nav = Nav(@"[{""id"":1,""parent"":0,""text"":""Faith & <Doubt>"",""pageId"":1,""pageName"":""X""}]");
        Assert.DoesNotContain("<Doubt>", nav);
        Assert.Contains("&amp;", nav);
    }

    // ---------------------------------------------------------------- drift detection

    /// <summary>
    /// Walk up from the test binary to the repo root -- both an api\ and a react\ directory.
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

    private static string PublishScript() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "scripts", "publish-static-pages.ps1"));

    [Fact]
    public void PowerShellRenderer_AlsoRecurses()
    {
        // BuildNav (C#, the save hook) and Build-MenuNav (PowerShell, the bulk backfill) render
        // into the same bucket. When they disagree, a page's nav depends on which one last
        // touched it -- this rollout has already produced four bugs of that exact shape.
        var ps = PublishScript();

        Assert.Contains("function Build-MenuLevel", ps);
        Assert.Contains("Build-MenuLevel -Menu $Menu -ParentId $id", ps);   // the recursive call
    }

    [Fact]
    public void PowerShellRenderer_UsesTheSameDepthCap()
    {
        var m = Regex.Match(PublishScript(), @"\$script:MaxNavDepth\s*=\s*(\d+)");
        Assert.True(m.Success, "publish-static-pages.ps1 no longer defines $script:MaxNavDepth");
        Assert.Equal(StaticPageRenderer.MaxNavDepth, int.Parse(m.Groups[1].Value));
    }

    [Fact]
    public void McpTools_RefuseWhatTheRendererWouldTruncate()
    {
        // The MCP is the only writer that can refuse a bad menu BEFORE it reaches S3. If its cap
        // drifts above the renderer's, it happily creates items that render nowhere.
        var js = File.ReadAllText(Path.Combine(RepoRoot(), "api", "mcp", "src", "tools", "navigation.js"));
        var m = Regex.Match(js, @"MAX_MENU_DEPTH\s*=\s*(\d+)");
        Assert.True(m.Success, "navigation.js no longer defines MAX_MENU_DEPTH");
        Assert.Equal(StaticPageRenderer.MaxNavDepth, int.Parse(m.Groups[1].Value));
    }

    [Fact]
    public void LayoutCss_StylesSubmenusAtEveryDepth()
    {
        // "> ul > li > ul" styles exactly one dropdown level: deeper items would be emitted into
        // the HTML and then be invisible forever, which looks like the renderer dropping them.
        var css = StaticLayoutCss.Css;
        Assert.Contains(".menuContents ul ul ul", css);
        Assert.Contains(".menuContents li:hover > ul", css);
    }
}
