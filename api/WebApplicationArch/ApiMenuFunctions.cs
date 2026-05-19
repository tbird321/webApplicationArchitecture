using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using MySQLConnector;
using MySQLConnector.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApplicationArch.content;

namespace WebApplicationArch
{
    public class ApiMenuFunctions : ApiBaseFunctions
    {
        private const string MENU_FILENAME = "sitemenu.json";
        private const int MAX_RETRIES = 5;

        // Prevents concurrent menu writes within the same Lambda instance.
        // ETag optimistic locking (UploadFileIfMatch) covers concurrent Lambda instances.
        private static readonly SemaphoreSlim _menuLock = new SemaphoreSlim(1, 1);

        public ApiMenuFunctions() : base() { }

        private async Task<string> GetMenuS3Folder(int websiteId, string environment)
        {
            WebsiteDAO websiteDao = new(await ConnectionInfoAsync(environment));
            var websites = await websiteDao.GetWebsites();
            var website = websites.FirstOrDefault(w => w.id == websiteId);
            string websiteName = website?.name ?? websiteId.ToString();
            return $"public/websites/{websiteName}";
        }

        private async Task<(AmazonS3Storage s3, string folder)> GetS3(int websiteId, string environment)
        {
            string bucket = Environment.GetEnvironmentVariable("CONTENT_BUCKET") ?? "www-websitecontent";
            var s3 = new AmazonS3Storage(bucket, "us-west-2");
            string folder = await GetMenuS3Folder(websiteId, environment);
            return (s3, folder);
        }

        private static List<JObject> ParseMenuItems(string json)
            => JsonConvert.DeserializeObject<List<JObject>>(json) ?? new List<JObject>();

        private static int NextId(List<JObject> items)
            => items.Count == 0 ? 1 : items.Max(i => i["id"]?.Value<int>() ?? 0) + 1;

