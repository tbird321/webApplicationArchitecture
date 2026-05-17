using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplicationArch.model
{
    [Serializable]
    public class s3FileInfo
    {
        public string fileName { get; set; }
        public long size { get; set; }

    }
}
