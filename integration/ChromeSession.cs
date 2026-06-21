using System.Diagnostics;
using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Connects Playwright to YOUR Chrome over CDP. Attaches to an instance already
/// listening on the debug port; otherwise launches a dedicated-profile Chrome with
/// the flag (so it never fights your everyday browsing).
/// </summary>
public static class ChromeSession
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

    public static async Task<IBrowser> ConnectAsync(IPlaywright pw, Config cfg)
    {
        if (!await IsPortOpenAsync(cfg.DebugPort))
        {
            if (cfg.AttachOnly)
                throw new InvalidOperationException(
                    $"Nothing is listening on CDP port {cfg.DebugPort} and AttachOnly=true.\n" +
                    $"Start Chrome yourself with --remote-debugging-port={cfg.DebugPort} (see launch-chrome.cmd), then rerun.");

            Console.WriteLine($"No Chrome on port {cfg.DebugPort}; launching a dedicated-profile Chrome…");
            LaunchChrome(cfg);
            await WaitForPortAsync(cfg.DebugPort, TimeSpan.FromSeconds(30));
        }
        else
        {
            Console.WriteLine($"Attaching to the Chrome already running on port {cfg.DebugPort}.");
        }

        return await pw.Chromium.ConnectOverCDPAsync($"http://localhost:{cfg.DebugPort}");
    }

    /// <summary>Returns the first existing page in the first context, creating one if needed.</summary>
    public static async Task<IPage> GetPageAsync(IBrowser browser)
    {
        var ctx = browser.Contexts.FirstOrDefault() ?? await browser.NewContextAsync();
        return ctx.Pages.FirstOrDefault() ?? await ctx.NewPageAsync();
    }

    private static void LaunchChrome(Config cfg)
    {
        if (!File.Exists(cfg.ChromePath))
            throw new FileNotFoundException(
                $"chrome.exe not found at '{cfg.ChromePath}'. Set ChromePath in appsettings.json.");

        Directory.CreateDirectory(cfg.UserDataDir);

        var psi = new ProcessStartInfo
        {
            FileName = cfg.ChromePath,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add($"--remote-debugging-port={cfg.DebugPort}");
        psi.ArgumentList.Add($"--user-data-dir={cfg.UserDataDir}");
        psi.ArgumentList.Add("--no-first-run");
        psi.ArgumentList.Add("--no-default-browser-check");
        psi.ArgumentList.Add(cfg.StartUrl);

        Process.Start(psi);
    }

    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            var resp = await Http.GetAsync($"http://localhost:{port}/json/version");
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForPortAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsPortOpenAsync(port)) return;
            await Task.Delay(500);
        }
        throw new TimeoutException($"Chrome did not expose CDP on port {port} within {timeout.TotalSeconds:0}s.");
    }
}
