using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using PreferenceSystem;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Namespace should have "Kitchen" in the beginning
namespace KitchenBetterTableAssignment
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = "IcedMilo.PlateUp.BetterTableAssignment";
        public const string MOD_NAME = "BetterTableAssignment";
        public const string MOD_VERSION = "1.0.3";

        internal static PreferenceSystemManager PrefManager;

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
        }

        public void PreInject()
        {
            SetupPreferences();
        }

        public void PostInject() { }

        private void SetupPreferences()
        {
            PrefManager = new PreferenceSystemManager(MOD_GUID, MOD_NAME);

            List<Appliance> tables = new List<Appliance>();
            foreach (Appliance appliance in GameData.Main.Get<Appliance>().Where(
                x => x.Properties.Select(prop => prop?.GetType()).Contains(typeof(CApplianceTable))))
            {
                foreach (IApplianceProperty prop in appliance.Properties)
                {
                    if (prop != null &&
                        prop is CApplianceTable cApplianceTable && !cApplianceTable.IsWaitingTable)
                    {
                        tables.Add(appliance);
                        break;
                    }
                }
            }

            List<int> intsList = new List<int>()
            {
                int.MaxValue
            };
            List<string> stringsList = new List<string>()
            {
                "Ignore"
            };
            for (int i = 1; i < tables.Count + 1; i++)
            {
                intsList.Add(i);
                stringsList.Add(i.ToString());
            }


            int[] ints = intsList.ToArray();
            string[] strings = stringsList.ToArray();
            PrefManager
                .AddLabel("Better Table Assignment")
                .AddInfo("Assign table priority. Customers will prefer tables with a higher priority.")
                .AddInfo("Highest priority: 1")
                .AddInfo($"Lowest priority : {tables.Count}")
                .AddPageSelector(3)
                .AddSpacer();
            foreach (Appliance table in tables)
            {
                PrefManager
                    .StartPagedItem()
                    .AddLabel(table.name)
                    .AddOption(
                        table.ID.ToString(),
                        int.MaxValue,
                        ints,
                        strings)
                    .PagedItemDone();
            }
            PrefManager
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
