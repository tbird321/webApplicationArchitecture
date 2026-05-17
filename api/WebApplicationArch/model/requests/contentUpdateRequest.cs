using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model.requests
{
    [Serializable]
    public class contentUpdateRequest
    {
        public contentUpdateRequest()
        {
            content = new List<contentInfo>();
        }
        public string authToken { get; set; }
        public string userName { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        public List<contentInfo> content { get; set; }
    }
}
