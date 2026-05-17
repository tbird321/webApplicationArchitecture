using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationArch.model.requests
{
    public class searchCriteria
    {
        public List<string> Keywords { get; set; }
        public List<string> Topics { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string WebsiteId { get; set; }
        public bool isValidRequest()
        {
            bool hasWebsiteId = WebsiteId != null && !String.IsNullOrEmpty(WebsiteId);
            return hasWebsiteId;
        }
    }
}
