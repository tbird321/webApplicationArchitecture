using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Pre-flight check: before a run, confirm every group in the plan is one you can actually
/// post to and that its stored Name still matches Facebook's live name. The picker hooks
/// groups BY NAME, so a renamed group silently fails to auto-select — this catches that
/// ahead of time and can update the plan's names in place.
/// </summary>
public static class GroupValidator
{
    public static async Task RunAsync(IPage page, Plan plan, string planPath)
    {
        Console.WriteLine($"\nValidating {plan.Groups.Count} group(s) against your live Facebook groups…");

        var live = await GroupScraper.ScrapeAsync(page);
        if (live.Count == 0)
        {
            Console.WriteLine("! Couldn't read your joined groups — are you logged in? Aborting validation.");
            return;
        }

        // Index the live groups by normalized URL.
        var byUrl = new Dictionary<string, (string Name, long Minutes)>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in live) byUrl[GroupScraper.NormalizeUrl(kv.Key)] = kv.Value;

        var ok = new List<Group>();
        var renamed = new List<(Group Plan, string Live)>();
        var missing = new List<Group>();

        foreach (var g in plan.Groups)
        {
            var key = GroupScraper.NormalizeUrl(g.Url);
            if (!byUrl.TryGetValue(key, out var liveGroup))
            {
                missing.Add(g);
            }
            else if (!NameMatches(g.Name, liveGroup.Name))
            {
                renamed.Add((g, liveGroup.Name));
            }
            else
            {
                ok.Add(g);
            }
        }

        // ── Report ────────────────────────────────────────────────────────────────
        Console.WriteLine($"\n=== Validation summary ({plan.Groups.Count} groups) ===");
        Console.WriteLine($"  ✓ {ok.Count} OK — name matches, will auto-select");
        Console.WriteLine($"  ✎ {renamed.Count} RENAMED — auto-select will fail until the name is updated");
        Console.WriteLine($"  ✗ {missing.Count} NOT FOUND — not in your joined groups (left, wrong URL, or can't post)");

        if (renamed.Count > 0)
        {
            Console.WriteLine("\n  Renamed groups (plan name → live Facebook name):");
            foreach (var (p, liveName) in renamed)
                Console.WriteLine($"      • \"{p.Name}\"  →  \"{liveName}\"   {p.Url}");
        }
        if (missing.Count > 0)
        {
            Console.WriteLine("\n  Not found:");
            foreach (var m in missing)
                Console.WriteLine($"      • \"{m.Name}\"   {m.Url}");
        }

        // ── Write a reviewable report next to the plan ──────────────────────────────
        var reportPath = Path.Combine(Path.GetDirectoryName(planPath) ?? ".", "group-validation.log");
        try
        {
            var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var lines = new List<string> { $"# Validation {stamp} — plan: {Path.GetFileName(planPath)}" };
            lines.Add($"# OK={ok.Count} RENAMED={renamed.Count} MISSING={missing.Count}");
            foreach (var g in ok) lines.Add($"OK\t{g.Name}\t{g.Url}");
            foreach (var (p, liveName) in renamed) lines.Add($"RENAMED\t{p.Name}\t=>\t{liveName}\t{p.Url}");
            foreach (var m in missing) lines.Add($"MISSING\t{m.Name}\t{m.Url}");
            await File.WriteAllLinesAsync(reportPath, lines);
            Console.WriteLine($"\n  Report written to {reportPath}");
        }
        catch (Exception ex) { Console.WriteLine($"  (couldn't write report: {ex.Message})"); }

        // ── Offer to fix renamed groups across ALL plan files in the folder ─────────
        // Groups are shared between plans (posts.json, doctrinepost.json, posts.*.json),
        // so a rename should be corrected everywhere that group's URL appears — matched by
        // URL, not by the old name.
        if (renamed.Count > 0)
        {
            Console.Write($"\nUpdate {renamed.Count} renamed group name(s) across all plan files in this folder? [y/N]: ");
            var ans = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (ans == "y" || ans == "yes")
            {
                var newNameByUrl = renamed.ToDictionary(
                    r => GroupScraper.NormalizeUrl(r.Plan.Url), r => r.Live, StringComparer.OrdinalIgnoreCase);
                var dir = Path.GetDirectoryName(Path.GetFullPath(planPath)) ?? ".";
                FixNamesAcrossPlans(dir, newNameByUrl);
            }
            else
            {
                Console.WriteLine("  · left the plans unchanged.");
            }
        }

        if (missing.Count > 0)
            Console.WriteLine("\n! Heads up: NOT FOUND groups won't receive the post. Verify the URLs or your membership.");

        Console.WriteLine("\nValidation complete.");
    }

    /// <summary>
    /// Apply renamed group names (keyed by normalized URL) to every plan file in the
    /// folder that contains those groups. Only files that actually change are rewritten.
    /// </summary>
    private static void FixNamesAcrossPlans(string dir, Dictionary<string, string> newNameByUrl)
    {
        int filesChanged = 0, namesChanged = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            Plan? p;
            try { p = PlanStore.Load(file); }
            catch { continue; } // not a readable plan (e.g. groups-scraped.json, appsettings.json)
            if (p is null || p.Groups.Count == 0) continue;

            int changedHere = 0;
            foreach (var g in p.Groups)
            {
                if (newNameByUrl.TryGetValue(GroupScraper.NormalizeUrl(g.Url), out var newName)
                    && !string.Equals(g.Name, newName, StringComparison.Ordinal))
                {
                    g.Name = newName;
                    changedHere++;
                }
            }
            if (changedHere > 0)
            {
                PlanStore.Save(file, p);
                filesChanged++;
                namesChanged += changedHere;
                Console.WriteLine($"  ✓ {Path.GetFileName(file)}: updated {changedHere} name(s).");
            }
        }
        Console.WriteLine(filesChanged == 0
            ? "  · no plan files needed changes."
            : $"  ✓ Updated {namesChanged} name(s) across {filesChanged} plan file(s).");
    }

    /// <summary>
    /// The picker matches names with whitespace normalized but case-sensitive, so compare
    /// the same way: collapse internal whitespace, trim, then ordinal compare.
    /// </summary>
    private static bool NameMatches(string planName, string liveName)
    {
        static string Norm(string s) => string.Join(' ',
            (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.Equals(Norm(planName), Norm(liveName), StringComparison.Ordinal);
    }
}
