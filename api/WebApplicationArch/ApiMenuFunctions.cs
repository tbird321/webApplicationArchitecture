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

        /// <summary>
        /// Validate a menu that is about to be written. Returns the problems, empty if it is fine.
        ///
        /// Every one of these is a SITEWIDE fault with no error anywhere: the nav is baked into
        /// every page, so a bad menu is not a bad page, it is a bad site. The item-at-a-time tools
        /// can check one edit against a menu that is otherwise known-good; a whole-file replace has
        /// no such luxury and has to prove the result is coherent by itself.
        /// </summary>
        public static List<string> ValidateMenu(List<JObject> items, HashSet<string> publishedPageNames)
        {
            var errors = new List<string>();
            var byId = new Dictionary<string, JObject>(StringComparer.Ordinal);

            foreach (var m in items)
            {
                var id = m?["id"]?.ToString();
                if (string.IsNullOrEmpty(id)) { errors.Add("An item has no id."); continue; }
                if (byId.ContainsKey(id)) { errors.Add($"Duplicate menu item id {id}."); continue; }
                if (string.IsNullOrWhiteSpace(m["text"]?.ToString()))
                    errors.Add($"Item {id} has no text, so it would render as a blank menu entry.");
                byId[id] = m;
            }

            string PageNameOf(JObject m) => (m["pageName"] ?? m["data"]?["pageName"])?.ToString();
            string ParentOf(JObject m) => m["parent"]?.ToString() ?? "0";

            foreach (var m in items)
            {
                var id = m?["id"]?.ToString();
                if (string.IsNullOrEmpty(id)) continue;

                var parent = ParentOf(m);
                if (parent != "0" && !byId.ContainsKey(parent))
                    errors.Add($"Item {id} (\"{m["text"]}\") names parent {parent}, which does not exist. The whole branch would be unreachable and vanish from every page.");

                var pageName = PageNameOf(m);
                if (!string.IsNullOrEmpty(pageName) && publishedPageNames != null &&
                    publishedPageNames.Count > 0 && !publishedPageNames.Contains(pageName))
                    errors.Add($"Item {id} (\"{m["text"]}\") links to page \"{pageName}\", which is not a published page on this site. That is a dead link on EVERY page.");
            }

            // Reachability and depth in one walk from the root. Anything unvisited is in a cycle
            // or hanging off a missing parent -- either way the renderer never draws it.
            var depth = new Dictionary<string, int>(StringComparer.Ordinal);
            var queue = new Queue<JObject>();
            foreach (var m in items)
            {
                var id = m?["id"]?.ToString();
                if (!string.IsNullOrEmpty(id) && ParentOf(m) == "0" && !depth.ContainsKey(id))
                {
                    depth[id] = 0;
                    queue.Enqueue(m);
                }
            }
            var childrenOf = items.Where(m => !string.IsNullOrEmpty(m?["id"]?.ToString()))
                                  .GroupBy(m => ParentOf(m))
                                  .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                var cid = cur["id"].ToString();
                if (!childrenOf.TryGetValue(cid, out var kids)) continue;
                foreach (var k in kids)
                {
                    var kid = k["id"].ToString();
                    if (depth.ContainsKey(kid)) continue;
                    depth[kid] = depth[cid] + 1;
                    queue.Enqueue(k);
                }
            }

            foreach (var m in items)
            {
                var id = m?["id"]?.ToString();
                if (string.IsNullOrEmpty(id)) continue;
                if (!depth.TryGetValue(id, out var d))
                    errors.Add($"Item {id} (\"{m["text"]}\") is not reachable from the top level -- a parent cycle or a detached branch. It would sit in the file and appear in no navigation.");
                else if (d >= StaticPageRenderer.MaxNavDepth)
                    errors.Add($"Item {id} (\"{m["text"]}\") sits at level {d + 1}, but the nav renders only {StaticPageRenderer.MaxNavDepth} levels, so it would appear nowhere.");
            }

            return errors;
        }

        // POST /menu/{websiteId}/replace  — replace the ENTIRE menu in one write
        //
        // WHY THIS EXISTS: the item-at-a-time tools each trigger a full-site re-render, so a
        // restructure that moves 140 items costs ~140 renders of every page on the site. This is
        // ONE write and ONE render. It is also the only way to control sibling ORDER -- sibling
        // order is array order, there is no sort key, and adding an item can only append.
        public async Task<APIGatewayProxyResponse> ReplaceMenu(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var auth = ValidateApiKey(request);
            if (auth != null) return auth;
            try
            {
                if (!int.TryParse(request.PathParameters["websiteId"], out int websiteId))
                    return BadRequest("Invalid websiteId");

                var body = JObject.Parse(request.Body ?? "{}");
                var itemsToken = body["items"] as JArray;
                if (itemsToken == null)
                    return BadRequest("Body must be {\"items\": [...]} carrying the COMPLETE menu. A partial list would delete everything omitted.");

                var newItems = itemsToken.OfType<JObject>().ToList();
                if (newItems.Count == 0)
                    return BadRequest("Refusing to write an empty menu -- that erases the navigation on every page of the site. To empty it deliberately, delete the items individually.");

                bool allowUnlink = body["allowUnlink"]?.Value<bool>() ?? false;
                string environment = GetEnvironment(request);

                // Resolve the pages that actually exist on THIS site. Menu items are addressed
                // positionally and carry no owner, so a page-name check against the target site is
                // the thing that catches a menu aimed at the wrong website.
                var publishedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var conn = await ConnectionInfoAsync(environment);
                    var pages = await new PageDAO(conn).SearchPages(
                        new List<string>(), new List<string>(), null, null, websiteId.ToString());
                    foreach (var p in pages ?? new List<PageModel>())
                        if (!string.IsNullOrWhiteSpace(p.name) &&
                            (string.IsNullOrEmpty(p.status) || p.status == "published"))
                            publishedNames.Add(p.name);
                }
                catch (Exception ex)
                {
                    // Validating against an empty page list would silently skip the dead-link check
                    // and let a menu full of broken links through, which is worse than refusing.
                    context.Logger.LogError($"ReplaceMenu: could not load pages: {ex.Message}");
                    return ServerError("Could not load the site's pages, so the menu could not be checked for dead links. Nothing was written.");
                }

                var errors = ValidateMenu(newItems, publishedNames);

                string PageNameOf(JObject m) => (m["pageName"] ?? m["data"]?["pageName"])?.ToString();

                var (s3, folder) = await GetS3(websiteId, environment);

                // Read the CURRENT menu before touching anything, so a refusal writes nothing at
                // all. Doing this comparison inside WithMenuLock would mean the refusal path still
                // had to hand back an item list -- and WithMenuLock writes whatever it is given,
                // which would re-upload sitemenu.json and trigger a full-site re-render for a
                // request that was rejected.
                List<JObject> current;
                using (var stream = s3.DownloadFile(MENU_FILENAME, folder))
                using (var reader = new StreamReader(stream))
                    current = ParseMenuItems(await reader.ReadToEndAsync());

                var before = new HashSet<string>(
                    current.Select(PageNameOf).Where(n => !string.IsNullOrEmpty(n)), StringComparer.OrdinalIgnoreCase);
                var after = new HashSet<string>(
                    newItems.Select(PageNameOf).Where(n => !string.IsNullOrEmpty(n)), StringComparer.OrdinalIgnoreCase);

                var unlinked = before.Where(n => !after.Contains(n)).OrderBy(n => n).ToList();
                var added = after.Where(n => !before.Contains(n)).OrderBy(n => n).ToList();

                // A page dropping out of the navigation is usually an accident, and a silent one --
                // the page still resolves, it just stops being reachable by clicking.
                if (unlinked.Count > 0 && !allowUnlink)
                    errors.Add($"{unlinked.Count} page(s) linked today are absent from the new menu: " +
                               $"{string.Join(", ", unlinked.Take(15))}{(unlinked.Count > 15 ? ", ..." : "")}. " +
                               "Pass allowUnlink: true if that is intended -- the pages are not deleted, they just stop being reachable from the nav.");

                if (errors.Count > 0)
                    return BadRequest(JsonConvert.SerializeObject(new { refused = true, errors }));

                // The ETag lock still guards the write itself: if another writer landed between the
                // read above and here, UploadFileIfMatch fails and WithMenuLock retries.
                await WithMenuLock(s3, folder, _ => (newItems, true));

                return Ok(JsonConvert.SerializeObject(new
                {
                    success = true,
                    itemsWritten = newItems.Count,
                    pagesLinked = after.Count,
                    newlyLinked = added,
                    noLongerLinked = unlinked,
                    note = "One write, one site-wide re-render. The nav is baked into every page, so the change is not visible until that finishes."
                }));
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"ReplaceMenu error: {ex.Message}");
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
