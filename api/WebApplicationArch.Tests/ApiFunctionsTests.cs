using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using WebApplicationArch.model.requests;
using WebApplicationArch.Tests.TestModels;
using Xunit;

namespace WebApplicationArch.Tests;

public class ApiFunctionsTests
{
    public ApiFunctionsTests()
    {
    }

    public IDictionary<string, string> GetServerVariables()
    {
        IDictionary<string, string> serverVars = new Dictionary<string, string>();
        serverVars.Add("Environment", "Stage");
        return serverVars;
    }

    public APIGatewayProxyRequest CreateRequest(Dictionary<string, string> pathParameters)
    {
        return new APIGatewayProxyRequest
        {
            PathParameters = pathParameters,
            StageVariables = GetServerVariables(),
        };
    }

    public APIGatewayProxyRequest CreateRequest(string jsonBody)
    {
        return new APIGatewayProxyRequest
        {
            Body = jsonBody,
            StageVariables = GetServerVariables(),
        };
    }

    public APIGatewayProxyRequest CreateRequest()
    {
        IDictionary<string, string> serverVars = new Dictionary<string, string>();
        serverVars.Add("Environment", "Stage");
        return new APIGatewayProxyRequest
        {
            StageVariables = GetServerVariables(),
        };
    }

    [Fact]
    public async Task TestGetMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with the required path parameter
        var request = CreateRequest(new Dictionary<string, string> { { "id", "1" },{"websiteId","2" } });

        var response = await functions.GetPageById(request, context);

        Assert.Equal((int)System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assuming the response body contains JSON serialized data
        var pageData = JsonConvert.DeserializeObject<PageModel>(response.Body);

        var page2 = await MockWebsiteProcessing.GetPageModelFromJsonResource("BasicPage.json");

        Assert.True(ModelComparer.AreEqual(pageData, page2));
    }

    [Fact]
    public async Task TestSearchPagesMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a search request body
        var searchRequest = new searchCriteria()
        {
            // Initialize with test data
            Name = "1",
        };

        var request =CreateRequest(JsonConvert.SerializeObject(searchRequest));

        var response = await functions.SearchPages(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var pagesData = JsonConvert.DeserializeObject<List<PageModel>>(response.Body);

        List<PageModel> pages = new List<PageModel>()
            {
                await MockWebsiteProcessing.GetPageModelFromJsonResource("BasicPage.json")
            };

        Assert.True(ModelComparer.AreEqual(pagesData, pages));

    }

