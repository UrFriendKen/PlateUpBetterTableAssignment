using Kitchen;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.Entities;

namespace KitchenBetterTableAssignment.Patches
{
    [HarmonyPatch(typeof(GroupHandleQueue), "AttemptSeating")]
    public class GroupHandleQueue_AttemptSeating_Patch
    {
        // Kitchen.GroupHandleQueue.GroupLocation
        private static Type groupLocationType = typeof(GroupHandleQueue).GetNestedType("GroupLocation", BindingFlags.NonPublic);

        // Kitchen.GroupHandleQueue.GroupLocation.MaxCapacity: int
        private static FieldInfo maxCapacityField = groupLocationType.GetField("MaxCapacity", BindingFlags.Instance | BindingFlags.Public);

        // Kitchen.GroupHandleQueue.GroupLocation.MaxCapacity: int
        private static FieldInfo entityField = groupLocationType.GetField("Entity", BindingFlags.Instance | BindingFlags.Public);

        // Kitchen.GroupHandleQueue.Locations: List<GroupLocation>
        private static FieldInfo locationsField = typeof(GroupHandleQueue).GetField("Locations", BindingFlags.Instance | BindingFlags.NonPublic);

        // MethodBase to invoke HandleGroupLocations during runtime.
        private static MethodInfo handleGroupLocations = typeof(GroupHandleQueue_AttemptSeating_Patch)
            .GetMethod(nameof(HandleGroupLocations), BindingFlags.NonPublic | BindingFlags.Static)
            .MakeGenericMethod(groupLocationType);

        // Sorts MaxCapacity of each location in Locations list
        // Locations: List containing GroupLocation(s) which are the locations that can hold groups (Tables, Coffee Tables))
        // By sorting in increasing order, tables with smaller seat numbers are prioritized.
        public static void Prefix(GroupHandleQueue __instance)
        {
            // Get GroupLocation instance
            var locations = locationsField.GetValue(__instance);
            
            // Sort GroupLocation instance. This is done by reference. No need to set the value.
            handleGroupLocations.Invoke(null, new object[] { locations });
        }

        private static void HandleGroupLocations<T>(List<T> groupLocations)
        {
            // Sort group Locations based on MaxCapacity field in ascending order
            groupLocations.Sort((g1, g2) =>
            {
                Entity entity1 = (Entity)entityField.GetValue(g1);
                Entity entity2 = (Entity)entityField.GetValue(g2);

                var maxCapacity1 = (int)maxCapacityField.GetValue(g1);
                var maxCapacity2 = (int)maxCapacityField.GetValue(g2);

                return maxCapacity1.CompareTo(maxCapacity2);
            });
        }
    }
}