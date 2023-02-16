using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.Event;
using KitchenLib.References;
using KitchenLib.Utils;
using KitchenMods;
using System.Reflection;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenBetterTableAssignment
{
    public class Main : BaseMod
    {
        // guid must be unique and is recommended to be in reverse domain name notation
        // mod name that is displayed to the player and listed in the mods menu
        // mod version must follow semver e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.BetterTableAssignment";
        public const string MOD_NAME = "BetterTableAssignment";
        public const string MOD_VERSION = "1.0.0";
        public const string MOD_AUTHOR = "IcedMilo";
        public const string MOD_GAMEVERSION = ">=1.1.1";
        // Game version this mod is designed for in semver
        // e.g. ">=1.1.1" current and all future
        // e.g. ">=1.1.1 <=1.2.3" for all from/until

        internal static Main Instance;
        internal static PreferencesManager PrefManager;
        internal const string BAR_TABLE_ID = "barTable";
        internal const string SIMPLE_TABLE_ID = "simpleTable";
        internal const string FANCY_TABLE_ID = "fancyTable";
        internal const string METAL_TABLE_ID = "metalTable";
        internal const string UNKNOWN_TABLE_ID = "otherTable";

        public Main() : base(MOD_GUID, MOD_NAME, MOD_AUTHOR, MOD_VERSION, MOD_GAMEVERSION, Assembly.GetExecutingAssembly()) { }

        protected override void OnPostActivate(Mod mod)
        {
            // For log file output so the official plateup support staff can identify if/which a mod is being used
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
            Instance = this;
            PrefManager = new PreferencesManager(MOD_GUID, MOD_NAME);
            SetupMenu();

            Events.BuildGameDataEvent += delegate
            {
                Appliance barTable = GDOUtils.GetExistingGDO(ApplianceReferences.TableBar) as Appliance;
                bool found = false;
                for (int i = 0; i < barTable.Properties.Count; i++)
                {
                    if (barTable.Properties[i].GetType() == typeof(CTablePrioritiseCorrectGroups))
                    {
                        found = true;
                        LogInfo("Successfully removed CTablePrioritiseCorrectGroups from Bar Tables");
                        barTable.Properties.RemoveAt(i);
                    }
                }
                if (!found)
                {
                    LogWarning("Failed to remove CTablePrioritiseCorrectGroups from Bar Tables!");
                }
            };
        }

        private void SetupMenu()
        {
            PrefManager.AddLabel("Better Table Assignment");
            PrefManager.AddInfo("Assign table priority. Customers will prefer tables with a higher priority.");

            PrefManager.AddInfo("Highest priority: 1");
            PrefManager.AddInfo("Lowest priotity : 5");

            int[] ints = { 1, 2, 3, 4, 5 };
            string[] strings = { "1", "2", "3", "4", "5" };

            PrefManager.AddLabel("Bar Table");
            PrefManager.AddOption<int>(BAR_TABLE_ID, "Bar Table", 1, ints, strings);
            PrefManager.AddLabel("Simple Table");
            PrefManager.AddOption<int>(SIMPLE_TABLE_ID, "Simple Table", 1, ints, strings);
            PrefManager.AddLabel("Fancy Table");
            PrefManager.AddOption<int>(FANCY_TABLE_ID, "Fancy Table", 1, ints, strings);
            PrefManager.AddLabel("Metal Table");
            PrefManager.AddOption<int>(METAL_TABLE_ID, "Metal Table", 1, ints, strings);
            PrefManager.AddLabel("Others");
            PrefManager.AddOption<int>(UNKNOWN_TABLE_ID, "Others", 1, ints, strings);

            PrefManager.AddSpacer();
            PrefManager.AddSpacer();

            PrefManager.RegisterMenu(PreferencesManager.MenuType.PauseMenu);
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
