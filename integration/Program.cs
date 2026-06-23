using FacebookPoster;
using Microsoft.Playwright;

// ── Facebook group publishing assistant (human-in-the-loop) ───────────────────
//
//   One run = the NEXT un-posted article from posts.json. It's cross-posted to your
//   shared groups in batches of GroupsPerPost (Facebook only lets you hook ~8 groups
//   onto a single post), so the run makes ceil(groups / GroupsPerPost) posts. For
//   each batch the tool fills the composer and tries to hook the groups — then YOU
//   verify and click Post. When every group is done the article is marked and the
//   run stops. Schedule it (e.g. daily) to advance one article at a time.
//
//   The tool NEVER clicks Post — that's always you. There is no auto-post mode.
//
//   Modes:
//     (default)    Semi-auto: prepare each batch; you click Post.
//     login        Open Chrome at Facebook to establish the session, then exit.
//     validate     Pre-flight: check group names/membership, then exit.
//     --dry-run    Prepare composers but never mark posted.
// ──────────────────────────────────────────────────────────────────────────────

var argv = Environment.GetCommandLineArgs().Skip(1).ToArray();
bool loginOnly = argv.Contains("login", StringComparer.OrdinalIgnoreCase);
bool scrapeGroups = argv.Contains("scrape-groups", StringComparer.OrdinalIgnoreCase);
bool validate = argv.Contains("validate", StringComparer.OrdinalIgnoreCase);
bool dryRun = argv.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
bool keepArtifacts = argv.Contains("--keep-artifacts", StringComparer.OrdinalIgnoreCase);

var cfg = Config.Load(Path.GetFullPath("appsettings.json"));
bool cleanArtifacts = cfg.CleanArtifacts && !keepArtifacts;

// Pick which plan/list to run, e.g. `--plan apologetics` -> posts.apologetics.json,
// or `--plan posts.ldsdoctrines.json`. Defaults to appsettings' QueueFile (posts.json).
var planArg = GetOption(argv, "--plan");
if (planArg is not null) cfg.QueueFile = ResolvePlanPath(planArg);
Console.WriteLine($"Plan file: {cfg.QueueFile}");

using var pw = await Playwright.CreateAsync();
var browser = await ChromeSession.ConnectAsync(pw, cfg);
var page = await ChromeSession.GetPageAsync(browser);

await EnsureLoggedIn(page, cfg);

if (loginOnly)
{
    Console.WriteLine("Session is ready in the dedicated profile. You can close this window.");
    return;
}

if (scrapeGroups)
{
    await GroupScraper.RunAsync(page);
    return;
}

var plan = PlanStore.Load(cfg.QueueFile);

if (plan.Groups.Count == 0)
{
    Console.WriteLine("No groups configured. Add your groups to \"Groups\": [ { \"Name\": ..., \"Url\": ... } ] in posts.json.");
    return;
}

// Pre-flight: validate the plan's groups (membership + live name) before any posting.
if (validate)
{
    await GroupValidator.RunAsync(page, plan, cfg.QueueFile);
    return;
}

var article = plan.Articles.FirstOrDefault(a => !a.IsComplete);
if (article is null)
{
    Console.WriteLine("All articles have been posted to every group. Nothing to do.");
    return;
}

var cmp = StringComparer.OrdinalIgnoreCase;
var remaining = plan.Groups.Where(g => !article.PostedGroups.Contains(g.Url, cmp)).ToList();
int per = plan.GroupsPerPost > 0 ? plan.GroupsPerPost : Math.Max(1, remaining.Count);
int postCount = (int)Math.Ceiling(remaining.Count / (double)per);

Console.WriteLine($"\nNext article: [{article.Id}]");
Console.WriteLine($"  {article.PostedGroups.Count}/{plan.Groups.Count} groups already done; {remaining.Count} remaining.");
Console.WriteLine($"  {per} group(s) per post → {postCount} post(s) this run. " +
                  $"Mode: {(dryRun ? "DRY-RUN" : "SEMI-AUTO (you click Post)")}.");

// Post text: hand-written override if present, otherwise fetched from the live page.
string body = article.Message ?? "";
if (string.IsNullOrWhiteSpace(body))
{
    Console.WriteLine($"\nFetching post text from {article.Link} …");
    body = await ContentFetcher.FetchAsync(page, article.Link ?? "") ?? "";
    if (string.IsNullOrWhiteSpace(body))
    {
        Console.WriteLine("  ! Couldn't fetch text from the site. Add a \"Message\" to this article, or check the Link.");
        return;
    }
    Console.WriteLine("  ✓ using this text:");
    foreach (var line in body.Split('\n')) Console.WriteLine($"      {line}");
}

// Start clean: wipe last run's screenshots/temp and reset this run's fail log.
if (cleanArtifacts) PurgeArtifacts(cfg, resetFailLog: true);

var poster = new Poster(cfg);
int posted = 0, skipped = 0;

