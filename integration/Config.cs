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

    /// <summary>If true, fill the post then wait for you to confirm in the console before submitting.</summary>
    public bool SemiAuto { get; set; } = true;

    public int MinDelayMs { get; set; } = 4000;
    public int MaxDelayMs { get; set; } = 12000;

    /// <summary>How long to wait after typing a link for Facebook to render its preview card.</summary>
    public int LinkPreviewWaitMs { get; set; } = 6000;

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
