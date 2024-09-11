namespace ManagedCode.Storage.Client.Services.Interfaces
{
    public interface IJsonSerializer
    {
        string Serialize<TModel>(TModel data);

        TModel? Deserialize<TModel>(string value);
    }
}