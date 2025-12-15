namespace ManagedCode.Storage.Core.Constants;

/// <summary>
/// Standard metadata keys for storage providers
/// </summary>
public static class MetadataKeys
{
    // File system metadata
    public const string Permissions = "permissions";
    public const string FileType = "file_type";
    public const string Owner = "owner";
    public const string Group = "group";
    public const string LastAccessed = "last_accessed";
    public const string Created = "created";
    public const string Modified = "modified";

    // FTP specific
    public const string FtpRawPermissions = "ftp_raw_permissions";
    public const string FtpFileType = "ftp_file_type";
    public const string FtpSize = "ftp_size";
    public const string FtpModifyTime = "ftp_modify_time";

    // Cloud storage metadata
    public const string ContentEncoding = "content_encoding";
    public const string ContentLanguage = "content_language";
    public const string CacheControl = "cache_control";
    public const string ETag = "etag";
    public const string ContentHash = "content_hash";
    public const string StorageClass = "storage_class";

    // Azure specific
    public const string AzureBlobType = "azure_blob_type";
    public const string AzureAccessTier = "azure_access_tier";
    public const string AzureServerEncrypted = "azure_server_encrypted";

    // AWS specific  
    public const string AwsStorageClass = "aws_storage_class";
    public const string AwsServerSideEncryption = "aws_server_side_encryption";
    public const string AwsVersionId = "aws_version_id";

    // Google Cloud specific
    public const string GcsStorageClass = "gcs_storage_class";
    public const string GcsGeneration = "gcs_generation";
    public const string GcsMetageneration = "gcs_metageneration";

    // Media metadata
    public const string ImageWidth = "image_width";
    public const string ImageHeight = "image_height";
    public const string VideoDuration = "video_duration";
    public const string AudioBitrate = "audio_bitrate";

    // Custom application metadata
    public const string ApplicationName = "app_name";
    public const string ApplicationVersion = "app_version";
    public const string UserId = "user_id";
    public const string SessionId = "session_id";

    // Processing metadata
    public const string ProcessingStatus = "processing_status";
    public const string ThumbnailGenerated = "thumbnail_generated";
    public const string VirusScanned = "virus_scanned";
    public const string Compressed = "compressed";
    public const string Encrypted = "encrypted";
}

/// <summary>
/// Standard metadata values for common scenarios
/// </summary>
public static class MetadataValues
{
    // File types
    public static class FileTypes
    {
        public const string File = "file";
        public const string Directory = "directory";
        public const string SymbolicLink = "symbolic_link";
        public const string Unknown = "unknown";
    }

    // Processing statuses
    public static class ProcessingStatus
    {
        public const string Pending = "pending";
        public const string Processing = "processing";
        public const string Completed = "completed";
        public const string Failed = "failed";
    }

    // Boolean values
    public static class Boolean
    {
        public const string True = "true";
        public const string False = "false";
    }

    // Storage classes
    public static class StorageClasses
    {
        public const string Standard = "standard";
        public const string InfrequentAccess = "infrequent_access";
        public const string Archive = "archive";
        public const string ColdStorage = "cold_storage";
    }
}