using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using System.Data;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class KeywordDAO : BaseMYSQLProcessing
    {
        public KeywordDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }

        public async Task<int> InsertKeyword(KeywordModel keyword, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO keyword (value) VALUES (@value); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@value", keyword.value);

            int keywordId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return keywordId;
        }

        public async Task UpdateKeyword(KeywordModel keyword, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE keyword SET value = @value WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", keyword.id);
            command.Parameters.AddWithValue("@value", keyword.value);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteKeyword(string keyword, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM keyword WHERE value = @value", connection);
            command.Parameters.AddWithValue("@value", keyword);
            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteKeyword(int keywordId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM keyword WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", keywordId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertKeyword(string keywordValue, MySqlConnection? myConnect = null, int keywordId = 0)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var keywordUpsertCommand = new MySqlCommand("UpsertKeyword", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // If the keyword ID is not known or it's a new keyword, set it to 0

            // Configure the ID as an INOUT parameter
            keywordUpsertCommand.Parameters.Add("_id", MySqlDbType.Int32);
            keywordUpsertCommand.Parameters.AddWithValue("_value", keywordValue);

            keywordUpsertCommand.Parameters["_id"].Direction = ParameterDirection.InputOutput;

            await keywordUpsertCommand.ExecuteNonQueryAsync();

            // Retrieve the new or existing ID
            int newOrExistingId = (int)keywordUpsertCommand.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await myConnect.CloseAsync();
            }

            return newOrExistingId; // This ID can then be used for further processing or association
        }

        public async Task<List<string>> GetKeywords()
        {
            List<string> keywords = new List<string>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();

                //Use transactions to validate that all adjustments were made before comit
                using (var command = new MySqlCommand("Select distinct value from keyword", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string keyword = reader.SafeRead<string>("value");
                            keywords.Add(keyword);
                        }
                    }
                }
            }

            return keywords;
        }

    }
}
