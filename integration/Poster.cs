using System.Text;
using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>Outcome of waiting for you to click Post.</summary>
public enum PostWait { Posted, Skipped, TimedOut }

/// <summary>
/// Drives Facebook's group composer, ONE GROUP AT A TIME: a fresh native post in each
/// group. The "post to more groups" picker is deliberately not used — hooking many
/// groups onto a single post is what trips Facebook's rapid-selection rate limit, and
/// a native per-group post also reaches each group's feed on its own terms.
///
/// It only PREPARES the post — fills text/link/image — then leaves YOU to verify and
/// click "Post".
///
/// Facebook's DOM is obfuscated and changes often, so selectors are candidate lists
/// matched by role/aria/visible-text. Expect to tune these; --dry-run + the
/// screenshots make that quick.
/// </summary>
public sealed class Poster
{
    private readonly Config _cfg;
    private readonly Random _rng = new();

    /// <summary>Remote image URL → local temp file, so a given image is downloaded once
    /// per run and reused across every group instead of re-fetched for each post.</summary>
    private readonly Dictionary<string, string> _imageCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Clipboard permission is a context-level grant, so ask for it once per run
    /// rather than before every group's paste.</summary>
    private bool _clipboardGranted;

    public Poster(Config cfg) => _cfg = cfg;

    /// <summary>Where groups whose composer couldn't be prepared get recorded for review.</summary>
    private string FailLogPath => Path.Combine(Path.GetDirectoryName(_cfg.QueueFile) ?? ".", "failed-groups.log");

