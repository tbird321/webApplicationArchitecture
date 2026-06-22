using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Scrapes the Facebook "Groups you've joined" page. <see cref="ScrapeAsync"/> returns the
/// live url -> (name, lastActive) map; <see cref="RunAsync"/> writes it to
/// groups-scraped.json sorted by last-active time (most recent first).
/// </summary>
public static class GroupScraper
{
    public static async Task RunAsync(IPage page)
    {
        var groups = await ScrapeAsync(page);
        if (groups.Count == 0)
        {
            Console.WriteLine("\n! No groups found. Make sure you're logged in and on the Groups page.");
            return;
        }

        var sorted = groups
            .OrderBy(g => g.Value.Minutes)
            .ThenBy(g => g.Value.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"\nFound {sorted.Count} group(s), sorted by last active.");

        var lines = sorted.Select(g =>
        {
            var safeName = g.Value.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"    {{ \"Name\": \"{safeName}\", \"Url\": \"{g.Key}\" }}";
        });
        var json = "[\n" + string.Join(",\n", lines) + "\n]";

        var outPath = Path.GetFullPath("groups-scraped.json");
        await File.WriteAllTextAsync(outPath, json);
        Console.WriteLine($"Written to: {outPath}");
        Console.WriteLine("Paste the contents into the \"Groups\" array in posts.json, then remove any you don't want.");
    }

    /// <summary>
    /// Scrolls the joined-groups page and returns a map of normalized group URL ->
    /// (current display name, minutes since last active).
    /// </summary>
    public static async Task<Dictionary<string, (string Name, long Minutes)>> ScrapeAsync(IPage page)
    {
        Console.WriteLine("\nNavigating to your joined groups…");
        await page.GotoAsync("https://www.facebook.com/groups/?category=joined",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(2500);

        Console.WriteLine("Scrolling to load all groups (this may take a few seconds)…");

        var groups = new Dictionary<string, (string Name, long Minutes)>(StringComparer.OrdinalIgnoreCase);
        int prevCount = 0;
        int unchangedRounds = 0;

        for (int round = 0; round < 40 && unchangedRounds < 5; round++)
        {
            var anchors = await page.Locator("a[href*='facebook.com/groups/']").AllAsync();
            foreach (var a in anchors)
            {
                string? href;
                try { href = await a.GetAttributeAsync("href"); }
                catch { continue; }
                if (string.IsNullOrWhiteSpace(href)) continue;

                var uri = new Uri(href);
                var seg = uri.AbsolutePath.TrimStart('/');
                if (!seg.StartsWith("groups/", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = seg.Split('/');
                if (parts.Length < 2 || parts[1] is "feed" or "discover" or "joins"
                    or "create" or "search" or "requests" or "pending" or "") continue;

                var clean = $"https://www.facebook.com/groups/{parts[1].TrimEnd('/')}";

                // Get the group name from the first multi-word visible text span.
                string name = "";
                try
                {
                    var spans = await a.Locator("span").AllAsync();
                    foreach (var s in spans)
                    {
                        var t = (await s.InnerTextAsync()).Trim();
                        if (t.Length > 6 && t.Contains(' ') && !t.StartsWith("Last active"))
                        { name = t; break; }
                    }
                }
                catch { }
                if (string.IsNullOrWhiteSpace(name)) continue;

                // Get "last active" from the abbr aria-label inside this anchor.
                long minutes = long.MaxValue;
                try
                {
                    var abbr = a.Locator("abbr[aria-label]").First;
                    if (await abbr.CountAsync() > 0)
                    {
                        var label = await abbr.GetAttributeAsync("aria-label") ?? "";
                        minutes = ParseMinutesAgo(label);
                    }
                }
                catch { }

                if (!groups.TryGetValue(clean, out var existing) || name.Length > existing.Name.Length)
                    groups[clean] = (name, minutes);
            }

            if (groups.Count == prevCount) unchangedRounds++;
            else { unchangedRounds = 0; prevCount = groups.Count; }

            await page.EvaluateAsync("window.scrollBy(0, 900)");
            await page.WaitForTimeoutAsync(800);
        }

        return groups;
    }

    /// <summary>Converts a relative-time string like "17 minutes ago" to total minutes.</summary>
    private static long ParseMinutesAgo(string label)
    {
        label = label.Trim().ToLowerInvariant();
        if (label.Contains("just now") || label.Contains("moment")) return 0;

        static int FirstInt(string s)
        {
            var digits = new string(s.TakeWhile(c => char.IsDigit(c) || c == ' ')
                                     .Where(char.IsDigit).ToArray());
            return digits.Length > 0 ? int.Parse(digits) : 1;
        }

        if (label.Contains("minute"))  return FirstInt(label);
        if (label.Contains("hour"))    return FirstInt(label) * 60L;
        if (label.Contains("day"))     return FirstInt(label) * 1440L;
        if (label.Contains("week"))    return FirstInt(label) * 10080L;
        if (label.Contains("month"))   return FirstInt(label) * 43200L;
        if (label.Contains("year"))    return FirstInt(label) * 525600L;
        return long.MaxValue;
    }

    /// <summary>Normalize a group URL to "groups/&lt;slug&gt;" (lowercased) for matching.</summary>
    public static string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (parts.Length >= 2 && parts[0].Equals("groups", StringComparison.OrdinalIgnoreCase))
                return "groups/" + parts[1].ToLowerInvariant();
            return url.Trim().ToLowerInvariant();
        }
        catch { return url.Trim().ToLowerInvariant(); }
    }
}
