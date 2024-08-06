using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace RealisticParking.Systems
{
    public partial class FreeParkingSystem : GameSystemBase
    {
        private SimulationSystem simulationSystem;
        private EntityQuery parkLaneQuery;
        private EntityQuery garageLaneQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    //this.UpdateSettings((Setting)settings);
                }
            };

            //this.UpdateSettings(Mod.INSTANCE.settings);


            parkLaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
            {
                ComponentType.ReadOnly<ParkingLane>(),
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

            garageLaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
            {
                ComponentType.ReadOnly<GarageLane>(),
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
        }

        protected override void OnUpdate()
        {
            if (simulationSystem.selectedSpeed <= 0)
            {
                return;
            }

            NativeArray<Entity> parkingEntities = parkLaneQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < parkingEntities.Length; i++)
            {
                var entity = parkingEntities[i];
                if (EntityManager.TryGetComponent(entity, out ParkingLane parking))
                {
                    parking.m_FreeSpace = 0f;
                    EntityManager.SetComponentData(entity, parking);
                    EntityManager.AddComponent<Updated>(entity);
                }
            }

            /*NativeArray<Entity> garageEntities = this.garageLaneQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < garageEntities.Length; i++)
            {
                var entity = garageEntities[i];
                if (EntityManager.TryGetComponent(entity, out GarageLane garage))
                {
                    garage.m_VehicleCapacity = 100;
                    EntityManager.SetComponentData(entity, garage);
                }
            }*/
        }
    }
}
