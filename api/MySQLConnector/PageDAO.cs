using MySql.Data.MySqlClient;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class PageDAO : BaseMYSQLProcessing, IKeywordRelationshipUpdate, ITopicRelationshipUpdate
    {
        private ArticleDAO articleDAO;

        public PageDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
            articleDAO = new ArticleDAO(connectionModel);

        }

        public async Task<int> InsertPage(PageModel page, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO page (name, description, layoutid) VALUES (@name, @description, @layoutid); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", page.name);
            command.Parameters.AddWithValue("@description", page.description);
            command.Parameters.AddWithValue("@layoutid", page.layoutid);

            int pageId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return pageId;
        }

        public async Task UpdatePage(PageModel page, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE page SET name = @name, description = @description, layoutid = @layoutid WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", page.id);
            command.Parameters.AddWithValue("@name", page.name);
            command.Parameters.AddWithValue("@description", page.description);
            command.Parameters.AddWithValue("@layoutid", page.layoutid);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task SetPageStatus(int pageId, string status, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE page SET status = @status WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", pageId);
            command.Parameters.AddWithValue("@status", status);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeletePage(int pageId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM page WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", pageId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertPage(PageModel page, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UpsertPage", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("_id", MySqlDbType.Int32).Value = page.id;
            command.Parameters["_id"].Direction = ParameterDirection.InputOutput;

            command.Parameters.AddWithValue("_name", page.name);
            command.Parameters.AddWithValue("_description", page.description);
            command.Parameters.AddWithValue("_layoutid", page.layoutid);
            command.Parameters.AddWithValue("_layoutName", page.layout);
            command.Parameters.AddWithValue("_style", page.style);
            command.Parameters.AddWithValue("_websiteId", page.websiteId);
            command.Parameters.AddWithValue("_status", page.status ?? "draft");

            await command.ExecuteNonQueryAsync();

            int newOrExistingId = (int)command.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return newOrExistingId;
        }

        public async Task DeleteAllAssociationsForPage(int pageId, MySqlConnection connection)
        {
            var deleteAllAssociationsCommand = new MySqlCommand("DeleteAllAssociationsForPage", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            deleteAllAssociationsCommand.Parameters.AddWithValue("_pageId", pageId);
            await deleteAllAssociationsCommand.ExecuteNonQueryAsync();
        }

        public async Task DeletePageAndAllofItsArticlesAndAssociations(int pageId, MySqlConnection connection)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }
            var deleteCommand = new MySqlCommand("DeletePageAndAssociations", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            deleteCommand.Parameters.AddWithValue("_pageId", pageId);
            await deleteCommand.ExecuteNonQueryAsync();
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<PageModel> GetPageDetailsAsync(int pageId,int websiteId, MySqlConnection connection)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var pageModel = new PageModel();

            using (var command = new MySqlCommand("GetPageDetails", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("pageId", pageId);
                command.Parameters.AddWithValue("websiteId", websiteId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // First Recordset: Retrieve page information
                    if (await reader.ReadAsync())
                    {
                        pageModel.id = reader.SafeRead<int>("id");
                        pageModel.name = reader.SafeRead<string>("name");
                        pageModel.description = reader.SafeRead<string>("description");
                        pageModel.layoutid = reader.SafeRead<int?>("layoutid");
                        pageModel.style=reader.SafeRead<string>("style");
                        pageModel.websiteId = reader.SafeRead<int>("websiteId");
                        // Add other fields as necessary
                    }

                    // Second Recordset: Retrieve keywords associated with the page
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        pageModel.keywords.Add(reader.GetString(0)); // Assuming keyword value is in the first column
                    }

                    // Third Recordset: Retrieve topics associated with the page
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        pageModel.topics.Add(reader.GetString(0)); // Assuming topic value is in the first column
                    }

                    // Fourth Recordset: Retrieve articles associated with the page
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        var article = new ArticleModel
                        {
                            id = reader.SafeRead<int>("id"),
                            sequence_no = reader.SafeRead<int>("sequence_no"),
                            articleId = reader.SafeRead<string>("articleId"),
                            name = reader.SafeRead<string>("name"),
                            description = reader.SafeRead<string>("description"),
                            memeImagePath = reader.SafeRead<string>("memeImagePath"),
                            articlePath = reader.SafeRead<string>("articlePath"),
                            topics = new List<string>(), // Initialize for later population
                            keywords = new List<string>()  // Initialize for later population
                        };
                        pageModel.articles.Add(article);
                    }

                    // Fifth Recordset: Retrieve keywords associated with the articles
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        int articleId = reader.GetInt32("article_id"); // Adjust based on actual column name
                        string keywordOrTopic = reader.GetString("value"); // Adjust based on actual column name

                        var article = pageModel.articles.FirstOrDefault(a => a.id == articleId);
                        if (article != null)
                        {
                            article.keywords.Add(keywordOrTopic);
                        }
                    }

                    //  Sixth Recordset: Retrieve topics associated with the articles
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        int articleId = reader.GetInt32("article_id"); // Adjust based on actual column name
                        string keywordOrTopic = reader.GetString("value"); // Adjust based on actual column name

                        var article = pageModel.articles.FirstOrDefault(a => a.id == articleId);
                        if (article != null)
                        {
                            article.topics.Add(keywordOrTopic);
                        }
                    }

                    // Seventh Recordset: Retrieve Layouts associated with page
                    await reader.NextResultAsync();
                    while (await reader.ReadAsync())
                    {
                        string layoutName = reader.GetString("name"); // Adjust based on actual column name
                        pageModel.layout = layoutName;
                    }
                }
            }
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
            return pageModel;
        }

        public async Task<PageModel> GetPageAsync(int pageId,int websiteId)
        {
            PageModel currentPage = null;
            try
            {
                using (var connection = CreateMySqlConnection())
                {
                    await connection.OpenAsync();
                    currentPage = await GetPageDetailsAsync(pageId, websiteId, connection);
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as necessary
                throw; // or handle the exception as needed
            }
            return currentPage;
        }

        public async Task<PageModel> GetPageByNameAsync(string pageName,String? websiteId)
        {
            PageModel currentPage = null;
            try
            {
                using (var connection = CreateMySqlConnection())
                {
                    await connection.OpenAsync();
                    var query = "SELECT id FROM page WHERE name = @pageName and websiteID = @websiteId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        int pageId = 0;
                        command.Parameters.AddWithValue("@pageName", pageName);
                        command.Parameters.AddWithValue("@websiteId", websiteId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                pageId = reader.SafeRead<int>("id");
                            }
                        }
                        if (pageId > 0)
                        {
                            int websiteID = 0;
                            int.TryParse(websiteId,out websiteID);
                            currentPage = await GetPageDetailsAsync(pageId, websiteID, connection);
                        }                        
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as necessary
                throw; // or handle the exception as needed
            }
            return currentPage;
        }


        public async Task<List<PageModel>> SearchPages(List<string> keywords, List<string> topics, String? name, String? description,String? websiteId)
        {
            List<PageModel> pages = new List<PageModel>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();
                Dictionary<int, PageModel> pageDict = await SearchPagesByKeywordAndTopic(keywords, topics, description, name, connection,websiteId);
                pages = await RetrievePagesbyPageId(pageDict, connection);
            }
            return pages;
        }

        public async Task<List<PageModel>> SearchPagesByPageAndArticleInfo(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId)
        {
            List<PageModel> pages = new List<PageModel>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();
                Dictionary<int, PageModel> pageDict = await SearchPagesByPageAndArticleKeywordAndTopic(keywords, topics, description, name, connection,websiteId);
                pages = await RetrievePagesbyPageId(pageDict, connection);
            }
            return pages;
        }

        private async Task<Dictionary<int, PageModel>> SearchPagesByKeywordAndTopic(
            List<string> keywords,
            List<string> topics,
            string? description,
            string? name,
            MySqlConnection connection,
            String? websiteId)
        {
            Dictionary<int, PageModel> results = new Dictionary<int, PageModel>();
            var queryBuilder = new StringBuilder("SELECT * FROM page_keywords_topics_view WHERE ");
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            int paramCount = 0;

            if (keywords != null && keywords.Count > 0)
            {
                var keywordConditions = keywords.Select(keyword => {
                    var paramName = $"@keyword{paramCount++}";
                    parameters.Add(new MySqlParameter(paramName, $"%{keyword}%"));
                    return $"page_keywords LIKE {paramName}";
                });
                conditions.Add("(" + string.Join(" OR ", keywordConditions) + ")");
            }

            if (topics != null && topics.Count > 0)
            {
                var topicConditions = topics.Select(topic => {
                    var paramName = $"@topic{paramCount++}";
                    parameters.Add(new MySqlParameter(paramName, $"%{topic}%"));
                    return $"page_topics LIKE {paramName}";
                });
                conditions.Add("(" + string.Join(" OR ", topicConditions) + ")");
            }

            if (!string.IsNullOrEmpty(description))
            {
                var paramName = $"@description{paramCount}";
                parameters.Add(new MySqlParameter(paramName, $"%{description}%"));
                conditions.Add($"page_description LIKE {paramName}");
            }

            var webParmName = $"@websiteId{paramCount}";
            parameters.Add(new MySqlParameter(webParmName, $"{websiteId}"));
            conditions.Add($"websiteId = {webParmName}");

            if (!string.IsNullOrEmpty(name))
            {
                var paramName = $"@name{paramCount}";
                parameters.Add(new MySqlParameter(paramName, $"%{name}%"));
                conditions.Add($"page_name LIKE {paramName}");
            }

            if (conditions.Count == 0)
            {
                queryBuilder.Append("1 = 1"); // Default condition if there are no filters
            }
            else
            {
                queryBuilder.Append(string.Join(" OR ", conditions));
            }

            using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int pageId = reader.GetInt32(reader.GetOrdinal("page_id"));
                        if (!results.ContainsKey(pageId))
                        {
                            PageModel currentPage = new PageModel
                            {
                                topics = reader.SafeRead<string>("page_topics")?.Split(',')?.ToList(),
                                keywords = reader.SafeRead<string>("page_keywords")?.Split(',')?.ToList()
                            };
                            results.Add(pageId, currentPage);
                        }
                    }
                }
            }
            return results;
        }


        private async Task<Dictionary<int, PageModel>> SearchPagesByPageAndArticleKeywordAndTopic(
            List<string> keywords,
            List<string> topics,
            string? description,
            string? name,
            MySqlConnection connection,
            string? websiteId)
        {
            Dictionary<int, PageModel> results = new Dictionary<int, PageModel>();
            var queryBuilder = new StringBuilder("SELECT * FROM get_page_article_keywords_topics_view WHERE ");
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            int paramCount = 0;

            if (keywords != null && keywords.Count > 0)
            {
                var keywordConditions = keywords.Select(keyword => {
                    var paramName = $"@keyword{paramCount++}";
                    parameters.Add(new MySqlParameter(paramName, $"%{keyword}%"));
                    return $"(article_keywords LIKE {paramName} OR page_keywords LIKE {paramName})";
                });
                conditions.Add("(" + string.Join(" OR ", keywordConditions) + ")");
            }

            if (topics != null && topics.Count > 0)
            {
                var topicConditions = topics.Select(topic => {
                    var paramName = $"@topic{paramCount++}";
                    parameters.Add(new MySqlParameter(paramName, $"%{topic}%"));
                    return $"(article_topics LIKE {paramName} OR page_topics LIKE {paramName})";
                });
                conditions.Add("(" + string.Join(" OR ", topicConditions) + ")");
            }

            if (!string.IsNullOrEmpty(description))
            {
                var descriptionParam = $"@description{paramCount}";
                parameters.Add(new MySqlParameter(descriptionParam, $"%{description}%"));
                conditions.Add($"(article_descriptions LIKE {descriptionParam} OR page_description LIKE {descriptionParam})");
            }

            if (!string.IsNullOrEmpty(websiteId))
            {
                var descriptionParam = $"@websiteId{paramCount}";
                parameters.Add(new MySqlParameter(descriptionParam, $"{websiteId}"));
                conditions.Add($"(websiteId =  {websiteId})");
            }

            if (!string.IsNullOrEmpty(name))
            {
                var nameParam = $"@name{paramCount}";
                parameters.Add(new MySqlParameter(nameParam, $"%{name}%"));
                conditions.Add($"(page_name LIKE {nameParam} OR article_names LIKE {nameParam})");
            }

            if (conditions.Count == 0)
            {
                queryBuilder.Append("1 = 1"); // Default condition if there are no filters
            }
            else
            {
                queryBuilder.Append(string.Join(" OR ", conditions));
            }

            using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int pageId = reader.GetInt32(reader.GetOrdinal("page_id"));
                        if (!results.ContainsKey(pageId))
                        {
                            PageModel currentPage = new PageModel
                            {
                                topics = reader.SafeRead<string>("page_topics")?.Split(',')?.ToList(),
                                keywords = reader.SafeRead<string>("page_keywords")?.Split(',')?.ToList()
                            };
                            results.Add(pageId, currentPage);
                        }
                    }
                }
            }
            return results;
        }


        private async Task<List<PageModel>> RetrievePagesbyPageId(Dictionary<int, PageModel> pagesToPopulate, MySqlConnection connection)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var pages = new List<PageModel>();
            var queryBuilder = new StringBuilder("SELECT * FROM get_page_articles_view WHERE ");
            queryBuilder.Append($" page_id in ({string.Join(",", pagesToPopulate.Keys)})");
            if (pagesToPopulate.Count == 0)
            {
                return pages;
            }
            HashSet<int> newPageIds = new HashSet<int>();
            using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {

                    while (await reader.ReadAsync())
                    {
                        int pageId = reader.GetInt32(reader.GetOrdinal("page_id"));
                        PageModel currentPage = pagesToPopulate[pageId];
                        if (!newPageIds.Contains(pageId))
                        {
                            newPageIds.Add(pageId);
                            RetrievePagesMapPage(reader, currentPage);
                            var curArticle = ArticleDAO.RetrievePagesMapArticle(reader);
                            currentPage.articles.Add(curArticle);
                        }
                        else
                        {
                            var curArticle = ArticleDAO.RetrievePagesMapArticle(reader);
                            currentPage.articles.Add(curArticle);
                        }
                    }
                }
            }
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
            return new List<PageModel>(pagesToPopulate.Values);
        }

        private void RetrievePagesMapPage(DbDataReader reader, PageModel currentPage)
        {
            currentPage.id = reader.SafeRead<int>("page_id");
            currentPage.name = reader.SafeRead<string>("page_name");
            currentPage.description = reader.SafeRead<string>("page_description");
            currentPage.style = reader.SafeRead<string>("page_style");
            currentPage.layout = reader.SafeRead<string>("layout_name");
            currentPage.layoutid = reader.SafeRead<int>("layout_id");
            currentPage.topics = reader.SafeRead<string>("page_topics")?.Split(',')?.ToList() ?? new List<string>();
            currentPage.keywords = reader.SafeRead<string>("page_keywords")?.Split(',')?.ToList() ?? new List<string>();
            currentPage.websiteId = reader.SafeRead<int>("websiteId");
        }

        private async Task CacheKeywordsAndTopics(List<string> topics, List<string> keywords, Dictionary<string, int> keywordCache, Dictionary<string, int> topicCache, MySqlConnection connection)
        {
            TopicDAO topicProcessing = new TopicDAO(CurrentConnectionInfo);
            KeywordDAO keywordProcessing= new KeywordDAO(CurrentConnectionInfo);

            foreach (var keywordString in keywords)
            {
                if (!keywordCache.TryGetValue(keywordString, out var keywordId))
                {
                    keywordId = await keywordProcessing.UpsertKeyword(keywordString, connection);
                    keywordCache[keywordString] = keywordId;
                }
            }

            foreach (var topicString in topics)
            {
                if (!topicCache.TryGetValue(topicString, out var topicId))
                {
                    topicId = await topicProcessing.UpsertTopic(topicString, connection, topicId);
                    topicCache[topicString] = topicId;
                }
            }
        }

        public async Task AssociateWithKeyword(int pageId, int keywordId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var pageKeywordUpsertCommand = new MySqlCommand("UpsertPageKeyword", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            pageKeywordUpsertCommand.Parameters.AddWithValue("_pageId", pageId);
            pageKeywordUpsertCommand.Parameters.AddWithValue("_keywordId", keywordId);

            await pageKeywordUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

        public async Task AssociateWithArticle(int pageId, int articleId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var pageKeywordUpsertCommand = new MySqlCommand("UpsertPageArticle", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            pageKeywordUpsertCommand.Parameters.AddWithValue("_pageId", pageId);
            pageKeywordUpsertCommand.Parameters.AddWithValue("_articleId", articleId);

            await pageKeywordUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

        public async Task AssociateWithTopic(int pageId, int topicId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var pageTopicUpsertCommand = new MySqlCommand("UpsertPageTopic", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            pageTopicUpsertCommand.Parameters.AddWithValue("_pageId", pageId);
            pageTopicUpsertCommand.Parameters.AddWithValue("_topicId", topicId);

            await pageTopicUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

        public async Task<PageModel> UpsertPageWithArticles(PageModel pageModel)
        {
            Dictionary<string, int> keywordCache = new Dictionary<string, int>();
            Dictionary<string, int> topicCache = new Dictionary<string, int>();

            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();

                //Use transactions to validate that all adjustments were made before comit
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        await CacheKeywordsAndTopics(pageModel.topics, pageModel.keywords, keywordCache, topicCache, connection);

                        pageModel.id = pageModel.id > 0 ? pageModel.id : 0;

                        pageModel.id = await UpsertPage(pageModel, connection);

                        //remove all article and keyword associates for the page and all articles within that page... 
                        await DeleteAllAssociationsForPage(pageModel.id.Value, connection);

                        //connect page keywords
                        foreach (var keywordString in pageModel.keywords)
                        {
                            await AssociateWithKeyword(pageModel.id.Value, keywordCache[keywordString], connection);
                        }

                        //connect page topics
                        foreach (var topicString in pageModel.topics)
                        {
                            await AssociateWithTopic(pageModel.id.Value, topicCache[topicString], connection);
                        }

                        foreach (var article in pageModel.articles)
                        {
                            await CacheKeywordsAndTopics(article.topics, article.keywords, keywordCache, topicCache, connection);

                            article.id = await articleDAO.UpsertArticle(article);
                            await AssociateWithArticle(pageModel.id.Value,article.id.Value, connection);
                            //connect article keywords
                            foreach (var keywordString in article.keywords)
                            {
                                await articleDAO.AssociateWithKeyword(article.id.Value, keywordCache[keywordString], connection);
                            }

                            //connect article topics
                            foreach (var topicString in article.topics)
                            {
                                await articleDAO.AssociateWithTopic(article.id.Value, topicCache[topicString], connection);
                            }
                        }
                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        // If something goes wrong, roll back the transaction
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                await connection.CloseAsync();
                return pageModel;
            }
        }

    }
}
