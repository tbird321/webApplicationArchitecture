using MySql.Data.MySqlClient;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MySQLConnector
{
    public class WebsiteProcessing: BaseMYSQLProcessing, IWebsiteProcessing
    {
        PageDAO pageDAO;
        TopicDAO topicDAO;
        KeywordDAO keywordDAO;
        ArticleDAO articleDAO;
        CollectionDAO collectionDAO;
        WebsiteDAO websiteDAO;
        LayoutDAO layoutDAO;

        public string GetEnvironment { get; set; }

        public WebsiteProcessing(ConnectionModel connectionModel) : base(connectionModel) 
        {
            pageDAO=new PageDAO(connectionModel);
            topicDAO=new TopicDAO(connectionModel); 
            keywordDAO = new KeywordDAO(connectionModel);
            articleDAO = new ArticleDAO(connectionModel);
            collectionDAO = new CollectionDAO(connectionModel);
            websiteDAO = new WebsiteDAO(connectionModel);
            layoutDAO=new LayoutDAO(connectionModel);
        }

        public async Task<int> UpsertTopic(string topicValue, MySqlConnection? myConnect = null, int topicId = 0)
        {

            return await topicDAO.UpsertTopic(topicValue, myConnect, topicId);
        }


        public async Task<int> UpsertKeyword(string keywordValue, MySqlConnection? myConnect = null, int keywordId = 0)
        {
            return await keywordDAO.UpsertKeyword(keywordValue, myConnect, keywordId);
        }

        public async Task UpsertArticleKeyword(int articleId, int keywordId, MySqlConnection? myConnect = null)
        {
            await articleDAO.AssociateWithKeyword(articleId, keywordId,myConnect);
        }

        public async Task UpsertArticleTopic(int articleId, int topicId, MySqlConnection? myConnect = null)
        {
            await articleDAO.AssociateWithTopic(articleId, topicId,myConnect);
        }

        public async Task UpsertPageTopic(int pageId, int topicId, MySqlConnection? myConnect = null)
        {            
            await pageDAO.AssociateWithTopic(pageId, topicId,myConnect);
        }

        public async Task UpsertPageKeyword(int pageId, int keywordId, MySqlConnection? myConnect = null)
        {
            await pageDAO.AssociateWithKeyword(pageId, keywordId,myConnect);
        }

        public async Task DeleteAllAssociationsForPage(int pageId, MySqlConnection connection)
        {
            await pageDAO.DeleteAllAssociationsForPage(pageId,connection);
        }

        public async Task DeleteKeyword(string keyword, MySqlConnection? connection = null)
        {
            await keywordDAO.DeleteKeyword(keyword,connection);
        }

        public async Task DeleteTopic(string topic, MySqlConnection? connection = null)
        {
            await topicDAO.DeleteTopic(topic,connection);
        }

        public async Task<int> InsertWebsite(string name, string description, MySqlConnection? connection = null)
        {
            return await websiteDAO.InsertWebsite(name,description,connection);
        }

        public async Task InsertWebsitePage(int websiteId, int pageId, MySqlConnection? connection = null)
        {
            await websiteDAO.AssociateWithPage(websiteId,pageId,connection);
        }


        public async Task UpsertPageWithArticles(PageModel pageModel)
        {
            await pageDAO.UpsertPageWithArticles(pageModel);
        }

        public async Task DeletePageAndAllofItsArticlesAndAssociations(int pageId, MySqlConnection connection)
        {
            await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(pageId,connection);
        }

        public async Task<List<string>> GetKeywords()
        {
            return await keywordDAO.GetKeywords();
        }

        public async Task<List<string>> GetTopics()
        {
            return await topicDAO.GetTopics();
        }

        public async Task<List<string>> GetLayouts()
        {
            return await layoutDAO.GetLayouts();
        }

        public async Task<List<WebsiteModel>> GetWebsites()
        {
            return await websiteDAO.GetWebsites();
        }

        public async Task<PageModel> GetPageAsync(int pageId,int websiteId)
        {
            return await pageDAO.GetPageAsync(pageId,websiteId);
        }

        public async Task<ArticleModel> GetArticleAsync(int articleId)
        {
            return await articleDAO.GetArticleAsync(articleId);
        }

        public async Task<List<PageModel>> SearchPages(List<string> keywords, List<string> topics,String? name,String? description,String? websiteId)
        {
            return await pageDAO.SearchPages(keywords, topics, name, description, websiteId);
        }

        public async Task<List<ArticleModel>> SearchArticles(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId)
        {
            return await articleDAO.SearchArticles(keywords, topics, name, description, websiteId);
        }

        public async Task<List<PageModel>> SearchPagesByPageAndArticleInfo(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId)
        {
            return await pageDAO.SearchPagesByPageAndArticleInfo(keywords, topics, name, description, websiteId);
        }

        public async Task<PageModel> GetPageAsyncByName(string pageName,string websiteId)
        {
           return await pageDAO.GetPageByNameAsync(pageName, websiteId);
        }

        public async Task<PageModel> SavePageAndArticlesAsync(PageModel pageInfo)
        {
            return await pageDAO.UpsertPageWithArticles(pageInfo);
        }

        public async Task<ArticleModel> UpsertArticle(ArticleModel article)
        {
            using (var connection = CreateMySqlConnection())
            {
                connection.Open();
                int articleId = await articleDAO.UpsertArticle(article, connection);
                article.id = articleId;
            }                            
            return article;
        }
        public async Task<CollectionModel> UpsertCollection(CollectionModel collection)
        {
            using (var connection = CreateMySqlConnection())
            {
                connection.Open();
                int collectionId = await collectionDAO.UpsertCollection(collection, connection);
                collection.id = collectionId;
            }
            return collection;
        }
        public async Task<CollectionPageDetail> GetCollectionPageAsync(int collectionId)
        {
            return await collectionDAO.GetCollectionPageAsync(collectionId);
        }

        public async Task<CollectionPageDetail> AssociatePagesWithCollection(CollectionPageDetail collectionPageDetail)
        {
            return await collectionDAO.AssociatePagesWithCollection(collectionPageDetail);
        }

        public async Task<bool> DeletePageFromCollection(int collectionId, int pageId)
        {
            return await collectionDAO.DeletePageFromCollection(collectionId,pageId);
        }

        public async Task<bool> DeleteCollection(int collectionId)
        {
            return await collectionDAO.DeleteCollection(collectionId);
        }

        public Task<int> AssociateCollectionWithPage(CollectionPageModel collectionPage)
        {
            throw new NotImplementedException();
        }

        async Task<CollectionPageDetail> IWebsiteProcessing.AssociatePagesWithCollection(CollectionPageDetail collectionPage)
        {
            return await collectionDAO.AssociatePagesWithCollection(collectionPage);
        }

        public  async Task<List<CollectionModel>> SearchCollections(string? name, string? websiteId)
        {
            return await collectionDAO.SearchCollections(name, websiteId);
        }

        public async Task PublishPage(int pageId)
        {
            await pageDAO.SetPageStatus(pageId, "published");
        }

        public async Task UnpublishPage(int pageId)
        {
            await pageDAO.SetPageStatus(pageId, "draft");
        }

        public async Task PublishArticle(int articleId)
        {
            await articleDAO.SetArticleStatus(articleId, "published");
        }

        public async Task UnpublishArticle(int articleId)
        {
            await articleDAO.SetArticleStatus(articleId, "draft");
        }

    }


}