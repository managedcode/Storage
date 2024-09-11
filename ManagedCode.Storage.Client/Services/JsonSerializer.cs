using Newtonsoft.Json;

namespace ManagedCode.Storage.Client.Services
{
    public class JsonSerializer : IJsonSerializer
    {
        public string Serialize<TModel>(TModel data)
        {
            return JsonConvert.SerializeObject(data);
        }

        public TModel? Deserialize<TModel>(string value)
        {
            return JsonConvert.DeserializeObject<TModel>(value);
        }
    }
}