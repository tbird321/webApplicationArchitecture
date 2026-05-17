public class CollectionModel
{
    public int? id { get; set; }
    public int websiteId { get; set; }
    public string name { get; set; }
    public String description { get; set; }
    public string type { get; set; } = "standard"; // "standard" or "gallery"
}
