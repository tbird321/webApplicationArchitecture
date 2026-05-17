using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using System.Data;
using System.Text;

namespace MySQLConnector
{
    public class CollectionDAO : BaseMYSQLProcessing
    {
        public CollectionDAO(ConnectionModel connectionModel) : base(connectionModel)
        {
        }
        public async Task<CollectionPageDetail> GetCollectionPageAsync(int collectionId, MySqlConnection? connection = null)
        {
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
            }
            CollectionPageDetail collectionPageDetail = new CollectionPageDetail();
            try
            {
                //Use transactions to validate that all adjustments were made before comit
                using (var command = new MySqlCommand("Select *  from collection_page_view WHERE collection_id = @collectionId", connection))
                {
                    command.Parameters.AddWithValue("@collectionId", collectionId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        int rowCount = 0;
                        while (await reader.ReadAsync())
                        {
                            if (rowCount==0) 
                            {
                                collectionPageDetail.collection.id = reader.SafeRead<int>("collection_id");
                                collectionPageDetail.collection.name = reader.SafeRead<string>("collection_name");
                                collectionPageDetail.collection.description = reader.SafeRead<string>("collection_description");
                                collectionPageDetail.collection.websiteId = reader.SafeRead<int>("collection_websiteId");
                                rowCount++;
                            }
                            PageDetail currentCollectionPage = new PageDetail();
                            currentCollectionPage.id = reader.SafeRead<int>("page_id");
                            currentCollectionPage.collection_page_id = reader.SafeRead<int>("collection_page_id");
                            currentCollectionPage.name = reader.SafeRead<string>("page_name");
                            currentCollectionPage.description = reader.SafeRead<string>("description");
                            currentCollectionPage.sequence = reader.SafeRead<int>("sequence");
                            currentCollectionPage.websiteId = reader.SafeRead<int>("websiteId");
                            if (currentCollectionPage.id != 0)
                            {
                                collectionPageDetail.pages.Add(currentCollectionPage);
                            }
                        }
                    }
                }
                return collectionPageDetail;
            }
            catch (Exception ex)
            {
                // Handle or log the exception as necessary
                throw; // or handle the exception as needed
            }
        }
        public async Task<int> InsertCollection(CollectionModel collection, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }
            int collectionId = 0;
            using (var command = new MySqlCommand("INSERT INTO collection (name, description, websiteId) VALUES (@name, @description, @websiteId); SELECT LAST_INSERT_ID();", connection))
            {
                command.Parameters.AddWithValue("@name", collection.name);
                command.Parameters.AddWithValue("@description", collection.description);
                command.Parameters.AddWithValue("@websiteId", collection.websiteId);

                collectionId = Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return collectionId;
        }

