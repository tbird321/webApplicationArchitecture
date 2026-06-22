using System.Text;
using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Drives Facebook's group composer + "post to more groups" picker. In the intended
/// (semi-auto) mode it only PREPARES the post — fills text/link/image and tries to
/// hook the batch's other groups — then leaves YOU to verify and click "Post".
///
/// Facebook's DOM is obfuscated and changes often, so selectors are candidate lists
/// matched by role/aria/visible-text. Expect to tune these; --dry-run + the
/// screenshots make that quick.
/// </summary>
public sealed class Poster
{
    private readonly Config _cfg;
    private readonly Random _rng = new();

    public Poster(Config cfg) => _cfg = cfg;

    /// <summary>Where groups that couldn't be auto-selected get recorded for review.</summary>
    private string FailLogPath => Path.Combine(Path.GetDirectoryName(_cfg.QueueFile) ?? ".", "failed-groups.log");

    /// <summary>Append each un-selectable group (with the article and reason) to the fail log.</summary>
    private void LogFailedGroups(Article article, IEnumerable<Group> groups, string reason)
    {
        try
        {
            var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var lines = groups.Select(g => $"{stamp}\t[{article.Id}]\t{reason}\t{g.Name}\t{g.Url}");
            File.AppendAllLines(FailLogPath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  (couldn't write fail log: {ex.Message})");
        }
    }

    private static readonly string[] ComposerTriggers =
    {
        "div[role='button']:has-text('Write something')",
        "[aria-label*='Write something']",
        "text=Write something",
        "text=Create a public post",
    };

    // Order matters: match the Create-post editor by its "Write something…" placeholder
    // first so we never grab a feed comment box (aria-placeholder="Comment as …") that
    // happens to sit behind the dialog overlay.
    private static readonly string[] Textboxes =
    {
        "div[role='dialog'] div[contenteditable='true'][aria-placeholder^='Write']",
        "div[aria-label='Create post'] div[contenteditable='true'][role='textbox']",
        "div[role='dialog'] div[role='textbox'][data-lexical-editor='true']",
        "div[role='dialog'] div[role='textbox']:not([aria-placeholder^='Comment'])",
    };

    private static readonly string[] PhotoButtons =
    {
        "div[role='dialog'] [aria-label='Photo/video']",
        "div[role='dialog'] [aria-label='Photo/Video']",
        "div[role='dialog'] [aria-label='Photos/videos']",
        "div[role='dialog'] [aria-label*='hoto']",
        "div[role='dialog'] div[role='button']:has-text('Photo')",
        "[aria-label='Photo/video']",
        "[aria-label*='hoto']",
        "text=Photo/video",
        "text=Photo",
    };

    private static readonly string[] MoreGroupsButtons =
    {
        "div[role='dialog'] [aria-label='Add groups']",
        "div[role='dialog'] div[role='button']:has-text('Add groups')",
        "text=Add groups",
        "text=Post to more groups",
        "[aria-label*='more groups']",
    };

    // The picker's OWN search box — scoped to a dialog and to "groups" so it can never
    // match Facebook's global top "Search Facebook" bar (typing there navigates the page).
    private static readonly string[] GroupSearchBoxes =
    {
        "div[role='dialog'] input[aria-label='Search groups']",
        "div[role='dialog'] input[placeholder='Search groups']",
        "div[role='dialog'] input[aria-label*='Search groups']",
        "div[role='dialog'] input[placeholder*='Search groups']",
        "div[role='dialog'] input[type='search']",
        "div[role='dialog'] input[aria-label*='Search']",
    };

    private static readonly string[] ConfirmButtons =
    {
        "div[role='dialog'] [aria-label='Done']",
        "div[role='dialog'] div[role='button']:has-text('Done')",
    };

    /// <summary>
    /// Compose one post in the batch's first group and try to hook the rest. Returns
    /// false if the composer couldn't be opened. Does NOT submit — you click Post.
    /// </summary>
    public async Task<bool> PrepareMultiAsync(IPage page, Article article, IReadOnlyList<Group> batch, string body)
    {
        var primary = batch[0];
        Console.WriteLine($"\n→ [{article.Id}] composing in: {primary.Name}");
        await page.GotoAsync(primary.Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Jitter(page);

        var trigger = await FirstVisible(page, ComposerTriggers);
        if (trigger is null)
        {
            Console.WriteLine("  ! Couldn't find the composer ('Write something') on this page.");
            await Screenshot(page, article, "no-composer");
            return false;
        }
        await trigger.ClickAsync();

        // Wait for the "Create post" dialog to actually open before hunting for its editor,
        // otherwise we can match a feed comment box that's still visible mid-animation.
        try
        {
            await page.Locator("div[role='dialog'][aria-label='Create post'], div[aria-label='Create post']")
                      .First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 8000 });
        }
        catch { /* fall through; the textbox search below still has its own polling */ }
        await page.WaitForTimeoutAsync(800);

        var textbox = await FirstVisible(page, Textboxes);
        if (textbox is null)
        {
            Console.WriteLine("  ! Composer opened but no text box was found.");
            await Screenshot(page, article, "no-textbox");
            return false;
        }

        await StepPause(page);

        // Attach the image first — Facebook removes the Photo/video button once groups are
        // hooked onto the post, but keeps "Add groups" available when an image is attached.
        // Order: image → groups → text.
        if (!string.IsNullOrWhiteSpace(article.ImagePath))
        {
            await AttachImage(page, article);
            await StepPause(page);
        }

        // Hook the batch's other groups AFTER the image (if any). Facebook hides the
        // "Add groups" control as soon as the composer has text (unless an image is attached).
        if (batch.Count > 1)
        {
            await TryHookMoreGroups(page, article, batch.Skip(1).ToList());
            await StepPause(page);
        }

        if (!await FillComposerAsync(page, textbox, BuildBody(body, article.Link)))
        {
            Console.WriteLine("  ! Composer text box found but typing didn't land (Lexical editor).");
            await Screenshot(page, article, "empty-textbox");
            return false;
        }
        await StepPause(page);

        if (!string.IsNullOrWhiteSpace(article.Link))
        {
            Console.WriteLine("  · waiting for link preview…");
            await page.WaitForTimeoutAsync(_cfg.LinkPreviewWaitMs);
        }

        await Screenshot(page, article, "prepared");
        Console.WriteLine("  ✓ composer filled. Confirm the groups, then post it yourself.");
        return true;
    }

    /// <summary>Best-effort: open "Post to more groups" and tick the batch's other groups by name.</summary>
    private async Task TryHookMoreGroups(IPage page, Article article, IReadOnlyList<Group> others)
    {
        var moreBtn = await FirstVisible(page, MoreGroupsButtons, 5000);
        if (moreBtn is null)
        {
            Console.WriteLine("  (couldn't find 'Post to more groups' — add these manually before posting:)");
            foreach (var g in others) Console.WriteLine($"      • {g.Name}");
            LogFailedGroups(article, others, "picker-not-found");
            return;
        }
        await moreBtn.ClickAsync();
        await page.WaitForTimeoutAsync(1500);

        // Capture the picker the instant it opens, before touching anything.
        await Screenshot(page, article, "group-picker");

        // The picker's group list is virtualized — with many groups, a given row isn't in
        // the DOM until you filter to it via the picker's search box. So grab that search
        // box once and type each group's name before ticking it.
        var search = await FirstVisible(page, GroupSearchBoxes, 4000);
        if (search is null)
            Console.WriteLine("  (no picker search box found — only currently-visible groups can be ticked)");

        var failed = new List<Group>();
        foreach (var g in others)
        {
            bool ok = await TickGroupAsync(page, search, g.Name);
            if (!ok)
            {
                // Facebook's picker search intermittently returns nothing for a group you're
                // really in — a known FB bug. Retry once; it usually surfaces the 2nd time.
                await page.WaitForTimeoutAsync(700);
                ok = await TickGroupAsync(page, search, g.Name);
            }
            if (ok)
            {
                Console.WriteLine($"      ✓ selected: {g.Name}");
            }
            else
            {
                Console.WriteLine($"      ! couldn't auto-select '{g.Name}' — tick it manually in the picker.");
                failed.Add(g);
            }
        }
        if (failed.Count > 0)
        {
            LogFailedGroups(article, failed, "not-selected");
            Console.WriteLine($"  ! {failed.Count}/{others.Count} group(s) in this batch couldn't be auto-selected — logged to {FailLogPath}");
        }

        await Screenshot(page, article, "group-picker-selected");

        var done = await FirstVisible(page, ConfirmButtons, 3000);
        if (done is not null)
        {
            Console.WriteLine("  · clicking picker confirm button…");
            await done.ClickAsync();
        }
        else
        {
            Console.WriteLine("  ! couldn't find a Done/confirm button to close the group picker.");
        }
        await page.WaitForTimeoutAsync(1200);
        await Screenshot(page, article, "after-add-groups");
    }

    /// <summary>
    /// Tick a group's checkbox in the "Add groups" picker. The clickable target is the
    /// row's checkbox/button — NOT the name text span (clicking the label does nothing) —
    /// so we walk up from the name to the nearest interactive ancestor and click that,
    /// then verify the row's checkbox actually flipped to checked.
    /// </summary>
    private static async Task<bool> TickGroupAsync(IPage page, ILocator? search, string name)
    {
        var target = NormName(name);

        // No picker search box: just match among whatever rows are currently rendered.
        if (search is null)
        {
            var visible = await PollCandidatesAsync(page);
            int i0 = ExactIndex(visible, target);
            return i0 >= 0 && await TickByIndexAsync(page, i0);
        }

        // Type the name ONE WORD AT A TIME, checking after each word. As soon as the list
        // narrows to a single row (or an exact-name row appears), tick it. This is far more
        // robust than typing the whole name: distinctive early words usually narrow to one
        // hit, and we never depend on Facebook's search liking a long, punctuated string.
        var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        try { await search.ClickAsync(); await search.FillAsync(""); } catch { }

        var prev = new List<(int Idx, string Name)>();
        for (int i = 0; i < words.Length; i++)
        {
            try
            {
                if (i > 0) await search.PressSequentiallyAsync(" ", new() { Delay = 8 });
                await search.PressSequentiallyAsync(words[i], new() { Delay = 12 });
            }
            catch { }

            var cands = await PollCandidatesAsync(page);

            int exact = ExactIndex(cands, target);
            if (exact >= 0) return await TickByIndexAsync(page, exact);
            if (cands.Count == 1) return await TickByIndexAsync(page, cands[0].Idx);
            if (cands.Count == 0)
            {
                // This word over-filtered (the plan name diverges from Facebook's past here).
                // Fall back to the results we had BEFORE adding it.
                int pe = ExactIndex(prev, target);
                if (pe >= 0) return await TickByIndexAsync(page, pe);
                if (prev.Count == 1) return await TickByIndexAsync(page, prev[0].Idx);
                return false;
            }
            prev = cands;
        }

        // Ran out of words with more than one candidate and no exact match — ambiguous, so
        // leave it for manual ticking rather than risk selecting the wrong group.
        return false;
    }

    private static int ExactIndex(List<(int Idx, string Name)> cands, string target)
    {
        foreach (var c in cands)
            if (NormName(c.Name) == target) return c.Idx;
        return -1;
    }

    /// <summary>Tick a checkbox by its document-order index (matches the JS enumeration).</summary>
    private static async Task<bool> TickByIndexAsync(IPage page, int idx)
    {
        var checkbox = page.Locator("input[type='checkbox']").Nth(idx);
        try
        {
            if (await IsCheckedNow(checkbox)) return true;
            await checkbox.CheckAsync(new() { Force = true });
            if (await IsCheckedNow(checkbox)) return true;
        }
        catch { /* fall through to row click */ }

        try
        {
            var row = checkbox.Locator("xpath=ancestor::div[.//*[local-name()='image' or local-name()='img']][1]").First;
            await row.ClickAsync(new() { Timeout = 3000, Force = true });
            return await IsCheckedNow(checkbox);
        }
        catch { return false; }
    }

    /// <summary>
    /// Read the currently-rendered group rows as (document-order checkbox index, name).
    /// Polls briefly because search results render a beat after typing.
    /// </summary>
    private static async Task<List<(int Idx, string Name)>> PollCandidatesAsync(IPage page)
    {
        var list = new List<(int, string)>();
        for (int t = 0; t < 4; t++)
        {
            await page.WaitForTimeoutAsync(t == 0 ? 500 : 300);
            try
            {
                var raw = await page.EvaluateAsync<string[]>(CandidatesJs);
                list = ParseCandidates(raw);
            }
            catch { list = new(); }
            if (list.Count > 0) return list;
        }
        return list;
    }

    private static List<(int Idx, string Name)> ParseCandidates(string[] raw)
    {
        var list = new List<(int, string)>();
        foreach (var r in raw)
        {
            var bar = r.IndexOf('|');
            if (bar > 0 && int.TryParse(r[..bar], out var idx))
                list.Add((idx, r[(bar + 1)..]));
        }
        return list;
    }

    /// <summary>Normalize a group name for tolerant comparison: lowercase, collapse
    /// whitespace, drop trailing punctuation.</summary>
    private static string NormName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var collapsed = string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.TrimEnd('.', ',', ';', ':', '!', ' ').ToLowerInvariant();
    }

