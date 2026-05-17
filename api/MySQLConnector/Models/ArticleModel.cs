using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Models
{
    public class ArticleModel
    {
        public int? id { get; set; }
        public int sequence_no { get; set; }
        public string articleId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public List<string> topics { get; set; }
        public List<string> keywords { get; set; }
        public string memeImagePath { get; set; }
        public string articlePath { get; set; }
        public int websiteId { get; set; }
        public string? status { get; set; }
    }
}