    /// <summary>Append a group we couldn't compose in (with the article and reason) to the fail log.</summary>
    private void LogFailedGroup(Article article, Group group, string reason)
    {
        try
        {
            var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllLines(FailLogPath, new[] { $"{stamp}\t[{article.Id}]\t{reason}\t{group.Name}\t{group.Url}" });
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

    // The composer's submit button. Matched by aria-label / exact text only: a loose
    // has-text('Post') also matches other composer chrome, and mis-clicking in a dialog
    // that is about to publish is the one mistake with no undo.
    private static readonly string[] PostButtons =
    {
        "div[role='dialog'] div[role='button'][aria-label='Post']",
        "div[aria-label='Create post'] div[role='button'][aria-label='Post']",
        "div[role='dialog'] [role='button'][aria-label='Post']",
        "div[role='dialog'] div[role='button']:text-is('Post')",
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

    /// <summary>
    /// Compose one post in one group. Returns false if the composer couldn't be opened
    /// or filled. Does NOT submit — you click Post.
    /// </summary>
    public async Task<bool> PrepareAsync(IPage page, Article article, Group group, string body)
    {
        Console.WriteLine($"\n→ [{article.Id}] composing in: {group.Name}");
        await page.GotoAsync(group.Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Jitter(page);

        var trigger = await FirstVisible(page, ComposerTriggers);
        if (trigger is null)
        {
            Console.WriteLine("  ! Couldn't find the composer ('Write something') on this page.");
            await Screenshot(page, article, "no-composer");
            LogFailedGroup(article, group, "no-composer");
            return false;
        }
        await MoveAndClickAsync(page, trigger);

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
            LogFailedGroup(article, group, "no-textbox");
            return false;
        }

        await StepPause(page);

        // Attach the image before typing: the Photo/video button sits in the composer's
        // footer, which Facebook reflows once the box has content and a link preview.
        if (!string.IsNullOrWhiteSpace(article.ImagePath))
        {
            await AttachImage(page, article);
            await StepPause(page);
        }

        if (!await FillComposerAsync(page, textbox, BuildBody(body, article.Link)))
        {
            Console.WriteLine("  ! Composer text box found but typing didn't land (Lexical editor).");
            await Screenshot(page, article, "empty-textbox");
            LogFailedGroup(article, group, "empty-textbox");
            return false;
        }
        await StepPause(page);

        if (!string.IsNullOrWhiteSpace(article.Link))
        {
            // The URL rides along in the paste as the last line; confirm it survived, then
            // give Facebook time to render its preview card from it.
            await EnsureLinkAtEndAsync(page, textbox, article.Link!);
            Console.WriteLine("  · waiting for link preview…");
            await page.WaitForTimeoutAsync(_cfg.LinkPreviewWaitMs);
        }

        await Screenshot(page, article, "prepared");
        Console.WriteLine("  ✓ composer filled. Give it a look, then post it yourself.");
        return true;
    }

    /// <summary>
    /// Click the composer's Post button after a randomized pause — the beat a person takes
    /// to read what they wrote before submitting. Waits for the button to be ENABLED first
    /// (Facebook disables it while an image upload or link preview is still resolving), then
    /// re-resolves it after the pause in case the dialog re-rendered. Returns false if no
    /// enabled Post button ever appeared, so the caller can hand the click back to you.
    /// </summary>
    public async Task<bool> ClickPostAsync(IPage page, Article article)
    {
        var btn = await FirstEnabled(page, PostButtons, 30000);
        if (btn is null)
        {
            Console.WriteLine("  ! no enabled Post button found (still uploading, or the selector drifted).");
            await Screenshot(page, article, "no-post-button");
            return false;
        }

        int lo = Math.Max(0, _cfg.PreClickPostMinMs);
        int wait = _rng.Next(lo, Math.Max(lo + 1, _cfg.PreClickPostMaxMs));
        Console.WriteLine($"  · reading it over for {wait / 1000.0:0.#}s, then clicking Post…");
        await page.WaitForTimeoutAsync(wait);

        // Re-resolve: the pause is long enough for a preview card to land and re-render the
        // dialog, which would leave the old handle detached.
        btn = await FirstEnabled(page, PostButtons, 5000) ?? btn;
        try
        {
            await MoveAndClickAsync(page, btn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! clicking Post failed ({ex.Message}) — click it yourself.");
            await Screenshot(page, article, "post-click-failed");
            return false;
        }
        Console.WriteLine("  · clicked Post.");
        return true;
    }

    // Wording Facebook uses when it rate-limits posting. Matched ONLY inside a dialog/alert —
    // these group feeds are full of members talking about being blocked, and scanning the
    // whole page would abort the run on someone else's post text.
    private static readonly string[] BlockMarkers =
    {
        "temporarily blocked",
        "action was blocked",
        "can't use this feature",
        "cannot use this feature",
        "we limit how often",
        "try again later",
        "you're posting too",
    };

    /// <summary>
    /// Look for Facebook's rate-limit notice in an open dialog/alert. Returns the phrase
    /// that matched, or null. The caller stops the run on a hit: once you're blocked, the
    /// remaining groups will fail anyway and pushing on makes the block worse.
    /// </summary>
    public async Task<string?> DetectBlockAsync(IPage page)
    {
        try
        {
            var text = await page.EvaluateAsync<string>(@"() => [...document.querySelectorAll(
                ""div[role='dialog'], div[role='alertdialog'], div[role='alert']"")]
                .map(d => d.innerText || '').join('\n').toLowerCase()");
            if (string.IsNullOrWhiteSpace(text)) return null;
            foreach (var m in BlockMarkers)
                if (text.Contains(m, StringComparison.OrdinalIgnoreCase)) return m;
        }
        catch { /* unreadable page — treat as no block */ }
        return null;
    }

    /// <summary>First visible candidate that is not aria-disabled, or null on timeout.</summary>
    private static async Task<ILocator?> FirstEnabled(IPage page, string[] selectors, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var sel in selectors)
            {
                var loc = page.Locator(sel).First;
                try
                {
                    if (!await loc.IsVisibleAsync()) continue;
                    if (await loc.GetAttributeAsync("aria-disabled") == "true") continue;
                    return loc;
                }
                catch { /* try the next candidate */ }
            }
            await page.WaitForTimeoutAsync(400);
        }
        return null;
    }

    /// <summary>
    /// Block until the "Create post" composer closes — i.e. YOU clicked Post (or discarded
    /// it) — so the run can advance to the next group with no Enter needed. Polls the
    /// dialog's visibility; calls <paramref name="shouldSkip"/> each tick so the caller can
    /// offer a keyboard skip. Returns Posted when the dialog goes away, Skipped if the
    /// caller bails, or TimedOut after <paramref name="timeoutMs"/>.
    /// </summary>
    public async Task<PostWait> WaitForComposerClosedAsync(IPage page, Func<bool> shouldSkip, int timeoutMs = 600_000)
    {
        var dialog = page.Locator("div[role='dialog'][aria-label='Create post'], div[aria-label='Create post']").First;

        // First confirm the composer is actually open, so a momentary read-miss right after
        // prepare can't be mistaken for "posted". If it never opens we still fall through.
        var openDeadline = DateTime.UtcNow.AddMilliseconds(8000);
        bool sawOpen = false;
        while (DateTime.UtcNow < openDeadline)
        {
            if (shouldSkip()) return PostWait.Skipped;
            try { if (await dialog.IsVisibleAsync()) { sawOpen = true; break; } } catch { }
            await page.WaitForTimeoutAsync(250);
        }
        if (!sawOpen)
            Console.WriteLine("  (note: never saw the composer open — watching for it to close anyway.)");

        // Now wait for it to disappear: that's the click-Post signal.
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (shouldSkip()) return PostWait.Skipped;
            bool visible;
            try { visible = await dialog.IsVisibleAsync(); }
            catch { visible = false; }
            if (!visible)
            {
                // Settle a beat so a transient re-render mid-submit doesn't double-count.
                await page.WaitForTimeoutAsync(600);
                try { if (await dialog.IsVisibleAsync()) continue; } catch { }
                return PostWait.Posted;
            }
            await page.WaitForTimeoutAsync(400);
        }
        return PostWait.TimedOut;
    }

