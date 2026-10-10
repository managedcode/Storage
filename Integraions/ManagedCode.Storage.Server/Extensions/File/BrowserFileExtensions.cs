using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace ManagedCode.Storage.Server.Extensions.File;

public static class BrowserFileExtensions
{
    public static async Task<LocalFile> ToLocalFileAsync(this IBrowserFile formFile, CancellationToken cancellationToken = default)
    {
        var localFile = LocalFile.FromRandomNameWithExtension(formFile.Name);
        try
        {
            await using var stream = formFile.OpenReadStream(maxAllowedSize: formFile.Size, cancellationToken: cancellationToken);
            await localFile.CopyFromStreamAsync(stream, cancellationToken);
            return localFile;
        }
        catch
        {
            await localFile.DisposeAsync();
            throw;
        }
    }
}