for (int i = 0; i < remaining.Count; i += per)
{
    var batch = remaining.Skip(i).Take(per).ToList();
    Console.WriteLine($"\n— Post {i / per + 1}/{postCount}: hook these {batch.Count} group(s):");
    foreach (var g in batch) Console.WriteLine($"      • {g.Name}");

    // A short, watchable pause before each post — keeps the pacing human and gives you
    // a moment to see what's coming before the composer starts filling.
    if (cfg.PrePostDelayMs > 0)
    {
        Console.WriteLine($"  · pausing {cfg.PrePostDelayMs / 1000.0:0.#}s before composing…");
        await page.WaitForTimeoutAsync(cfg.PrePostDelayMs);
    }

    bool prepared = await poster.PrepareMultiAsync(page, article, batch, body);
    if (!prepared) { skipped += batch.Count; continue; }

    if (dryRun)
    {
        Console.WriteLine("  (dry-run: not marking posted)");
        continue;
    }

    // Semi-auto only: YOU click Post. The tool never submits on its own.
    Console.Write("  → Make sure the groups are selected, click POST yourself, then press [Enter] (s+[Enter] to skip): ");
    var key = Console.ReadLine()?.Trim().ToLowerInvariant();
    if (key == "s") { skipped += batch.Count; Console.WriteLine("  · skipped."); continue; }

    foreach (var g in batch) article.PostedGroups.Add(g.Url);
    PlanStore.Save(cfg.QueueFile, plan);   // persist after each batch → resume-safe
    posted += batch.Count;
    Console.WriteLine($"  ✓ {article.PostedGroups.Count}/{plan.Groups.Count} groups done.");
}

bool complete = plan.Groups.All(g => article.PostedGroups.Contains(g.Url, cmp));
if (!dryRun && complete)
{
    article.PostedAtUtc = DateTime.UtcNow;
    PlanStore.Save(cfg.QueueFile, plan);
    Console.WriteLine($"\n✓ [{article.Id}] complete across all {plan.Groups.Count} groups (marked {article.PostedAtUtc:u}).");
}
else if (!dryRun)
{
    Console.WriteLine($"\n[{article.Id}] still has {plan.Groups.Count - article.PostedGroups.Count} group(s) to go; rerun to finish it.");
}

Console.WriteLine($"\nRun finished. Group posts completed: {posted}, skipped/failed: {skipped}.");

// Tidy up after ourselves: remove this run's screenshots + temp files. The fail log
// (failed-groups.log) is kept as the post-run report unless there were no failures.
if (cleanArtifacts) PurgeArtifacts(cfg, resetFailLog: false);


// ── helpers ───────────────────────────────────────────────────────────────────

// Remove transient run artifacts: everything in the screenshots dir and any stray *.tmp
// next to the plan file. Never touches the plan, the Chrome profile, or report logs.
// With resetFailLog, also clears failed-groups.log so a fresh run starts with an empty
// failure list; otherwise an empty (no-failures) log is removed so nothing is left behind.
static void PurgeArtifacts(Config cfg, bool resetFailLog)
{
    try
    {
        if (Directory.Exists(cfg.ScreenshotDir))
            foreach (var f in Directory.EnumerateFiles(cfg.ScreenshotDir))
                TryDelete(f);

        var planDir = Path.GetDirectoryName(cfg.QueueFile) ?? ".";
        foreach (var f in Directory.EnumerateFiles(planDir, "*.tmp")) TryDelete(f);

        var failLog = Path.Combine(planDir, "failed-groups.log");
        if (File.Exists(failLog) && (resetFailLog || new FileInfo(failLog).Length == 0))
            TryDelete(failLog);
    }
    catch (Exception ex) { Console.WriteLine($"  (cleanup note: {ex.Message})"); }
}

static void TryDelete(string path)
{
    try { File.Delete(path); } catch { /* in use / already gone — ignore */ }
}

static string? GetOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    return null;
}

// "apologetics"            -> <cwd>/posts.apologetics.json
// "posts.ldsdoctrines.json"-> that file as given
static string ResolvePlanPath(string value)
{
    bool looksLikePath = value.Contains('.') || value.Contains('/') || value.Contains('\\');
    return Path.GetFullPath(looksLikePath ? value : $"posts.{value}.json");
}

static async Task EnsureLoggedIn(IPage page, Config cfg)
{
    await page.GotoAsync(cfg.StartUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

    var loginField = page.Locator("input[name='email']").First;
    bool loggedOut;
    try { loggedOut = await loginField.IsVisibleAsync(); }
    catch { loggedOut = false; }

    if (loggedOut)
    {
        Console.WriteLine("\nNot logged in. Log into Facebook in the Chrome window, then come back here.");
        Console.Write("Press [Enter] once you're logged in… ");
        Console.ReadLine();
    }
    else
    {
        Console.WriteLine("Facebook session looks active.");
    }
}
