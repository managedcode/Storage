using System;
using ManagedCode.Storage.VirtualFileSystem.Core;

namespace ManagedCode.Storage.VirtualFileSystem.Exceptions;

/// <summary>
/// Base exception for virtual file system operations
/// </summary>
public abstract class VfsException : Exception
{
    /// <summary>
    /// Initializes a new instance of VfsException
    /// </summary>
    protected VfsException()
    {
    }

    /// <summary>
    /// Initializes a new instance of VfsException with the specified message
    /// </summary>
    /// <param name="message">Error message</param>
    protected VfsException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of VfsException with the specified message and inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    protected VfsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a concurrent modification is detected
/// </summary>
public class VfsConcurrencyException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsConcurrencyException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="path">Path of the file that had concurrent modification</param>
    /// <param name="expectedETag">Expected ETag</param>
    /// <param name="actualETag">Actual ETag</param>
    public VfsConcurrencyException(string message, VfsPath path, string? expectedETag, string? actualETag)
        : base(message)
    {
        Path = path;
        ExpectedETag = expectedETag;
        ActualETag = actualETag;
    }

    /// <summary>
    /// Gets the path of the file that had concurrent modification
    /// </summary>
    public VfsPath Path { get; }

    /// <summary>
    /// Gets the expected ETag
    /// </summary>
    public string? ExpectedETag { get; }

    /// <summary>
    /// Gets the actual ETag
    /// </summary>
    public string? ActualETag { get; }
}

/// <summary>
/// Exception thrown when a file or directory is not found
/// </summary>
public class VfsNotFoundException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsNotFoundException
    /// </summary>
    /// <param name="path">Path that was not found</param>
    public VfsNotFoundException(VfsPath path)
        : base($"Path not found: {path}")
    {
        Path = path;
    }

    /// <summary>
    /// Initializes a new instance of VfsNotFoundException
    /// </summary>
    /// <param name="path">Path that was not found</param>
    /// <param name="message">Custom error message</param>
    public VfsNotFoundException(VfsPath path, string message)
        : base(message)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the path that was not found
    /// </summary>
    public VfsPath Path { get; }
}

/// <summary>
/// Exception thrown when a file or directory already exists and overwrite is not allowed
/// </summary>
public class VfsAlreadyExistsException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsAlreadyExistsException
    /// </summary>
    /// <param name="path">Path that already exists</param>
    public VfsAlreadyExistsException(VfsPath path)
        : base($"Path already exists: {path}")
    {
        Path = path;
    }

    /// <summary>
    /// Initializes a new instance of VfsAlreadyExistsException
    /// </summary>
    /// <param name="path">Path that already exists</param>
    /// <param name="message">Custom error message</param>
    public VfsAlreadyExistsException(VfsPath path, string message)
        : base(message)
    {
        Path = path;
    }

    /// <summary>
    /// Gets the path that already exists
    /// </summary>
    public VfsPath Path { get; }
}

/// <summary>
/// Exception thrown when an invalid path is provided
/// </summary>
public class VfsInvalidPathException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsInvalidPathException
    /// </summary>
    /// <param name="path">Invalid path</param>
    /// <param name="reason">Reason why the path is invalid</param>
    public VfsInvalidPathException(string path, string reason)
        : base($"Invalid path '{path}': {reason}")
    {
        InvalidPath = path;
        Reason = reason;
    }

    /// <summary>
    /// Gets the invalid path
    /// </summary>
    public string InvalidPath { get; }

    /// <summary>
    /// Gets the reason why the path is invalid
    /// </summary>
    public string Reason { get; }
}

/// <summary>
/// Exception thrown when an operation is not supported
/// </summary>
public class VfsNotSupportedException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsNotSupportedException
    /// </summary>
    /// <param name="operation">Operation that is not supported</param>
    public VfsNotSupportedException(string operation)
        : base($"Operation not supported: {operation}")
    {
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of VfsNotSupportedException
    /// </summary>
    /// <param name="operation">Operation that is not supported</param>
    /// <param name="reason">Reason why the operation is not supported</param>
    public VfsNotSupportedException(string operation, string reason)
        : base($"Operation not supported: {operation}. {reason}")
    {
        Operation = operation;
        Reason = reason;
    }

    /// <summary>
    /// Gets the operation that is not supported
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Gets the reason why the operation is not supported
    /// </summary>
    public string? Reason { get; }
}

/// <summary>
/// General VFS operation exception
/// </summary>
public class VfsOperationException : VfsException
{
    /// <summary>
    /// Initializes a new instance of VfsOperationException
    /// </summary>
    /// <param name="message">Error message</param>
    public VfsOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of VfsOperationException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public VfsOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}