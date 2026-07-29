using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebApplicationArch.content;

namespace WebApplicationArch
{
    /// <summary>
    /// Manual CDN cache control.
    ///
    /// Static pages carry Cache-Control: max-age=300, so a single edit reaches the public site
    /// on its own within five minutes and needs nothing from this endpoint. It exists for the
    /// case that motivated it: a BATCH of edits you want live now, without opening the AWS
    /// console to do it.
    ///
    /// Deliberately database-free. The only thing it needs is website id -> domain, which
    /// StaticPageRenderer already holds for every migrated site, so this endpoint has no RDS
    /// dependency, no Secrets Manager permission, and no cold-start database connection.
    /// </summary>
    public class ApiCacheFunctions : ApiBaseFunctions
    {
        private class InvalidateRequest
        {
            public List<string> paths { get; set; }
        }

        /// <summary>
        /// POST /cache/invalidate?websiteId={id}
        /// Body (optional): { "paths": ["/about-us/", "https://www.site.com/x/"] }
        /// Omitted or empty paths means "/*" -- the whole distribution, which is also the
        /// cheapest option since CloudFront bills "/*" as one path.
        /// </summary>
        public async Task<APIGatewayProxyResponse> InvalidateCache(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Every other endpoint on this API is unauthenticated, which is a known gap
                // (SECURITYISSUES.md). This one is checked regardless, because unlike the
                // others it COSTS MONEY per call once the free 1,000 paths/month are used.
                if (!IsAuthorized(request))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.Unauthorized,
                        Body = JsonConvert.SerializeObject(new { error = "unauthorized", message = "Valid X-API-Key header required." }),
                        Headers = PostHeaders
                    };
                }

                if (request.QueryStringParameters == null
                    || !request.QueryStringParameters.ContainsKey("websiteId")
                    || !int.TryParse(request.QueryStringParameters["websiteId"], out int websiteId))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = JsonConvert.SerializeObject(new { error = "missing_website_id", message = "Missing or invalid 'websiteId' query parameter." }),
                        Headers = PostHeaders
                    };
                }

                if (!StaticPageRenderer.TryGetSiteDomain(websiteId, out string domain))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = JsonConvert.SerializeObject(new
                        {
                            error = "unknown_website_id",
                            websiteId,
                            known = StaticPageRenderer.KnownSiteIds.OrderBy(i => i).ToArray(),
                            message = "This website id is not in the static-site registry (StaticPageRenderer.SiteMetaById). Add it there first."
                        }),
                        Headers = PostHeaders
                    };
                }

                List<string> requested = null;
                if (!string.IsNullOrWhiteSpace(request.Body))
                {
                    try
                    {
                        requested = JsonConvert.DeserializeObject<InvalidateRequest>(request.Body)?.paths;
                    }
                    catch (JsonException ex)
                    {
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.BadRequest,
                            Body = JsonConvert.SerializeObject(new { error = "bad_body", message = ex.Message }),
                            Headers = PostHeaders
                        };
                    }
                }

                var result = await CloudFrontCache.InvalidateAsync(domain, requested, "manual", context);

                if (!result.Succeeded)
                {
                    context.Logger.LogError($"Invalidation for {domain} failed: {result.Error}");
                    return new APIGatewayProxyResponse
                    {
                        // The caller asked for exactly one thing and it did not happen, so this
                        // must not return 200 with a quiet "succeeded: false" -- the MCP client
                        // surfaces a non-2xx as an error and a 200 as a result.
                        StatusCode = (int)HttpStatusCode.BadGateway,
                        Body = JsonConvert.SerializeObject(new
                        {
                            error = "invalidation_failed",
                            websiteId,
                            site = domain,
                            paths = result.Paths,
                            message = result.Error
                        }),
                        Headers = PostHeaders
                    };
                }

                context.Logger.Log($"Invalidated {string.Join(" ", result.Paths)} on {domain} ({result.DistributionId}) as {result.InvalidationId}.");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new
                    {
                        websiteId,
                        site = domain,
                        distributionId = result.DistributionId,
                        invalidationId = result.InvalidationId,
                        paths = result.Paths,
                        note = "CloudFront usually completes an invalidation in 1-3 minutes."
                    }),
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error invalidating cache: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error: {ex.Message}",
                    Headers = PostHeaders
                };
            }
        }

        /// <summary>
        /// True when the request carries the MCP API key. If MCP_API_KEY is not configured the
        /// request is refused rather than allowed -- a missing key must not silently open a
        /// billable endpoint.
        /// </summary>
        private static bool IsAuthorized(APIGatewayProxyRequest request)
        {
            var expected = Environment.GetEnvironmentVariable("MCP_API_KEY");
            if (string.IsNullOrWhiteSpace(expected)) return false;

            string supplied = GetHeader(request, "X-API-Key");
            if (string.IsNullOrEmpty(supplied)) return false;

            // Length-independent comparison is pointless here (the header length is public),
            // but a constant-time compare of equal-length keys costs nothing.
            var a = System.Text.Encoding.UTF8.GetBytes(supplied);
            var b = System.Text.Encoding.UTF8.GetBytes(expected);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>API Gateway header casing is not guaranteed, so match case-insensitively.</summary>
        private static string GetHeader(APIGatewayProxyRequest request, string name)
        {
            if (request?.Headers == null) return null;
            foreach (var kv in request.Headers)
            {
                if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            }
            return null;
        }
    }
}
