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

    /// <summary>A deliberate, watchable pause before each post (batch) so you can see what
    /// it's doing and to keep the pacing human. Set to 0 to disable.</summary>
    public int PrePostDelayMs { get; set; } = 3000;

    /// <summary>Small pause between in-composer steps (open, add groups, type) so each
    /// stage is visible rather than happening in a blink. Set to 0 to disable.</summary>
    public int StepDelayMs { get; set; } = 1200;

    /// <summary>Pause between ticking each group in the "Add groups" picker. Facebook
    /// watches for rapid selection, so space them out. Set to 0 to disable.</summary>
    public int GroupSelectDelayMs { get; set; } = 2000;

    /// <summary>Move a real cursor (stepped mouse path + hover) before clicks, and type
    /// at human speed, instead of teleporting/forced clicks. Leave on to look human.</summary>
    public bool HumanizeInput { get; set; } = true;

    /// <summary>Per-keystroke delay range (ms) when typing like a human, e.g. into the
    /// group picker's search box. Each key waits a random value in [min,max].</summary>
    public int TypeMinDelayMs { get; set; } = 70;
    public int TypeMaxDelayMs { get; set; } = 180;

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
