using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebApplicationArch.model
{
    [Serializable]
    public class fileTransferInfo
    {
        public Stream FileStream { get; set; }
        public string SourceFilePath { get; set; }
        public string BucketName { get; set; }
        public string DestBucketName { get; set; }
        public string DestFilePath { get; set; }
    }
}