    [Fact]
    public async Task TestGetArticleMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);
       
        // Prepare the request with the required path parameter
        var request = CreateRequest(new Dictionary<string, string> { { "id", "1" } });

        var response = await functions.GetArticleById(request, context);

        Assert.Equal((int)System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assuming the response body contains JSON serialized data
        var articleData = JsonConvert.DeserializeObject<ArticleModel>(response.Body);       
        var articleInfo = await MockWebsiteProcessing.GetPageModelFromJsonResource("BasicPage.json");

        Assert.True(ModelComparer.AreEqual(articleData, articleInfo.articles[0]));
    }

    [Fact]
    public async Task TestSearchArticlesMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a search request body
        var searchRequest = new searchCriteria()
        {
            // Initialize with test data
            Name = "page 1",
        };

        var request = CreateRequest(JsonConvert.SerializeObject(searchRequest));

        var response = await functions.SearchArticles(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var pagesData = JsonConvert.DeserializeObject<List<ArticleModel>>(response.Body);

        PageModel pageModel = await MockWebsiteProcessing.GetPageModelFromJsonResource("BasicPage.json");
        List<ArticleModel> articles = new List<ArticleModel>()
            {
                pageModel.articles[0],
                pageModel.articles[1],
            };
        Assert.True(ModelComparer.AreEqual(pagesData, articles));


    }

    [Fact]
    public async Task TestUpsertCollectionMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a collection model
        var collectionModel = new CollectionModel
        {
            id = 1,
            name = "Test Collection",
            description = "A test collection for unit testing",
            type = "standard",
            websiteId = 1
        };

        var request = CreateRequest(JsonConvert.SerializeObject(collectionModel));

        var response = await functions.UpsertCollection(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var collectionData = JsonConvert.DeserializeObject<CollectionModel>(response.Body);
        var expectedCollection = await MockWebsiteProcessing.GetCollectionModelFromJsonResource("BasicCollection.json");

        Assert.True(ModelComparer.AreEqual(collectionData, expectedCollection));
    }

    [Fact]
    public async Task TestSearchCollectionsMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a search request body
        var searchRequest = new searchCriteria()
        {
            Name = "Test Collection",
            WebsiteId = "1"
        };

        var request = CreateRequest(JsonConvert.SerializeObject(searchRequest));

        var response = await functions.searchCollections(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var collectionsData = JsonConvert.DeserializeObject<List<CollectionModel>>(response.Body);

        List<CollectionModel> expectedCollections = new List<CollectionModel>()
        {
            await MockWebsiteProcessing.GetCollectionModelFromJsonResource("BasicCollection.json")
        };

        Assert.True(ModelComparer.AreEqual(collectionsData, expectedCollections));
    }

    [Fact]
    public async Task TestGetCollectionPageAsyncMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with the required path parameter
        var request = CreateRequest(new Dictionary<string, string> { { "id", "1" } });

        var response = await functions.GetCollectionPageAsync(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var collectionPageDetail = JsonConvert.DeserializeObject<CollectionPageDetail>(response.Body);
        var expectedCollectionPageDetail = await MockWebsiteProcessing.GetCollectionPageDetailFromJsonResource("BasicCollectionPageDetail.json");

        Assert.True(ModelComparer.AreEqual(collectionPageDetail, expectedCollectionPageDetail));
    }

    [Fact]
    public async Task TestAssociatePagesWithCollectionMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a collection page detail
        var collectionPageDetail = new CollectionPageDetail
        {
            collection = new CollectionModel
            {
                id = 1,
                name = "Test Collection",
                description = "A test collection for unit testing",
                type = "standard",
                websiteId = 1
            },
            pages = new List<PageDetail>
            {
                new PageDetail
                {
                    id = 1,
                    name = "Test Page 1",
                    description = "First test page",
                    topics = new List<string> { "Test", "Unit" },
                    keywords = new List<string> { "test", "unit" },
                    websiteId = 1,
                    sequence = 1,
                    page_id = 1
                }
            }
        };

        var request = CreateRequest(JsonConvert.SerializeObject(collectionPageDetail));

        var response = await functions.AssociatePagesWithCollection(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Deserialize the response body
        var result = JsonConvert.DeserializeObject<CollectionPageDetail>(response.Body);
        var expectedResult = await MockWebsiteProcessing.GetCollectionPageDetailFromJsonResource("BasicCollectionPageDetail.json");

        Assert.True(ModelComparer.AreEqual(result, expectedResult));
    }

    [Fact]
    public async Task TestDeleteCollectionMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with the required path parameter
        var request = CreateRequest(new Dictionary<string, string> { { "id", "1" } });

        var response = await functions.DeleteCollection(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Verify the response body indicates success
        var result = JsonConvert.DeserializeObject<bool>(response.Body);
        Assert.True(result);
    }

    [Fact]
    public async Task TestDeletePageFromCollectionMethod()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with the required path parameters
        var request = CreateRequest(new Dictionary<string, string> { { "id", "1" }, { "pageId", "1" } });

        var response = await functions.DeletePageFromCollection(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);

        // Verify the response body indicates success
        var result = JsonConvert.DeserializeObject<bool>(response.Body);
        Assert.True(result);
    }

    [Fact]
    public async Task TestSearchCollectionsMethodNotFound()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request with a search request that won't find anything
        var searchRequest = new searchCriteria()
        {
            Name = "NonExistentCollection",
            WebsiteId = "999"
        };

        var request = CreateRequest(JsonConvert.SerializeObject(searchRequest));

        var response = await functions.searchCollections(request, context);

        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestGetCollectionPageAsyncMethodBadRequest()
    {
        var context = new TestLambdaContext();
        var mockHandler = new MockWebsiteProcessing();
        var functions = new ApiFunctions(mockHandler);

        // Prepare the request without the required path parameter
        var request = CreateRequest(new Dictionary<string, string>());

        var response = await functions.GetCollectionPageAsync(request, context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Invalid path parameters for Id", response.Body);
    }
}
