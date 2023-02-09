using Kitchen;
using KitchenData;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenBetterTableAssignment
{
    // Atttempt at changing priority of table types
    [UpdateInGroup(typeof(UpdateCustomerStatesGroup))]
    public class NewGroupHandleQueue : Kitchen.GroupHandleQueue
    {
        private enum GroupState
        {
            Null,
            Queue,
            WaitingTable,
            HostStand,
            FullTable
        }

        private struct WaitingGroup : IComparable<WaitingGroup>
        {
            public Entity Group;

            public int MemberCount;

            public GroupState State;

            public float PatienceRemaining;

            public bool IsUrgent;

            public Entity ForceLocation;

            public WaitingGroup(Entity group, int member_count, GroupState state, float patience_remaining, bool is_urgent, Entity force_location)
            {
                Group = group;
                MemberCount = member_count;
                State = state;
                PatienceRemaining = patience_remaining;
                IsUrgent = is_urgent;
                ForceLocation = force_location;
            }

            public bool WillMoveTo(GroupLocation location)
            {
                return State < location.State && (ForceLocation == default(Entity) || ForceLocation == location.Entity);
            }

            public int CompareTo(WaitingGroup other)
            {
                int state = (int)State;
                int num = -state.CompareTo((int)other.State);
                return (num == 0) ? PatienceRemaining.CompareTo(other.PatienceRemaining) : num;
            }

            public static implicit operator Entity(WaitingGroup wg)
            {
                return wg.Group;
            }
        }

        private struct GroupLocation : IComparable<GroupLocation>
        {
            public Entity Entity;

            public GroupState State;

            public int MaxCapacity;

            public float Attractiveness;

            public bool PrioritiseExactSize;

            public bool CanFit(WaitingGroup group)
            {
                return MaxCapacity < 0 || MaxCapacity >= group.MemberCount;
            }

            public int CompareTo(GroupLocation other)
            {
                int state = (int)State;
                int num = -state.CompareTo((int)other.State);
                return (num == 0) ? (-Attractiveness.CompareTo(other.Attractiveness)) : num;
            }
        }

        private EntityQuery QueueGroupsQuery;

        private EntityQuery FreeTablesQuery;

        private EntityQuery HostStandsQuery;

        private EntityQuery MenusQuery;

        private NativeArray<Entity> QueueGroups;

        private NativeArray<Entity> FreeTables;

        private NativeArray<Entity> HostStands;

        private NativeArray<Entity> Menus;

        private EntityContext Context;

        private List<WaitingGroup> Groups = new List<WaitingGroup>();

        private List<GroupLocation> Locations = new List<GroupLocation>();

        private EntityQuery SQueueMarker;

        // Sorts MaxCapacity of each location in Locations list
        // Locations: List containing GroupLocation(s) which are the locations that can hold groups (Tables, Coffee Tables))
        // By sorting in increasing order, tables with smaller seat numbers are prioritized.
        protected override void Initialise()
        {
            base.Initialise();
            FreeTablesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { typeof(CTableSet) },
                None = new ComponentType[1] { typeof(COccupiedByGroup) }
            });
            HostStandsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                typeof(CApplianceHostStand),
                typeof(CHostStandQueueLocation)
                }
            });
            QueueGroupsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                typeof(CGroupPhaseQueue),
                typeof(CPatience)
                }
            });
            MenusQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { typeof(CMenu) }
            });
        }

        protected override void OnUpdate()
        {
            RunQueries();
            Context = new EntityContext(base.EntityManager);
            BuildWaitingGroups();
            BuildLocations();
            int num = AttemptSeating();
            for (int i = 0; i < num; i++)
            {
                if (!HasStatus(RestaurantStatus.NoQueueReset))
                {
                    Entity entity = base.EntityManager.CreateEntity(typeof(UpdateQueuePatience.CQueuePatienceBoost));
                    base.EntityManager.SetComponentData(entity, new UpdateQueuePatience.CQueuePatienceBoost
                    {
                        Seconds = GameData.Main.Difficulty.QueuePatienceBoost
                    });
                }
            }
            DisposeQueries();

            //base.OnUpdate();
        }

        protected new void RunQueries()
        {
            QueueGroups = QueueGroupsQuery.ToEntityArray(Allocator.Temp);
            HostStands = HostStandsQuery.ToEntityArray(Allocator.Temp);
            FreeTables = FreeTablesQuery.ToEntityArray(Allocator.Temp);
            Menus = MenusQuery.ToEntityArray(Allocator.Temp);
        }

        protected new void DisposeQueries()
        {
            QueueGroups.Dispose();
            HostStands.Dispose();
            FreeTables.Dispose();
            Menus.Dispose();
        }

        protected new void BuildWaitingGroups()
        {
            Groups.Clear();
            foreach (Entity queueGroup in QueueGroups)
            {
                if (!HasComponent<CCustomerGroup>(queueGroup) || !RequireBuffer(queueGroup, out DynamicBuffer<CGroupMember> comp) || !Require<CPatience>(queueGroup, out CPatience comp2))
                {
                    continue;
                }
                GroupState groupState = GroupState.Null;
                float patience_remaining = 0f;
                bool is_urgent = false;
                Entity force_location = default(Entity);
                if (Require<CAssignedMenu>(queueGroup, out CAssignedMenu comp3))
                {
                    if (!Require<CMenu>((Entity)comp3, out CMenu _) || !Require<CHeldBy>((Entity)comp3, out CHeldBy comp5) || !Require<CPartOfTableSet>((Entity)comp5, out CPartOfTableSet comp6))
                    {
                        continue;
                    }
                    force_location = comp6.TableSet;
                }
                CAssignedStand comp9;
                CGroupAtWaitingTable comp10;
                CGroupGoingToTable comp11;
                if (Require<CQueuePosition>(queueGroup, out CQueuePosition comp7))
                {
                    groupState = GroupState.Queue;
                    patience_remaining = comp7.QueuePosition;
                    if (Has<SQueueMarker>())
                    {
                        is_urgent = Require<CPatience>(SQueueMarker.GetSingletonEntity(), out CPatience comp8) && comp8.RemainingTime < 0.1f;
                    }
                }
                else if (Require<CAssignedStand>(queueGroup, out comp9))
                {
                    groupState = GroupState.HostStand;
                    patience_remaining = comp2.RemainingTime;
                    is_urgent = comp2.RemainingTime < 0.1f;
                }
                else if (Require<CGroupAtWaitingTable>(queueGroup, out comp10))
                {
                    groupState = GroupState.WaitingTable;
                    patience_remaining = comp2.RemainingTime;
                    is_urgent = comp2.RemainingTime < 0.1f;
                }
                else if (Require<CGroupGoingToTable>(queueGroup, out comp11))
                {
                    groupState = GroupState.WaitingTable;
                    patience_remaining = comp2.RemainingTime;
                    is_urgent = comp2.RemainingTime < 0.1f;
                }
                if (groupState != 0)
                {
                    Groups.Add(new WaitingGroup(queueGroup, comp.Length, groupState, patience_remaining, is_urgent, force_location));
                }
            }
            Groups.Sort();
        }

        protected new void BuildLocations()
        {
            Locations.Clear();
            foreach (Entity hostStand in HostStands)
            {
                if (!HasComponent<COccupiedByGroup>(hostStand))
                {
                    Locations.Add(new GroupLocation
                    {
                        Entity = hostStand,
                        MaxCapacity = -1,
                        State = GroupState.HostStand
                    });
                }
            }
            foreach (Entity freeTable in FreeTables)
            {
                if (HasComponent<CTableReadyForCustomers>(freeTable) && !HasComponent<COccupiedByGroup>(freeTable))
                {
                    int length = GetBuffer<CTablePlace>(freeTable).Length;
                    bool isWaitingTable = GetComponent<CTableSet>(freeTable).IsWaitingTable;
                    float attractiveness = GetComponent<CTableSetModifier>(freeTable).Attractiveness;
                    Locations.Add(new GroupLocation
                    {
                        Entity = freeTable,
                        MaxCapacity = length,
                        State = (isWaitingTable ? GroupState.WaitingTable : GroupState.FullTable),
                        Attractiveness = attractiveness,
                        PrioritiseExactSize = Has<CTablePrioritiseCorrectGroups>(freeTable)
                    });
                }
            }

            Locations.Sort((g1, g2) =>
            {
                return g1.MaxCapacity.CompareTo(g2.MaxCapacity);
            });
        }

        protected new int AttemptSeating()
        {
            string s = "";
            for (int i = 0; i < Locations.Count; i++)
            {
                s += Locations[i].MaxCapacity + ", ";
            }
            Main.LogInfo(s);

            
            foreach (WaitingGroup group in Groups)
            {
                foreach (GroupLocation location in Locations)
                {
                    if ((location.State <= GroupState.HostStand || group.State >= GroupState.HostStand || HostStands.Length <= 0 || group.IsUrgent) && AttemptMoveGroup(group, location))
                    {
                        return (group.State == GroupState.Queue) ? 1 : 0;
                    }
                }
            }
            return 0;
        }
        private bool AttemptMoveGroup(WaitingGroup group, GroupLocation location)
        {
            if (group.WillMoveTo(location) && location.CanFit(group))
            {
                if (Has<CQueuePosition>(group))
                {
                    Context.Remove<CQueuePosition>(group);
                }
                Context.Add<CGroupStateChanged>(group);
                Context.Add<CUpdateGroupInstruction>(group);
                Context.Set(location.Entity, new COccupiedByGroup
                {
                    Group = group
                });
                if (Has<CGroupAtWaitingTable>(group))
                {
                    Context.Remove<CGroupAtWaitingTable>(group);
                }
                if (Has<CGroupWaitingForTable>(group))
                {
                    Context.Remove<CGroupWaitingForTable>(group);
                }
                if (Require<CAssignedTable>((Entity)group, out CAssignedTable comp))
                {
                    Context.Remove<COccupiedByGroup>(comp);
                    Context.Remove<CAssignedTable>(group);
                }
                if (Require<CAssignedStand>((Entity)group, out CAssignedStand comp2))
                {
                    Context.Remove<COccupiedByGroup>(comp2);
                    Context.Remove<CAssignedStand>(group);
                }
                if (Require<CAssignedMenu>((Entity)group, out CAssignedMenu comp3))
                {
                    Context.Destroy(comp3);
                    Context.Remove<CAssignedMenu>(group);
                }
                switch (location.State)
                {
                    case GroupState.FullTable:
                        Context.Remove<CGroupQueue>(group);
                        Context.Set(group, new CAssignedTable
                        {
                            Table = location.Entity
                        });
                        Context.Add<CGroupGoingToTable>(group);
                        break;
                    case GroupState.WaitingTable:
                        Context.Set(group, new CAssignedTable
                        {
                            Table = location.Entity
                        });
                        Context.Add<CGroupGoingToTable>(group);
                        break;
                    case GroupState.HostStand:
                        Context.Set(group, new CAssignedStand
                        {
                            Stand = location.Entity
                        });
                        Context.Add<CGroupWaitingForTable>(group);
                        Context.Set(group, GetComponent<CCustomerSettings>(group).NewPhase(PatienceReason.Seating));
                        break;
                }
                return true;
            }
            return false;
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            SQueueMarker = GetEntityQuery(ComponentType.ReadOnly<SQueueMarker>());
        }

    }
}
