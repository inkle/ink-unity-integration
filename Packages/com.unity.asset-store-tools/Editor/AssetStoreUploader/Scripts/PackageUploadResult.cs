using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetStoreTools.Uploader
{
    internal class PackageUploadResult
    {
        public enum UploadStatus
        {
            Success = 1,
            Fail = 2,
            Cancelled = 3
        }

        public UploadStatus Status;
        public ASError Error;

        private PackageUploadResult() { }

        public static PackageUploadResult PackageUploadSuccess() => new PackageUploadResult() { Status = UploadStatus.Success };

        public static PackageUploadResult PackageUploadFail(ASError e) => new PackageUploadResult() { Status = UploadStatus.Fail, Error = e };

        public static PackageUploadResult PackageUploadCancelled() => new PackageUploadResult() { Status = UploadStatus.Cancelled };
    }
}
