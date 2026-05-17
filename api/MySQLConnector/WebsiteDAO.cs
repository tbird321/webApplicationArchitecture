using MySql.Data.MySqlClient;
using MySQLConnector.Interfaces;
using MySQLConnector.Models;
using System.Data;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class WebsiteDAO : BaseMYSQLProcessing, IPageRelationshipUpdate
    {
        public WebsiteDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }

        public async Task<int> InsertWebsite(string name, string description, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO website (name, description) VALUES (@name, @description); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@description", description);

            int websiteId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return websiteId;
        }

        public async Task<int> InsertWebsite(WebsiteModel website, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO website (name) VALUES (@name); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", website.name);

            int websiteId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return websiteId;
        }

        public async Task UpdateWebsite(WebsiteModel website, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE website SET name = @name WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", website.id);
            command.Parameters.AddWithValue("@name", website.name);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteWebsite(int websiteId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM website WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", websiteId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertWebsite(WebsiteModel website, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UpsertWebsite", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("_id", MySqlDbType.Int32).Value = website.id;
            command.Parameters["_id"].Direction = ParameterDirection.InputOutput;
            command.Parameters.AddWithValue("_name", website.name);

            await command.ExecuteNonQueryAsync();

            int newOrExistingId = (int)command.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return newOrExistingId;
        }

        public async Task<List<WebsiteModel>> GetWebsites()
        {
            List<WebsiteModel> websites = new List<WebsiteModel>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();

                //Use transactions to validate that all adjustments were made before comit
                using (var command = new MySqlCommand("Select distinct * from website", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            WebsiteModel currentSite = new WebsiteModel();
                            currentSite.id = reader.SafeRead<int>("id");
                            currentSite.name = reader.SafeRead<string>("name");
                            websites.Add(currentSite);
                        }
                    }
                }
            }

            return websites;
        }

        public async Task AssociateWithPage(int websiteId, int pageId, MySqlConnection? myConnect = null)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO website_page (website_id, page_id) VALUES (@websiteId, @pageId);", myConnect);
            command.Parameters.AddWithValue("@websiteId", websiteId);
            command.Parameters.AddWithValue("@pageId", pageId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }
        }
    }
}
