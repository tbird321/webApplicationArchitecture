using WebApplicationArch.model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ContentManagementApplication.interfaces
{

    public delegate void FileUploadError(fileTransferInfo fileInfo, System.Exception ex);
    public delegate void FileUploaded(fileTransferInfo fileInfo);
    public delegate void FileUploadProgress(object sender, uploadProgressStatus result);

    public interface IThirdPartyStorage
    {

        Stream DownloadFile(int accountId, int workCenterId, string environment, Guid fileName);
        void UploadFile(Stream filestream, int createdByuserId, int accountId, int workCenterId, string docpath, Guid fileName);
        void CopyFile(int createdByuserId, int accountId, int workCenterId, Guid fileName, string sourceDocPath, int destAccountId, int destWorkCenterId, Guid destinationFileName, string destDocPath);
        void DeleteFile(int createdByuserId, int accountId, int workCenterId, Guid fileName, string sourceDocPath);
        bool DoesFileExist(int accountId, int workCenterId, Guid fileName, string sourceDocPath);
        event FileUploaded OnFileUploaded;
        event FileUploadProgress OnFileUploadProgress;
        event FileUploadError OnFileUploadedError;
    }
}
