using FacebookPoster;
using Microsoft.Playwright;

// ── Facebook group publishing assistant (human-in-the-loop) ───────────────────
//
//   One run = the NEXT un-posted article from posts.json, posted ONE GROUP AT A TIME:
//   a separate native post in each of your groups, so the run makes as many posts as
//   there are groups left. Per group it pastes the post in, pauses, clicks Post, and
//   advances once the post box closes. When every group is done the article is marked
//   and the run stops. Schedule it (e.g. daily) to advance one article at a time.
//
//   The "Add groups" picker is deliberately NOT used: hooking many groups onto one
//   post is what trips Facebook's rapid-selection rate limit.
//
//   Pacing is randomized throughout — 10–30s before each post, 5–10s between filling
//   the composer and the Post click — so the run doesn't move at machine speed. Set
//   AutoClickPost=false in appsettings.json to go back to clicking Post yourself.
//
//   Modes:
//     (default)    Paste into each group's composer in turn and post it.
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

// `links`: rewrite the plan's legacy ?page= URLs to canonical form and exit. Handled before
// Chrome is touched — it's a pure file operation, so it needs no browser and no login.
if (argv.Contains("links", StringComparer.OrdinalIgnoreCase))
{
    var linkPlan = PlanStore.Load(cfg.QueueFile);
    var fixes = Links.NormalizePlan(linkPlan);
    if (fixes.Count == 0)
    {
        Console.WriteLine($"All {linkPlan.Articles.Count} article link(s) are already canonical.");
        return;
    }
    foreach (var (id, from, to) in fixes)
        Console.WriteLine($"  [{id}]\n    {from}\n → {to}");
    if (dryRun)
    {
        Console.WriteLine($"\n{fixes.Count} link(s) would change (dry-run: plan not saved).");
    }
    else
    {
        PlanStore.Save(cfg.QueueFile, linkPlan);
        Console.WriteLine($"\n✓ Rewrote {fixes.Count} link(s) in {Path.GetFileName(cfg.QueueFile)}.");
    }
    return;
}

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

// Post the canonical URL, never the legacy ?page= form that 301-redirects. Rewriting here
// means the plan self-heals: the file is corrected once and stays corrected.
var linkFixes = Links.NormalizePlan(plan);
if (linkFixes.Count > 0)
{
    Console.WriteLine($"  · rewrote {linkFixes.Count} legacy ?page= link(s) to canonical form" +
                      (dryRun ? " (dry-run: not saved)." : "."));
    if (!dryRun) PlanStore.Save(cfg.QueueFile, plan);
}

// Reconcile first: finalize any article whose recorded group-posts already cover every
// group (e.g. a prior run posted the last group but was interrupted before marking it
// complete). This keeps the "first incomplete article" selection below honest.
if (!dryRun && plan.Groups.Count > 0)
{
    bool changed = false;
    foreach (var a in plan.Articles)
    {
        if (a.IsComplete) continue;
        var done = new HashSet<string>(a.PostedGroups, StringComparer.OrdinalIgnoreCase);
        if (plan.Groups.All(g => done.Contains(GroupKey(g))))
        {
            a.PostedAtUtc = DateTime.UtcNow;
            changed = true;
        }
    }
    if (changed) PlanStore.Save(cfg.QueueFile, plan);
}

var article = plan.Articles.FirstOrDefault(a => !a.IsComplete);
if (article is null)
{
    Console.WriteLine("All articles have been posted to every group. Nothing to do.");
    return;
}

// Skip groups this article was already posted to — only the rest remain this run.
var alreadyPosted = new HashSet<string>(article.PostedGroups, StringComparer.OrdinalIgnoreCase);
var remaining = plan.Groups.Where(g => !alreadyPosted.Contains(GroupKey(g))).ToList();
int alreadyDone = plan.Groups.Count - remaining.Count;

if (remaining.Count == 0)
{
    if (!dryRun)
    {
        article.PostedAtUtc = DateTime.UtcNow;
        PlanStore.Save(cfg.QueueFile, plan);
    }
    Console.WriteLine($"[{article.Id}] was already posted to every group — marked complete.");
    return;
}

// One group per post, always: a separate native post in each group instead of hooking
// many onto one via the "Add groups" picker (which is what trips the rate limit).
string modeLabel = dryRun ? "DRY-RUN"
                 : cfg.AutoClickPost ? "ONE POST PER GROUP (auto-posts)"
                 : "ONE POST PER GROUP (you click Post)";

Console.WriteLine($"\nNext article: [{article.Id}]");
if (alreadyDone > 0)
    Console.WriteLine($"  {alreadyDone} group(s) already posted (skipping). {remaining.Count} left.");
else
    Console.WriteLine($"  {remaining.Count} group(s) to post.");
Console.WriteLine($"  {remaining.Count} separate post(s) this run — one per group. Mode: {modeLabel}.");

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

    // The fetch just navigated to the article, so the browser's final URL is authoritative:
    // adopt it if the site redirected somewhere we didn't predict. A landing on the site
    // root means the page is gone — say so rather than posting a link to the homepage.
    var landed = page.Url;
    if (!string.Equals(landed, article.Link, StringComparison.OrdinalIgnoreCase)
        && Uri.TryCreate(landed, UriKind.Absolute, out var landedUri))
    {
        if (landedUri.AbsolutePath is "" or "/")
            Console.WriteLine($"  ! {article.Link} redirected to the site root — check that this page still exists.");
        else
        {
            Console.WriteLine($"  · using the url it resolved to: {landed}");
            article.Link = landed;
            if (!dryRun) PlanStore.Save(cfg.QueueFile, plan);
        }
    }
}

