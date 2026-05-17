using MySql.Data.MySqlClient;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class ArticleDAO : BaseMYSQLProcessing, IKeywordRelationshipUpdate,ITopicRelationshipUpdate
    {
        public ArticleDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }

        public async Task<int> InsertArticle(ArticleModel article, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO article (name, description, articlepath, memepath, sequence_no, articleid,websiteId) VALUES (@name, @description, @articlepath, @memepath, @sequence_no, @articleid,@websiteId); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", article.name);
            command.Parameters.AddWithValue("@description", article.description);
            command.Parameters.AddWithValue("@articlepath", article.articlePath);
            command.Parameters.AddWithValue("@memepath", article.memeImagePath);
            command.Parameters.AddWithValue("@sequence_no", article.sequence_no);
            command.Parameters.AddWithValue("@articleid", article.articleId);
            command.Parameters.AddWithValue("@websiteId", article.websiteId);

            int articleId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return articleId;
        }

        public async Task UpdateArticle(ArticleModel article, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE article SET name = @name, description = @description, articlepath = @articlepath, memepath = @memepath, sequence_no = @sequence_no, articleid = @articleid WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", article.id);
            command.Parameters.AddWithValue("@name", article.name);
            command.Parameters.AddWithValue("@description", article.description);
            command.Parameters.AddWithValue("@articlepath", article.articlePath);
            command.Parameters.AddWithValue("@memepath", article.memeImagePath);
            command.Parameters.AddWithValue("@sequence_no", article.sequence_no);
            command.Parameters.AddWithValue("@articleid", article.articleId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task SetArticleStatus(int articleId, string status, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE article SET status = @status WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", articleId);
            command.Parameters.AddWithValue("@status", status);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteArticle(int articleId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM article WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", articleId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertArticle(ArticleModel article, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UpsertArticle", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Assuming '_id' is an INOUT parameter in your stored procedure
            command.Parameters.Add("_id", MySqlDbType.Int32).Value = article.id;
            command.Parameters["_id"].Direction = ParameterDirection.InputOutput;

            // Add other parameters
            command.Parameters.AddWithValue("_name", article.name);
            command.Parameters.AddWithValue("_description", article.description);
            command.Parameters.AddWithValue("_articlePath", article.articlePath);
            command.Parameters.AddWithValue("_memeImagePath", article.memeImagePath);
            command.Parameters.AddWithValue("_sequence_no", article.sequence_no);
            command.Parameters.AddWithValue("_articleId", article.articleId);
            command.Parameters.AddWithValue("_websiteId", article.websiteId);

            await command.ExecuteNonQueryAsync();

            // Retrieve the new or existing ID
            int newOrExistingId = (int)command.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return newOrExistingId; // This ID can then be used for further processing or association
        }

        public async Task UpsertArticleKeyword(int articleId, int keywordId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var articleKeywordUpsertCommand = new MySqlCommand("UpsertArticleKeyword", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            articleKeywordUpsertCommand.Parameters.AddWithValue("_articleId", articleId);
            articleKeywordUpsertCommand.Parameters.AddWithValue("_keywordId", keywordId);

            await articleKeywordUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

        public async Task<ArticleModel> GetArticleAsync(int articleId)
        {
            ArticleModel currentArticle = null;
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();
                var query = "SELECT * FROM article_keywords_topics_view WHERE article_id = @articleID";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@articleID", articleId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            currentArticle = RetrievePagesMapArticle(reader);
                        }
                    }
                }
            }
            return currentArticle;
        }

        public async Task<List<ArticleModel>> SearchArticles(List<string> keywords, List<string> topics, String? name, String? description, String? websiteId)
        {
            List<ArticleModel> pages = new List<ArticleModel>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();
                Dictionary<int, ArticleModel> pageDict = await SearchArticlesByKeywordAndTopic(keywords, topics, description, name, connection,websiteId);
                return new List<ArticleModel>(pageDict.Values);
            }
        }

        private async Task<Dictionary<int, ArticleModel>> SearchArticlesByKeywordAndTopic(List<string> keywords, List<string> topics, string? description, string? name, MySqlConnection connection,String? websiteId)
        {
            Dictionary<int, ArticleModel> results = new Dictionary<int, ArticleModel>();
            var queryBuilder = new StringBuilder("SELECT * FROM article_keywords_topics_view WHERE ");
            var conditions = new List<string>();
            if (keywords != null && keywords.Count > 0)
            {
                conditions.Add($"article_keywords LIKE '%{string.Join("%' OR article_keywords LIKE '%", keywords)}%'");
            }
            if (topics != null && topics.Count > 0)
            {
                conditions.Add($"article_topics LIKE '%{string.Join("%' OR article_topics LIKE '%", topics)}%'");
            }
            if (!string.IsNullOrEmpty(description))
            {
                conditions.Add($"article_description LIKE '%{description}%'");
            }
            if (!string.IsNullOrEmpty(name))
            {
                conditions.Add($"article_name LIKE '%{name}%'");
            }
            conditions.Add($"websiteId = '{websiteId}'");
            queryBuilder.Append(string.Join(" OR ", conditions));
            using (var command = new MySqlCommand(queryBuilder.ToString(), connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int articleId = reader.GetInt32(reader.GetOrdinal("article_id"));
                        if (!results.ContainsKey(articleId))
                        {

                            ArticleModel currentArticle = RetrievePagesMapArticle(reader);
                            currentArticle.topics = reader.SafeRead<string>("article_topics")?.Split(',')?.ToList();
                            currentArticle.keywords = reader.SafeRead<string>("article_keywords")?.Split(',')?.ToList();
                            results.Add(articleId, currentArticle);
                        }
                    }
                }
            }
            return results;
        }

        public static  ArticleModel RetrievePagesMapArticle(DbDataReader reader)
        {
            ArticleModel articleModel = new ArticleModel();
            articleModel.id = reader.SafeRead<int>("article_id");
            articleModel.articleId = reader.SafeRead<string>("articleid");
            articleModel.sequence_no = reader.SafeRead<int>("article_sequence_no");
            articleModel.name = reader.SafeRead<string>("article_name");
            articleModel.description = reader.SafeRead<string>("article_description");
            articleModel.articlePath = reader.SafeRead<string>("article_articlepath");
            articleModel.memeImagePath = reader.SafeRead<string>("article_memepath");
            articleModel.topics = reader.SafeRead<string>("article_topics")?.Split(',')?.ToList() ?? new List<string>();
            articleModel.keywords = reader.SafeRead<string>("article_keywords")?.Split(',')?.ToList() ?? new List<string>();
            articleModel.websiteId = reader.SafeRead<int>("websiteid");
            return articleModel;
        }

        public async Task AssociateWithKeyword(int parentId, int keywordId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }
            var articleTopicUpsertCommand = new MySqlCommand("UpsertArticleKeyword", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            articleTopicUpsertCommand.Parameters.AddWithValue("_articleId", parentId);
            articleTopicUpsertCommand.Parameters.AddWithValue("_keywordId", keywordId);

            await articleTopicUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

        public async Task AssociateWithTopic(int parentId, int topicId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }
            var articleTopicUpsertCommand = new MySqlCommand("UpsertArticleTopic", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Add parameters
            articleTopicUpsertCommand.Parameters.AddWithValue("_articleId", parentId);
            articleTopicUpsertCommand.Parameters.AddWithValue("_topicId", topicId);

            await articleTopicUpsertCommand.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }

    }
}
