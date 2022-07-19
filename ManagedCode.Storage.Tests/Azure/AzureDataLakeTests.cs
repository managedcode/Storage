using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using CsvHelper;
using CsvHelper.Configuration;
using FluentAssertions;
using Xunit;

namespace ManagedCode.Storage.Tests.Azure;

public class AzureDataLakeTests //: StorageBaseTests
{
    private string connectionString =
        "";



    public AzureDataLakeTests()
    {
        var lines = File.ReadAllLines("test.csv").Last();
        var fs = File.OpenWrite("test.csv");
        using var sw = new StreamWriter(fs);
        for (int i = 0; i < 150_000; i++)
        {
            sw.WriteLine(lines);
        }
    }
    
    
    [Fact]
    public async Task QueryDataLakeTest()
    {
        var c = new BlockBlobClient(connectionString, "keyload-file-system", "analytics/web-site-id-keyload");
        await QueryHemingway(c);
        await QueryHemingway2(c);
        
        
    }
    
    [Fact]
    public async Task DataLakeTest()
    {
        
        File.Exists("test.csv").Should().BeTrue();

        
        
        
        
        //StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(AccountName, AccountKey);
        //string dfsUri = "https://" + AccountName + ".dfs.core.windows.net";

        var dataLakeServiceClient = new DataLakeServiceClient (connectionString);

        var x = await dataLakeServiceClient.GetFileSystemsAsync().AsPages().ToListAsync();
        
        //tests
        var fs = await CreateFileSystem(dataLakeServiceClient, "keyload-file-system");
        var dir = await CreateDirectory(dataLakeServiceClient, "keyload-file-system", "test");
        await UploadFile(fs, "test", "test.csv","test.csv");
        await ListFilesInDirectory(fs, "test");
        
        
      //  BlobQuickQueryClient 
        
   

    }


    //Create a container
    public async Task<DataLakeFileSystemClient> CreateFileSystem(DataLakeServiceClient serviceClient, string fileSystemName)
    {
        return await serviceClient.CreateFileSystemAsync(fileSystemName);
    }
    
  
    //Create a directory
    public async Task<DataLakeDirectoryClient> CreateDirectory(DataLakeServiceClient serviceClient, string fileSystemName, string directoryName)
    {
        DataLakeFileSystemClient fileSystemClient = serviceClient.GetFileSystemClient(fileSystemName);
        return await fileSystemClient.CreateDirectoryAsync(directoryName);
    }
    
    //Create a subdirectory
    public async Task<DataLakeDirectoryClient> CreateDirectory(DataLakeServiceClient serviceClient, string fileSystemName, string directoryName, string subdirectory)
    {
        DataLakeFileSystemClient fileSystemClient = serviceClient.GetFileSystemClient(fileSystemName);
        DataLakeDirectoryClient directoryClient = await fileSystemClient.CreateDirectoryAsync(directoryName);
        return await directoryClient.CreateSubDirectoryAsync(subdirectory);
    }
    

    //Rename or move a directory
    public async Task<DataLakeDirectoryClient> RenameDirectory(DataLakeFileSystemClient fileSystemClient)
    {
        DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient("my-directory/my-subdirectory");
        return await directoryClient.RenameAsync("my-directory/my-subdirectory-renamed");
    }
    
    
    //Delete a directory
    public async Task DeleteDirectory(DataLakeFileSystemClient fileSystemClient)
    {
        DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient("my-directory");
        await directoryClient.DeleteAsync();
    }
    
    
    //Upload a file to a directory
    public async Task UploadFile(DataLakeFileSystemClient fileSystemClient, string directory, string fileName, string filePath)
    {
        DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient(directory);
        DataLakeFileClient fileClient = await directoryClient.CreateFileAsync(fileName);
        
        FileStream fileStream = File.OpenRead(filePath);
        long fileSize = fileStream.Length;
        await fileClient.AppendAsync(fileStream, offset: 0);
        await fileClient.FlushAsync(position: fileSize);
    }
    
    
    //Upload a large file to a directory
    public async Task UploadFileBulk(DataLakeFileSystemClient fileSystemClient)
    {
        DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient("my-directory");
        DataLakeFileClient fileClient = directoryClient.GetFileClient("uploaded-file.txt");
        FileStream fileStream = File.OpenRead("C:\\Users\\contoso\\file-to-upload.txt");
        await fileClient.UploadAsync(fileStream);
    }
    
