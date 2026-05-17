using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MySQLConnector;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using WebApplicationArch.model.requests;

namespace WebApplicationArch
{
    public class ApiArticleFunctions: ApiBaseFunctions
    {
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


    }
}
