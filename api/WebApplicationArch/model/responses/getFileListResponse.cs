using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model.responses
{
    [Serializable]
    public class getFileListResponse
    {
        public getFileListResponse()
        {
            files = new List<s3FileInfo>();
        }
        public Boolean status { get; set; }
        public string errorMessage { get; set; }
        public List<s3FileInfo> files { get; set; }
    }
}
