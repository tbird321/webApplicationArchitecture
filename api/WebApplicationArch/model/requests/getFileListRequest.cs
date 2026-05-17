using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model.requests
{
    [Serializable]
    public class getFileListRequest
    {        
        public string authToken { get; set; }
        public string userName { get; set; }
        public string filePath { get; set; }
    }
}