        /// <summary>
        /// Reads the menu, applies a mutation function, and writes back using ETag optimistic
        /// locking. Retries up to MAX_RETRIES times if a concurrent write is detected.
        /// The SemaphoreSlim prevents concurrent calls within the same Lambda instance.
        /// </summary>
        private async Task<T> WithMenuLock<T>(
            AmazonS3Storage s3, string folder,
            Func<List<JObject>, (List<JObject> updatedItems, T result)> mutate)
        {
            await _menuLock.WaitAsync();
            try
            {
                for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
                {
                    var (stream, etag) = await s3.DownloadFileWithETag(MENU_FILENAME, folder);
                    string json;
                    using (var reader = new StreamReader(stream))
                        json = await reader.ReadToEndAsync();

                    var items = ParseMenuItems(json);
                    var (updatedItems, result) = mutate(items);

                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updatedItems));
                    using var writeStream = new MemoryStream(bytes);
                    bool written = await s3.UploadFileIfMatch(writeStream, MENU_FILENAME, folder, etag);

                    if (written) return result;

                    // Another Lambda instance wrote concurrently — back off and retry
                    await Task.Delay(50 * (attempt + 1));
                }
                throw new Exception($"Menu write failed after {MAX_RETRIES} retries due to concurrent modifications.");
            }
            finally
            {
                _menuLock.Release();
            }
        }

        // GET /menu/{websiteId}
        public async Task<APIGatewayProxyResponse> GetMenu(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var auth = ValidateApiKey(request);
            if (auth != null) return auth;
            try
            {
                if (!int.TryParse(request.PathParameters["websiteId"], out int websiteId))
                    return BadRequest("Invalid websiteId");

                string environment = GetEnvironment(request);
                var (s3, folder) = await GetS3(websiteId, environment);

                using var stream = s3.DownloadFile(MENU_FILENAME, folder);
                using var reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync();

                return Ok(json, "application/json");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"GetMenu error: {ex.Message}");
                return ServerError(ex.Message);
            }
        }

        // POST /menu/{websiteId}/item  — add a new item
        public async Task<APIGatewayProxyResponse> AddMenuItem(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var auth = ValidateApiKey(request);
            if (auth != null) return auth;
            try
            {
                if (!int.TryParse(request.PathParameters["websiteId"], out int websiteId))
                    return BadRequest("Invalid websiteId");

                var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.Body);
                if (body == null || !body.ContainsKey("parentId") || !body.ContainsKey("itemTitle"))
                    return BadRequest("parentId and itemTitle are required");

                string environment = GetEnvironment(request);
                var (s3, folder) = await GetS3(websiteId, environment);

                int parentId = Convert.ToInt32(body["parentId"]);
                string itemTitle = body["itemTitle"].ToString();
                bool hasPageId = body.ContainsKey("pageId") && body["pageId"] != null;
                int? pageId = hasPageId ? (int?)Convert.ToInt32(body["pageId"]) : null;
                string? pageName = body.ContainsKey("pageName") ? body["pageName"]?.ToString() : null;

                var result = await WithMenuLock(s3, folder, items =>
                {
                    var newItem = new JObject
                    {
                        ["id"] = NextId(items),
                        ["parent"] = parentId,
                        ["droppable"] = !hasPageId,
                        ["text"] = itemTitle
                    };
                    if (pageId.HasValue) newItem["pageId"] = pageId.Value;
                    if (!string.IsNullOrEmpty(pageName)) newItem["pageName"] = pageName;

                    items.Add(newItem);
                    return (items, new { success = true, addedItem = newItem, totalItems = items.Count });
                });

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"AddMenuItem error: {ex.Message}");
                return ServerError(ex.Message);
            }
        }

        // POST /menu/{websiteId}/item/{id}  — update an existing item
        public async Task<APIGatewayProxyResponse> UpdateMenuItem(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var auth = ValidateApiKey(request);
            if (auth != null) return auth;
            try
            {
                if (!int.TryParse(request.PathParameters["websiteId"], out int websiteId) ||
                    !int.TryParse(request.PathParameters["id"], out int itemId))
                    return BadRequest("Invalid websiteId or item id");

                var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.Body);
                if (body == null) return BadRequest("Request body is required");

                string environment = GetEnvironment(request);
                var (s3, folder) = await GetS3(websiteId, environment);

                var result = await WithMenuLock(s3, folder, items =>
                {
                    var item = items.FirstOrDefault(i => i["id"]?.Value<int>() == itemId);
                    if (item == null)
                        throw new KeyNotFoundException($"Menu item {itemId} not found");

                    if (body.ContainsKey("itemTitle") && body["itemTitle"] != null)
                        item["text"] = body["itemTitle"].ToString();
                    if (body.ContainsKey("pageId") && body["pageId"] != null)
                        item["pageId"] = Convert.ToInt32(body["pageId"]);
                    if (body.ContainsKey("pageName") && body["pageName"] != null)
                        item["pageName"] = body["pageName"].ToString();
                    if (body.ContainsKey("parentId") && body["parentId"] != null)
                        item["parent"] = Convert.ToInt32(body["parentId"]);

                    return (items, new { success = true, updatedItem = item });
                });

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"UpdateMenuItem error: {ex.Message}");
                return ServerError(ex.Message);
            }
        }

        // DELETE /menu/{websiteId}/item/{id}  — delete item and all descendants
        public async Task<APIGatewayProxyResponse> DeleteMenuItem(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var auth = ValidateApiKey(request);
            if (auth != null) return auth;
            try
            {
                if (!int.TryParse(request.PathParameters["websiteId"], out int websiteId) ||
                    !int.TryParse(request.PathParameters["id"], out int itemId))
                    return BadRequest("Invalid websiteId or item id");

                string environment = GetEnvironment(request);
                var (s3, folder) = await GetS3(websiteId, environment);

                var result = await WithMenuLock(s3, folder, items =>
                {
                    var toDelete = new HashSet<int> { itemId };
                    bool changed = true;
                    while (changed)
                    {
                        changed = false;
                        foreach (var i in items)
                        {
                            int id = i["id"]?.Value<int>() ?? -1;
                            int parent = i["parent"]?.Value<int>() ?? -1;
                            if (!toDelete.Contains(id) && toDelete.Contains(parent))
                            {
                                toDelete.Add(id);
                                changed = true;
                            }
                        }
                    }
                    var filtered = items.Where(i => !toDelete.Contains(i["id"]?.Value<int>() ?? -1)).ToList();
                    return (filtered, new { success = true, deletedIds = toDelete, removedCount = toDelete.Count });
                });

                return Ok(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"DeleteMenuItem error: {ex.Message}");
                return ServerError(ex.Message);
            }
        }

        private APIGatewayProxyResponse Ok(string body, string contentType = "application/json") =>
            new() { StatusCode = (int)HttpStatusCode.OK, Body = body, Headers = new Dictionary<string, string> { { "Content-Type", contentType }, { "Access-Control-Allow-Origin", "*" } } };

        private APIGatewayProxyResponse BadRequest(string msg) =>
            new() { StatusCode = (int)HttpStatusCode.BadRequest, Body = msg, Headers = PostHeaders };

        private APIGatewayProxyResponse NotFound(string msg) =>
            new() { StatusCode = (int)HttpStatusCode.NotFound, Body = msg, Headers = PostHeaders };

        private APIGatewayProxyResponse ServerError(string msg) =>
            new() { StatusCode = (int)HttpStatusCode.InternalServerError, Body = $"Error: {msg}", Headers = PostHeaders };
    }
}
