using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector.Models
{
    public class CollectionPageDetail
    {
        public CollectionModel collection { get; set; }
        public List<PageDetail> pages { get; set; }
        public CollectionPageDetail ()
        {
            pages = new List<PageDetail>();
            collection = new CollectionModel ();
        }
    }
    public class PageDetail : PageModel
    {
        public int? collection_page_id { get; set; }
        public int? page_id { get; set; }
        public int? sequence { get; set; }
    }
}