// Start clean: wipe last run's screenshots/temp and reset this run's fail log.
if (cleanArtifacts) PurgeArtifacts(cfg, resetFailLog: true);

var poster = new Poster(cfg);
var rng = new Random();
int posted = 0, skipped = 0;

// Record a group as posted and persist immediately, so progress survives an interrupt.
void MarkGroupPosted(Group g)
{
    if (alreadyPosted.Add(GroupKey(g)))
    {
        article.PostedGroups.Add(GroupKey(g));
        PlanStore.Save(cfg.QueueFile, plan);
    }
}

for (int i = 0; i < remaining.Count; i++)
{
    var group = remaining[i];

    // One group at a time: no per-group Enter. The composer fills, you click Post, and we
    // advance the moment the post box disappears. Spacing comes from your own Post clicks
    // plus the pauses below.
    Console.WriteLine($"\n— Group {i + 1}/{remaining.Count}: {group.Name}");

    // A randomized beat before every post — the pause a person takes before starting one,
    // rather than jumping into the next group the instant the last box closes. It applies to
    // the first group too, so a rerun resuming a part-finished article doesn't open fast.
    if (!dryRun && cfg.BetweenPostsMaxMs > 0)
    {
        int lo = Math.Max(0, cfg.BetweenPostsMinMs);
        int wait = rng.Next(lo, Math.Max(lo + 1, cfg.BetweenPostsMaxMs));
        Console.WriteLine($"  · pausing {wait / 1000.0:0.#}s before composing…");
        await page.WaitForTimeoutAsync(wait);
    }

    // A short, watchable pause before the composer starts filling.
    if (cfg.PrePostDelayMs > 0)
        await page.WaitForTimeoutAsync(cfg.PrePostDelayMs);

    bool prepared = await poster.PrepareAsync(page, article, group, body);
    if (!prepared) { skipped++; continue; }

    if (dryRun)
    {
        // Gate on you even in dry-run: navigating away from a filled composer pops
        // Facebook's discard prompt, so let it be dismissed before the next group.
        Console.WriteLine("  (dry-run: not marking posted)");
        Console.Write("  → Discard the post yourself, then press [Enter] for the next group… ");
        Console.ReadLine();
        continue;
    }

    // Submit. With AutoClickPost the tool clicks Post itself after a randomized pause; if
    // that button can't be found (or auto-click is off) the click falls back to you. Either
    // way the group is only recorded once the post box actually closes.
    if (cfg.AutoClickPost)
    {
        if (!await poster.ClickPostAsync(page, article))
            Console.WriteLine("  → Click POST yourself — I'll wait and then move on. (press 's' to skip)");
        else
            Console.WriteLine("  · waiting for the post box to close… (press 's' to skip)");
    }
    else
    {
        Console.WriteLine("  → Click POST in this group — when the post box closes I'll move to the next. (press 's' to skip)");
    }

    var result = await poster.WaitForComposerClosedAsync(page, SkipKeyPressed);

    if (result == PostWait.Skipped)
    {
        skipped++;
        Console.WriteLine("  · skipped.");
        continue;
    }

    // Facebook's rate-limit notice: stop the whole run. The remaining groups would fail
    // anyway, and continuing to hammer it is what turns a short block into a long one.
    var block = await poster.DetectBlockAsync(page);
    if (block is not null)
    {
        Console.WriteLine($"\n! Facebook is blocking posts right now (\"{block}\").");
        Console.WriteLine("  Stopping this run. Progress is saved — rerun later and it resumes at this group.");
        if (result == PostWait.Posted) { MarkGroupPosted(group); posted++; } else skipped++;
        break;
    }

    if (result == PostWait.TimedOut)
    {
        skipped++;
        Console.WriteLine("  · timed out waiting for the post box to close — skipping.");
        continue;
    }

    MarkGroupPosted(group);
    posted++;
    Console.WriteLine($"  ✓ posted to {group.Name} ({article.PostedGroups.Count}/{plan.Groups.Count}).");
}

// Complete when the recorded posts cover every group — not just this run's count, so a
// run that finishes the last remaining groups still finalizes the article.
bool complete = plan.Groups.All(g => alreadyPosted.Contains(GroupKey(g)));
if (!dryRun && complete)
{
    article.PostedAtUtc = DateTime.UtcNow;
    PlanStore.Save(cfg.QueueFile, plan);
    Console.WriteLine($"\n✓ [{article.Id}] complete across all {plan.Groups.Count} groups (marked {article.PostedAtUtc:u}).");
}
else if (!dryRun)
{
    int left = plan.Groups.Count - article.PostedGroups.Count;
    Console.WriteLine($"\n[{article.Id}] still has {left} group(s) to go; rerun to finish it.");
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

// Non-blocking peek for an 's' keypress so single-group mode can offer a skip while it
// polls for the post box to close. No-op if console input is redirected.
static bool SkipKeyPressed()
{
    try
    {
        if (Console.KeyAvailable)
        {
            var k = Console.ReadKey(intercept: true);
            return k.Key == ConsoleKey.S;
        }
    }
    catch { /* input redirected / no console — interactive skip unavailable */ }
    return false;
}

// Stable per-group identity used to record/skip posted groups: the Url, or the Name if
// no Url is set. Trimmed; compared case-insensitively by the callers' HashSets.
static string GroupKey(Group g) =>
    !string.IsNullOrWhiteSpace(g.Url) ? g.Url.Trim() : g.Name.Trim();

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
