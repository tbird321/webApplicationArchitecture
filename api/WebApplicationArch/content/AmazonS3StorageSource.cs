//using Amazon;
//using Amazon.S3;
//using Amazon.S3.Model;
//using Amazon.S3.Transfer;
//using ContentManagementApplication.interfaces;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net.Http;
//using System.Text;

//namespace ContentManagementApplication.content
//{
//    public class AmazonS3StorageSource
//    {
//        #region Constructor
//        private string[] ExclusiveFileNamePrefix = new string[] { @"~$" };
//        private string[] FilterFileExt = new string[] { ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".ppt", ".pptx", ".txt", ".log", ".csv", ".jpg", ".jpeg", ".gif", ".png" };

//        public AmazonS3StorageSource(string awsAccessKey, string awsSecretAccessKey, string bucketName)
//        {
//            amazonClient = new AmazonS3Client(awsAccessKey, awsSecretAccessKey, new AmazonS3Config()
//            {
//                MaxErrorRetry = 15,
//                RegionEndpoint = RegionEndpoint.GetBySystemName("us-west-2")
//            });
//            BucketName = bucketName;
//        }

//        #endregion

//        #region Events

//        public event FileUploadProgress OnFileUploadProgress;
//        public event FileUploaded OnFileUploaded;
//        public event FileUploadError OnFileUploadedError;
//        public event FileUploadError OnFileCopyBetweenBucketsError;

//        #endregion

//        #region Properties
//        IAmazonS3 amazonClient;
//        private long PART_SIZE = 50 * (int)Math.Pow(2, 20);//50meg

//        private string TemporaryFolder { get; set; }
//        private string BucketName { get; set; }

//        #endregion


//        #region PublicMethods
//        public List<string> GetFileList(string bucketPath)
//        {
//            var imageFiles = ListingObjects(bucketPath);
//            return imageFiles;
//        }
//        public Stream DownloadFile(string filename, string path)
//        {
//            string s3Filename = GetS3Filename(filename, path);
//            System.Diagnostics.Debug.WriteLine("S3 Download " + s3Filename);
//            System.Diagnostics.Debug.WriteLine(BucketName);
//            return DownloadS3File(s3Filename, BucketName);
//        }

//        public void UploadFile(Stream filestream, string filename, string path)
//        {
//            //save file locally - then upload to s3
//            string s3Filename = GetS3Filename(filename, path);
//            string localFilePath = HttpContent.Current.Server.MapPath("~/App_Data/" + Guid.NewGuid());
//            using (filestream)
//            {
//                CopyFileLocal(filestream, localFilePath);
//                filestream.Close();
//            }
//            UploadFile(localFilePath, s3Filename, BucketName);
//        }

//        /*
//        public void CopyFile(int createdByuserId, int accountId, int workCenterId, Guid fileName, string docPath, int destAccountId, int destWorkCenterId, Guid destinationFileName, string destDocPath)
//        {
//            string sourcePath = DeterminePath(accountId, workCenterId, docPath, fileName);
//            string destPath = DeterminePath(destAccountId, destWorkCenterId, destDocPath, destinationFileName);
//            //Copy File on Seperate Thread
//            FileTransferInfo startTransfer = new FileTransferInfo()
//            {
//                AccountId = accountId,
//                WorkCenterId = workCenterId,
//                FileName = fileName,
//                SourceFilePath = docPath,
//                TemporaryFolder = TemporaryFolder,
//                BucketName = BucketName,
//                DestAccountId = destAccountId,
//                DestWorkCenterId = destWorkCenterId,
//                DestFileName = destinationFileName,
//                DestBucketName = BucketName,
//                DestFilePath = destDocPath
//            };
//            CopyFileThread(startTransfer);
//        }

//        public void CopyFileBetweenBuckets(Guid fileName, string sourceBucket, string destinationBucket, int accountId, int workCenterId, string environment)
//        {
//            FileTransferInfo startTransfer = new FileTransferInfo()
//            {
//                AccountId = accountId,
//                WorkCenterId = workCenterId,
//                FileName = fileName,
//                SourceFilePath = environment,
//                TemporaryFolder = TemporaryFolder,
//                BucketName = BucketName,
//                DestBucketName = destinationBucket
//            };
//            CopyFileBetweenBucketsThread(startTransfer);
//        }
//        */
//        #endregion

//        #region PrivateMethods
//        public void SynchDirectories(string localFilePath, string path)
//        {
//            var s3Objects = listObjects();
//            var localFiles = Directory.GetFiles(localFilePath, "*", SearchOption.AllDirectories);
//            var localDirs = Directory.GetDirectories(localFilePath, "*", SearchOption.AllDirectories);

