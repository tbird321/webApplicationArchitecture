using System;
using System.Collections.Generic;
using System.Text;

namespace WebApplicationArch.model
{
    public class uploadProgressStatus
    {
        public uploadProgressStatus(int percentDone, long totalBytes, long transferedBytes, string filePath)
        {
            PercentComplete = percentDone;
            TotalBytes = totalBytes;
            TransferredBytes = transferedBytes;
            FilePath = filePath;
        }

        public int PercentComplete { get; set; }
        public long TotalBytes { get; set; }
        public long TransferredBytes { get; set; }
        public string FilePath { get; set; }
    }
}
