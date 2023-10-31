using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace KitchenBetterTableAssignment.Patches
{
    [HarmonyPatch]
    internal static class LocationComparer_Patch
    {
        private static int CompareTableType(int table1ID, int table2ID)
        {
            try
            {
                return Main.PrefManager.Get<int>(table1ID.ToString()).CompareTo(Main.PrefManager.Get<int>(table2ID.ToString()));
            }
            catch { return 0; }
        }


        [HarmonyPatch(typeof(LocationComparer), nameof(LocationComparer.Compare))]
        [HarmonyPrefix]
        public static bool Compare_Prefix(ref int __result, CAvailableAssignment x, CAvailableAssignment y)
        {
            int state = (int)x.State;
            __result = -state.CompareTo((int)y.State);

            if (__result != 0)
            {
                return false;
            }

            if (TableController.Static_TryGetBuffer(x.Entity, out DynamicBuffer<CTableSetParts> buffer1) &&
                buffer1.Length > 0 &&
                TableController.Static_Require(buffer1[0].Entity, out CAppliance table1) &&
                TableController.Static_TryGetBuffer(y.Entity, out DynamicBuffer<CTableSetParts> buffer2) &&
                buffer2.Length > 0 &&
                TableController.Static_Require(buffer2[0].Entity, out CAppliance table2))
            {
                __result = CompareTableType(table1.ID, table2.ID);
            }

            if (__result != 0)
            {
                return false;
            }

            __result = x.MaxCapacity.CompareTo(y.MaxCapacity);

            if (__result != 0)
            {
                return false;
            }

            return true;
        }
    }
}
