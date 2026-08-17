namespace FacebookPoster;

/// <summary>
/// Rewrites the legacy <c>?page=Slug</c> article URL to the site's canonical form.
///
/// The old form still works, but it 301-redirects — and a redirect is worth avoiding in a
/// Facebook post: the scraper follows it to build the preview card, the URL a reader sees
/// isn't the one they land on, and the link equity goes to the redirect rather than the
/// page. Verified against the live sites (and their own <c>&lt;link rel="canonical"&gt;</c>):
///
///   https://www.ldsapologetics.com/?page=AbrahamsTest
///     → 301 → https://www.ldsapologetics.com/abrahamstest/
///
/// So the canonical shape is <c>https://www.host/{lowercased-slug}/</c>. Two details that
/// matter: the slug is <b>lowercased</b>, and the <b>www</b> host is required — the apex
/// returns 404 for article paths, so an apex link must be upgraded, not just rewritten.
/// </summary>
public static class Links
{
    /// <summary>
    /// The canonical form of <paramref name="url"/>, or the input unchanged when it isn't
    /// the legacy <c>?page=</c> shape (already-canonical links, other query strings, and
    /// anything unparseable are all left exactly as they are).
    /// </summary>
    public static string? Canonical(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        Uri uri;
        try { uri = new Uri(url.Trim()); }
        catch { return url; }                                  // not a URL we understand

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return url;

        // Legacy form only: the slug lives in the query of the site root, e.g. "/?page=Foo".
        if (uri.AbsolutePath is not ("" or "/")) return url;

        var slug = SinglePageParam(uri.Query);
        if (slug is null) return url;

        var host = uri.Host;
        // Article paths only resolve on the www host; the apex 404s. Add www to a bare
        // two-label domain, and leave any other subdomain (admin., staging., …) alone.
        if (host.Count(c => c == '.') == 1) host = "www." + host;

        var canonical = $"https://{host}/{slug.ToLowerInvariant()}/";
        return string.IsNullOrEmpty(uri.Fragment) ? canonical : canonical + uri.Fragment;
    }

    /// <summary>True if the URL would change under <see cref="Canonical"/>.</summary>
    public static bool IsLegacy(string? url) =>
        !string.IsNullOrWhiteSpace(url) && !string.Equals(Canonical(url), url, StringComparison.Ordinal);

    /// <summary>
    /// The slug from a query that is exactly one <c>page=</c> parameter, or null. Anything
    /// with extra parameters is left alone rather than guessed at — dropping a parameter we
    /// don't understand could change which page the link opens.
    /// </summary>
    private static string? SinglePageParam(string query)
    {
        var q = query.TrimStart('?');
        if (q.Length == 0) return null;
        if (q.Contains('&')) return null;

        var eq = q.IndexOf('=');
        if (eq <= 0) return null;
        if (!q[..eq].Equals("page", StringComparison.OrdinalIgnoreCase)) return null;

        var slug = Uri.UnescapeDataString(q[(eq + 1)..]).Trim().Trim('/');
        return slug.Length == 0 ? null : slug;
    }

    /// <summary>
    /// Rewrite every article link in the plan to canonical form. Returns the (old, new)
    /// pairs that changed, so the caller can report them and decide whether to save.
    /// </summary>
    public static List<(string Id, string From, string To)> NormalizePlan(Plan plan)
    {
        var changed = new List<(string, string, string)>();
        foreach (var a in plan.Articles)
        {
            var canon = Canonical(a.Link);
            if (canon is not null && a.Link is not null && !string.Equals(canon, a.Link, StringComparison.Ordinal))
            {
                changed.Add((a.Id, a.Link, canon));
                a.Link = canon;
            }
        }
        return changed;
    }
}
