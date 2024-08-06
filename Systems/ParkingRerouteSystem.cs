using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using RealisticParking.Components;
using Unity.Collections;
using Unity.Entities;

namespace RealisticParking
{
    public partial class ParkingRerouteSystem : GameSystemBase
    {
        private SimulationSystem simulationSystem;
        private EntityQuery personalCarQuery;
        [ReadOnly] public ComponentLookup<Game.Net.CarLane> carLaneLookup;
        [ReadOnly] public ComponentLookup<Game.Net.ParkingLane> parkingLaneLookup;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    this.UpdateSettings((Setting)settings);
                }
            };
            this.UpdateSettings(Mod.INSTANCE.settings);

            this.personalCarQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
            {
                ComponentType.ReadOnly<PathOwner>(),
                ComponentType.ReadOnly<PersonalCar>()
            },
                Any = new ComponentType[]
            {
            },
                None = new ComponentType[]
            {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<Building>(),
                }
            });

            carLaneLookup = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
            parkingLaneLookup = SystemAPI.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
        }


        private int frameCount = 0;
        private int rerouteLimit;
        private bool disableObsoleteHide;

        protected override void OnUpdate()
        {
            if (this.simulationSystem.selectedSpeed <= 0)
            {
                return;
            }

            if (frameCount++ % 4 != 0)
            {
                return;
            }

            NativeArray<Entity> carEntities = this.personalCarQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < carEntities.Length; i++)
            {
                Entity carEntity = carEntities[i];
                if (EntityManager.TryGetComponent(carEntity, out PathOwner pathOwner)
                    && EntityManager.TryGetBuffer(carEntity, true, out DynamicBuffer<PathElement> path))
                {
                    if ((pathOwner.m_State & PathFlags.Updated) != 0)
                    {
                        if (!EntityManager.TryGetComponent(carEntity, out ParkingTarget parkingTarget))
                            EntityManager.AddComponent<ParkingTarget>(carEntity);

                        if ((pathOwner.m_State & PathFlags.Updated) != 0)
                        {
                            ParkingTarget newParkingTarget = GetParkingTarget(path);
                            EntityManager.SetComponentData(carEntity, newParkingTarget);

                            if (newParkingTarget.target != Entity.Null)
                            {
                                EntityManager.AddComponent<VehicleQueued>(newParkingTarget.target);
                                EntityManager.AddComponent<PathfindUpdated>(newParkingTarget.target);
                            }
                        }
                    }

                    bool hideObsolete = false;
                    if (disableObsoleteHide)
                        return;

                    if (EntityManager.TryGetBuffer(carEntity, true, out DynamicBuffer<CarNavigationLane> nextLanes))
                    {
                        if (nextLanes.Length + path.Length - pathOwner.m_ElementIndex >= rerouteLimit)
                        {
                            if (rerouteLimit <= nextLanes.Length)
                            {
                                if (carLaneLookup.HasComponent(nextLanes[rerouteLimit - 1].m_Lane))
                                    hideObsolete = true;
                            }
                            else if (carLaneLookup.HasComponent(path[pathOwner.m_ElementIndex - nextLanes.Length + rerouteLimit - 1].m_Target))
                                hideObsolete = true;
                        }
                    }

                    if (hideObsolete)
                    {
                        pathOwner.m_State &= ~PathFlags.Obsolete;
                        EntityManager.SetComponentData(carEntity, pathOwner);
                    }
                }
            }
        }

        private ParkingTarget GetParkingTarget(DynamicBuffer<PathElement> path)
        {
            return new ParkingTarget(ParkingTargetBinSearch(path, 0, path.Length - 1));
        }
        private Entity ParkingTargetBinSearch(DynamicBuffer<PathElement> path, int low, int high) {
            while (low <= high)
            {
                int mid = low + (high - low) / 2;

                if (carLaneLookup.HasComponent(path[mid].m_Target))
                    low = mid + 1;
                else if (parkingLaneLookup.HasComponent(path[mid].m_Target))
                    return path[mid].m_Target;
                else
                    high = mid - 1;
            }

            return default;
        }

        private void UpdateSettings(Setting settings)
        {
            this.disableObsoleteHide = !settings.Enable;
            this.rerouteLimit = settings.RerouteDistance;
        }
    }
}
