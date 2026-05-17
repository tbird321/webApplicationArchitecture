using MySQLConnector.Models;
using MySQLConnector;
using Newtonsoft.Json;

namespace MySQLConnectorTest
{
    internal class CollectionTests: BaseADOTest
    {

        private CollectionDAO collectionDAO;
        private PageDAO pageDAO;
        private List<CollectionModel> collectionModels;
        private ConnectionModel connectionModel;
        [SetUp]
        public async Task Setup()
        {
            collectionModels = new List<CollectionModel>();
            if (connectionModel == null)
            {
                ConnectionManager conTest = new ConnectionManager();
                connectionModel = await conTest.GetSecretAsync("Dev");
            }
            collectionDAO = new CollectionDAO(connectionModel);
            pageDAO = new PageDAO(connectionModel);
        }


        [Test]
        public async Task InsertCollection()
        {
            int collectionId = 0;
            try
            {
                var newCollection = new CollectionModel { name = "InsertCollection Test Collection", description = "InsertCollection Test Description", websiteId = 20 };
                collectionId = await collectionDAO.InsertCollection(newCollection);
                newCollection.id = collectionId; // Set the ID for cleanup

                Assert.IsTrue(collectionId > 0, "Collection ID should be greater than 0");
                var retrievedCollection = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.IsNotNull(retrievedCollection, "Retrieved collection should not be null");
                Assert.AreEqual(newCollection.id, retrievedCollection.collection.id);
                Assert.AreEqual(newCollection.name, retrievedCollection.collection.name);
                Assert.AreEqual(newCollection.description, retrievedCollection.collection.description);
                Assert.AreEqual(newCollection.websiteId, retrievedCollection.collection.websiteId);
            }
            finally
            {
                await collectionDAO.DeleteCollection(collectionId);
            }
        }
        [Test]
        public async Task UpdateCollection()
        {
            int collectionId = 0;
            try
            {
                var newCollection = new CollectionModel { name = "InsertCollection Test Collection", description = "InsertCollection Test Description", websiteId = 20 };
                collectionId = await collectionDAO.InsertCollection(newCollection);
                newCollection.id = collectionId;

                newCollection.name = "Updated Name";
                collectionModels.Add(newCollection);
                await collectionDAO.UpdateCollection(newCollection);

                var updatedCollection = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.AreEqual(newCollection.description, updatedCollection.collection.description);
                Assert.AreEqual(newCollection.websiteId, updatedCollection.collection.websiteId);
                Assert.AreEqual(newCollection.id, updatedCollection.collection.id);
                Assert.AreEqual(newCollection.name, updatedCollection.collection.name);
            }
            finally
            {
                await collectionDAO.DeleteCollection(collectionId);
            }
        }
        [Test]
        public async Task UpsertCollection()
        {
            int collectionId = 0;
            try
            {
                var newCollection = new CollectionModel { name = "InsertCollection Test Collection", description = "InsertCollection Test Description", websiteId = 20 };
                collectionId = await collectionDAO.UpsertCollection(newCollection);
                newCollection.id = collectionId;
                collectionModels.Add(newCollection);

                Assert.IsTrue(collectionId > 0, "Collection ID should be greater than 0");

                var retrievedCollection = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.IsNotNull(retrievedCollection, "Retrieved collection should not be null");
                Assert.AreEqual(newCollection.name, retrievedCollection.collection.name);
                Assert.AreEqual(newCollection.description, retrievedCollection.collection.description);
                Assert.AreEqual(newCollection.websiteId, retrievedCollection.collection.websiteId);
            }
            finally
            {
                await collectionDAO.DeleteCollection(collectionId);
            }
        }
        public async Task<int> InsertPage()
        {
            int pageId = 0;

            var newPage = new PageModel { name = "InsertPage Test Page", description = "InsertPage Test Description", layoutid = 1 };
            pageId = await pageDAO.InsertPage(newPage);
            newPage.id = pageId; // Set the ID for cleanup
            return pageId;
        }
        [Test]
        public async Task AssociatePagesWithCollection()
        {
            int collectionId = 0;
            List<int> pageIds = new List<int>();
            try
            {
                // Create a new collection
                var newCollection = new CollectionModel { name = "InsertCollection Test Collection", description = "InsertCollection Test Description", websiteId = 20};
                collectionId = await collectionDAO.InsertCollection(newCollection);
                newCollection.id = collectionId;

                // Create a list of new pages to associate with the collection
                var pagesToAssociate = new List<PageDetail>();
                for (int i = 0; i < 2; i++) // Example: Adding two pages
                {
                    int pageId = await InsertPage();
                    pageIds.Add(pageId); // Keep track of page IDs for cleanup
                    pagesToAssociate.Add(new PageDetail { id = pageId, sequence = i + 1 });
                }

                var collectionPagesDetail = new CollectionPageDetail
                {
                    collection = newCollection,
                    pages = pagesToAssociate
                };

                // Call the method being tested
                CollectionPageDetail newCollectionPagesDetail = await collectionDAO.AssociatePagesWithCollection(collectionPagesDetail);

                // Validation
                Assert.IsNotNull(newCollectionPagesDetail, "Returned collectionPagesDetail should not be null");

                // Additional assertions on the newCollectionPagesDetail if needed
                Assert.AreEqual(newCollection.id, newCollectionPagesDetail.collection.id);
                // Add more assertions based on the contents of newCollectionPagesDetail if needed

                var retrievedCollectionPage = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.IsNotNull(retrievedCollectionPage, "Retrieved collectionPage should not be null");
                Assert.AreEqual(collectionId, retrievedCollectionPage.collection.id);
                // Additional assertions can be made here based on the contents of retrievedCollectionPage

            }
            finally
            {
                // Cleanup: Delete the collection and pages created for the test
                foreach (var pageId in pageIds)
                {
                    await pageDAO.DeletePage(pageId);
                }
                await collectionDAO.DeleteCollection(collectionId);
            }
        }


