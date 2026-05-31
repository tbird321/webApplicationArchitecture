using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MySQLConnector;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using WebApplicationArch.model.requests;

namespace WebApplicationArch;

public class ApiFunctions
{
    private static Dictionary<string,ConnectionModel> connectionOptions = new Dictionary<string,ConnectionModel>();
    private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public static async Task<ConnectionModel> ConnectionInfoAsync(string environment)
    {
        ConnectionModel connectionModel;

        if (connectionOptions.ContainsKey(environment))
        {
            connectionModel = connectionOptions[environment];
        }
        else
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                ConnectionManager conTest = new ConnectionManager();
                connectionModel = await conTest.GetSecretAsync(environment);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        return connectionModel;
    }

    private IWebsiteProcessing _webProcessor;

    // Constructor for dependency injection
    public ApiFunctions(IWebsiteProcessing webProcessor)
    {
        _webProcessor = webProcessor;
    }

    // Default constructor
    public ApiFunctions()
    {
        // This constructor is empty and will be used when no dependency is injected
    }

    private Dictionary<string, string> getHeaders
    {
        get
        {
            var headers = new Dictionary<string, string>() { { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" } };
            return headers;
        }
    }

    private Dictionary<string, string> postHeaders
    {
        get
        {
            var headers = new Dictionary<string, string>(){ { "Content-Type", "application/json" }, { "Access-Control-Allow-Origin", "*" }, { "Access-Control-Allow-Methods", "POST" } };
            return headers;
        }
    }

    private async Task<IWebsiteProcessing> GetProcessorAsync(string environment)
    {
        if (_webProcessor == null)
        {
            _webProcessor = new WebsiteProcessing(await ConnectionInfoAsync(environment));
        }
        return _webProcessor;
    }
    private string GetEnvironment(APIGatewayProxyRequest request)
    {
        String environment = "Prod";
        try
        {
            IDictionary<string, string> stageVariables = request.StageVariables;
            if (stageVariables != null && stageVariables.Count > 0)
            {
                environment = stageVariables["Environment"];
            }
        } catch
        {

        }
        return environment;
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
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

                // Implement logic to retrieve the page by ID and serialize the result
                var page = await pageProcessing.GetPageAsync(pageId,websiteId);
                if (page != null)
                {
                    string responseBody = JsonConvert.SerializeObject(page);
                    // Return the response with the page data
                    context.Logger.Log($"Page Retrieved: {pageId}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = responseBody,
                        Headers = getHeaders
                    };
                }
                else
                {
                    context.Logger.LogError($"Page Not Found: {pageId}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = "PageID not found",
                        Headers = getHeaders
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
                    Headers = getHeaders
                };
            }
            
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting page by ID: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = getHeaders
            };
        }
    }

