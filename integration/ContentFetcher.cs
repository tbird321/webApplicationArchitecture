using Microsoft.Playwright;

namespace FacebookPoster;

/// <summary>
/// Fetches the post text from the live article page instead of storing it in the plan.
/// The post leads with the hook — the title, the italic subtitle/lede, and the full first
/// paragraph — and, on articles that have one, closes with the "Verdict" line as the
/// payoff. Uses the already-open Chrome page (the site is client-rendered, so a plain HTTP
/// GET would only see the app shell).
/// </summary>
public static class ContentFetcher
{
    public static async Task<string?> FetchAsync(IPage page, string url, int timeoutMs = 15000)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator("h1").First.WaitForAsync(new() { Timeout = timeoutMs });

            // One DOM pass over the rendered page: the first NON-EMPTY <h1> (some articles
            // have a stray empty heading first), the first two non-empty paragraphs (the
            // subtitle/lede + the full first body paragraph), and the first paragraph under
            // a Verdict/Conclusion-style heading if the article has one.
            var data = await page.EvaluateAsync<string[]>(@"() => {
                const txt = el => (el && el.innerText ? el.innerText : '').replace(/\s+/g, ' ').trim();
                // Anchor on the first non-empty <h1> (the article title). Everything we want
                // comes AFTER it; restricting to following elements drops the site header
                // chrome (nav, a 'Login' button, etc.) that would otherwise pollute the text.
                const h1s = [...document.querySelectorAll('h1')];
                const titleEl = h1s.find(h => txt(h).length > 0) || h1s[0] || null;
                const title = titleEl ? txt(titleEl) : '';
                const after = el => titleEl
                    ? !!(titleEl.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING)
                    : true;
                const ps = [...document.querySelectorAll('p')].filter(after).map(txt).filter(t => t.length > 1);
                const lede = ps[0] || '';
                const firstBody = ps[1] || '';
                const firstParaAfter = (h2) => {
                    let n = h2 ? h2.nextElementSibling : null;
                    while (n && n.tagName !== 'P') n = n.nextElementSibling;
                    return n ? txt(n) : '';
                };
                const h2s = [...document.querySelectorAll('h2')].filter(after);
                const keys = ['verdict', 'conclusion', 'summary', 'the bottom line'];
                // Closing hook: the first paragraph under a Verdict/Conclusion-style heading;
                // if there is none, fall back to the first paragraph of the LAST section.
                let verdict = '';
                const closing = h2s.find(h2 => keys.some(k => txt(h2).toLowerCase().includes(k)));
                if (closing) verdict = firstParaAfter(closing);
                else if (h2s.length) verdict = firstParaAfter(h2s[h2s.length - 1]);
                return [title, lede, firstBody, verdict];
            }");

            var (title, lede, firstBody, verdict) = (data[0], data[1], data[2], data[3]);

            var parts = new List<string>();
            void Add(string s)
            {
                if (!string.IsNullOrWhiteSpace(s) && !parts.Any(p => LooksLikeSame(s, p)))
                    parts.Add(s);
            }
            Add(title);
            Add(lede);
            Add(firstBody);
            Add(verdict);

            return parts.Count > 0 ? string.Join("\n\n", parts) : null;
        }
        catch
        {
            return null;
        }
    }

    // Cheap dedupe: treat two passages as "the same" if one contains a long prefix of the
    // other, so a repeated lede/verdict isn't printed twice.
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
}
