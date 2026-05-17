using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using System.Data;
using System.Threading.Tasks;

namespace MySQLConnector
{
    public class LayoutDAO : BaseMYSQLProcessing
    {
        public LayoutDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }

        public async Task<int> InsertLayout(LayoutModel layout, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("INSERT INTO layout (name) VALUES (@name); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", layout.name);

            int layoutId = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return layoutId;
        }

        public async Task UpdateLayout(LayoutModel layout, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE layout SET name = @name WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", layout.id);
            command.Parameters.AddWithValue("@name", layout.name);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task DeleteLayout(int layoutId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("DELETE FROM layout WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", layoutId);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<int> UpsertLayout(LayoutModel layout, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UpsertLayout", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("_id", MySqlDbType.Int32).Value = layout.id;
            command.Parameters["_id"].Direction = ParameterDirection.InputOutput;
            command.Parameters.AddWithValue("_name", layout.name);

            await command.ExecuteNonQueryAsync();

            int newOrExistingId = (int)command.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return newOrExistingId;
        }
        public async Task<List<string>> GetLayouts()
        {
            List<string> layouts = new List<string>();
            using (var connection = CreateMySqlConnection())
            {
                await connection.OpenAsync();

                //Use transactions to validate that all adjustments were made before comit
                using (var command = new MySqlCommand("Select distinct name from layout", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string keyword = reader.SafeRead<string>("name");
                            layouts.Add(keyword);
                        }
                    }
                }
            }

            return layouts;
        }
    }
}
