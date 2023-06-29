using Kitchen;
using KitchenData;
using KitchenMods;

namespace KitchenBetterTableAssignment
{
    public class UpdateTableProperties : FranchiseFirstFrameSystem, IModSystem
    {
        protected override void Initialise()
        {
            base.Initialise();
        }

        protected override void OnUpdate()
        {   
            // Bar Table
            if (!GameData.Main.TryGet(-3721951, out Appliance barTable, warn_if_fail: true))
                return;
            for (int i = 0; i < barTable.Properties.Count; i++)
            {
                if (barTable.Properties[i].GetType() == typeof(CTablePrioritiseCorrectGroups))
                {
                    Main.LogInfo("Removed CTablePrioritiseCorrectGroups from Bar Tables");
                    barTable.Properties.RemoveAt(i);
                }
            }
        }
    }
}
