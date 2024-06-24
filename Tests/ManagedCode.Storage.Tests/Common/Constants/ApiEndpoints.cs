﻿namespace ManagedCode.Storage.Tests.Common.Constants;

public static class ApiEndpoints
{
    public const string Azure = "azure";

    public static class Base
    {
        public const string UploadFile = "{0}/upload";
        public const string UploadLargeFile = "{0}/upload-chunks";
        public const string DownloadFile = "{0}/download";
        public const string StreamFile = "{0}/stream";
    }
}