        public async Task UpdateCollection(CollectionModel collection, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UPDATE collection SET name = @name, description = @description, websiteId= @websiteId WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", collection.id);
            command.Parameters.AddWithValue("@websiteId", collection.websiteId);
            command.Parameters.AddWithValue("@name", collection.name);
            command.Parameters.AddWithValue("@description", collection.description);

            await command.ExecuteNonQueryAsync();

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<bool> DeleteCollection(int collectionId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            try
            {
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // First, delete all related rows from the `collection_page` table
                        var deleteCollectionPageCommand = new MySqlCommand("DELETE FROM collection_page WHERE collection_id = @collectionId", connection, transaction);
                        deleteCollectionPageCommand.Parameters.AddWithValue("@collectionId", collectionId);
                        await deleteCollectionPageCommand.ExecuteNonQueryAsync();

                        // Next, delete the collection itself
                        var deleteCollectionCommand = new MySqlCommand("DELETE FROM collection WHERE id = @id", connection, transaction);
                        deleteCollectionCommand.Parameters.AddWithValue("@id", collectionId);
                        await deleteCollectionCommand.ExecuteNonQueryAsync();

                        // Commit the transaction
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        // If there's an error within the transaction, rollback
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed
                // For example: Console.WriteLine($"Error deleting collection: {ex.Message}");
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
                return false; // Return false if an error occurred
            }

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
            return true; // Return true if deletion was successful
        }



        public async Task<int> UpsertCollection(CollectionModel collection, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            var command = new MySqlCommand("UpsertCollection", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.Add("_id", MySqlDbType.Int32).Value = collection.id;
            command.Parameters["_id"].Direction = ParameterDirection.InputOutput;
            command.Parameters.AddWithValue("_name", collection.name);
            command.Parameters.AddWithValue("_websiteId", collection.websiteId);
            command.Parameters.AddWithValue("_description", collection.description);

            await command.ExecuteNonQueryAsync();

            int newOrExistingId = (int)command.Parameters["_id"].Value;

            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }

            return newOrExistingId;
        }

        public async Task<CollectionPageDetail> AssociatePagesWithCollection(CollectionPageDetail collectionPageDetail, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            try
            {
                foreach (var page in collectionPageDetail.pages)
                {
                    var command = new MySqlCommand("AssociateCollectionWithPage", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.Add("_id", MySqlDbType.Int32).Value = page.collection_page_id.HasValue ? page.collection_page_id.Value : 0;
                    command.Parameters["_id"].Direction = ParameterDirection.InputOutput;
                    command.Parameters.AddWithValue("_collection_id", collectionPageDetail.collection.id);
                    command.Parameters.AddWithValue("_page_id", page.id);
                    command.Parameters.AddWithValue("_sequence", page.sequence.HasValue ? page.sequence.Value : 0);

                    await command.ExecuteNonQueryAsync();

                    int newOrExistingId = (int)command.Parameters["_id"].Value;

                    if (newOrExistingId > 0)
                    {
                        // Update collectionPageDetail with the new information if necessary
                        // For example, you might need to update the page's collection_page_id
                        page.collection_page_id = newOrExistingId;
                    }
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }

            return collectionPageDetail; // Returns the modified CollectionPageDetail.
        }
        public async Task<List<CollectionModel>> SearchCollections(string? name, string? websiteId, MySqlConnection? connection = null)
        {
            // Checking if the connection is not already open
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection();
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }
            var results = new List<CollectionModel>();

            // Ensuring both name and websiteId are provided and valid before proceeding.

            if (string.IsNullOrWhiteSpace(websiteId))
            {
                throw new ArgumentException("websiteId is required for the search.");
            }

            var query = "SELECT * FROM collection WHERE name LIKE %@name% AND websiteId = @websiteId";
            if (string.IsNullOrEmpty(name))
            {
                query = "SELECT * FROM collection WHERE websiteId = @websiteId";
            }
            using (var command = new MySqlCommand(query, connection))
            {
                // Using parameters to avoid SQL injection and to filter the search based on inputs.
                command.Parameters.AddWithValue("@name", $"%{name}%");
                command.Parameters.AddWithValue("@websiteId", websiteId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var collection = new CollectionModel
                        {
                            id = reader.GetInt32("id"),
                            name = reader.GetString("name"),
                            description = reader.GetString("description"),
                            websiteId = reader.GetInt32("websiteId")
                        };

                        results.Add(collection);
                    }
                }
            }

            // If we opened the connection in this method, close it
            if (shouldCloseConnection)
            {
                connection.Close();
            }

            return results;
        }



        public async Task<bool> DeletePageFromCollection (int collectionId, int pageId, MySqlConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = CreateMySqlConnection(); // Assumes existence of a method to create a new connection
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            try
            {
                using (var command = new MySqlCommand("DeletePageFromCollection", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters for collection ID and page ID
                    command.Parameters.AddWithValue("_collection_id", collectionId);
                    command.Parameters.AddWithValue("_page_id", pageId);

                    // Execute the command
                    int affectedRows = await command.ExecuteNonQueryAsync();

                    // Check if a row was affected
                    return affectedRows > 0;
                }
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

    }
}
