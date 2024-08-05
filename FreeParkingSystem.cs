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

namespace RealisticParking
{
    public partial class FreeParkingSystem : GameSystemBase
    {
        private SimulationSystem simulationSystem;
        private EntityQuery parkLaneQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            this.simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    //this.UpdateSettings((Setting)settings);
                }
            };

            //this.UpdateSettings(Mod.INSTANCE.settings);


            this.parkLaneQuery = GetEntityQuery(new EntityQueryDesc
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
        }

        protected override void OnUpdate()
        {
            if (this.simulationSystem.selectedSpeed <= 0)
            {
                return;
            }

            NativeArray<Entity> parkingEntities = this.parkLaneQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < parkingEntities.Length; i++)
            {
                var entity = parkingEntities[i];
                if (EntityManager.TryGetComponent(entity, out ParkingLane parking))
                {
                    parking.m_FreeSpace = 0;
                    EntityManager.SetComponentData(entity, parking);
                }
            }
        }
        }
}
