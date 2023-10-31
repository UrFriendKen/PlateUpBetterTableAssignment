using Kitchen;
using KitchenMods;
using Unity.Entities;

namespace KitchenBetterTableAssignment
{
    internal class TableController : GameSystemBase, IModSystem
    {
        private static TableController _instance;

        protected override void Initialise()
        {
            base.Initialise();
            _instance = this;
        }

        protected override void OnUpdate()
        {
        }

        public static bool Static_Require<T>(Entity e, out T comp) where T : struct, IComponentData
        {
            comp = default;
            return _instance?.Require(e, out comp) ?? false;
        }

        public static bool Static_TryGetBuffer<T>(Entity e, out DynamicBuffer<T> buffer) where T : struct, IBufferElementData
        {
            buffer = default;
            return _instance?.RequireBuffer(e, out buffer) ?? false;
        }
    }
}