    // Returns the document-order index (among all page checkboxes) of the group row whose
    // name matches, or -1. Match is normalized: lowercased, whitespace collapsed, trailing
    // punctuation dropped. The row is the nearest ancestor of the checkbox that holds an
    // avatar image (so the composer's lone "anonymous" toggle never matches a group name).
    private const string MatchCheckboxJs = @"(name) => {
        const norm = s => (s || '').toLowerCase().replace(/[\s ]+/g, ' ').replace(/[.,;:!]+$/, '').trim();
        const target = norm(name);
        const all = [...document.querySelectorAll(""input[type=checkbox]"")];
        for (let i = 0; i < all.length; i++) {
            let row = all[i];
            while (row && !(row.querySelector && (row.querySelector('image') || row.querySelector('img')))) row = row.parentElement;
            if (!row) continue;
            const line = (row.innerText || '').split('\n').map(s => s.trim()).filter(Boolean)[0] || '';
            if (norm(line) === target) return i;
        }
        return -1;
    }";

    // Lists rendered group rows as "index|name": index is the checkbox's position among ALL
    // page checkboxes (document order), so Playwright's Nth(index) selects the same element.
    // The row is the nearest ancestor of the checkbox holding an avatar image, so the
    // composer's lone "anonymous" toggle never shows up as a group.
    private const string CandidatesJs = @"() => {
        const all = [...document.querySelectorAll(""input[type=checkbox]"")];
        const out = [];
        for (let i = 0; i < all.length; i++) {
            let row = all[i];
            while (row && !(row.querySelector && (row.querySelector('image') || row.querySelector('img')))) row = row.parentElement;
            if (!row) continue;
            const line = (row.innerText || '').split('\n').map(s => s.trim()).filter(Boolean)[0] || '';
            if (line) out.push(i + '|' + line);
        }
        return out;
    }";

    /// <summary>True if the given checkbox input reads as checked.</summary>
    private static async Task<bool> IsCheckedNow(ILocator checkbox)
    {
        try
        {
            var aria = await checkbox.GetAttributeAsync("aria-checked");
            if (aria is not null) return aria == "true";
            return await checkbox.IsCheckedAsync();
        }
        catch { return false; }
    }

    private async Task AttachImage(IPage page, Article article)
    {
        var imagePath = article.ImagePath!;

        // If it's a URL, download it to a temp file first.
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  · downloading image from URL…");
            var ext = Path.GetExtension(new Uri(imagePath).AbsolutePath);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var tmp = Path.Combine(Path.GetTempPath(), $"fb-img-{Guid.NewGuid():N}{ext}");
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var bytes = await http.GetByteArrayAsync(imagePath);
                await File.WriteAllBytesAsync(tmp, bytes);
                imagePath = tmp;
                Console.WriteLine($"  · saved to {tmp}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ! Failed to download image: {ex.Message} (skipping attachment).");
                return;
            }
        }
        else if (!File.Exists(imagePath))
        {
            Console.WriteLine($"  ! Image not found: {imagePath} (skipping attachment).");
            return;
        }

        // Give the composer a moment to settle after group picker closes.
        await page.WaitForTimeoutAsync(1500);
        await Screenshot(page, article, "before-photo-btn");

        var photoBtn = await FirstVisible(page, PhotoButtons);
        if (photoBtn is null)
        {
            Console.WriteLine("  ! Photo/video button not found (skipping attachment).");
            await Screenshot(page, article, "photo-btn-fail");
            return;
        }

        var chooser = await page.RunAndWaitForFileChooserAsync(async () => await photoBtn.ClickAsync());
        await chooser.SetFilesAsync(imagePath);
        await page.WaitForTimeoutAsync(2500);
    }

    /// <summary>
    /// Type into Facebook's Lexical contenteditable composer reliably. First tries
    /// per-key typing; if the box is still empty (Lexical can drop key events when
    /// focus isn't settled), falls back to InsertText, which Lexical accepts via its
    /// beforeinput/insertText handler. Newlines are sent as Enter presses.
    /// </summary>
    private async Task<bool> FillComposerAsync(IPage page, ILocator textbox, string text)
    {
        int candidates = await page.Locator("div[role='dialog'] div[role='textbox']").CountAsync();
        Console.WriteLine($"  · composer textbox candidates in dialog: {candidates}");

        // If the composer editor isn't present we're likely still in the group picker.
        // Bail fast rather than hang 30s on a click that can never resolve.
        try { await textbox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 }); }
        catch { return false; }

        // Strategy 1: paste-style insertion — drop each line in one shot via InsertText
        // (Lexical accepts it as an insertText input event). This is near-instant; per-key
        // typing of a multi-paragraph post took ~12s. Line breaks are sent as Enter.
        await textbox.ClickAsync();
        await page.WaitForTimeoutAsync(300);
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) await page.Keyboard.PressAsync("Enter");
            if (lines[i].Length > 0) await page.Keyboard.InsertTextAsync(lines[i]);
        }
        await page.WaitForTimeoutAsync(400);
        var after1 = await SafeInner(textbox);
        Console.WriteLine($"  · after insert, box reads: \"{Trunc(after1)}\"");
        if (HasRealText(after1)) return true;

        // Strategy 2 (fallback): slower per-key typing, in case InsertText was dropped.
        Console.WriteLine("  · retrying via per-key typing…");
        await textbox.ClickAsync();
        await page.WaitForTimeoutAsync(250);
        await textbox.PressSequentiallyAsync(text, new() { Delay = 15 });
        await page.WaitForTimeoutAsync(500);
        var after2 = await SafeInner(textbox);
        Console.WriteLine($"  · after typing, box reads: \"{Trunc(after2)}\"");
        return HasRealText(after2);
    }

    private static async Task<string> SafeInner(ILocator textbox)
    {
        try { return await textbox.InnerTextAsync(); }
        catch (Exception ex) { return $"<read failed: {ex.Message}>"; }
    }

    private static bool HasRealText(string txt) =>
        !string.IsNullOrWhiteSpace(txt) && txt.Trim() != "Write something..." && !txt.StartsWith("<read failed");

    private static string Trunc(string s) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length > 60 ? s[..60].Replace("\n", "\\n") + "…" : s.Replace("\n", "\\n"));

    private static string BuildBody(string body, string? link)
    {
        var sb = new StringBuilder(body);
        if (!string.IsNullOrWhiteSpace(link))
        {
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(link);
        }
        return sb.ToString();
    }

    private static async Task<ILocator?> FirstVisible(IPage page, string[] selectors, int timeoutMs = 9000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var sel in selectors)
            {
                var loc = page.Locator(sel).First;
                try
                {
                    if (await loc.IsVisibleAsync()) return loc;
                }
                catch { /* try the next candidate */ }
            }
            await page.WaitForTimeoutAsync(300);
        }
        return null;
    }

    private async Task Jitter(IPage page)
    {
        var ms = _rng.Next(_cfg.MinDelayMs, Math.Max(_cfg.MinDelayMs + 1, _cfg.MaxDelayMs));
        await page.WaitForTimeoutAsync(ms);
    }

    /// <summary>A short, watchable pause between composer steps so each stage is visible.</summary>
    private async Task StepPause(IPage page)
    {
        if (_cfg.StepDelayMs > 0) await page.WaitForTimeoutAsync(_cfg.StepDelayMs);
    }

    private async Task Screenshot(IPage page, Article article, string tag)
    {
        try
        {
            Directory.CreateDirectory(_cfg.ScreenshotDir);
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var path = Path.Combine(_cfg.ScreenshotDir, $"{stamp}_{article.Id}_{tag}.png");
            await page.ScreenshotAsync(new() { Path = path, FullPage = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  (screenshot failed: {ex.Message})");
        }
    }
}
