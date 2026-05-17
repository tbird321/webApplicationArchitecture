using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using System.Data;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class TopicDAO : BaseMYSQLProcessing
    {
        public TopicDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }

        public async Task<int> InsertTopic(TopicModel topic, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO topic (value) VALUES (@value); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@value", topic.value);

            int topicId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return topicId;
        }

        public async Task UpdateTopic(TopicModel topic, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE topic SET value = @value WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", topic.id);
            command.Parameters.AddWithValue("@value", topic.value);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteTopic(int topicId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM topic WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", topicId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteTopic(string topic, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM topic WHERE value = @value", connection);
            command.Parameters.AddWithValue("@value", topic);
            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertTopic(string topicVaue, MySqlConnection? myConnect = null, int keywordId = 0)
        {
            bool shouldCloseConnection = false;
            if (myConnect == null)
            {
                myConnect = CreateMySqlConnection();
                await myConnect.OpenAsync();
                shouldCloseConnection = true;
            }

            var keywordUpsertCommand = new MySqlCommand("UpsertTopic", myConnect)
            {
                CommandType = CommandType.StoredProcedure
            };

            // If the keyword ID is not known or it's a new keyword, set it to 0

            // Configure the ID as an INOUT parameter
            keywordUpsertCommand.Parameters.Add("_id", MySqlDbType.Int32);
            keywordUpsertCommand.Parameters.AddWithValue("_value", topicVaue);

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

        public async Task<List<string>> GetTopics()
        {
            List<string> topics = new List<string>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();

                //Use transactions to validate that all adjustments were made before comit
                using (var command = new MySqlCommand("Select distinct value from topic", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string keyword = reader.SafeRead<string>("value");
                            topics.Add(keyword);
                        }
                    }
                }
            }

            return topics;
        }
    }
}
