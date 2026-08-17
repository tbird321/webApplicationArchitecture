using System.Text.Json;
using System.Text.Json.Serialization;

namespace FacebookPoster;

/// <summary>
/// Runtime configuration, loaded from appsettings.json next to the working directory.
/// All relative paths are resolved against the current working directory.
/// </summary>
public sealed class Config
{
    /// <summary>Full path to chrome.exe used when the launcher has to start Chrome itself.</summary>
    public string ChromePath { get; set; } = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    /// <summary>CDP remote-debugging port to attach to / launch with.</summary>
    public int DebugPort { get; set; } = 9222;

    /// <summary>Dedicated Chrome profile directory. Log into Facebook here once; the session persists.</summary>
    public string UserDataDir { get; set; } = @".\.fb-profile";

    /// <summary>If true, never launch Chrome — only attach to an instance you started yourself with the debug port.</summary>
    public bool AttachOnly { get; set; } = false;

    public int MinDelayMs { get; set; } = 4000;
    public int MaxDelayMs { get; set; } = 12000;

    /// <summary>A deliberate, watchable pause before each group's post so you can see what
    /// it's doing and to keep the pacing human. Set to 0 to disable.</summary>
    public int PrePostDelayMs { get; set; } = 3000;

    /// <summary>How many groups a single run will post to before stopping, so an article is
    /// spread over several sittings instead of hitting every group at once. The run posts the
    /// next this-many un-posted groups and exits; rerun to take the following set. Progress is
    /// recorded per group, so nothing is repeated. 0 means no limit; --max N overrides it.</summary>
    public int MaxGroupsPerRun { get; set; } = 10;

    /// <summary>Stop the run after this many groups in a row fail to give a usable composer.
    /// One such group is its own problem (admins-only posting, membership lapsed) and is just
    /// skipped; several in a row means the account is restricted, and continuing makes it
    /// worse. Facebook's explicit rate-limit notice stops the run immediately regardless.</summary>
    public int StopAfterConsecutiveFailures { get; set; } = 3;

    /// <summary>Click the composer's Post button automatically once it's filled. Set false to
    /// go back to a fully human-in-the-loop run, where the tool prepares the post and waits
    /// for you to submit it yourself. Never applies to --dry-run.</summary>
    public bool AutoClickPost { get; set; } = true;

    /// <summary>Randomized pause between the composer being filled and the Post click — a
    /// person reading over what they wrote. A random value in [min,max].</summary>
    public int PreClickPostMinMs { get; set; } = 5000;
    public int PreClickPostMaxMs { get; set; } = 10000;

    /// <summary>Randomized pause before each post — the beat a person takes before starting
    /// one, instead of moving the instant the previous post box closes. A random value in
    /// [min,max] is waited before EVERY group, the first one included, so a rerun that
    /// resumes a part-finished article doesn't open a composer the second it launches.
    /// Set Max to 0 to disable.</summary>
    public int BetweenPostsMinMs { get; set; } = 10000;
    public int BetweenPostsMaxMs { get; set; } = 30000;

    /// <summary>Small pause between in-composer steps (open, attach image, type) so each
    /// stage is visible rather than happening in a blink. Set to 0 to disable.</summary>
    public int StepDelayMs { get; set; } = 1200;

    /// <summary>Move a real cursor to each control (stepped mouse path + hover dwell) before
    /// clicking, instead of teleporting or forcing synthetic clicks. Leave on to look human.
    /// The post body itself is always pasted in one shot, not typed.</summary>
    public bool HumanizeInput { get; set; } = true;

    /// <summary>How long to wait after typing a link for Facebook to render its preview card.</summary>
    public int LinkPreviewWaitMs { get; set; } = 6000;

    /// <summary>Wipe transient run artifacts (screenshots, *.tmp) before and after a run so
    /// nothing is left lying around. Override per-run with --keep-artifacts when debugging.</summary>
    public bool CleanArtifacts { get; set; } = true;

    public string QueueFile { get; set; } = @".\posts.json";
    public string ScreenshotDir { get; set; } = @".\screenshots";
    public string StartUrl { get; set; } = "https://www.facebook.com";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Config Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"No config at {path} — using defaults.");
            return new Config();
        }

        var json = File.ReadAllText(path);
        var cfg = JsonSerializer.Deserialize<Config>(json, JsonOpts) ?? new Config();

        // Resolve relative paths against the working directory so the queue/screenshots
        // land somewhere predictable regardless of where the .exe lives.
        cfg.UserDataDir = Path.GetFullPath(cfg.UserDataDir);
        cfg.QueueFile = Path.GetFullPath(cfg.QueueFile);
        cfg.ScreenshotDir = Path.GetFullPath(cfg.ScreenshotDir);
        return cfg;
    }
}
