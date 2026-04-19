using System;
using System.IO;
using System.Reflection;
using OverseerProtocol.Core.Logging;
using UnityEngine;

namespace OverseerProtocol.Features.HostFlow.AdvancedCompanyPort;

internal sealed class AdvancedCompanyAssetLoader
{
    private const string LobbyScreenPath = "Assets/Prefabs/UI/LobbyScreen.prefab";
    private object? _bundle;
    private GameObject? _lobbyScreenPrefab;
    private string _loadedPath = "";

    public bool TryLoadLobbyScreenPrefab(out GameObject? prefab, out string error)
    {
        prefab = null;
        error = "";

        if (_lobbyScreenPrefab != null)
        {
            prefab = _lobbyScreenPrefab;
            return true;
        }

        if (!TryLoadBundle(out error))
            return false;

        try
        {
                var loadAsset = _bundle!.GetType().GetMethod("LoadAsset", new[] { typeof(string), typeof(Type) });
                _lobbyScreenPrefab = loadAsset?.Invoke(_bundle, new object[] { LobbyScreenPath, typeof(GameObject) }) as GameObject;
            if (_lobbyScreenPrefab == null)
            {
                error = "AdvancedCompany lobby prefab was not found in advancedcompanyassets.";
                return false;
            }

            OPLog.Info("HostFlow", "Loaded AdvancedCompany lobby prefab from " + _loadedPath);
            prefab = _lobbyScreenPrefab;
            return true;
        }
        catch (Exception ex)
        {
            error = "Could not load AdvancedCompany lobby prefab: " + ex.Message;
            OPLog.Warning("HostFlow", error);
            return false;
        }
    }

    private bool TryLoadBundle(out string error)
    {
        error = "";
        if (_bundle != null)
            return true;

        foreach (var candidate in CandidatePaths())
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                var assetBundleType = Type.GetType("UnityEngine.AssetBundle, UnityEngine.AssetBundleModule");
                var loadFromFile = assetBundleType?.GetMethod("LoadFromFile", new[] { typeof(string) });
                _bundle = loadFromFile?.Invoke(null, new object[] { candidate });
                if (_bundle == null)
                    continue;

                _loadedPath = candidate;
                return true;
            }
            catch (Exception ex)
            {
                OPLog.Warning("HostFlow", "Could not load AdvancedCompany asset bundle at " + candidate + ": " + ex.Message);
            }
        }

        error = "advancedcompanyassets was not found next to the plugin.";
        return false;
    }

    private static string[] CandidatePaths()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        return new[]
        {
            Path.Combine(assemblyDir, "advancedcompanyassets"),
            Path.Combine(assemblyDir, "advancedcompany", "advancedcompanyassets"),
            Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "OverseerProtocol", "advancedcompanyassets")
        };
    }
}
