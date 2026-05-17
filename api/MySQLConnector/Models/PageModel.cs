using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Models
{
    public class PageModel
    {
        public int? id { get; set; }
        public int? websiteId { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public List<string> topics { get; set; }
        public List<string> keywords { get; set; }
        public string? style { get; set; }
        public string? layout { get; set; }
        public int? layoutid { get; set; }
        public string? status { get; set; }
        public List<ArticleModel> articles { get; set; }
        public PageModel() {
            topics= new List<string>();
            keywords= new List<string>();
            articles= new List<ArticleModel>();
        }
    }

}
