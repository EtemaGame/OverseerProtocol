using System;
using System.IO;
using OverseerProtocol.Core.Logging;
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
            OPLog.Info("Json", $"JSON file not found: {path}");
            return default;
        }

        try
        {
            var json = File.ReadAllText(path);
            OPLog.Info("Json", $"Reading JSON file: {path} ({json.Length} chars)");
            var result = JsonConvert.DeserializeObject<T>(json, Settings);
            OPLog.Info("Json", $"Read JSON file OK: {path}, type={typeof(T).Name}, resultNull={result == null}");
            return result;
        }
        catch (Exception ex)
        {
            OPLog.Warning("Json", $"Failed to read JSON file '{path}': {ex.Message}");
            return default;
        }
    }
}
