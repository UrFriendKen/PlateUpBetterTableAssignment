using Kitchen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;

namespace KitchenBetterTableAssignment
{
    internal class TableController : GameSystemBase
    {
        internal static EntityManager Manager; 

        protected override void OnUpdate()
        {
            Manager = EntityManager;
        }
    }
}
