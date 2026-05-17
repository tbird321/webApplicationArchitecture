using MySQLConnector;
using MySQLConnector.Models;

namespace MySQLConnectorTest
{
    internal class PageTests: BaseADOTest
    {
        private PageDAO pageDAO;
        private KeywordDAO keywordDAO;
        private TopicDAO topicDAO;
        private List<PageModel> pageModels;
        private ConnectionModel connectionModel;
        [SetUp]
        public async Task Setup()
        {
            pageModels = new List<PageModel>();
            if (connectionModel == null)
            {
                ConnectionManager conTest = new ConnectionManager();
                connectionModel = await conTest.GetSecretAsync("Dev");
            }
            pageDAO = new PageDAO(connectionModel);
            keywordDAO = new KeywordDAO(connectionModel);
            topicDAO = new TopicDAO(connectionModel);
        }

        [TearDown] 
        public void TearDown() {
            
        }

        [Test]
        public async Task InsertPage()
        {
            int pageId=0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "InsertPage Test Page", description = "InsertPage Test Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);
                newPage.id = pageId; // Set the ID for cleanup

                Assert.IsTrue(pageId > 0, "Page ID should be greater than 0");

                var retrievedPage = await pageDAO.GetPageAsync(pageId, websiteId);
                Assert.IsNotNull(retrievedPage, "Retrieved page should not be null");
                Assert.AreEqual(newPage.name, retrievedPage.name);
                Assert.AreEqual(newPage.description, retrievedPage.description);
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
           
        }

        [Test]
        public async Task UpdatePage()
        {
            int pageId = 0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "Test Page", description = "Test Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);
                newPage.id = pageId;
                newPage.name = "Updated Name";
                pageModels.Add(newPage);

                await pageDAO.UpdatePage(newPage); 

                var updatedPage = await pageDAO.GetPageAsync(pageId, websiteId);
                Assert.AreEqual(newPage.description, updatedPage.description);
                Assert.AreEqual("Updated Name", updatedPage.name);
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }

        [Test]
        public async Task DeletePage()
        {

            int pageId = 0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "Delete Page", description = "Test Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);

                await pageDAO.DeletePage(pageId);

                var deletedPage = await pageDAO.GetPageAsync(pageId, websiteId);
                Assert.IsNull(deletedPage.id, "Deleted page should not be retrievable");
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }

        [Test]
        public async Task UpsertPage_Insert()
        {
            int pageId = 0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "Upsert Insert Page", description = "Test Description", layoutid = 1 };
                pageId = await pageDAO.UpsertPage(newPage);
                newPage.id = pageId;
                pageModels.Add(newPage);

                Assert.IsTrue(pageId > 0, "Page ID should be greater than 0");

                var retrievedPage = await pageDAO.GetPageAsync(pageId, websiteId);
                Assert.IsNotNull(retrievedPage, "Retrieved page should not be null");
                Assert.AreEqual(newPage.name, retrievedPage.name);

            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }

        [Test]
        public async Task UpsertPage_Update()
        {
            int pageId = 0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "Upsert Update Page", description = "Test Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);
                newPage.id = pageId;
                pageModels.Add(newPage);

                int updatedPageId = await pageDAO.UpsertPage(newPage);

                Assert.AreEqual(pageId, updatedPageId, "Updated page ID should match original page ID");

                var updatedPage = await pageDAO.GetPageAsync(pageId, websiteId);
                Assert.AreEqual(newPage.name, updatedPage.name);
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }

        /*
        [Test]
        public async Task GetPageByNameAsync()
        {
            int pageId = 0;
            try
            {

                var retrievedPage = await pageDAO.GetPageByNameAsync("Home");

                Assert.IsNotNull(retrievedPage, "Retrieved page should not be null");
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }
        */

