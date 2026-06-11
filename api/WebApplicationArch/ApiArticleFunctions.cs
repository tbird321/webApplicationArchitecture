using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MySQLConnector;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using WebApplicationArch.content;
using WebApplicationArch.model.requests;

namespace WebApplicationArch
{
    public class ApiArticleFunctions: ApiBaseFunctions
    {
        public ApiArticleFunctions() : base() { }

        public ApiArticleFunctions(IWebsiteProcessing connectionModel) : base()
        {

        }

        public async Task<APIGatewayProxyResponse> GetArticleById(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Extract the articleId ID from the request path
                int articleid = 0;
                if (int.TryParse(request.PathParameters["id"], out articleid))
                {
                      ArticleDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                    // Implement logic to retrieve the article by ID and serialize the result
                    var article = await pageProcessing.GetArticleAsync(articleid);
                    if (article != null)
                    {
                        string responseBody = JsonConvert.SerializeObject(article);
                        // Return the response with the article data
                        context.Logger.Log($"Article Retrieved: {articleid}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Body = responseBody,
                            Headers = GetHeaders
                        };
                    }
                    else
                    {
                        context.Logger.LogError($"article Not Found: {articleid}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Body = "Article Id not found",
                            Headers = GetHeaders
                        };
                    }

                }
                else
                {
                    context.Logger.LogError($"article Request Bad: {articleid}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid Article Id provided",
                        Headers = GetHeaders
                    };
                }

            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting Article by ID: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error occurred: {ex.Message}",
                    Headers = GetHeaders
                };
            }

        }

        public async Task<APIGatewayProxyResponse> SearchArticles(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // Deserialize the request body to get the search criteria
            var searchRequest = JsonConvert.DeserializeObject<searchCriteria>(request.Body);

            // Implement logic to search article based on the criteria

            try
            {

                searchCriteria searchInfo = JsonConvert.DeserializeObject<searchCriteria>(request.Body);
                //validate search request...
                if (searchInfo == null || !searchInfo.isValidRequest())
                {
                    context.Logger.LogError($"Article Search Request Bad: {request.Body}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid Article ID provided",
                        Headers = PostHeaders
                    };
                }
                else
                {
                    ArticleDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                    // Implement logic to retrieve the page by ID and serialize the result
                    var articles = await pageProcessing.SearchArticles(searchInfo.Keywords, searchInfo.Topics, searchInfo.Name, searchInfo.Description,searchInfo.WebsiteId);
                    if (articles != null && articles.Count > 0)
                    {
                        string responseBody = JsonConvert.SerializeObject(articles);
                        // Return the response with the page data
                        context.Logger.Log($"Articles Retrieved: {articles.Count}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Body = responseBody,
                            Headers = PostHeaders
                        };
                    }
                    else
                    {
                        context.Logger.LogError($"No Articles Found: {request.Body}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Body = "Article not found",
                            Headers = PostHeaders
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting article: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error occurred: {ex.Message}",
                    Headers = PostHeaders
                };
            }
        }


        public async Task<APIGatewayProxyResponse> GetArticleContent(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (!int.TryParse(request.PathParameters["id"], out int articleId))
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest, Body = "Invalid article id", Headers = GetHeaders };

                string environment = GetEnvironment(request);
                ArticleDAO dao = new(await ConnectionInfoAsync(environment));
                var article = await dao.GetArticleAsync(articleId);
                if (article == null)
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.NotFound, Body = "Article not found", Headers = GetHeaders };

                if (string.IsNullOrEmpty(article.articlePath))
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.NotFound, Body = "Article has no content path", Headers = GetHeaders };

                string bucket = Environment.GetEnvironmentVariable("CONTENT_BUCKET") ?? "www-websitecontent";
                var s3 = new AmazonS3Storage(bucket, "us-west-2");
                string s3Folder = await GetArticleS3Folder(article.websiteId, environment);

                using var stream = s3.DownloadFile(article.articlePath, s3Folder);
                using var reader = new StreamReader(stream);
                string html = await reader.ReadToEndAsync();

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = html,
                    Headers = new Dictionary<string, string> { { "Content-Type", "text/html" }, { "Access-Control-Allow-Origin", "*" } }
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting article content: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Body = $"Error: {ex.Message}", Headers = GetHeaders };
            }
        }

        public async Task<APIGatewayProxyResponse> SetArticleContent(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (!int.TryParse(request.PathParameters["id"], out int articleId))
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest, Body = "Invalid article id", Headers = PostHeaders };

                var body = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Body);
                if (body == null || !body.ContainsKey("htmlContent"))
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest, Body = "htmlContent is required", Headers = PostHeaders };

                string environment = GetEnvironment(request);
                ArticleDAO dao = new(await ConnectionInfoAsync(environment));
                var article = await dao.GetArticleAsync(articleId);
                if (article == null)
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.NotFound, Body = "Article not found", Headers = PostHeaders };

                string bucket = Environment.GetEnvironmentVariable("CONTENT_BUCKET") ?? "www-websitecontent";
                var s3 = new AmazonS3Storage(bucket, "us-west-2");
                string s3Folder = await GetArticleS3Folder(article.websiteId, environment);

                string filename = article.articlePath;
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"{article.name}.html";
                    article.articlePath = filename;
                    await dao.UpsertArticle(article);
                }

                byte[] bytes = Encoding.UTF8.GetBytes(body["htmlContent"]);
                using var stream = new MemoryStream(bytes);
                await s3.UploadFile(stream, filename, s3Folder);

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new { articlePath = filename }),
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error setting article content: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Body = $"Error: {ex.Message}", Headers = PostHeaders };
            }
        }

        private async Task<string> GetArticleS3Folder(int websiteId, string environment)
        {
            WebsiteDAO websiteDao = new(await ConnectionInfoAsync(environment));
            var websites = await websiteDao.GetWebsites();
            var website = websites.FirstOrDefault(w => w.id == websiteId);
            string websiteName = website?.name ?? websiteId.ToString();
            return $"public/websites/{websiteName}/articles";
        }

        // Builds a sitemap.xml from every served page for the given website and uploads it to the site's
        // S3 root at public/websites/{websiteName}/sitemap.xml. Run this whenever a content batch has been
        // published. Served = status "published" OR null (legacy pages that still resolve).
        public async Task<APIGatewayProxyResponse> RegenerateSitemap(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (request.QueryStringParameters == null
                    || !request.QueryStringParameters.ContainsKey("websiteId")
                    || !int.TryParse(request.QueryStringParameters["websiteId"], out int websiteId))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Missing or invalid 'websiteId' query parameter.",
                        Headers = PostHeaders
                    };
                }

                string environment = GetEnvironment(request);
                IWebsiteProcessing processor = new WebsiteProcessing(await ConnectionInfoAsync(environment));

                // Resolve the website's name (used both as the public domain and as the S3 folder).
                WebsiteDAO websiteDao = new(await ConnectionInfoAsync(environment));
                var websites = await websiteDao.GetWebsites();
                var site = websites.FirstOrDefault(w => w.id == websiteId);
                if (site == null || string.IsNullOrWhiteSpace(site.name))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = JsonConvert.SerializeObject(new { error = "website_not_found", websiteId }),
                        Headers = PostHeaders
                    };
                }
                string siteName = site.name;

                // Query every page for this website, then keep only those that are served.
                var allPages = await processor.SearchPages(new List<string>(), new List<string>(), null, null, websiteId.ToString());
                var servedPages = (allPages ?? new List<PageModel>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.name)
                                && (string.IsNullOrEmpty(p.status) || p.status == "published"))
                    .ToList();

                // Build the XML.
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>https://www.{siteName}/</loc>");
                sb.AppendLine($"    <lastmod>{today}</lastmod>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>1.0</priority>");
                sb.AppendLine("  </url>");
                foreach (var p in servedPages)
                {
                    string encoded = Uri.EscapeDataString(p.name!);
                    sb.AppendLine("  <url>");
                    sb.AppendLine($"    <loc>https://www.{siteName}/?page={encoded}</loc>");
                    sb.AppendLine($"    <lastmod>{today}</lastmod>");
                    sb.AppendLine("    <changefreq>monthly</changefreq>");
                    sb.AppendLine("    <priority>0.7</priority>");
                    sb.AppendLine("  </url>");
                }
                sb.AppendLine("</urlset>");

                // Upload to the PUBLIC web hosting bucket (not the CMS content store).
                // Bucket convention: www.{domain}, files at bucket root so they serve at
                // https://www.{domain}/sitemap.xml and https://www.{domain}/robots.txt directly.
                string bucket = $"www.{siteName}";
                var s3 = new AmazonS3Storage(bucket, "us-west-2");

                // 1) sitemap.xml
                byte[] sitemapBytes = Encoding.UTF8.GetBytes(sb.ToString());
                using (var sitemapStream = new MemoryStream(sitemapBytes))
                {
                    await s3.UploadFile(sitemapStream, "sitemap.xml", string.Empty);
                }

                // 2) robots.txt — allow all crawlers, point them at the sitemap.
                var robots = new StringBuilder();
                robots.AppendLine("User-agent: *");
                robots.AppendLine("Allow: /");
                robots.AppendLine();
                robots.AppendLine($"Sitemap: https://www.{siteName}/sitemap.xml");
                byte[] robotsBytes = Encoding.UTF8.GetBytes(robots.ToString());
                using (var robotsStream = new MemoryStream(robotsBytes))
                {
                    await s3.UploadFile(robotsStream, "robots.txt", string.Empty);
                }

                // 3) Best-effort CloudFront invalidation so the updated robots.txt / sitemap.xml are
                //    served immediately instead of waiting for the CDN's TTL to expire. If the
                //    distribution can't be found or the call fails, the S3 write has already
                //    succeeded — we just surface the invalidation status in the response.
                bool invalidationAttempted = false;
                bool invalidationSucceeded = false;
                string? invalidationDistributionId = null;
                string? invalidationId = null;
                string? invalidationError = null;
                try
                {
                    invalidationAttempted = true;
                    using var cf = new AmazonCloudFrontClient();
                    string aliasWww = $"www.{siteName}";
                    string aliasBare = siteName;
                    var distros = await cf.ListDistributionsAsync(new ListDistributionsRequest());
                    var match = distros.DistributionList?.Items?.FirstOrDefault(d =>
                        d.Aliases?.Items != null &&
                        d.Aliases.Items.Any(a => a == aliasWww || a == aliasBare));
                    if (match != null)
                    {
                        invalidationDistributionId = match.Id;
                        var invReq = new CreateInvalidationRequest
                        {
                            DistributionId = match.Id,
                            InvalidationBatch = new InvalidationBatch
                            {
                                CallerReference = $"sitemap-{siteName}-{DateTime.UtcNow.Ticks}",
                                Paths = new Paths
                                {
                                    Quantity = 2,
                                    Items = new List<string> { "/sitemap.xml", "/robots.txt" }
                                }
                            }
                        };
                        var invResp = await cf.CreateInvalidationAsync(invReq);
                        invalidationId = invResp?.Invalidation?.Id;
                        invalidationSucceeded = true;
                    }
                    else
                    {
                        invalidationError = $"No CloudFront distribution found with alias '{aliasWww}' or '{aliasBare}'.";
                    }
                }
                catch (Exception cfEx)
                {
                    invalidationError = cfEx.Message;
                    context.Logger.LogError($"CloudFront invalidation failed for {siteName}: {cfEx.Message}");
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new
                    {
                        websiteId,
                        site = siteName,
                        pages = servedPages.Count,
                        sitemap = new
                        {
                            s3Path = $"s3://{bucket}/sitemap.xml",
                            publicUrl = $"https://www.{siteName}/sitemap.xml"
                        },
                        robots = new
                        {
                            s3Path = $"s3://{bucket}/robots.txt",
                            publicUrl = $"https://www.{siteName}/robots.txt"
                        },
                        cloudFrontInvalidation = new
                        {
                            attempted = invalidationAttempted,
                            succeeded = invalidationSucceeded,
                            distributionId = invalidationDistributionId,
                            invalidationId,
                            error = invalidationError
                        }
                    }),
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error regenerating sitemap: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Body = $"Error: {ex.Message}", Headers = PostHeaders };
            }
        }

        // Deletes the article DB row AND its S3 HTML file. Caller is responsible for ensuring no page
        // still references the article (detach via update_page with the article omitted from the articles
        // array) — this endpoint does NOT check for parent-page references, so a stale junction may
        // remain if the caller skips that step. Status is intentionally NOT checked: delete is a strict
        // superset of unpublish. S3 cleanup is best-effort; if it fails, the DB row is still gone and the
        // S3 path is surfaced in the response so it can be manually cleaned up.
        public async Task<APIGatewayProxyResponse> DeleteArticle(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                if (!request.PathParameters.ContainsKey("id") || !int.TryParse(request.PathParameters["id"], out int articleId))
                {
                    return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.BadRequest, Body = "Invalid article id", Headers = PostHeaders };
                }

                string environment = GetEnvironment(request);
                ArticleDAO dao = new(await ConnectionInfoAsync(environment));

                // Fetch the article so we can capture its S3 path BEFORE deleting the DB row.
                var article = await dao.GetArticleAsync(articleId);
                if (article == null || article.id == null || article.id == 0)
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = JsonConvert.SerializeObject(new { error = "article_not_found", articleId }),
                        Headers = PostHeaders
                    };
                }

                string? articlePath = article.articlePath;
                int websiteIdForS3 = article.websiteId;

                // Delete the DB row first. If this fails, S3 is left alone (consistent — DB still points at a real file).
                await dao.DeleteArticle(articleId);

                // Now best-effort S3 cleanup. A failure here leaves the S3 file as orphan storage but does
                // NOT roll back the DB delete — the article is correctly gone from a user perspective.
                bool s3Attempted = false;
                bool s3Deleted = false;
                string? s3Error = null;
                if (!string.IsNullOrEmpty(articlePath))
                {
                    s3Attempted = true;
                    try
                    {
                        string bucket = Environment.GetEnvironmentVariable("CONTENT_BUCKET") ?? "www-websitecontent";
                        var s3 = new AmazonS3Storage(bucket, "us-west-2");
                        string s3Folder = await GetArticleS3Folder(websiteIdForS3, environment);
                        s3Deleted = await s3.DeleteFile(articlePath, s3Folder);
                    }
                    catch (Exception s3ex)
                    {
                        s3Error = s3ex.Message;
                        context.Logger.LogError($"S3 cleanup failed for article {articleId} ({articlePath}): {s3ex.Message}");
                    }
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = JsonConvert.SerializeObject(new
                    {
                        id = articleId,
                        deleted = true,
                        s3Path = articlePath,
                        s3Attempted,
                        s3Deleted,
                        s3Error
                    }),
                    Headers = PostHeaders
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error deleting article: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = (int)HttpStatusCode.InternalServerError, Body = $"Error: {ex.Message}", Headers = PostHeaders };
            }
        }

    }
}
