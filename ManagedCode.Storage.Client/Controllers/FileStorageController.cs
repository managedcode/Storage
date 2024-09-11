using ManagedCode.Storage.Client.Constants;
using ManagedCode.Storage.FileSystem;

namespace ManagedCode.Storage.Client.Controllers
{
    // Create controllers for different storages using BaseStorageController as a base class
    [ApiController]
    [Route(RouteConstants.Storage)]
    public class FileStorageController : BaseStorageController
    {
        public FileStorageController(IFileSystemStorage storage, IOptions<FormOptions> formOptions) : base(storage, formOptions)
        {
        }
    }
}