//            foreach (var s3obj in s3Objects)
//            {
//                var localPath = getLocalPath(s3obj.Key, localFilePath);
//                if (s3obj.Key.EndsWith("/"))
//                {
//                    // dir → Create Local Dir
//                    Directory.CreateDirectory(localPath);
//                }
//                else
//                {
//                    // file
//                    // check exist and download, if S3 file is newer or not exitst.
//                    if (File.Exists(localPath))
//                    {
//                        if (TimeZone.CurrentTimeZone.ToUniversalTime(File.GetLastWriteTime(localPath)) < s3obj.LastModified)
//                        {
//                            // s3 file is newer → download s3 file.
//                            //download(s3obj.Key, localPath);
//                        }
//                        else
//                        {
//                            // local file is newer → upload local file.
//                            var key = getS3Key(localPath, path);
//                            UploadFile(localPath, key, BucketName, false);
//                        }
//                    }
//                    else
//                    {
//                        // Not exsist on local → download s3 file.
//                        download(s3obj.Key, localPath);
//                    }
//                }
//            }

//            foreach (var localfile in localFiles)
//            {
//                var needUpload = true;
//                foreach (var s3obj in s3Objects)
//                {
//                    var s3filename = getLocalPath(s3obj.Key, path);
//                    if (s3filename == localfile)
//                    {
//                        needUpload = false;
//                        continue;
//                    }
//                }
//                if (needUpload)
//                {
//                    if (checkFile(localfile))
//                    {
//                        var key = getS3Key(localfile, localFilePath);
//                        UploadFile(localfile, key, BucketName, false);
//                        //upload(localfile, key);
//                    }
//                }
//            }

//            foreach (var localDir in localDirs)
//            {
//                var entryCount = Directory.GetFileSystemEntries(localDir).Count();
//                if (entryCount == 0)
//                {
//                    // create dir if empty
//                    var key = getS3DirKey(localDir, path);
//                    createDir(key);
//                }
//            }
//        }
//        private void createDir(string key)
//        {
//            var putRequest = new PutObjectRequest
//            {
//                BucketName = BucketName,
//                Key = key,
//                ContentType = "application/octet-stream"
//            };
//            amazonClient.PutObject(putRequest);
//        }
//        private string getS3DirKey(string localPath, string s3path)
//        {
//            return getS3Key(localPath, s3path) + @"/";
//        }
//        private List<S3Object> listObjects()
//        {
//            return amazonClient.ListObjects(new ListObjectsRequest() { BucketName = BucketName }).S3Objects;
//        }
//        private string getLocalPath(string s3Path, string synchDir)
//        {
//            return String.Format(@"{0}\{1}", synchDir, s3Path.Replace(@"/", @"\"));
//        }
//        private Boolean checkFile(string localfile)
//        {
//            if (checkFileName(localfile) == false) { return false; }
//            if (checkFileExt(localfile) == false) { return false; }
//            if (checkFileAttribute(localfile) == false) { return false; }

//            return true;
//        }
//        private Boolean checkFileExt(string filePath)
//        {
//            foreach (var filterExt in FilterFileExt)
//            {
//                var fileExt = Path.GetExtension(filePath);
//                if (fileExt == filterExt)
//                {
//                    return true;
//                }
//                if (fileExt == filterExt.ToUpper())
//                {
//                    return true;
//                }
//            }
//            return false;
//        }
//        private Boolean checkFileAttribute(string filePath)
//        {
//            var attr = File.GetAttributes(filePath);
//            if ((attr & FileAttributes.Hidden) == FileAttributes.Hidden) { return false; }
//            if ((attr & FileAttributes.Temporary) == FileAttributes.Temporary) { return false; }

//            return true;
//        }
//        private void download(string key, string localPath)
//        {
//            GetObjectRequest getRequest = new GetObjectRequest
//            {
//                BucketName = BucketName,
//                Key = key
//            };
//            var response = amazonClient.GetObject(getRequest);
//            response.WriteResponseStreamToFile(localPath);
//        }
//        private void upload(string localFilePath, string key)
//        {
//            var putRequest = new PutObjectRequest
//            {
//                BucketName = BucketName,
//                Key = key,
//                FilePath = localFilePath,
//                ContentType = "application/octet-stream"
//            };
//            try
//            {
//                amazonClient.PutObject(putRequest);
//            }
//            catch (IOException ex)
//            {
//                // Local file opened → do nothing
//            }
//        }
//        private Boolean checkFileName(string filePath)
//        {
//            foreach (var prefix in ExclusiveFileNamePrefix)
//            {
//                if (filePath.StartsWith(prefix) == true)
//                {
//                    return false;
//                }
//            }
//            return true;
//        }
//        private string getS3Key(string locaPath, string localFilePath)
//        {
//            var bucketKey = locaPath.Replace(localFilePath + @"\", "").Replace(@"\", @"/");
//            return bucketKey;
//        }
//        private List<string> ListingObjects(string prefix)
//        {
//            List<string> imageFiles = new List<string>();
//            try
//            {
//                ListObjectsRequest request = new ListObjectsRequest
//                {
//                    BucketName = BucketName,
//                    MaxKeys = 2,
//                    Prefix = prefix
//                };