    public async Task<APIGatewayProxyResponse> GetPageByName(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extract the page ID from the request path
            int pageId = 0;
            string pageName = request.PathParameters["name"] as string;
            string websiteId = request.PathParameters["websiteId"] as string;
            if (!string.IsNullOrEmpty(pageName))
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

                // Implement logic to retrieve the page by ID and serialize the result
                var page = await pageProcessing.GetPageAsyncByName(pageName, websiteId);
                if (page != null)
                {
                    string responseBody = JsonConvert.SerializeObject(page);
                    // Return the response with the page data
                    context.Logger.Log($"Page Retrieved: {pageId}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = responseBody,
                        Headers = getHeaders
                    };
                }
                else
                {
                    context.Logger.LogError($"Page Not Found: {pageId}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = "PageID not found",
                        Headers = getHeaders
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
                    Headers = getHeaders
                };
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting page by Name: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = getHeaders
            };
        }
    }    

    public async Task<APIGatewayProxyResponse> SavePageAndArticles(APIGatewayProxyRequest request, ILambdaContext context)
    {
        PageModel pageInfo = null;
        try
        {

            pageInfo = JsonConvert.DeserializeObject<PageModel>(request.Body);
            //validate search request...
            if (pageInfo == null)
            {
                context.Logger.LogError($"Page Search Request Bad: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid PageID provided",
                    Headers = postHeaders
                };
            }
            else
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

                // Implement logic to retrieve the page by ID and serialize the result
                var pages = await pageProcessing.SavePageAndArticlesAsync(pageInfo);
                if (pages != null)
                {
                    string responseBody = JsonConvert.SerializeObject(pages);
                    // Return the response with the page data
                    context.Logger.Log($"Page Updated: {responseBody}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.OK,
                        Body = responseBody,
                        Headers = postHeaders
                    };
                }
                else
                {
                    context.Logger.LogError($"No Pages Found: {request.Body}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = "Page not found",
                        Headers = postHeaders
                    };
                }
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error Saving page: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
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
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

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
                    Headers = postHeaders
                };
            }
            else
            {
                context.Logger.LogError($"No Pages Found: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = "Page not found",
                    Headers = postHeaders
                };
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error Searching for page: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }
    }


    public async Task<APIGatewayProxyResponse> GetTopics(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extract the articleId ID from the request path
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);
            // Implement logic to retrieve the article by ID and serialize the result
            var topicsAndKeywords = await pageProcessing.GetTopics();
            string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
            // Return the response with the page data
            context.Logger.Log($"Topics Retrieved: {topicsAndKeywords.Count}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting Topics: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }

    }

    public async Task<APIGatewayProxyResponse> GetKeywords(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extract the articleId ID from the request path
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);
            // Implement logic to retrieve the article by ID and serialize the result
            var topicsAndKeywords = await pageProcessing.GetKeywords();
            string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
            // Return the response with the page data
            context.Logger.Log($"Keywords Retrieved: {topicsAndKeywords.Count}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting Keywords: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }

    }

    public async Task<APIGatewayProxyResponse> GetLayouts(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extract the articleId ID from the request path
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);
            // Implement logic to retrieve the article by ID and serialize the result
            var topicsAndKeywords = await pageProcessing.GetLayouts();
            string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
            // Return the response with the page data
            context.Logger.Log($"Layouts Retrieved: {topicsAndKeywords.Count}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting Layouts: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }

    }

    public async Task<APIGatewayProxyResponse> GetWebsites(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extract the articleId ID from the request path
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);
            // Implement logic to retrieve the article by ID and serialize the result
            var topicsAndKeywords = await pageProcessing.GetWebsites();
            string responseBody = JsonConvert.SerializeObject(topicsAndKeywords);
            // Return the response with the page data
            context.Logger.Log($"Websites Retrieved: {topicsAndKeywords.Count}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting Websites: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }

    }

    public async Task<APIGatewayProxyResponse> GetArticleById(APIGatewayProxyRequest request, ILambdaContext context)
    {
       try
        {
            // Extract the articleId ID from the request path
            int articleid = 0;
            if (int.TryParse(request.PathParameters["id"], out articleid))
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

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
                        Headers = getHeaders
                    };
                }
                else
                {
                    context.Logger.LogError($"article Not Found: {articleid}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.NotFound,
                        Body = "Article Id not found",
                        Headers = getHeaders
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
                    Headers = getHeaders
                };
            }
            
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting Article by ID: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}"
            };
        }
 
    }

    public async Task<APIGatewayProxyResponse> SearchArticles(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Implement logic to search article based on the criteria
        try
        {

            searchCriteria searchInfo = JsonConvert.DeserializeObject<searchCriteria>(request.Body);
            //validate search request...
            if (searchInfo == null || !searchInfo.isValidRequest())
            {
                context.Logger.LogError($"Article Search Request Bad: {request.Body}");                
            }

            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);

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
                    Headers = postHeaders
                };
            }
            else
            {
                context.Logger.LogError($"No Articles Found: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = "Article not found",
                    Headers = postHeaders
                };
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting article: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }

    public async Task<APIGatewayProxyResponse> UpsertArticle(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Implement logic to search article based on the criteria

        try
        {

            var articleModel = JsonConvert.DeserializeObject<ArticleModel>(request.Body);
            if (articleModel == null)
            {
                context.Logger.LogError($"Article Upsert Request Bad: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid Article provided",
                    Headers = postHeaders
                };
            }
            else
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing pageProcessing = await GetProcessorAsync(environment);
                ArticleModel result = await pageProcessing.UpsertArticle(articleModel);
                string responseBody = JsonConvert.SerializeObject(result);
                // Return the response with the article data
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = postHeaders
                };
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error saving article: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    public async Task<APIGatewayProxyResponse> UpsertCollection(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var collectionModel = JsonConvert.DeserializeObject<CollectionModel>(request.Body);
            if (collectionModel == null)
            {
                context.Logger.LogError($"Collection Upsert Request Bad: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid Collection provided",
                    Headers = postHeaders
                };
            }
            else
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);
                CollectionModel result = await websiteProcessing.UpsertCollection(collectionModel);
                string responseBody = JsonConvert.SerializeObject(result);
                // Return the response with the article data
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = postHeaders
                };
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error saving collection: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    public async Task<APIGatewayProxyResponse> GetCollectionPageAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (!request.PathParameters.ContainsKey("id"))
            {
                context.Logger.LogError($"Invalid path parameters:");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for Id",
                    Headers = postHeaders
                };
            }
            string collectionIdString = request.PathParameters["id"];

            if (!int.TryParse(collectionIdString, out int collectionId))
            {
                context.Logger.LogError($"Invalid path parameters: Id: {collectionIdString}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for Id",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);
            var result = await websiteProcessing.GetCollectionPageAsync(collectionId);
            string responseBody = JsonConvert.SerializeObject(result);
            // Return the response with the article data
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting collection pages: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    public async Task<APIGatewayProxyResponse> AssociatePagesWithCollection(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            var collectionPageDetail = JsonConvert.DeserializeObject<CollectionPageDetail>(request.Body);
            if (collectionPageDetail == null)
            {
                context.Logger.LogError($"CollectionPage Association Request Bad: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid CollectionPage provided",
                    Headers = postHeaders
                };
            }else if (collectionPageDetail.pages == null || collectionPageDetail.collection == null)
            {
                    context.Logger.LogError($"CollectionPage Association Request Bad: {request.Body}");
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Body = "Invalid CollectionPage provided",
                        Headers = postHeaders
                    };
            }else
            {
                string environment = GetEnvironment(request);
                context.Logger.Log($"Environment Retrieved: {environment}");
                IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

                var result = await websiteProcessing.AssociatePagesWithCollection(collectionPageDetail);
                string responseBody = JsonConvert.SerializeObject(result);
                // Return the response with the article data
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = postHeaders
                };
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error saving pages to collection: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    public async Task<APIGatewayProxyResponse> searchCollections(APIGatewayProxyRequest request, ILambdaContext context)
    {
        // Deserialize the request body to get the search criteria
        var searchRequest = JsonConvert.DeserializeObject<searchCriteria>(request.Body);

        // Implement logic to search pages based on the criteria

        try
        {

            searchCriteria searchInfo = JsonConvert.DeserializeObject<searchCriteria>(request.Body);
            //validate search request...
            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            // Implement logic to retrieve the page by ID and serialize the result
            var collections = await websiteProcessing.SearchCollections(searchInfo.Name, searchInfo.WebsiteId);
            if (collections != null && collections.Count > 0)
            {
                string responseBody = JsonConvert.SerializeObject(collections);
                // Return the response with the page data
                context.Logger.Log($"Collections Retrieved: {collections.Count}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.OK,
                    Body = responseBody,
                    Headers = postHeaders
                };
            }
            else
            {
                context.Logger.LogError($"No Collections Found: {request.Body}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = "Page not found",
                    Headers = postHeaders
                };
            }

        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error getting collection: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    public async Task<APIGatewayProxyResponse> DeleteCollection(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extracting collectionId and pageId from path parameters
            if (!request.PathParameters.ContainsKey("id"))
            {
                context.Logger.LogError($"Invalid path parameters:");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for Id",
                    Headers = postHeaders
                };
            }
            string collectionIdString = request.PathParameters["id"];
            
            if (!int.TryParse(collectionIdString, out int collectionId))
            {
                context.Logger.LogError($"Invalid path parameters: Id: {collectionIdString}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for Id",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            var result = await websiteProcessing.DeleteCollection(collectionId);
            string responseBody = JsonConvert.SerializeObject(result);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting page from collection: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }
    private async Task<APIGatewayProxyResponse> SetPageStatusInternal(APIGatewayProxyRequest request, ILambdaContext context, string status)
    {
        try
        {
            if (!request.PathParameters.ContainsKey("id") || !int.TryParse(request.PathParameters["id"], out int pageId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid page id",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            if (status == "published")
            {
                await websiteProcessing.PublishPage(pageId);
            }
            else
            {
                await websiteProcessing.UnpublishPage(pageId);
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new { id = pageId, status }),
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error setting page status: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }

    private async Task<APIGatewayProxyResponse> SetArticleStatusInternal(APIGatewayProxyRequest request, ILambdaContext context, string status)
    {
        try
        {
            if (!request.PathParameters.ContainsKey("id") || !int.TryParse(request.PathParameters["id"], out int articleId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid article id",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            if (status == "published")
            {
                await websiteProcessing.PublishArticle(articleId);
            }
            else
            {
                await websiteProcessing.UnpublishArticle(articleId);
            }

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new { id = articleId, status }),
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error setting article status: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }

    public Task<APIGatewayProxyResponse> PublishPage(APIGatewayProxyRequest request, ILambdaContext context)
        => SetPageStatusInternal(request, context, "published");

    public Task<APIGatewayProxyResponse> UnpublishPage(APIGatewayProxyRequest request, ILambdaContext context)
        => SetPageStatusInternal(request, context, "draft");

    public Task<APIGatewayProxyResponse> PublishArticle(APIGatewayProxyRequest request, ILambdaContext context)
        => SetArticleStatusInternal(request, context, "published");

    public Task<APIGatewayProxyResponse> UnpublishArticle(APIGatewayProxyRequest request, ILambdaContext context)
        => SetArticleStatusInternal(request, context, "draft");

    // Deletes a single page row by id ONLY when the page is safe to delete:
    //   - status is draft (never delete a published page through this endpoint)
    //   - articles list is empty (no linked articles)
    // Caller is responsible for removing menu items / collection associations BEFORE calling.
    // For pages with articles/content, use the stored-procedure cascade path (separate endpoint, not exposed yet).
    public async Task<APIGatewayProxyResponse> DeletePage(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (!request.PathParameters.ContainsKey("id") || !int.TryParse(request.PathParameters["id"], out int pageId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid page id",
                    Headers = postHeaders
                };
            }

            // websiteId is required as a query parameter so we can look up the page record for the safety check.
            if (request.QueryStringParameters == null
                || !request.QueryStringParameters.ContainsKey("websiteId")
                || !int.TryParse(request.QueryStringParameters["websiteId"], out int websiteId))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Missing or invalid 'websiteId' query parameter (required for the safety pre-check).",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            // Safety pre-check: fetch the page, refuse to delete anything that has content or is published.
            var page = await websiteProcessing.GetPageAsync(pageId, websiteId);
            if (page == null || page.id == null)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonConvert.SerializeObject(new { error = "page_not_found", message = $"Page {pageId} not found on websiteId {websiteId}.", pageId, websiteId }),
                    Headers = postHeaders
                };
            }

            if (page.articles != null && page.articles.Count > 0)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Body = JsonConvert.SerializeObject(new
                    {
                        error = "page_has_articles",
                        message = $"Refusing to delete: page {pageId} has {page.articles.Count} linked article(s). Detach or delete the articles first.",
                        pageId,
                        articleCount = page.articles.Count
                    }),
                    Headers = postHeaders
                };
            }

            if (!string.IsNullOrEmpty(page.status) && page.status != "draft")
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Body = JsonConvert.SerializeObject(new
                    {
                        error = "page_is_published",
                        message = $"Refusing to delete: page {pageId} has status '{page.status}'. Unpublish it first.",
                        pageId,
                        status = page.status
                    }),
                    Headers = postHeaders
                };
            }

            await websiteProcessing.DeletePage(pageId);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonConvert.SerializeObject(new { id = pageId, deleted = true }),
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting page: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }

    public async Task<APIGatewayProxyResponse> DeletePageFromCollection(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            // Extracting collectionId and pageId from path parameters
            if (!request.PathParameters.ContainsKey("id")|| !request.PathParameters.ContainsKey("pageId"))
            {
                context.Logger.LogError($"Invalid path parameters:");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for CollectionId or PageId",
                    Headers = postHeaders
                };
            }
            string collectionIdString = request.PathParameters["id"];
            string pageIdString = request.PathParameters["pageId"];

            if (!int.TryParse(collectionIdString, out int collectionId) || !int.TryParse(pageIdString, out int pageId))
            {
                context.Logger.LogError($"Invalid path parameters: CollectionId: {collectionIdString}, PageId: {pageIdString}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "Invalid path parameters for CollectionId or PageId",
                    Headers = postHeaders
                };
            }

            string environment = GetEnvironment(request);
            context.Logger.Log($"Environment Retrieved: {environment}");
            IWebsiteProcessing websiteProcessing = await GetProcessorAsync(environment);

            var result = await websiteProcessing.DeletePageFromCollection(collectionId, pageId);
            string responseBody = JsonConvert.SerializeObject(result);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = postHeaders
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error deleting page from collection: {ex.Message}");
            context.Logger.LogError($"Error Trace: {ex.StackTrace}");
            context.Logger.LogError($"Error Request: {request.Body}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Error occurred: {ex.Message}",
                Headers = postHeaders
            };
        }
    }


}
