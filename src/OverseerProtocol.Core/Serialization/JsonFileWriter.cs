using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OverseerProtocol.Core.Serialization;

public static class JsonFileWriter
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public static void Write<T>(string path, T data)
    {
        var json = JsonConvert.SerializeObject(data, Settings);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, json);
    }
}
