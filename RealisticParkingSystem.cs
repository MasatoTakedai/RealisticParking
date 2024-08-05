using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Pathfind;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace RealisticParking
{
    public partial class RealisticParkingSystem : GameSystemBase
    {
        private SimulationSystem simulationSystem;
        private EntityQuery personalCarQuery;
        [ReadOnly] public ComponentLookup<Game.Net.CarLane> carLaneLookup;

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

            carLaneLookup = SystemAPI.GetComponentLookup<Game.Net.CarLane>(true);
        }


        private int frameCount = 0;
        private int rerouteLimit;
        private bool disable;

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
                Entity entity = carEntities[i];
                if (EntityManager.TryGetComponent(entity, out PathOwner pathOwner))
                {
                    bool hideObsolete = false;
                    if (!disable && EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<CarNavigationLane> nextLanes) 
                        && EntityManager.TryGetBuffer(entity, true, out DynamicBuffer<PathElement> path))
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
                        EntityManager.SetComponentData(entity, pathOwner);
                    }
                }
            }
        }

        private void UpdateSettings(Setting settings)
        {
            this.disable = !settings.Enable;
            this.rerouteLimit = settings.RerouteDistance;
        }
    }
}
