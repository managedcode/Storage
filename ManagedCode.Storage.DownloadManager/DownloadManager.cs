using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;

namespace ManagedCode.Storage.DownloadManager;

public class DownloadManager : IDownloadManager
{
    private readonly IStorage _storage;

    public DownloadManager(IStorage storage)
    {
        _storage = storage;
    }

    public Task<Stream> Download(string blob)
    {
        throw new NotImplementedException();
    }

    public Task Upload(Stream stream)
    {
        throw new NotImplementedException();
    }
}