using HarmonyLib;
using KitchenMods;
using PreferenceSystem;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenBetterTableAssignment
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.BetterTableAssignment";
        public const string MOD_NAME = "BetterTableAssignment";
        public const string MOD_VERSION = "1.0.1";

        internal static PreferenceSystemManager PrefManager;
        internal const string BAR_TABLE_ID = "barTable";
        internal const string SIMPLE_TABLE_ID = "simpleTable";
        internal const string FANCY_TABLE_ID = "fancyTable";
        internal const string METAL_TABLE_ID = "metalTable";
        internal const string UNKNOWN_TABLE_ID = "otherTable";

        Harmony _harmony;
        static List<Assembly> PatchedAssemblies = new List<Assembly>();

        public Main()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony(MOD_GUID);
            }
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (assembly != null && !PatchedAssemblies.Contains(assembly))
            {
                _harmony.PatchAll(assembly);
                PatchedAssemblies.Add(assembly);
            }
        }

        public void PostActivate(Mod mod)
        {
            // For log file output so the official plateup support staff can identify if/which a mod is being used
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            SetupPreferences();
        }

        public void PreInject() { }

        public void PostInject() { }

        private void SetupPreferences()
        {
            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);

            int[] ints = { 1, 2, 3, 4, 5 };
            string[] strings = { "1", "2", "3", "4", "5" };
            PrefManager
                .AddLabel("Better Table Assignment")
                .AddInfo("Assign table priority. Customers will prefer tables with a higher priority.")
                .AddInfo("Highest priority: 1")
                .AddInfo("Lowest priotity : 5")
                .AddLabel("Bar Table")
                .AddOption<int>(BAR_TABLE_ID, 1, ints, strings)
                .AddLabel("Simple Table")
                .AddOption<int>(SIMPLE_TABLE_ID, 1, ints, strings)
                .AddLabel("Fancy Table")
                .AddOption<int>(FANCY_TABLE_ID, 1, ints, strings)
                .AddLabel("Metal Table")
                .AddOption<int>(METAL_TABLE_ID, 1, ints, strings)
                .AddLabel("Others")
                .AddOption<int>(UNKNOWN_TABLE_ID, 1, ints, strings)
                .AddSpacer()
                .AddSpacer();

            PrefManager.RegisterMenu(PreferenceSystemManager.MenuType.PauseMenu);
        }

        #region Logging
        // You can remove this, I just prefer a more standardized logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