//                do
//                {
//                    ListObjectsResponse response = amazonClient.ListObjects(request);

//                    // Process response.
//                    foreach (S3Object entry in response.S3Objects)
//                    {
//                        System.Diagnostics.Debug.WriteLine("key = {0} size = {1}",
//                            entry.Key, entry.Size);
//                        if (!entry.Key.Equals("data/Images/"))
//                        {
//                            imageFiles.Add(entry.Key);
//                        }
//                    }

//                    // If response is truncated, set the marker to get the next 
//                    // set of keys.
//                    if (response.IsTruncated)
//                    {
//                        request.Marker = response.NextMarker;
//                    }
//                    else
//                    {
//                        request = null;
//                    }
//                } while (request != null);
//            }
//            catch (AmazonS3Exception amazonS3Exception)
//            {
//                if (amazonS3Exception.ErrorCode != null &&
//                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
//                    ||
//                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
//                {
//                    Console.WriteLine("Check the provided AWS Credentials.");
//                    Console.WriteLine(
//                    "To sign up for service, go to http://aws.amazon.com/s3");
//                }
//                else
//                {
//                    Console.WriteLine(
//                     "Error occurred. Message:'{0}' when listing objects",
//                     amazonS3Exception.Message);
//                }
//            }
//            return imageFiles;
//        }
//        public string GetS3Filename(string filename, string path)
//        {
//            string delimiter = "/";
//            string bucketKey = string.Concat(path, delimiter, filename);

//            if (string.IsNullOrEmpty(path))
//            {
//                bucketKey = string.Concat(filename);
//            }
//            return bucketKey;
//        }

//        private Stream DownloadS3File(string bucketKey, string bucketName)
//        {
//            try
//            {
//                TransferUtility oTransfer = new TransferUtility(amazonClient);
//                TransferUtilityOpenStreamRequest request = new TransferUtilityOpenStreamRequest()
//                {
//                    BucketName = bucketName,
//                    Key = bucketKey
//                };
//                return oTransfer.OpenStream(request);
//            }
//            catch (Amazon.S3.AmazonS3Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine(ex.ToString());
//                if (ex.ErrorCode.Equals("NoSuchKey", StringComparison.InvariantCultureIgnoreCase))
//                {
//                    throw new System.Exception("File not available yet", ex);
//                }
//                else
//                {
//                    throw new System.Exception("There was an issue retrieving your file", ex);
//                }
//            }
//        }

//        private void CopyFileLocal(Stream fileStream, string fileName)
//        {
//            using (System.IO.FileStream newFile = new FileStream(fileName, FileMode.CreateNew))
//            {
//                fileStream.CopyTo(newFile);
//                newFile.Close();
//            }
//        }

//        public void UploadFile(string filename, string bucketKey, string bucketName, bool deleteAfterUpload = true)
//        {

//            DateTime startTime = DateTime.Now;
//            System.Diagnostics.Debug.WriteLine(string.Concat(bucketKey, ":", startTime));
//            TransferUtility oTransfer = new TransferUtility(amazonClient);
//            TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
//            {
//                AutoCloseStream = false,
//                BucketName = bucketName,
//                CannedACL = S3CannedACL.PublicRead,
//                FilePath = filename,
//                Key = bucketKey,
//                PartSize = PART_SIZE,
//                Timeout = new TimeSpan(0, 25, 0)
//            };
//            request.Headers.CacheControl = "max-age=0";
//            request.UploadProgressEvent += new EventHandler<UploadProgressArgs>(UploadProgress);
//            oTransfer.Upload(request);
//            DateTime endTime = DateTime.Now;
//            TimeSpan totalTime = endTime - startTime;
//            System.Diagnostics.Debug.WriteLine("TransferTime:" + totalTime.TotalSeconds);
//            if (File.Exists(filename) && deleteAfterUpload)
//            {
//                File.Delete(filename);
//            }
//        }

