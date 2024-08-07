using Game;
using Game.Common;
using Game.Net;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using Game.Prefabs;
using Game.Areas;
using Game.Vehicles;
using Game.Buildings;
using Game.City;
using Game.Objects;
using Game.Pathfind;
using Unity.Collections;

namespace RealisticParking
{
    public partial class NewParkingLaneDataSystem : GameSystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private Game.Objects.SearchSystem m_ObjectSearchSystem;
        private ParkingLaneDataSystem parkingLaneDataSystem;
        private SimulationSystem simulationSystem;
        private CitySystem m_CitySystem;
        private EntityQuery m_LaneQuery;
        private EntityQuery updatedVehicleQueueQuery;

        private int garageSpotsMultiplier;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystemManaged<ModificationEndBarrier>();
            simulationSystem = World.GetExistingSystemManaged<SimulationSystem>();
            m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            parkingLaneDataSystem = base.World.GetOrCreateSystemManaged<ParkingLaneDataSystem>();
            parkingLaneDataSystem.Enabled = false;

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    this.UpdateSettings((Setting)settings);
                }
            };
            this.UpdateSettings(Mod.INSTANCE.settings);

            m_LaneQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                ComponentType.ReadOnly<GarageLane>()
                },
                None = new ComponentType[2]
                {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<PathfindUpdated>() },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                ComponentType.ReadOnly<GarageLane>()
                },
                None = new ComponentType[3]
                {
                ComponentType.ReadOnly<Updated>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });

            updatedVehicleQueueQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<PathfindUpdated>() },
                Any = new ComponentType[1]
                {
                ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                },
                None = new ComponentType[]
                {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });

            RequireForUpdate(m_LaneQuery);
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> parkingEntities = this.updatedVehicleQueueQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < parkingEntities.Length; i++)
            {
                Entity parkingEntity = parkingEntities[i];
                
            }

            UpdateLaneDataJob updateLaneJob = default(UpdateLaneDataJob);
            updateLaneJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
            updateLaneJob.m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(isReadOnly: true);
            updateLaneJob.m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true);
            updateLaneJob.m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(isReadOnly: true);
            updateLaneJob.m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            updateLaneJob.m_LaneOverlapType = SystemAPI.GetBufferTypeHandle<LaneOverlap>(isReadOnly: true);
            updateLaneJob.m_ParkingLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ParkingLane>(isReadOnly: true);
            updateLaneJob.m_ConnectionLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: true);
            updateLaneJob.m_GarageLaneType = SystemAPI.GetComponentTypeHandle<GarageLane>(isReadOnly: true);
            updateLaneJob.m_LaneObjectType = SystemAPI.GetBufferTypeHandle<LaneObject>(isReadOnly: true);
            updateLaneJob.m_OwnerData = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true);
            updateLaneJob.m_LaneData = SystemAPI.GetComponentLookup<Lane>(isReadOnly: true);
            updateLaneJob.m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
            updateLaneJob.m_RoadData = SystemAPI.GetComponentLookup<Road>(isReadOnly: true);
            updateLaneJob.m_ParkedCarData = SystemAPI.GetComponentLookup<ParkedCar>(isReadOnly: true);
            updateLaneJob.m_BorderDistrictData = SystemAPI.GetComponentLookup<BorderDistrict>(isReadOnly: true);
            updateLaneJob.m_DistrictData = SystemAPI.GetComponentLookup<District>(isReadOnly: true);
            updateLaneJob.m_BuildingData = SystemAPI.GetComponentLookup<Building>(isReadOnly: true);
            updateLaneJob.m_ParkingFacilityData = SystemAPI.GetComponentLookup<Game.Buildings.ParkingFacility>(isReadOnly: true);
            updateLaneJob.m_CityData = SystemAPI.GetComponentLookup<City>(isReadOnly: true);
            updateLaneJob.m_TransformData = SystemAPI.GetComponentLookup<Transform>(isReadOnly: true);
            updateLaneJob.m_UnspawnedData = SystemAPI.GetComponentLookup<Unspawned>(isReadOnly: true);
            updateLaneJob.m_AttachmentData = SystemAPI.GetComponentLookup<Attachment>(isReadOnly: true);
            updateLaneJob.m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);
            updateLaneJob.m_ObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
            updateLaneJob.m_ParkingLaneData = SystemAPI.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
            updateLaneJob.m_PrefabParkingFacilityData = SystemAPI.GetComponentLookup<ParkingFacilityData>(isReadOnly: true);
            updateLaneJob.m_PrefabBuildingData = SystemAPI.GetComponentLookup<BuildingData>(isReadOnly: true);
            updateLaneJob.m_PrefabBuildingPropertyData = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
            updateLaneJob.m_PrefabWorkplaceData = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true);
            updateLaneJob.m_PrefabGeometryData = SystemAPI.GetComponentLookup<NetGeometryData>(isReadOnly: true);
            updateLaneJob.m_DistrictModifiers = SystemAPI.GetBufferLookup<DistrictModifier>(isReadOnly: true);
            updateLaneJob.m_BuildingModifiers = SystemAPI.GetBufferLookup<BuildingModifier>(isReadOnly: true);
            updateLaneJob.m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
            updateLaneJob.m_SubLanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
            updateLaneJob.m_CityModifiers = SystemAPI.GetBufferLookup<CityModifier>(isReadOnly: true);
            updateLaneJob.m_ActivityLocations = SystemAPI.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
            updateLaneJob.m_City = m_CitySystem.City;
            updateLaneJob.m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out var dependencies);
            updateLaneJob.garageSpotsMultiplier = garageSpotsMultiplier;
            updateLaneJob.parkingPathfindLimitLookup = SystemAPI.GetComponentLookup<ParkingPathfindLimit>(isReadOnly: true);
            updateLaneJob.carQueuedLookup = SystemAPI.GetComponentLookup<CarQueued>(isReadOnly: true);
            updateLaneJob.frameDuration = simulationSystem.frameDuration;
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            updateLaneJob.commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(updateLaneJob, m_LaneQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
            m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
            base.Dependency = jobHandle;
        }

        private void UpdateSettings(Setting settings)
        {
            this.garageSpotsMultiplier = settings.GarageSpotsMultiplier;
        }
    }
}
