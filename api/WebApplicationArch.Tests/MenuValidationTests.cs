using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApplicationArch.content;
using Xunit;

namespace WebApplicationArch.Tests;

/// <summary>
/// Guards on the whole-menu replace endpoint.
///
/// The item-at-a-time menu tools can check one edit against a menu that is otherwise known
/// good. A whole-file replace has no such luxury: it has to prove the RESULT is coherent by
/// itself, because the nav is baked into every page of the site. A bad menu is not a bad
/// page, it is a bad site -- and none of these failures raise an error anywhere. They just
/// quietly remove things from the navigation.
/// </summary>
public class MenuValidationTests
{
    private static List<JObject> Menu(string json) =>
        JsonConvert.DeserializeObject<List<JObject>>(json)!;

    private static readonly HashSet<string> Pages =
        new(System.StringComparer.OrdinalIgnoreCase) { "Real-Page", "Other-Page" };

    private static List<string> Validate(string json, HashSet<string>? pages = null) =>
        ApiMenuFunctions.ValidateMenu(Menu(json), pages ?? Pages);

    [Fact]
    public void AValidMenu_PassesCleanly()
    {
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Section""},
            {""id"":2,""parent"":1,""text"":""Group""},
            {""id"":3,""parent"":2,""text"":""Leaf"",""pageId"":10,""pageName"":""Real-Page""}
        ]");
        Assert.Empty(errors);
    }

    [Fact]
    public void DuplicateIds_AreRejected()
    {
        // Two items with one id means one of them is unaddressable -- a later edit would hit
        // whichever the parser happened to return first.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""A""},
            {""id"":1,""parent"":0,""text"":""B""}
        ]");
        Assert.Contains(errors, e => e.Contains("Duplicate menu item id 1"));
    }

    [Fact]
    public void MissingParent_IsRejected_BecauseTheBranchWouldVanish()
    {
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Section""},
            {""id"":2,""parent"":999,""text"":""Orphan"",""pageId"":10,""pageName"":""Real-Page""}
        ]");
        Assert.Contains(errors, e => e.Contains("names parent 999"));
    }

    [Fact]
    public void ACycle_IsRejected()
    {
        // A cycle does not throw and does not corrupt the file. It silently detaches the branch
        // from the root, so every page under it disappears from every page's navigation at once.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Fine"",""pageId"":10,""pageName"":""Real-Page""},
            {""id"":2,""parent"":3,""text"":""A""},
            {""id"":3,""parent"":2,""text"":""B""}
        ]");
        Assert.Contains(errors, e => e.Contains("not reachable from the top level"));
    }

    [Fact]
    public void SelfParenting_IsRejected()
    {
        // ldsapologetics has exactly this today: item 20 ("LDS Doctrines") is its own parent.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Section""},
            {""id"":20,""parent"":20,""text"":""LDS Doctrines""}
        ]");
        Assert.Contains(errors, e => e.Contains("20") && e.Contains("not reachable"));
    }

    [Fact]
    public void DeeperThanTheRendererDraws_IsRejected()
    {
        // The renderer TRUNCATES; this REFUSES. That difference is the point -- a truncated item
        // is a real page, linked from sitemenu.json, appearing in the nav of no page at all.
        var items = Enumerable.Range(0, StaticPageRenderer.MaxNavDepth + 2)
            .Select(i => $@"{{""id"":{i + 1},""parent"":{i},""text"":""L{i}"",""pageId"":1,""pageName"":""Real-Page""}}");
        var errors = Validate("[" + string.Join(",", items) + "]");

        Assert.Contains(errors, e => e.Contains($"only {StaticPageRenderer.MaxNavDepth} levels"));
    }

    [Fact]
    public void ExactlyAtTheDepthLimit_IsAllowed()
    {
        var items = Enumerable.Range(0, StaticPageRenderer.MaxNavDepth)
            .Select(i => $@"{{""id"":{i + 1},""parent"":{i},""text"":""L{i}"",""pageId"":1,""pageName"":""Real-Page""}}");
        var errors = Validate("[" + string.Join(",", items) + "]");

        Assert.DoesNotContain(errors, e => e.Contains("levels"));
    }

    [Fact]
    public void ALinkToAPageNotOnThisSite_IsRejected()
    {
        // This is the guard that catches a menu aimed at the wrong website. Menu items are
        // addressed positionally and carry no owner field, so the linked PAGE is the only
        // thing that can reveal the mistake.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Leaf"",""pageId"":10,""pageName"":""Page-On-Another-Site""}
        ]");
        Assert.Contains(errors, e => e.Contains("not a published page on this site"));
    }

    [Fact]
    public void PageNameNestedUnderData_IsResolvedToo()
    {
        // react-dnd-treeview stores custom fields under `data` in some menus, top level in others.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Leaf"",""data"":{""pageName"":""Real-Page""}}
        ]");
        Assert.Empty(errors);
    }

    [Fact]
    public void BlankText_IsRejected()
    {
        var errors = Validate(@"[{""id"":1,""parent"":0,""text"":""   "",""pageId"":10,""pageName"":""Real-Page""}]");
        Assert.Contains(errors, e => e.Contains("has no text"));
    }

    [Fact]
    public void MissingId_IsRejected()
    {
        var errors = Validate(@"[{""parent"":0,""text"":""No id""}]");
        Assert.Contains(errors, e => e.Contains("no id"));
    }

    [Fact]
    public void SectionHeadingsWithNoPage_AreFine()
    {
        // A pageless heading is normal -- it is what a nesting level IS.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""Section""},
            {""id"":2,""parent"":1,""text"":""Subsection""},
            {""id"":3,""parent"":2,""text"":""Leaf"",""pageId"":1,""pageName"":""Real-Page""}
        ]");
        Assert.Empty(errors);
    }

    [Fact]
    public void ItemWithNoParentProperty_CountsAsTopLevel()
    {
        // Matches BuildNav's `it["parent"]?.ToString() ?? "0"`. Treating it as unreachable here
        // would refuse a menu that renders perfectly well.
        var errors = Validate(@"[{""id"":1,""text"":""Top"",""pageId"":1,""pageName"":""Real-Page""}]");
        Assert.Empty(errors);
    }

    [Fact]
    public void EveryProblemIsReported_NotJustTheFirst()
    {
        // A caller fixing one error at a time through a 160-item menu is a bad afternoon.
        var errors = Validate(@"[
            {""id"":1,""parent"":0,""text"":""""},
            {""id"":1,""parent"":0,""text"":""dup""},
            {""id"":3,""parent"":404,""text"":""orphan""}
        ]");
        Assert.True(errors.Count >= 3, "expected several problems, got: " + string.Join(" | ", errors));
    }
}
