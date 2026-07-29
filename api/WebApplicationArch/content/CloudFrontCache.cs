using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Lambda.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplicationArch.content
{
    /// <summary>
    /// Outcome of an invalidation attempt. Never throws for an operational failure -- the
    /// callers that invalidate as a side effect (sitemap regeneration, bulk re-render) have
    /// already written to S3 by the time this runs, and must not report failure for a CDN
    /// call that is only an optimisation. Callers that invalidate as their PRIMARY job
    /// inspect Succeeded and decide for themselves.
    /// </summary>
    public class InvalidationResult
    {
        public bool Attempted { get; set; }
        public bool Succeeded { get; set; }
        public string DistributionId { get; set; }
        public string InvalidationId { get; set; }
        public string Error { get; set; }
        public IReadOnlyList<string> Paths { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Finds a site's public CloudFront distribution and invalidates paths on it.
    ///
    /// Static pages are uploaded with Cache-Control: max-age=300, so an ordinary edit becomes
    /// visible on its own within five minutes and needs nothing from this. This exists for the
    /// cases where waiting is wrong: a batch of edits you want live now, a change to a file
    /// served with a longer TTL, or a rollback.
    ///
    /// COST: CloudFront bills per invalidation PATH beyond 1,000 per month. "/*" counts as a
    /// single path no matter how much it clears, so a whole-site flush is the CHEAP option --
    /// listing pages individually is what gets expensive.
    /// </summary>
    public static class CloudFrontCache
    {
        /// <summary>CloudFront's own per-request limit for a wildcard-bearing batch is 15;
        /// this cap is lower than the plain-path limit (3,000) on purpose, because anything
        /// approaching it should be a "/*" flush instead.</summary>
        public const int MaxPaths = 50;

        // Distribution ids never change for a live site, and ListDistributions is the slowest
        // part of the call, so a warm container answers the second request without it.
        private static readonly ConcurrentDictionary<string, string> _distributionByDomain =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Turn caller input into a valid, deduplicated CloudFront path list.
        ///
        /// Accepts a full URL as well as a path, because that is what you have in your hand
        /// when you have just looked at the page you want refreshed.
        /// </summary>
        /// <exception cref="ArgumentException">More than <see cref="MaxPaths"/> distinct paths.</exception>
        public static IReadOnlyList<string> NormalizePaths(IEnumerable<string> paths)
        {
            var cleaned = new List<string>();
            foreach (var raw in paths ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var p = raw.Trim();

                // "https://www.example.com/about-us/" -> "/about-us/"
                if (Uri.TryCreate(p, UriKind.Absolute, out var abs) &&
                    (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
                {
                    p = abs.AbsolutePath;
                }

                if (!p.StartsWith("/")) p = "/" + p;
                if (p.Length > 1) p = p.TrimEnd();
                if (string.IsNullOrWhiteSpace(p) || p == "/") { p = "/"; }

                if (!cleaned.Contains(p, StringComparer.Ordinal)) cleaned.Add(p);
            }

            // Nothing asked for means flush everything -- the common case, and the cheap one.
            if (cleaned.Count == 0) return new[] { "/*" };

            // "/*" supersedes every other entry, and collapsing to it also drops the bill for
            // this request to a single path.
            if (cleaned.Contains("/*", StringComparer.Ordinal)) return new[] { "/*" };

            if (cleaned.Count > MaxPaths)
            {
                throw new ArgumentException(
                    $"{cleaned.Count} paths requested; the limit is {MaxPaths}. Invalidate \"/*\" instead -- " +
                    "it clears the whole distribution and CloudFront bills it as one path.");
            }

            return cleaned;
        }

        /// <summary>
        /// Resolve the distribution serving www.{domain} (or the bare domain). Returns null
        /// when no distribution carries either alias.
        /// </summary>
        public static async Task<string> FindDistributionIdAsync(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return null;
            if (_distributionByDomain.TryGetValue(domain, out var cached)) return cached;

            string aliasWww = $"www.{domain}";
            using var cf = new AmazonCloudFrontClient();

            // ListDistributions pages at 100. Six sites have a public and an admin distribution
            // each, so one page covers it today -- but a silent miss here looks exactly like
            // "no distribution exists", which is a confusing thing to debug.
            string marker = null;
            do
            {
                var resp = await cf.ListDistributionsAsync(new ListDistributionsRequest { Marker = marker });
                var list = resp?.DistributionList;
                var match = list?.Items?.FirstOrDefault(d =>
                    d.Aliases?.Items != null &&
                    d.Aliases.Items.Any(a =>
                        string.Equals(a, aliasWww, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, domain, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    _distributionByDomain[domain] = match.Id;
                    return match.Id;
                }

                marker = (list != null && list.IsTruncated == true) ? list.NextMarker : null;
            } while (!string.IsNullOrEmpty(marker));

            return null;
        }

        /// <summary>
        /// Invalidate <paramref name="paths"/> on the distribution serving <paramref name="domain"/>.
        /// Operational failures are reported in the result, not thrown.
        /// </summary>
        /// <param name="callerPrefix">Short tag identifying the caller, for the CallerReference.</param>
        public static async Task<InvalidationResult> InvalidateAsync(
            string domain, IEnumerable<string> paths, string callerPrefix, ILambdaContext context = null)
        {
            var result = new InvalidationResult { Attempted = true };
            try
            {
                result.Paths = NormalizePaths(paths);

                var distributionId = await FindDistributionIdAsync(domain);
                if (string.IsNullOrEmpty(distributionId))
                {
                    result.Error = $"No CloudFront distribution found with alias 'www.{domain}' or '{domain}'.";
                    return result;
                }
                result.DistributionId = distributionId;

                using var cf = new AmazonCloudFrontClient();
                var resp = await cf.CreateInvalidationAsync(new CreateInvalidationRequest
                {
                    DistributionId = distributionId,
                    InvalidationBatch = new InvalidationBatch
                    {
                        // Must be unique per request or CloudFront returns the ORIGINAL
                        // invalidation instead of starting a new one.
                        CallerReference = $"{callerPrefix}-{domain}-{DateTime.UtcNow.Ticks}",
                        Paths = new Paths
                        {
                            Quantity = result.Paths.Count,
                            Items = result.Paths.ToList()
                        }
                    }
                });

                result.InvalidationId = resp?.Invalidation?.Id;
                result.Succeeded = true;
            }
            catch (ArgumentException ex)
            {
                // Bad input from the caller, not an AWS failure -- surfaced the same way so a
                // side-effect caller still cannot be broken by it.
                result.Error = ex.Message;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                context?.Logger?.LogError($"CloudFront invalidation failed for {domain}: {ex.Message}");
            }
            return result;
        }
    }
}
