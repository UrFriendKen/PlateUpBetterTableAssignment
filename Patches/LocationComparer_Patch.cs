using HarmonyLib;
using Kitchen;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KitchenBetterTableAssignment.Patches
{
    [HarmonyPatch]
    internal static class LocationComparer_Patch
    {
        private enum TableType
        {
            Unknown,
            Bar,
            Simple,
            Fancy,
            Metal
        }

        private static readonly Dictionary<TableType, string> tableTypePriorityPrefKey = new Dictionary<TableType, string>()
        {
            { TableType.Bar, Main.BAR_TABLE_ID },
            { TableType.Simple, Main.SIMPLE_TABLE_ID },
            { TableType.Fancy, Main.FANCY_TABLE_ID },
            { TableType.Metal, Main.METAL_TABLE_ID },
            { TableType.Unknown, Main.UNKNOWN_TABLE_ID }
        };

        private static int CompareTableType(TableType x, TableType y)
        {
            return Main.PrefManager.Get<int>(tableTypePriorityPrefKey[x]).CompareTo(Main.PrefManager.Get<int>(tableTypePriorityPrefKey[y]));
        }

        private static TableType GetTableType(CTableSetModifier modifier)
        {
            int thinking = Mathf.RoundToInt(modifier.PatienceModifiers.Thinking * 100);
            if (modifier.OrderingModifiers.SidesOptional)
            {
                return TableType.Metal;
            }
            else if (Mathf.Round(modifier.OrderingModifiers.PriceModifier * 10) == 5)
            {
                return TableType.Fancy;
            }
            else if (thinking == 5)
            {
                return TableType.Bar;
            }
            else if (modifier.OrderingModifiers.GroupOrdersSame && thinking == 300)
            {
                return TableType.Simple;
            }
            else
            {
                return TableType.Unknown;
            }
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

            EntityManager em = TableController.Manager;
            if (em != null)
            {
                if (em.HasComponent<CTableSetModifier>(x.Entity) && em.HasComponent<CTableSetModifier>(y.Entity))
                {
                    CTableSetModifier xModifier = em.GetComponentData<CTableSetModifier>(x.Entity);
                    CTableSetModifier yModifier = em.GetComponentData<CTableSetModifier>(y.Entity);

                    __result = CompareTableType(GetTableType(xModifier), GetTableType(yModifier));
                }
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
