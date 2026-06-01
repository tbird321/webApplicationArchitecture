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
