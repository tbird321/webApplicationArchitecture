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
//   The tool NEVER clicks Post for you in the normal flow. You do.
//
//   Modes:
//     (default)    Semi-auto: prepare each batch; you click Post.
//     login        Open Chrome at Facebook to establish the session, then exit.
//     --dry-run     Prepare composers but never mark posted.
//     --auto        Opt-in full-auto: the script clicks Post itself (higher ToS risk).
// ──────────────────────────────────────────────────────────────────────────────

var argv = Environment.GetCommandLineArgs().Skip(1).ToArray();
bool loginOnly = argv.Contains("login", StringComparer.OrdinalIgnoreCase);
bool forceAuto = argv.Contains("--auto", StringComparer.OrdinalIgnoreCase);
bool dryRun = argv.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

var cfg = Config.Load(Path.GetFullPath("appsettings.json"));
bool semiAuto = cfg.SemiAuto && !forceAuto;

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

var plan = PlanStore.Load(cfg.QueueFile);

if (plan.Groups.Count == 0)
{
    Console.WriteLine("No groups configured. Add your groups to \"Groups\": [ { \"Name\": ..., \"Url\": ... } ] in posts.json.");
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
                  $"Mode: {(dryRun ? "DRY-RUN" : semiAuto ? "SEMI-AUTO (you click Post)" : "FULL-AUTO")}.");

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

var poster = new Poster(cfg);
int posted = 0, skipped = 0;

for (int i = 0; i < remaining.Count; i += per)
{
    var batch = remaining.Skip(i).Take(per).ToList();
    Console.WriteLine($"\n— Post {i / per + 1}/{postCount}: hook these {batch.Count} group(s):");
    foreach (var g in batch) Console.WriteLine($"      • {g.Name}");

    bool prepared = await poster.PrepareMultiAsync(page, article, batch, body);
    if (!prepared) { skipped += batch.Count; continue; }

    if (dryRun)
    {
        Console.WriteLine("  (dry-run: not marking posted)");
        continue;
    }

    if (semiAuto)
    {
        Console.Write("  → Make sure the groups are selected, click POST yourself, then press [Enter] (s+[Enter] to skip): ");
        var key = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (key == "s") { skipped += batch.Count; Console.WriteLine("  · skipped."); continue; }
    }
    else
    {
        if (!await poster.SubmitAsync(page, article, batch[0].Url)) { skipped += batch.Count; continue; }
    }

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


// ── helpers ───────────────────────────────────────────────────────────────────
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
