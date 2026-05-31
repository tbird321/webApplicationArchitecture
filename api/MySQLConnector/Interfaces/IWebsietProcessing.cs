using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Interfaces
{
    public interface IWebsiteProcessing
    {
        string GetEnvironment { get; set; }
        Task<int> UpsertTopic(string topicValue, MySqlConnection? myConnect = null, int topicId = 0);
        Task<int> UpsertKeyword(string keywordValue, MySqlConnection? myConnect = null, int keywordId= 0);
        Task UpsertArticleKeyword(int articleId, int keywordId, MySqlConnection? myConnect = null);
        Task UpsertArticleTopic(int articleId, int topicId, MySqlConnection? myConnect = null);
        Task UpsertPageTopic(int pageId, int topicId, MySqlConnection? myConnect = null);
        Task UpsertPageKeyword(int pageId, int keywordId, MySqlConnection? myConnect = null);
        Task DeleteAllAssociationsForPage(int pageId, MySqlConnection connection);
        Task DeleteKeyword(string keyword, MySqlConnection? connection = null);
        Task DeleteTopic(string topic, MySqlConnection? connection = null);
        Task<int> InsertWebsite(string name, string description, MySqlConnection? connection = null);
        Task InsertWebsitePage(int websiteId, int pageId, MySqlConnection? connection = null);
        Task DeletePageAndAllofItsArticlesAndAssociations(int pageId, MySqlConnection connection);
        Task DeletePage(int pageId);
        Task<PageModel> GetPageAsync(int pageId,int websiteID);
        Task<PageModel> GetPageAsyncByName(string pageName,String? websiteID);
        Task <PageModel> SavePageAndArticlesAsync(PageModel pageName);
        Task<ArticleModel> UpsertArticle(ArticleModel article);
        Task<CollectionModel> UpsertCollection(CollectionModel collection);
        Task<List<CollectionModel>> SearchCollections(String? name, String? websiteId);
        Task<CollectionPageDetail> GetCollectionPageAsync(int collectionId);
        Task<CollectionPageDetail> AssociatePagesWithCollection(CollectionPageDetail collectionPage);
        Task<bool> DeletePageFromCollection(int collectionId, int pageId);
        Task<bool> DeleteCollection(int collectionId);
        Task<ArticleModel> GetArticleAsync(int articleId);
        Task<List<PageModel>> SearchPages(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId);
        Task<List<ArticleModel>> SearchArticles(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId);
        Task<List<PageModel>> SearchPagesByPageAndArticleInfo(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId);
        
        Task<List<string>> GetTopics();
        Task<List<string>> GetKeywords();
        Task<List<WebsiteModel>> GetWebsites();
        Task<List<string>> GetLayouts();
        Task PublishPage(int pageId);
        Task UnpublishPage(int pageId);
        Task PublishArticle(int articleId);
        Task UnpublishArticle(int articleId);
    }

}
