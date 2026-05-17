using MySql.Data.MySqlClient;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationArch.Tests
{
    internal class MockWebsiteProcessing : IWebsiteProcessing
    {
        public string? GetEnvironment { get; set; }
        public static async Task<PageModel> GetPageModelFromJsonResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WebApplicationArch.Tests.TestModels." + resourceFileName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    PageModel pageModel = JsonConvert.DeserializeObject<PageModel>(json);
                    return pageModel;
                }
            }
        }

        public static async Task<CollectionModel> GetCollectionModelFromJsonResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WebApplicationArch.Tests.TestModels." + resourceFileName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    CollectionModel collectionModel = JsonConvert.DeserializeObject<CollectionModel>(json);
                    return collectionModel;
                }
            }
        }

        public static async Task<CollectionPageDetail> GetCollectionPageDetailFromJsonResource(string resourceFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WebApplicationArch.Tests.TestModels." + resourceFileName;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    CollectionPageDetail collectionPageDetail = JsonConvert.DeserializeObject<CollectionPageDetail>(json);
                    return collectionPageDetail;
                }
            }
        }

        public Task DeleteAllAssociationsForPage(int pageId, MySqlConnection connection)
        {
            throw new NotImplementedException();
        }

        public Task DeleteKeyword(string keyword, MySqlConnection? connection = null)
        {
            throw new NotImplementedException();
        }

        public Task DeletePageAndAllofItsArticlesAndAssociations(int pageId, MySqlConnection connection)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTopic(string topic, MySqlConnection? connection = null)
        {
            throw new NotImplementedException();
        }

        public async Task<PageModel> GetPageAsync(int pageId,int websiteId)
        {
            return await GetPageModelFromJsonResource("BasicPage.json");
        }

        public Task<int> InsertWebsite(string name, string description, MySqlConnection? connection = null)
        {
            throw new NotImplementedException();
        }

        public Task InsertWebsitePage(int websiteId, int pageId, MySqlConnection? connection = null)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ArticleModel>> SearchArticles(List<string> keywords, List<string> topics, string? name, string? description, string? websiteId)
        {
            PageModel pageModel = await GetPageModelFromJsonResource("BasicPage.json");
            List<ArticleModel> articles = new List<ArticleModel>()
            {
                pageModel.articles[0],
                pageModel.articles[1],
            };
            return articles;
        }

        public async Task<List<PageModel>> SearchPages(List<string> keywords, List<string> topics, string? name, string? description, string? websiteId)
        {
            
            List<PageModel> pages = new List<PageModel>()
            {
                await GetPageModelFromJsonResource("BasicPage.json")
            };
            return pages;
        }

        public Task<List<PageModel>> SearchPagesByPageAndArticleInfo(List<string> keywords, List<string> topics, string? name, string? description, string? websiteId)
        {
            throw new NotImplementedException();
        }

        public Task UpsertArticleKeyword(int articleId, int keywordId, MySqlConnection? myConnect = null)
        {
            throw new NotImplementedException();
        }

        public Task UpsertArticleTopic(int articleId, int topicId, MySqlConnection? myConnect = null)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpsertKeyword(string keywordValue, MySqlConnection? myConnect = null)
        {
            throw new NotImplementedException();
        }

        public Task UpsertPageKeyword(int pageId, int keywordId, MySqlConnection? myConnect = null)
        {
            throw new NotImplementedException();
        }

        public Task UpsertPageTopic(int pageId, int topicId, MySqlConnection? myConnect = null)
        {
            throw new NotImplementedException();
        }

        public Task UpsertPageWithArticles(PageModel pageModel)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpsertTopic(string topicValue, MySqlConnection? myConnect = null,int topicId=0)
        {
            throw new NotImplementedException();
        }

        public async Task<ArticleModel> GetArticleAsync(int articleId)
        {
            var articleInfo = await GetPageModelFromJsonResource("BasicPage.json");
            return articleInfo.articles[0];
        }

        public Task<Tuple<List<string>, List<string>>> GetTopicsAndKeywords()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetTopics()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetKeywords()
        {
            throw new NotImplementedException();
        }

        public Task<List<WebsiteModel>> GetWebsites()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetLayouts()
        {
            throw new NotImplementedException();
        }

        public Task<int> UpsertKeyword(string keywordValue, MySqlConnection? myConnect = null, int keywordId = 0)
        {
            throw new NotImplementedException();
        }

        public Task<PageModel> GetPageAsyncByName(string pageName,string? websiteId)
        {
            throw new NotImplementedException();
        }

        public Task<PageModel> SavePageAndArticlesAsync(PageModel pageName)
        {
            throw new NotImplementedException();
        }

        public Task<ArticleModel> UpsertArticle(ArticleModel article)
        {
            throw new NotImplementedException();
        }

        public async Task<CollectionModel> UpsertCollection(CollectionModel collection)
        {
            return await GetCollectionModelFromJsonResource("BasicCollection.json");
        }

        public async Task<CollectionModel> GetCollectionAsync(int collectionId)
        {
            return await GetCollectionModelFromJsonResource("BasicCollection.json");
        }

        public async Task<CollectionPageDetail> GetCollectionPageAsync(int collectionId)
        {
            return await GetCollectionPageDetailFromJsonResource("BasicCollectionPageDetail.json");
        }

        public async Task<CollectionPageDetail> AssociateCollectionWithPage(CollectionPageModel collectionPage)
        {
            return await GetCollectionPageDetailFromJsonResource("BasicCollectionPageDetail.json");
        }

        public async Task<bool> DeletePageFromCollection(int collectionId, int pageId)
        {
            return true;
        }

        public async Task DeleteCollection(int collectionId)
        {
            // Mock implementation - just return
            await Task.CompletedTask;
        }

        Task<bool> IWebsiteProcessing.DeleteCollection(int collectionId)
        {
            return Task.FromResult(true);
        }

        public async Task<int> AssociatePagesWithCollection(CollectionPageDetail collectionPage)
        {
            return 1;
        }

        async Task<CollectionPageDetail> IWebsiteProcessing.AssociatePagesWithCollection(CollectionPageDetail collectionPage)
        {
            return await GetCollectionPageDetailFromJsonResource("BasicCollectionPageDetail.json");
        }

        public async Task<List<CollectionModel>> SearchCollections(string? name, string? websiteId)
        {
            List<CollectionModel> collections = new List<CollectionModel>()
            {
                await GetCollectionModelFromJsonResource("BasicCollection.json")
            };
            return collections;
        }

        public Task PublishPage(int pageId) => Task.CompletedTask;
        public Task UnpublishPage(int pageId) => Task.CompletedTask;
        public Task PublishArticle(int articleId) => Task.CompletedTask;
        public Task UnpublishArticle(int articleId) => Task.CompletedTask;
    }
}
