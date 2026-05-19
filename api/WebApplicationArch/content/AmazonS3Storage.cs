using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebApplicationArch.model;

namespace WebApplicationArch.content
{
    public class AmazonS3Storage
    {
        IAmazonS3 amazonClient;
        private long PART_SIZE = 50 * (int)Math.Pow(2, 20);//50meg

        private string BucketName { get; set; }

        // Preferred constructor — uses the Lambda execution role (IAM), no credentials needed.
        public AmazonS3Storage(string bucketName, string region)
        {
            amazonClient = new AmazonS3Client(new AmazonS3Config()
            {
                MaxErrorRetry = 15,
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            });
            BucketName = bucketName;
        }

        // Legacy constructor kept for local dev where IAM role is not available.
        // Prefer passing credentials via environment variables or AWS credential chain instead.
        public AmazonS3Storage(string awsAccessKey, string awsSecretAccessKey, string bucketName, string region)
        {
            amazonClient = new AmazonS3Client(awsAccessKey, awsSecretAccessKey, new AmazonS3Config()
            {
                MaxErrorRetry = 15,
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            });
            BucketName = bucketName;
        }

        public string GetS3Filename(string filename, string path)
        {
            string delimiter = "/";
            string bucketKey = string.Concat(path, delimiter, filename);

            if (string.IsNullOrEmpty(path))
            {
                bucketKey = string.Concat(filename);
            }
            return bucketKey;
        }

        public async Task<bool> Exists(string filename, string path)
        {
            try
            {
                var response = await amazonClient.GetObjectMetadataAsync(new GetObjectMetadataRequest()
                {
                    BucketName = BucketName,
                    Key = GetS3Filename(filename, path)
                });
                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
        }

        public Stream DownloadFile(string filename, string path)
        {
            string s3Filename = GetS3Filename(filename, path);
            System.Diagnostics.Debug.WriteLine("S3 Download " + s3Filename);
            System.Diagnostics.Debug.WriteLine(BucketName);
            return DownloadS3File(s3Filename, BucketName);
        }
        public async Task<bool> DeleteFile(string filename, string path)
        {
            string s3Filename = GetS3Filename(filename, path);
            try
            {
                if (await Exists(filename, path))
                {
                    System.Diagnostics.Debug.WriteLine(string.Concat("Start file delete process " + DateTime.Now.ToString(), " ", s3Filename));
                    DeleteObjectRequest request = new DeleteObjectRequest()
                    {
                        BucketName = BucketName,
                        Key = s3Filename
                    };
                    await amazonClient.DeleteObjectAsync(request);
                    System.Diagnostics.Debug.WriteLine(string.Concat("End Process " + DateTime.Now.ToString(), " ", s3Filename));
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        private Stream DownloadS3File(string bucketKey, string bucketName)
        {
            try
            {
                TransferUtility oTransfer = new TransferUtility(amazonClient);
                TransferUtilityOpenStreamRequest request = new TransferUtilityOpenStreamRequest()
                {
                    BucketName = bucketName,
                    Key = bucketKey
                };
                return oTransfer.OpenStream(request);
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                if (ex.ErrorCode.Equals("NoSuchKey", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new System.Exception("File not available yet", ex);
                }
                else
                {
                    throw new System.Exception("There was an issue retrieving your file", ex);
                }
            }
        }

        /// <summary>
        /// Downloads a file and returns its content stream together with the S3 ETag.
        /// Use the ETag with UploadFileIfMatch to implement optimistic concurrency control.
        /// </summary>
        public async Task<(Stream stream, string etag)> DownloadFileWithETag(string filename, string path)
        {
            string key = GetS3Filename(filename, path);
            var response = await amazonClient.GetObjectAsync(new GetObjectRequest
            {
                BucketName = BucketName,
                Key = key
            });
            return (response.ResponseStream, response.ETag);
        }

        /// <summary>
        /// Uploads a file only if the object's current ETag matches the provided value.
        /// Returns true on success, false if the ETag has changed (another writer beat us).
        /// Callers should retry the full read-modify-write cycle on false.
        /// </summary>
        public async Task<bool> UploadFileIfMatch(Stream filestream, string filename, string path, string etag)
        {
            string key = GetS3Filename(filename, path);
            try
            {
                var request = new PutObjectRequest
                {
                    InputStream = filestream,
                    BucketName = BucketName,
                    Key = key
                };
                request.Headers["If-Match"] = etag;
                await amazonClient.PutObjectAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed ||
                ex.ErrorCode == "PreconditionFailed")
            {
                return false;
            }
        }

        public async Task<bool> UploadFile(Stream filestream, string filename, string path)
        {
            string s3Filename = GetS3Filename(filename, path);
            DateTime startTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine(string.Concat(s3Filename, ":", startTime));
            PutObjectRequest uploadRequest = new PutObjectRequest();

            uploadRequest.InputStream = filestream;
            uploadRequest.BucketName = BucketName;
            uploadRequest.Key = s3Filename;

            await amazonClient.PutObjectAsync(uploadRequest);
            return true;
        }

        public async Task<List<s3FileInfo>> GetFileList(string bucketPath)
        {
            var imageFiles = await ListingObjects(bucketPath);
            return imageFiles;
        }

        private async Task<List<s3FileInfo>> ListingObjects(string prefix)
        {
            List<s3FileInfo> imageFiles = new List<s3FileInfo>();
            try
            {
                ListObjectsRequest request = new ListObjectsRequest
                {
                    BucketName = BucketName,
                    MaxKeys = 2,
                    Prefix = prefix
                };

                do
                {
                    ListObjectsResponse response = await amazonClient.ListObjectsAsync(request);

                    // Process response.
                    foreach (S3Object entry in response.S3Objects)
                    {
                        System.Diagnostics.Debug.WriteLine("key = {0} size = {1}",
                            entry.Key, entry.Size);
                        imageFiles.Add(new s3FileInfo()
                        {
                            fileName = entry.Key,
                            size = entry.Size
                        });
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Check the provided AWS Credentials.");
                    Console.WriteLine(
                    "To sign up for service, go to http://aws.amazon.com/s3");
                }
                else
                {
                    Console.WriteLine(
                     "Error occurred. Message:'{0}' when listing objects",
                     amazonS3Exception.Message);
                }
            }
            return imageFiles;
        }
    }

}