//        /*
//        private void CopyFileBetweenBucketsThread(object threadStart)
//        {
//            FileTransferInfo startinfo = (FileTransferInfo)threadStart;
//            string bucketKey = DeterminePath(startinfo.AccountId, startinfo.WorkCenterId, startinfo.SourceFilePath, startinfo.FileName);
//            string destbucketKey = DeterminePath(startinfo.DestAccountId, startinfo.DestWorkCenterId, startinfo.DestFilePath, startinfo.DestFileName);
//            CopyObjectRequest request = new CopyObjectRequest()
//            {
//                SourceBucket = startinfo.BucketName,
//                SourceKey = bucketKey,
//                DestinationBucket = startinfo.DestBucketName,
//                DestinationKey = destbucketKey
//            };
//            try
//            {
//                DateTime startTime = DateTime.Now;
//                System.Diagnostics.Debug.WriteLine("Start:" + startTime);
//                CopyObjectResponse response = amazonClient.CopyObject(request);
//                DateTime endTime = DateTime.Now;
//                System.Diagnostics.Debug.WriteLine("END:" + DateTime.Now);
//                TimeSpan totalTime = endTime - startTime;
//                System.Diagnostics.Debug.WriteLine("TransferTime:" + totalTime.TotalSeconds);

//            }
//            catch (Exception ex)
//            {
//                if (OnFileCopyBetweenBucketsError != null)
//                {
//                    OnFileCopyBetweenBucketsError(startinfo, ex);
//                }
//                else
//                {
//                    throw;
//                }
//            }
//        }

//        private void CopyFileThread(object threadStart)
//        {
//            FileTransferInfo startinfo = (FileTransferInfo)threadStart;
//            System.Diagnostics.Debug.WriteLine("Start S3 Process" + DateTime.Now.ToString());
//            string sourceKey = DeterminePath(startinfo.AccountId, startinfo.WorkCenterId, startinfo.SourceFilePath, startinfo.FileName);
//            string destKey = DeterminePath(startinfo.DestAccountId, startinfo.DestWorkCenterId, startinfo.DestFilePath, startinfo.DestFileName);
//            //if (startinfo.AccountId == startinfo.DestAccountId && startinfo.WorkCenterId == startinfo.DestWorkCenterId)
//            // {
//            //     CopyFileBetweenBucketsThread(startinfo);// For some reason this doesn't work - encryption gets messed up
//            // }
//            // else
//            //{
//            //Copy with new encryption
//            Stream downloadFile = DownloadFile(sourceKey, BucketName);
//            CopyFileLocal(downloadFile, startinfo.DestFileName, startinfo.DestWorkCenterId);
//            string downloadFilepath = string.Concat(TemporaryFolder, "\\", startinfo.DestFileName);
//            UploadFile(downloadFilepath, destKey, BucketName);
//            //}
//            System.Diagnostics.Debug.WriteLine("End Process" + DateTime.Now.ToString());
//        }
        
//        */

//        int currentprogress = 0;
//        static string objectlock = "";
//        private void UploadProgress(object sender, UploadProgressArgs result)
//        {
//            int temppercent = result.PercentDone % 10;
//            if (temppercent == 0)
//            {
//                bool writeOut = false;
//                lock (objectlock)
//                {
//                    if (currentprogress != result.PercentDone)
//                    {
//                        currentprogress = result.PercentDone;
//                        writeOut = true;
//                    }
//                }
//                if (writeOut)
//                {
//                    if (OnFileUploadProgress != null)
//                    {
//                        UploadProgressStatus progress = new UploadProgressStatus(result.PercentDone, result.TotalBytes, result.TransferredBytes, result.FilePath);
//                        OnFileUploadProgress(sender, progress);
//                    }
//                }
//            }
//        }
//        #endregion
//        /*
//        public void DeleteFile(int createdByuserId, int accountId, int workCenterId, Guid fileName, string sourceDocPath)
//        {
//            string bucketKey = DeterminePath(accountId, workCenterId, sourceDocPath, fileName);
//            if (IsFileInS3(BucketName, bucketKey))
//            {
//                System.Diagnostics.Debug.WriteLine(string.Concat("Start file delete process " + DateTime.Now.ToString(), " ", bucketKey));
//                DeleteObjectRequest request = new DeleteObjectRequest()
//                {
//                    BucketName = BucketName,
//                    Key = bucketKey
//                };
//                amazonClient.DeleteObject(request);
//                System.Diagnostics.Debug.WriteLine(string.Concat("End Process " + DateTime.Now.ToString(), " ", bucketKey));
//            }
//        }
//        */
//        private bool IsFileInS3(string bucketName, string fullFileName)
//        {
//            S3FileInfo fileInfo = new S3FileInfo(amazonClient, bucketName, fullFileName);
//            try
//            {
//                if (fileInfo.Exists)
//                {
//                    return true;
//                }
//                else
//                {
//                    return false;
//                }
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        /*
//                public bool DoesFileExist(int accountId, int workCenterId, Guid fileName, string sourceDocPath)
//                {
//                    string bucketKey = DeterminePath(accountId, workCenterId, sourceDocPath, fileName);
//                    return IsFileInS3(BucketName, bucketKey);
//                }
//         * */
//    }


//}
