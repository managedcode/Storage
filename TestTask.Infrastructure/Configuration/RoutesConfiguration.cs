namespace TestTask.Infrastructure.Configuration;

public class RoutesConfiguration
{
    public required string UploadRoute { set; get; }
    
    public required string DownloadRoute { set; get; }
    
    public required string DeleteRoute { set; get; }
}