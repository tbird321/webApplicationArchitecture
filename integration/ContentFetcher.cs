using System.Text;
using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Fetches the post text from the live article page instead of storing it in the plan.
/// These articles share a fixed shape — H1 title, an italic lede (the intro hook), body
/// sections, and a closing "Verdict" heading (the payoff). The post leads with the hook
/// (title + lede) and closes with the Verdict's first line to draw the reader to click.
/// Uses the already-open Chrome page (the site is client-rendered, so a plain HTTP GET
/// would only see the app shell).
/// </summary>
public static class ContentFetcher
{
    // Closing-hook headings, in preference order. The article's conclusion lives under
    // the first of these that exists.
    private static readonly string[] ClosingHeadings = { "Verdict", "Conclusion", "Summary", "The Bottom Line" };

    public static async Task<string?> FetchAsync(IPage page, string url, int timeoutMs = 15000)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            var h1 = page.Locator("h1").First;
            await h1.WaitForAsync(new() { Timeout = timeoutMs });
            var title = (await h1.InnerTextAsync()).Trim();

            // Intro hook: the italic lede paragraph right under the H1 (fallback: first <p>).
            string? lede = await TryText(page.Locator("h1 ~ p").First)
                        ?? await TryText(page.Locator("p").First);

            // Closing hook: the first body paragraph under the Verdict/Conclusion heading.
            string? verdict = await FirstParagraphUnderHeading(page, ClosingHeadings);

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(title)) sb.Append(title);
            if (!string.IsNullOrWhiteSpace(lede))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(lede);
            }
            // Only append the verdict if it adds something (not a near-duplicate of the lede).
            if (!string.IsNullOrWhiteSpace(verdict) && !LooksLikeSame(verdict, lede))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(verdict);
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the first paragraph that follows any of the given H2 headings (case-
    /// insensitive, matched as a substring). Uses XPath following:: so it survives the
    /// CMS wrapping each element in its own container.
    /// </summary>
    private static async Task<string?> FirstParagraphUnderHeading(IPage page, string[] headings)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        foreach (var h in headings)
        {
            var needle = h.ToLowerInvariant();
            var xpath = $"xpath=//h2[contains(translate(normalize-space(.), '{upper}', '{lower}'), '{needle}')]/following::p[1]";
            var txt = await TryText(page.Locator(xpath).First);
            if (!string.IsNullOrWhiteSpace(txt)) return txt;
        }
        return null;
    }

    // Cheap dedupe: treat two passages as "the same" if one contains a long prefix of the
    // other, so we don't print the lede twice when an article has no distinct verdict.
    private static bool LooksLikeSame(string a, string? b)
    {
        if (string.IsNullOrWhiteSpace(b)) return false;
        string Norm(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var na = Norm(a);
        var nb = Norm(b);
        if (na.Length == 0 || nb.Length == 0) return false;
        var shorter = na.Length <= nb.Length ? na : nb;
        var longer = na.Length <= nb.Length ? nb : na;
        var probe = shorter[..Math.Min(40, shorter.Length)];
        return longer.Contains(probe);
    }

    private static async Task<string?> TryText(ILocator loc)
    {
        try
        {
            if (await loc.IsVisibleAsync())
            {
                var t = (await loc.InnerTextAsync()).Trim();
                return string.IsNullOrWhiteSpace(t) ? null : t;
            }
        }
        catch { /* not present */ }
        return null;
    }
}
