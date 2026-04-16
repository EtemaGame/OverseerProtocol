using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OverseerProtocol.Core.Serialization;

public static class JsonFileReader
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public static T? Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch
        {
            return default;
        }
    }
}
