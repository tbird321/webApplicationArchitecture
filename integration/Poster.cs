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
        "[aria-label='Photo/video']",
        "text=Photo/video",
    };

    private static readonly string[] MoreGroupsButtons =
    {
        "div[role='dialog'] [aria-label='Add groups']",
        "div[role='dialog'] div[role='button']:has-text('Add groups')",
        "text=Add groups",
        "text=Post to more groups",
        "[aria-label*='more groups']",
    };

    private static readonly string[] GroupSearchBoxes =
    {
        "input[placeholder*='Search']",
        "[aria-label*='Search'] input",
        "[aria-label='Search']",
    };

    private static readonly string[] ConfirmButtons =
    {
        "div[role='dialog'] [aria-label='Done']",
        "div[role='dialog'] div[role='button']:has-text('Done')",
    };

    private static readonly string[] PostButtons =
    {
        "div[role='dialog'] [aria-label='Post']",
        "div[role='dialog'] div[role='button']:has-text('Post')",
        "[aria-label='Post']",
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

        // Hook the batch's other groups FIRST. Facebook hides the "Add groups" control
        // as soon as the composer has text (unless an image is attached), so groups must
        // be added before we type anything.
        if (batch.Count > 1)
            await TryHookMoreGroups(page, article, batch.Skip(1).ToList());

        // Attach the image (if any) next — also keeps "Add groups" available — then type.
        if (!string.IsNullOrWhiteSpace(article.ImagePath))
            await AttachImage(page, article);

        if (!await FillComposerAsync(page, textbox, BuildBody(body, article.Link)))
        {
            Console.WriteLine("  ! Composer text box found but typing didn't land (Lexical editor).");
            await Screenshot(page, article, "empty-textbox");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(article.Link))
        {
            Console.WriteLine("  · waiting for link preview…");
            await page.WaitForTimeoutAsync(_cfg.LinkPreviewWaitMs);
        }

        await Screenshot(page, article, "prepared");
        Console.WriteLine("  ✓ composer filled. Confirm the groups, then post it yourself.");
        return true;
    }

    /// <summary>Full-auto only (opt-in). Clicks Post for you.</summary>
    public async Task<bool> SubmitAsync(IPage page, Article article, string groupUrl)
    {
        _ = groupUrl;
        var post = await FirstVisible(page, PostButtons);
        if (post is null)
        {
            Console.WriteLine("  ! Post button not found.");
            await Screenshot(page, article, "no-post-button");
            return false;
        }
        await post.ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await Screenshot(page, article, "submitted");
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
            return;
        }
        await moreBtn.ClickAsync();
        await page.WaitForTimeoutAsync(1500);

        // Capture the picker the instant it opens, before touching anything.
        await Screenshot(page, article, "group-picker");

        // The picker is split across two dialogs: a header bar ("Add groups") and the
        // scrollable LIST that actually holds the group rows + checkboxes. Target the list
        // (identified by its "Select groups" text) — NOT the header that .Last would grab.
        var dialog = page.GetByRole(AriaRole.Dialog).Filter(new() { HasText = "Select groups" }).First;
        try { await dialog.WaitForAsync(new() { Timeout = 5000 }); }
        catch { dialog = page.Locator("div[role='dialog']").Last; }

        foreach (var g in others)
        {
            bool ok = await TickGroupAsync(dialog, g.Name);
            Console.WriteLine(ok
                ? $"      ✓ selected: {g.Name}"
                : $"      ! couldn't auto-select '{g.Name}' — tick it manually in the picker.");
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
    private static async Task<bool> TickGroupAsync(ILocator dialog, string name)
    {
        // Exact leaf match so "Christianity Vs Mormonism" doesn't also hit
        // "Christianity Vs Mormonism Discussion".
        var nameSpan = dialog.GetByText(name, new() { Exact = true }).First;
        try { await nameSpan.ScrollIntoViewIfNeededAsync(new() { Timeout = 3000 }); }
        catch { return false; }

        // Each row's toggle is a real <input type="checkbox"> (visually hidden, so Force).
        var checkbox = nameSpan
            .Locator("xpath=ancestor::div[.//input[@type='checkbox']][1]//input[@type='checkbox']")
            .First;
        try
        {
            if (await checkbox.CountAsync() > 0)
            {
                if (await IsCheckedNow(checkbox)) return true;
                await checkbox.CheckAsync(new() { Force = true });
                if (await IsCheckedNow(checkbox)) return true;
            }
        }
        catch { /* fall through to row click */ }

        // Fallback: click the whole row container.
        try
        {
            var row = nameSpan.Locator("xpath=ancestor::div[.//input[@type='checkbox']][1]").First;
            await row.ClickAsync(new() { Timeout = 3000, Force = true });
            return await IsCheckedNow(checkbox);
        }
        catch { return false; }
    }

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
        if (!File.Exists(article.ImagePath))
        {
            Console.WriteLine($"  ! Image not found: {article.ImagePath} (skipping attachment).");
            return;
        }

        var photoBtn = await FirstVisible(page, PhotoButtons);
        if (photoBtn is null)
        {
            Console.WriteLine("  ! Photo/video button not found (skipping attachment).");
            return;
        }

        var chooser = await page.RunAndWaitForFileChooserAsync(async () => await photoBtn.ClickAsync());
        await chooser.SetFilesAsync(article.ImagePath!);
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