    //Download from a directory
    public async Task DownloadFile(DataLakeFileSystemClient fileSystemClient)
    {
        DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient("my-directory");
        DataLakeFileClient fileClient = directoryClient.GetFileClient("my-image.png");
        Response<FileDownloadInfo> downloadResponse = await fileClient.ReadAsync();
        BinaryReader reader = new BinaryReader(downloadResponse.Value.Content);
        FileStream fileStream = File.OpenWrite("C:\\Users\\contoso\\my-image-downloaded.png");

        int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];
        int count;
        while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
        {
            fileStream.Write(buffer, 0, count);
        }
        await fileStream.FlushAsync();
        fileStream.Close();
    }
    
    //List directory contents
    public async Task ListFilesInDirectory(DataLakeFileSystemClient fileSystemClient, string directory)
    {
        IAsyncEnumerator<PathItem> enumerator = fileSystemClient.GetPathsAsync(directory).GetAsyncEnumerator();
        await enumerator.MoveNextAsync();
        PathItem item = enumerator.Current;
        while (item != null)
        {
            Console.WriteLine(item.Name);
            if (!await enumerator.MoveNextAsync())
            {
                break;
            }
            item = enumerator.Current;
        }

    }
    

    //https://docs.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-query-acceleration-how-to?tabs=dotnet
    //Retrieve data by using a filter  
    static async Task QueryHemingway(BlockBlobClient blob)
    {
        string query = @"SELECT * FROM BlobStorage WHERE _11 = 'Ukraine";
        await DumpQueryCsv(blob, query, false);
    }
    
    static async Task QueryHemingway2(BlockBlobClient blob)
    {
        string query = @"SELECT * FROM BlobStorage WHERE _12 = 'Kharkiv";
        await DumpQueryCsv(blob, query, false);
    }
    
    //Retrieve specific columns
    static async Task QueryBibNum(BlockBlobClient blob)
    {
        string query = @"SELECT BibNum FROM BlobStorage";
        await DumpQueryCsv(blob, query, true);
    }
    
    //Retrieve specific columns
    static async Task QueryDvds(BlockBlobClient blob)
    {
        string query = @"SELECT BibNum, Title, Author, ISBN, Publisher, ItemType
        FROM BlobStorage
        WHERE ItemType IN
            ('acdvd', 'cadvd', 'cadvdnf', 'calndvd', 'ccdvd', 'ccdvdnf', 'jcdvd', 'nadvd', 'nadvdnf', 'nalndvd', 'ncdvd', 'ncdvdnf')";
        await DumpQueryCsv(blob, query, true);
    }

    private static async Task DumpQueryCsv(BlockBlobClient blob, string query, bool headers)
    {
        try
        {
            var options = new BlobQueryOptions()
            {
                InputTextConfiguration = new BlobQueryCsvTextOptions()
                { 
                    HasHeaders = true, 
                    RecordSeparator = "\n", 
                    ColumnSeparator = ",", 
                    EscapeCharacter = '\\', 
                    QuotationCharacter = '"'
                },
                OutputTextConfiguration = new BlobQueryCsvTextOptions() 
                { 
                    HasHeaders = true, 
                    RecordSeparator = "\n", 
                    ColumnSeparator = ",", 
                    EscapeCharacter = '\\', 
                    QuotationCharacter = '"' },
                ProgressHandler = new Progress<long>((finishedBytes) => 
                    Console.Error.WriteLine($"Data read: {finishedBytes}"))
            };
            options.ErrorHandler += (BlobQueryError err) => {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: {err.Position}:{err.Name}:{err.Description}");
                Console.ResetColor();
            };
            // BlobDownloadInfo exposes a Stream that will make results available when received rather than blocking for the entire response.
            using (var reader = new StreamReader((await blob.QueryAsync(query, options)).Value.Content))
            {
                using (var parser = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = true }))
                {
                    while (await parser.ReadAsync())
                    {
                        Console.Out.WriteLine(String.Join(" ", parser.Parser.Record));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}