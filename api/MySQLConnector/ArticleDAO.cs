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
            command.Parameters.AddWithValue("_status", article.status ?? "draft");

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

            // Every filter value is bound as a parameter. These used to be interpolated straight
            // into the SQL, so a name containing an apostrophe broke the query and a crafted one
            // could inject.
            //
            // The site filter is ANDed, never ORed. It was previously appended as just another
            // condition and joined with OR, which produced
            //     WHERE article_name LIKE '%X%' OR websiteId = '2'
            // — matching every article on the site regardless of the search term, AND leaking
            // articles from OTHER sites whose name happened to match.
            var filters = new List<string>();
            var parameters = new List<MySqlParameter>();
            int paramCount = 0;

            if (keywords != null && keywords.Count > 0)
            {
                var parts = keywords.Select(keyword => {
                    var p = $"@keyword{paramCount++}";
                    parameters.Add(new MySqlParameter(p, $"%{keyword}%"));
                    return $"article_keywords LIKE {p}";
                });
                filters.Add("(" + string.Join(" OR ", parts) + ")");
            }
            if (topics != null && topics.Count > 0)
            {
                var parts = topics.Select(topic => {
                    var p = $"@topic{paramCount++}";
                    parameters.Add(new MySqlParameter(p, $"%{topic}%"));
                    return $"article_topics LIKE {p}";
                });
                filters.Add("(" + string.Join(" OR ", parts) + ")");
            }
            if (!string.IsNullOrEmpty(description))
            {
                var p = $"@description{paramCount++}";
                parameters.Add(new MySqlParameter(p, $"%{description}%"));
                filters.Add($"article_description LIKE {p}");
            }
            if (!string.IsNullOrEmpty(name))
            {
                var p = $"@name{paramCount++}";
                parameters.Add(new MySqlParameter(p, $"%{name}%"));
                filters.Add($"article_name LIKE {p}");
            }

            var queryBuilder = new StringBuilder("SELECT * FROM article_keywords_topics_view WHERE ");

            // Only constrain by site when one was supplied — a null/empty websiteId means the
            // caller deliberately wants an unscoped search, and filtering on '' would match
            // nothing at all rather than everything.
            string siteClause = null;
            if (!string.IsNullOrEmpty(websiteId))
            {
                var siteParam = $"@websiteId{paramCount++}";
                parameters.Add(new MySqlParameter(siteParam, websiteId));
                siteClause = $"websiteId = {siteParam}";
            }

            if (filters.Count > 0 && siteClause != null)
            {
                queryBuilder.Append("(").Append(string.Join(" OR ", filters)).Append(") AND ").Append(siteClause);
            }
            else if (siteClause != null)
            {
                queryBuilder.Append(siteClause);
            }
            else if (filters.Count > 0)
            {
                queryBuilder.Append(string.Join(" OR ", filters));
            }
            else
            {
                queryBuilder.Append("1 = 1");
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
            // status must be read back, not just written. UpsertArticle writes
            // `article.status ?? "draft"` unconditionally, so any caller that reads a record,
            // edits a field and writes it back would silently unpublish it if the read did not
            // return the current status. Column name differs between the view and the base
            // table; SafeRead returns default for a column that is not present, so trying both
            // is safe regardless of which one this reader exposes.
            articleModel.status = reader.SafeRead<string>("article_status")
                                  ?? reader.SafeRead<string>("status");
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
