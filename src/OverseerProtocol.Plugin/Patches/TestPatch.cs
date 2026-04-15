using HarmonyLib;
using UnityEngine;

namespace OverseerProtocol.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class TestPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void AddLogOnStart()
        {
            Plugin.Log.LogInfo("GameNetworkManager.Start detectado por Harmony patch (prueba ok).");
        }
    }
}
