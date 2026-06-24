using System.Text.Json;
using System.Text.Json.Serialization;

namespace FacebookPoster;

/// <summary>A group you post to. Name is needed because Facebook's "post to more
/// groups" picker searches by name; Url is where the primary post is composed.</summary>
public sealed class Group
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

/// <summary>One article to promote, cross-posted to the plan's shared group list.</summary>
public sealed class Article
{
    /// <summary>Stable id / slug (also used in screenshot names).</summary>
    public string Id { get; set; } = "";

    /// <summary>Optional hand-written override. If null/empty, the post text is
    /// fetched from the article page (Link) at run time.</summary>
    public string? Message { get; set; }

    /// <summary>The article URL. Drives Facebook's preview card AND is the source the
    /// post text is fetched from when Message is omitted.</summary>
    public string? Link { get; set; }

    /// <summary>Optional absolute path to an image to attach.</summary>
    public string? ImagePath { get; set; }

    /// <summary>Keys (group Url, or Name if no Url) of every group this article has been
    /// posted to. Written one group at a time as you post, so an interrupted run resumes
    /// where it left off and already-posted groups are skipped on the next run.</summary>
    public List<string> PostedGroups { get; set; } = new();

    /// <summary>Set once this article has been posted to ALL groups.</summary>
    public DateTime? PostedAtUtc { get; set; }

    [JsonIgnore]
    public bool IsComplete => PostedAtUtc is not null;
}

/// <summary>
/// The whole posting plan: the shared groups, Facebook's per-post group cap, and the
/// ordered list of articles. posts.json is the deterministic source of truth.
/// </summary>
public sealed class Plan
{
    /// <summary>You always post to these same groups, so they live here once.</summary>
    public List<Group> Groups { get; set; } = new();

    /// <summary>
    /// How many groups Facebook lets you hook onto a single post (~8). One run posts
    /// the next article as ceil(Groups / GroupsPerPost) posts, each hooking this many.
    /// </summary>
    public int GroupsPerPost { get; set; } = 8;

    /// <summary>Articles in the order you want them posted (one per scheduled run).</summary>
    public List<Article> Articles { get; set; } = new();
}

public static class PlanStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Plan Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Plan file not found: {path}");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Plan>(json, JsonOpts) ?? new Plan();
    }

    public static void Save(string path, Plan plan)
    {
        var json = JsonSerializer.Serialize(plan, JsonOpts);
        // Atomic-ish write so a crash mid-write can't corrupt the plan.
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, path, overwrite: true);
        File.Delete(tmp);
    }
}
