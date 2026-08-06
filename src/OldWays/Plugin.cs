using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace OldWays
{
    /// <summary>
    /// The Old Ways — player skill/weapon mastery and mastery-reactive enemy behavior.
    /// See CLAUDE.md and docs/ for the design; docs/09-roadmap.md for the phase plan.
    ///
    /// Phase 0 scaffold: loads, logs its version and environment, binds config. No behavior yet.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "yesu0725.oldways";
        public const string PluginName = "The Old Ways";
        public const string PluginVersion = "0.1.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        /// <summary>
        /// True on the dedicated server. Proven is server-authoritative
        /// (docs/07-technical-architecture.md), so most gameplay logic will branch on this.
        /// </summary>
        internal static bool IsHeadless => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            OldWaysConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded ({(IsHeadless ? "dedicated server" : "client")}).");
            Log.LogInfo($"Harmony patches applied: {_harmony.GetPatchedMethods().CountOrZero()}");
            Log.LogInfo($"ServerSync active — config is server-authoritative, " +
                        $"lock={OldWaysConfig.LockConfig.Value}, min client version={PluginVersion}.");

            if (!OldWaysConfig.ModEnabled.Value)
            {
                Log.LogWarning("Mod is disabled in config — no systems will run.");
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    internal static class EnumerableExt
    {
        internal static int CountOrZero<T>(this System.Collections.Generic.IEnumerable<T> source)
        {
            if (source == null) return 0;
            var n = 0;
            foreach (var _ in source) n++;
            return n;
        }
    }
}