        [Test]
        public async Task GetPageAsync()
        {
            int pageId = 0;
            int websiteId = 1;
            try
            {
                var newPage = new PageModel { name = "Get Page", description = "Test Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);
                newPage.id = pageId;
                pageModels.Add(newPage);

                var retrievedPage = await pageDAO.GetPageAsync(pageId, websiteId);

                Assert.IsNotNull(retrievedPage, "Retrieved page should not be null");
                Assert.AreEqual(newPage.name, retrievedPage.name);
                Assert.AreEqual(newPage.description, retrievedPage.description);
            }
            finally
            {
                await pageDAO.DeletePage(pageId);
            }
        }


        [Test]
        public async Task SearchPages()
        {
            int pageId = 0;
            try
            {
                var searchKeywords = new List<string> { PAGE_KEYWORD_1, ARTICLE_KEYWORD_1 };
                var searchTopics = new List<string> { ARTICLE_TOPIC_1,PAGE_TOPIC_2 };
                string searchName = "Sample Page";
                string searchDescription = "Sample Description";

                var newPage = new PageModel { name = searchName, description = searchDescription, layoutid = 1 };
                pageId = await pageDAO.InsertPage(newPage);
                newPage.id = pageId;
                pageModels.Add(newPage);
                //Search by Name
                var pages = await pageDAO.SearchPages(searchKeywords, searchTopics, searchName, searchDescription,null);

                Assert.IsNotNull(pages, "Search result should not be null");
                Assert.IsNotEmpty(pages, "Search result should contain pages");
            }
            finally
            {
                //no associations to delete
                using (var connection=pageDAO.CreateMySqlConnection())
                {
                    connection.Open();
                    await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(pageId, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_1, connection);
                    await keywordDAO.DeleteKeyword(ARTICLE_KEYWORD_1, connection);
                    await topicDAO.DeleteTopic(ARTICLE_TOPIC_1, connection);
                    await topicDAO.DeleteTopic(PAGE_TOPIC_2, connection);
                    connection.Close();
                }
                
            }
        }

        [Test]
        public async Task SearchPagesByPageAndArticleInfo()
        {
            int pageId = 0;
            try
            {
                var searchKeywords = new List<string> { PAGE_KEYWORD_1, ARTICLE_KEYWORD_1 };
                var searchTopics = new List<string> { ARTICLE_TOPIC_1, PAGE_TOPIC_2 };
                string searchName = "Sample Page";
                string searchDescription = "Sample Description";

                PageModel basicModel = await GetPageModelFromJsonResource("BasicPage.json");

                await pageDAO.UpsertPageWithArticles(basicModel);
                pageId = basicModel.id.Value;

                var pages = await pageDAO.SearchPagesByPageAndArticleInfo(searchKeywords, searchTopics, searchName, searchDescription,null);

                Assert.IsNotNull(pages, "Search result should not be null");
                Assert.IsNotEmpty(pages, "Search result should contain pages");
            }
            finally
            {
                using (var connection = pageDAO.CreateMySqlConnection())
                {
                    connection.Open();
                    await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(pageId, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_1, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_2, connection);
                    await keywordDAO.DeleteKeyword(ARTICLE_KEYWORD_1, connection);
                    await keywordDAO.DeleteKeyword(ARTICLE_KEYWORD_2, connection);
                    await topicDAO.DeleteTopic(ARTICLE_TOPIC_1, connection);
                    await topicDAO.DeleteTopic(ARTICLE_TOPIC_2, connection);
                    await topicDAO.DeleteTopic(PAGE_TOPIC_2, connection);
                    await topicDAO.DeleteTopic(PAGE_TOPIC_1, connection);
                    connection.Close();
                }               
            }
           
        }


        [Test]
        public async Task AssociateWithKeyword()
        {
            int pageId = 0;
            int websiteId = 1;
            try
            {
                var page = new PageModel { name = "Test Page for Keyword", description = "Description", layoutid = 1 };
                pageId = await pageDAO.InsertPage(page);
                page.id = pageId;
                pageModels.Add(page);

                int keywordId = await keywordDAO.UpsertKeyword(PAGE_KEYWORD_1);
                await pageDAO.AssociateWithKeyword(pageId, keywordId);

                // Verify association (retrieve page and check if keyword is associated)
                var updatedPage = await pageDAO.GetPageDetailsAsync(pageId,websiteId,null);
                Assert.IsTrue(updatedPage.keywords.Contains(PAGE_KEYWORD_1), "Page should be associated with "+ PAGE_KEYWORD_1);
            }
            finally
            {
                using (var connection = pageDAO.CreateMySqlConnection())
                {
                    connection.Open(); ;
                    await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(pageId, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_1, connection);
                    connection.Close();
                }
            }
        }
        
        [Test]
        public async Task SearchForPagesByNameDescriptionKeywordsAndTopic()
        {

            PageModel basicModel = await GetPageModelFromJsonResource("BasicPage.json");
            PageModel basicModel2 = await GetPageModelFromJsonResource("BasicPage2.json");
            try
            {

            await pageDAO.UpsertPageWithArticles(basicModel);
            await pageDAO.UpsertPageWithArticles(basicModel2);

            List<String> keywordList = new List<String>();
            List<String> topicList = new List<string>();
            string description = "Description";
            string name = null;
            List<PageModel> list = await pageDAO.SearchPages(keywordList, topicList,name,description,"1");
            Assert.AreEqual(true, list.Count>=3);
            foreach (PageModel pageModel in list)
            {
                if (pageModel.name== "page 1")
                {
                    Assert.AreEqual(basicModel.articles.Count, pageModel.articles.Count);
                }
            }
           

            //page 1
            name = "page 1";
            description = null;
            list = await pageDAO.SearchPages(keywordList, topicList, name, description, null);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(basicModel.articles.Count, list[0].articles.Count);
            Assert.Pass();
            }
            finally
            {
                using (var connection = pageDAO.CreateMySqlConnection())
                {
                    connection.Open();
                    await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(basicModel.id.Value, connection);
                    await pageDAO.DeletePageAndAllofItsArticlesAndAssociations(basicModel2.id.Value, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_1, connection);
                    await keywordDAO.DeleteKeyword(PAGE_KEYWORD_2, connection);
                    await keywordDAO.DeleteKeyword(ARTICLE_KEYWORD_1, connection);
                    await keywordDAO.DeleteKeyword(ARTICLE_KEYWORD_2, connection);
                    await topicDAO.DeleteTopic(ARTICLE_TOPIC_1, connection);
                    await topicDAO.DeleteTopic(ARTICLE_TOPIC_2, connection);
                    await topicDAO.DeleteTopic(PAGE_TOPIC_2, connection);
                    await topicDAO.DeleteTopic(PAGE_TOPIC_1, connection);
                    connection.Close();
                }
            }
            return;
        }
      
    }
}