        [Test]

        public async Task DeletePageFromCollectionTest()
        {
            int collectionId = 0;
            List<int> pageIds = new List<int>();
            try
            {
                // Step 1: Create a new collection.
                var newCollection = new CollectionModel { name = "Test Collection", description = "Test Description", websiteId = 20 };
                collectionId = await collectionDAO.InsertCollection(newCollection);
                newCollection.id = collectionId;
                // Step 2: Create a new page and store its ID.
                int pageId = await InsertPage();
                pageIds.Add(pageId);

                // Step 3: Associate the page with the collection using the new format.
                var collectionPageDetails = new CollectionPageDetail
                {
                    collection = newCollection,
                    pages = new List<PageDetail> { new PageDetail { id = pageId, sequence = 1 } } // Create a list with a single PageDetail
                };
        
                await collectionDAO.AssociatePagesWithCollection(collectionPageDetails); // Assuming this method accepts CollectionPageDetail and associates pages in bulk

                // Step 4: Validate the association was successful by attempting to retrieve it.
                var retrievedCollectionPage = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.IsNotNull(retrievedCollectionPage, "Retrieved collectionPage should not be null");
                Assert.AreEqual(collectionId, retrievedCollectionPage.collection.id);
                Assert.IsTrue(retrievedCollectionPage.pages.Any(p => pageId == p.id), "The associated page should be found.");

                // Step 5: Delete the association for the single page and validate deletion.
                bool isDeleted = await collectionDAO.DeletePageFromCollection(collectionId, pageId);
                Assert.IsTrue(isDeleted, "The association should be successfully deleted.");

                // Step 6: Attempt to retrieve the deleted association to confirm deletion.
                retrievedCollectionPage = await collectionDAO.GetCollectionPageAsync(collectionId);
                Assert.IsFalse(retrievedCollectionPage.pages.Any(p => pageId == p.id), "The associated page should not be found after deletion.");
            }
            finally
            {
                // Cleanup
                foreach (var pid in pageIds)
                {
                    await pageDAO.DeletePage(pid);
                }
                await collectionDAO.DeleteCollection(collectionId);
            }
        }
        [Test]

        public async Task SearchCollections()
        {
            int collectionId = 0;
            try
            {
                string searchName = "InsertCollection Test Collection";
                string searchDescription = "InsertCollection Test Description";
                int searchWebsiteId = 20;

                var newCollection = new CollectionModel { name = searchName, description = searchDescription, websiteId = searchWebsiteId };
                collectionId = await collectionDAO.InsertCollection(newCollection);
                newCollection.id = collectionId;
                collectionModels.Add(newCollection);
                //Search by Name
                var collections = await collectionDAO.SearchCollections( searchName, "20", null);

                Assert.IsNotNull(collections, "Search result should not be null");
                Assert.IsNotEmpty(collections, "Search result should contain collections");
            }
            finally
            {
                //no associations to delete
                using (var connection = pageDAO.CreateMySqlConnection())
                {
                    connection.Open();
                    await collectionDAO.DeleteCollection(collectionId, connection);
                    connection.Close();
                }

            }
        }

        [TearDown]
        public void TearDown()
        {

        }
    }
}
