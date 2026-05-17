using System.Net;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.S3;
using ContentManagementApplication.security;
using Newtonsoft.Json;
using WebApplicationArch.content;
using WebApplicationArch.model.requests;
using WebApplicationArch.model.responses;
using System.Threading.Tasks;
using MySQLConnector;
using MySQLConnector.Models;
using MySQLConnector.Interfaces;
using System.Reflection.PortableExecutable;


namespace WebApplicationArch
{
    public class ApiPageFunctions:ApiBaseFunctions
    {
        public ApiPageFunctions(IWebsiteProcessing connectionModel) : base()
        {

        }

        public async Task<APIGatewayProxyResponse> GetPageById(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Extract the page ID from the request path
                int pageId = 0;
                int websiteId = 0;
                if (!int.TryParse(request.PathParameters["websiteId"], out websiteId))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid Website Id provided",
                        Headers = getHeaders
                    };
                }
                if (int.TryParse(request.PathParameters["id"], out pageId))
                {
                    PageDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                    // Implement logic to retrieve the page by ID and serialize the result
                    var page = await pageProcessing.GetPageAsync(pageId, websiteId);
                    if (page != null)
                    {
                        string responseBody = JsonConvert.SerializeObject(page);
                        // Return the response with the page data
                        context.Logger.Log($"Page Retrieved: {pageId}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Body = responseBody,
                            Headers = GetHeaders
                        };
                    }
                    else
                    {
                        context.Logger.LogError($"Page Not Found: {pageId}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Body = "PageID not found",
                            Headers = GetHeaders
                        };
                    }

                }
                else
                {
                    context.Logger.LogError($"Page Request Bad: {pageId}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid PageID provided",
                        Headers = GetHeaders
                    };
                }

            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting page by ID: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error occurred: {ex.Message}",
                    Headers = GetHeaders
                };
            }
        }

        public async Task<APIGatewayProxyResponse> SearchPages(APIGatewayProxyRequest request, ILambdaContext context)
        {
            // Deserialize the request body to get the search criteria
            var searchRequest = JsonConvert.DeserializeObject<searchCriteria>(request.Body);

            // Implement logic to search pages based on the criteria

            try
            {

                searchCriteria searchInfo = JsonConvert.DeserializeObject<searchCriteria>(request.Body);
                //validate search request...
                if (searchInfo == null || !searchInfo.isValidRequest())
                {
                    context.Logger.LogError($"Page Search Request Bad: {request.Body}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid PageID provided",
                        Headers = PostHeaders
                    };
                }
                else
                {
                    PageDAO pageProcessing = new(await ConnectionInfoAsync(GetEnvironment(request)));

                    // Implement logic to retrieve the page by ID and serialize the result
                    var pages = await pageProcessing.SearchPages(searchInfo.Keywords, searchInfo.Topics, searchInfo.Name, searchInfo.Description,searchInfo.WebsiteId);
                    if (pages != null && pages.Count > 0)
                    {
                        string responseBody = JsonConvert.SerializeObject(pages);
                        // Return the response with the page data
                        context.Logger.Log($"Pages Retrieved: {pages.Count}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.OK,
                            Body = responseBody,
                            Headers = PostHeaders
                        };
                    }
                    else
                    {
                        context.Logger.LogError($"No Pages Found: {request.Body}");
                        return new APIGatewayProxyResponse
                        {
                            StatusCode = (int)HttpStatusCode.NotFound,
                            Body = "Page not found",
                            Headers = PostHeaders
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error getting page: {ex.Message}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = $"Error occurred: {ex.Message}",
                    Headers = PostHeaders
                };
            }
        }
        private Dictionary<string, string> getHeaders
        {
            get
            {
                var headers = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } };
                return headers;
            }
        }

    }
}