    private async Task AttachImage(IPage page, Article article)
    {
        var imagePath = article.ImagePath!;

        // If it's a URL, download it to a temp file first — but only once per run. The same
        // image goes to every group, so reuse the file we already pulled instead of re-fetching.
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (_imageCache.TryGetValue(imagePath, out var cached) && File.Exists(cached))
            {
                Console.WriteLine($"  · reusing downloaded image: {cached}");
                imagePath = cached;
            }
            else
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
                    _imageCache[imagePath] = tmp;
                    Console.WriteLine($"  · saved to {tmp}");
                    imagePath = tmp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ! Failed to download image: {ex.Message} (skipping attachment).");
                    return;
                }
            }
        }
        else if (!File.Exists(imagePath))
        {
            Console.WriteLine($"  ! Image not found: {imagePath} (skipping attachment).");
            return;
        }

        // Give the composer a moment to settle before reaching for its footer buttons.
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
    /// Put the whole post into Facebook's Lexical composer as one PASTE — the text (body,
    /// blank line, then the article URL as the last line) goes on the clipboard and we press
    /// Ctrl+V, exactly like drafting it elsewhere and pasting it in. Lexical's paste handler
    /// keeps the paragraph breaks. Two fallbacks in case the real key event never reaches the
    /// editor: a synthetic paste event carrying the same clipboard data, then a direct
    /// InsertText per line.
    /// </summary>
    private async Task<bool> FillComposerAsync(IPage page, ILocator textbox, string text)
    {
        int candidates = await page.Locator("div[role='dialog'] div[role='textbox']").CountAsync();
        Console.WriteLine($"  · composer textbox candidates in dialog: {candidates}");

        // If the composer editor isn't present we're likely still mid-animation. Bail fast
        // rather than hang 30s on a click that can never resolve.
        try { await textbox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 }); }
        catch { return false; }

        await MoveAndClickAsync(page, textbox);
        await page.WaitForTimeoutAsync(_rng.Next(400, 900));

        // Strategy 1: the real thing — clipboard + Ctrl+V, one shot.
        Console.WriteLine($"  · pasting {text.Length} chars (Ctrl+V)…");
        if (await CopyToClipboardAsync(page, text))
        {
            await page.Keyboard.PressAsync("Control+V");
            await page.WaitForTimeoutAsync(900);
            var after1 = await SafeInner(textbox);
            Console.WriteLine($"  · after paste, box reads: \"{Trunc(after1)}\"");
            if (HasRealText(after1)) return true;
            Console.WriteLine("  · Ctrl+V didn't land — retrying with a synthetic paste event…");
        }
        else
        {
            Console.WriteLine("  · couldn't write to the clipboard — using a synthetic paste event…");
        }

        // Strategy 2: dispatch a paste event carrying the text as clipboardData. Still a
        // paste as far as Lexical is concerned, just without the OS clipboard round trip.
        if (await SyntheticPasteAsync(textbox, text))
        {
            await page.WaitForTimeoutAsync(700);
            var after2 = await SafeInner(textbox);
            Console.WriteLine($"  · after synthetic paste, box reads: \"{Trunc(after2)}\"");
            if (HasRealText(after2)) return true;
        }

        // Strategy 3: last resort — insert line by line, which Lexical accepts through its
        // beforeinput/insertText handler.
        Console.WriteLine("  · retrying via direct insert…");
        await textbox.ClickAsync();
        await page.WaitForTimeoutAsync(300);
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) await page.Keyboard.PressAsync("Enter");
            if (lines[i].Length > 0) await page.Keyboard.InsertTextAsync(lines[i]);
        }
        await page.WaitForTimeoutAsync(500);
        var after3 = await SafeInner(textbox);
        Console.WriteLine($"  · after insert, box reads: \"{Trunc(after3)}\"");
        return HasRealText(after3);
    }

    /// <summary>
    /// Put text on the real clipboard from inside the page, so the Ctrl+V that follows
    /// pastes it. Needs clipboard-write permission on the context, granted once per run;
    /// returns false if the browser refuses (e.g. the window isn't focused) so the caller
    /// can fall back.
    /// </summary>
    private async Task<bool> CopyToClipboardAsync(IPage page, string text)
    {
        if (!_clipboardGranted)
        {
            try
            {
                await page.Context.GrantPermissionsAsync(
                    new[] { "clipboard-read", "clipboard-write" },
                    new() { Origin = "https://www.facebook.com" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  (clipboard permission not granted: {ex.Message})");
            }
            _clipboardGranted = true;   // don't re-attempt the grant for every group
        }

        try
        {
            return await page.EvaluateAsync<bool>(@"async (t) => {
                try { await navigator.clipboard.writeText(t); return true; }
                catch (e) { return false; }
            }", text);
        }
        catch { return false; }
    }

    /// <summary>Fire a paste event at the editor with the text as its clipboardData — the
    /// same event Ctrl+V produces, minus the OS clipboard round trip.</summary>
    private static async Task<bool> SyntheticPasteAsync(ILocator textbox, string text)
    {
        try
        {
            return await textbox.EvaluateAsync<bool>(@"(el, t) => {
                const dt = new DataTransfer();
                dt.setData('text/plain', t);
                el.focus();
                return el.dispatchEvent(new ClipboardEvent('paste', {
                    clipboardData: dt, bubbles: true, cancelable: true,
                }));
            }", text);
        }
        catch { return false; }
    }

    /// <summary>
    /// Make sure the article URL really is the last line of the composer. The paste
    /// normally carries it; this repairs the case where Lexical's link handling swallowed
    /// it, by moving to the end and typing it in.
    /// </summary>
    private async Task EnsureLinkAtEndAsync(IPage page, ILocator textbox, string link)
    {
        var current = await SafeInner(textbox);
        if (ContainsUrl(current, link))
        {
            Console.WriteLine($"  · source url present as the last line: {link}");
            return;
        }

        Console.WriteLine("  · url missing after paste — appending it at the end…");
        try
        {
            await textbox.ClickAsync();
            await page.Keyboard.PressAsync("Control+End");
            await page.Keyboard.PressAsync("Enter");
            await page.Keyboard.PressAsync("Enter");
            await page.Keyboard.InsertTextAsync(link);
            await page.WaitForTimeoutAsync(500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ! couldn't append the url ({ex.Message}) — add it before posting.");
            return;
        }

        if (!ContainsUrl(await SafeInner(textbox), link))
            Console.WriteLine("  ! the url still isn't in the box — add it before posting.");
    }

    /// <summary>Tolerant URL presence check: ignores scheme, www, and a trailing slash,
    /// since Facebook normalizes what it shows in the box.</summary>
    private static bool ContainsUrl(string haystack, string url)
    {
        static string Core(string s) => (s ?? "")
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("www.", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
        var needle = Core(url.Trim());
        return needle.Length > 0 && Core(haystack).Contains(needle, StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// Assemble what gets pasted: the fetched/overridden body, then a blank line, then the
    /// article URL as the LAST line — that's what Facebook builds its preview card from, and
    /// it's the link readers click. Any earlier occurrence of the same URL in the body is
    /// stripped so it appears exactly once, at the bottom.
    /// </summary>
    private static string BuildBody(string body, string? link)
    {
        var text = (body ?? "").TrimEnd();
        if (string.IsNullOrWhiteSpace(link)) return text;

        var url = link.Trim();
        var cleaned = text.Replace(url, "", StringComparison.OrdinalIgnoreCase).TrimEnd();
        var sb = new StringBuilder(cleaned);
        if (sb.Length > 0) sb.Append("\n\n");
        sb.Append(url);
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

    /// <summary>
    /// Click an element the way a person would: move a real cursor to it along a short
    /// stepped path, hover a beat, then press at that point — no teleport, no forced
    /// synthetic click. Falls back to a plain click if the box can't be measured or
    /// humanizing is turned off.
    /// </summary>
    private async Task MoveAndClickAsync(IPage page, ILocator loc)
    {
        if (_cfg.HumanizeInput)
        {
            try
            {
                await loc.ScrollIntoViewIfNeededAsync(new() { Timeout = 3000 });
                var box = await loc.BoundingBoxAsync();
                if (box is not null)
                {
                    // Aim at a random interior point, not the dead centre.
                    float tx = box.X + box.Width  * (float)(0.30 + _rng.NextDouble() * 0.40);
                    float ty = box.Y + box.Height * (float)(0.30 + _rng.NextDouble() * 0.40);
                    // Approach via a waypoint above the target so the path isn't a straight line.
                    float wx = box.X + box.Width * (float)_rng.NextDouble();
                    float wy = Math.Max(0, box.Y - _rng.Next(10, 70));

                    await page.Mouse.MoveAsync(wx, wy, new() { Steps = _rng.Next(6, 14) });
                    await page.Mouse.MoveAsync(tx, ty, new() { Steps = _rng.Next(12, 30) });
                    await page.WaitForTimeoutAsync(_rng.Next(90, 320));   // hover dwell
                    await page.Mouse.DownAsync();
                    await page.WaitForTimeoutAsync(_rng.Next(40, 120));   // press duration
                    await page.Mouse.UpAsync();
                    return;
                }
            }
            catch { /* fall back to a plain click */ }
        }
        await loc.ClickAsync();
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
