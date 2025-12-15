using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Http;

namespace ManagedCode.Storage.Tests.Common;

public static class FileHelper
{
    private static readonly Random Random = new();

    public static LocalFile GenerateLocalFile(LocalFile localFile, int byteSize)
    {
        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
    }

    public static LocalFile GenerateLocalFile(string fileName, int byteSize)
    {
        var path = Path.Combine(Environment.CurrentDirectory, fileName);
        var localFile = new LocalFile(path);

        var fs = localFile.FileStream;

        fs.Seek(byteSize, SeekOrigin.Begin);
        fs.WriteByte(0);
        fs.Close();

        return localFile;
    }

    public static LocalFile GenerateLocalFileWithData(LocalFile file, int sizeInBytes)
    {
        using (var fileStream = file.FileStream)
        {
            var random = new Random();
            var buffer = new byte[1024]; // Buffer for writing in chunks

            while (sizeInBytes > 0)
            {
                var bytesToWrite = Math.Min(sizeInBytes, buffer.Length);

                for (var i = 0; i < bytesToWrite; i++)
                {
                    buffer[i] = (byte)random.Next(65, 91); // 'A' to 'Z'
                    if (random.Next(2) == 0)
                        buffer[i] = (byte)random.Next(97, 123); // 'a' to 'z'
                }

                fileStream.Write(buffer, 0, bytesToWrite);
                sizeInBytes -= bytesToWrite;
            }
        }

        return file;
    }

    public static IFormFile GenerateFormFile(string fileName, int byteSize)
    {
        var localFile = GenerateLocalFile(fileName, byteSize);
        var contentType = MimeHelper.GetMimeType(localFile.FileInfo.Extension);

        byte[] bytes;
        using (localFile)
        {
            using var ms = new MemoryStream();
            localFile.FileStream.CopyTo(ms);
            bytes = ms.ToArray();
        }

        return new InMemoryFormFile(bytes, fileName, fileName, contentType);
    }

    public static string GenerateRandomFileName()
    {
        string[] extensions = { "txt", "jpg", "png", "pdf", "docx", "xlsx", "pptx", "mp3", "mp4", "zip" };
        var randomExtension = extensions[Random.Next(extensions.Length)];
        return $"{Guid.NewGuid().ToString("N").ToLowerInvariant()}.{randomExtension}";
    }

    public static string GenerateRandomFileContent(int charCount = 250_000)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ-0123456789_abcdefghijklmnopqrstuvwxyz";

        return new string(Enumerable.Repeat(chars, charCount)
            .Select(s => s[Random.Next(s.Length)])
            .ToArray());
    }

    private sealed class InMemoryFormFile : IFormFile
    {
        private readonly byte[] _content;

        public InMemoryFormFile(byte[] content, string name, string fileName, string contentType)
        {
            _content = content;
            Name = name;
            FileName = fileName;
            ContentType = contentType;
            Headers = new HeaderDictionary
            {
                { "Content-Type", contentType }
            };
            ContentDisposition = $"form-data; name=\"{name}\"; filename=\"{fileName}\"";
        }

        public string ContentType { get; }
        public string ContentDisposition { get; }
        public IHeaderDictionary Headers { get; }
        public long Length => _content.Length;
        public string Name { get; }
        public string FileName { get; }

        public Stream OpenReadStream() => new MemoryStream(_content, writable: false);

        public void CopyTo(Stream target)
        {
            ArgumentNullException.ThrowIfNull(target);

            using var stream = OpenReadStream();
            stream.CopyTo(target);
        }

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(target);

            await using var stream = OpenReadStream();
            await stream.CopyToAsync(target, cancellationToken);
        }
    }
}
