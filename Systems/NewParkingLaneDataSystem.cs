/// <summary>
/// Replaces ParkingLaneDataSystem with a custom UpdateLaneDataJob
/// </summary>

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
using Game.Companies;
using Unity.Collections;

namespace RealisticParking
{
    public partial class NewParkingLaneDataSystem : GameSystemBase
    {
        private ModificationEndBarrier modificationEndBarrier;
        private Game.Objects.SearchSystem m_ObjectSearchSystem;
        private ParkingLaneDataSystem parkingLaneDataSystem;
        private SimulationSystem simulationSystem;
        private CitySystem m_CitySystem;
        private EntityQuery updatedParkingQuery;
        private EntityQuery garageQuery;
        private float garageSpotsPerHousehold;
        private float garageSpotsPerWorker;
        private bool enableDemandSystem;
        private bool enableParkingMinimums;
        private int demandTolerance;
        private float demandSizePerSpot;

        protected override void OnCreate()
        {
            base.OnCreate();
            modificationEndBarrier = World.GetExistingSystemManaged<ModificationEndBarrier>();
            simulationSystem = World.GetExistingSystemManaged<SimulationSystem>();
            m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            parkingLaneDataSystem = base.World.GetOrCreateSystemManaged<ParkingLaneDataSystem>();
            parkingLaneDataSystem.Enabled = false;

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    this.UpdateSettings((Setting)settings, false);
                }
            };
            this.UpdateSettings(Mod.INSTANCE.settings, true);

            updatedParkingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
                Any = new ComponentType[3]
                {
                ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                ComponentType.ReadOnly<GarageLane>(),
                ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
                },
                None = new ComponentType[2]
                {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<PathfindUpdated>() },
                Any = new ComponentType[3]
                {
                ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                ComponentType.ReadOnly<GarageLane>(),
                ComponentType.ReadOnly<Game.Objects.SpawnLocation>()
                },
                None = new ComponentType[3]
                {
                ComponentType.ReadOnly<Updated>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });

            garageQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<GarageLane>() },
                Any = new ComponentType[0] {},
                None = new ComponentType[2]
                {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });

            RequireForUpdate(updatedParkingQuery);
        }

        protected override void OnUpdate()
        {
            JobHandle dependencies;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateLaneDataJob
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(isReadOnly: true),
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true),
                m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(isReadOnly: true),
                m_TransformType = SystemAPI.GetComponentTypeHandle<Transform>(isReadOnly: true),
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_LaneOverlapType = SystemAPI.GetBufferTypeHandle<LaneOverlap>(isReadOnly: true),
                m_ParkingLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ParkingLane>(isReadOnly: false),
                m_ConnectionLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: false),
                m_GarageLaneType = SystemAPI.GetComponentTypeHandle<GarageLane>(isReadOnly: false),
                m_SpawnLocationType = SystemAPI.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: false),
                m_LaneObjectType = SystemAPI.GetBufferTypeHandle<LaneObject>(isReadOnly: false),
                m_OwnerData = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                m_LaneData = SystemAPI.GetComponentLookup<Lane>(isReadOnly: true),
                m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true),
                m_RoadData = SystemAPI.GetComponentLookup<Road>(isReadOnly: true),
                m_ParkedCarData = SystemAPI.GetComponentLookup<ParkedCar>(isReadOnly: true),
                m_ParkedTrainData = SystemAPI.GetComponentLookup<ParkedTrain>(isReadOnly: true),
                m_ControllerData = SystemAPI.GetComponentLookup<Controller>(isReadOnly: true),
                m_BorderDistrictData = SystemAPI.GetComponentLookup<BorderDistrict>(isReadOnly: true),
                m_CurrentDistrictData = SystemAPI.GetComponentLookup<CurrentDistrict>(isReadOnly: true),
                m_DistrictData = SystemAPI.GetComponentLookup<District>(isReadOnly: true),
                m_BuildingData = SystemAPI.GetComponentLookup<Building>(isReadOnly: true),
                m_ParkingFacilityData = SystemAPI.GetComponentLookup<Game.Buildings.ParkingFacility>(isReadOnly: true),
                m_CityData = SystemAPI.GetComponentLookup<City>(isReadOnly: true),
                m_TransformData = SystemAPI.GetComponentLookup<Transform>(isReadOnly: true),
                m_UnspawnedData = SystemAPI.GetComponentLookup<Unspawned>(isReadOnly: true),
                m_AttachmentData = SystemAPI.GetComponentLookup<Attachment>(isReadOnly: true),
                m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_ObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(isReadOnly: true),
                m_ParkingLaneData = SystemAPI.GetComponentLookup<ParkingLaneData>(isReadOnly: true),
                m_PrefabParkingFacilityData = SystemAPI.GetComponentLookup<ParkingFacilityData>(isReadOnly: true),
                m_PrefabBuildingData = SystemAPI.GetComponentLookup<BuildingData>(isReadOnly: true),
                m_PrefabBuildingPropertyData = SystemAPI.GetComponentLookup<BuildingPropertyData>(isReadOnly: true),
                m_PrefabWorkplaceData = SystemAPI.GetComponentLookup<WorkplaceData>(isReadOnly: true),
                m_PrefabGeometryData = SystemAPI.GetComponentLookup<NetGeometryData>(isReadOnly: true),
                m_PrefabSpawnLocationData = SystemAPI.GetComponentLookup<SpawnLocationData>(isReadOnly: true),
                m_PrefabTransportStopData = SystemAPI.GetComponentLookup<TransportStopData>(isReadOnly: true),
                m_DistrictModifiers = SystemAPI.GetBufferLookup<DistrictModifier>(isReadOnly: true),
                m_BuildingModifiers = SystemAPI.GetBufferLookup<BuildingModifier>(isReadOnly: true),
                m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(isReadOnly: true),
                m_SubLanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true),
                m_LaneObjects = SystemAPI.GetBufferLookup<LaneObject>(isReadOnly: true),
                m_CityModifiers = SystemAPI.GetBufferLookup<CityModifier>(isReadOnly: true),
                m_ActivityLocations = SystemAPI.GetBufferLookup<ActivityLocationElement>(isReadOnly: true),
                m_City = m_CitySystem.City,
                m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies),
                garageSpotsPerHousehold = garageSpotsPerHousehold,
                garageSpotsPerWorker = garageSpotsPerWorker,
                renterLookup = SystemAPI.GetBufferLookup<Renter>(isReadOnly: true),
                workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(isReadOnly: true),
                parkingDemandLookup = SystemAPI.GetComponentLookup<ParkingDemand>(isReadOnly: true),
                garageCountLookup = SystemAPI.GetComponentLookup<GarageCount>(isReadOnly: true),
                commandBuffer = modificationEndBarrier.CreateCommandBuffer().AsParallelWriter(),
                enableDemandSystem = this.enableDemandSystem,
                enableParkingMinimums = this.enableParkingMinimums,
                demandTolerance = this.demandTolerance,
                demandSizePerSpot = this.demandSizePerSpot,
            }, updatedParkingQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
            m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
            modificationEndBarrier.AddJobHandleForProducer(jobHandle);

            base.Dependency = jobHandle;
        }

        private void UpdateSettings(Setting settings, bool init)
        {
            if (!init && (this.enableParkingMinimums != settings.EnableParkingMins || this.garageSpotsPerHousehold != settings.GarageSpotsPerResProp || this.garageSpotsPerWorker != settings.GarageSpotsPerWorker))
                UpdateGarageCapacities();
            this.enableParkingMinimums = settings.EnableParkingMins;
            this.garageSpotsPerHousehold = settings.GarageSpotsPerResProp;
            this.garageSpotsPerWorker = settings.GarageSpotsPerWorker;

            this.enableDemandSystem = settings.EnableInducedDemand;
            this.demandTolerance = settings.InducedDemandInitialTolerance;
            this.demandSizePerSpot = settings.InducedDemandQueueSizePerSpot;
        }

        public void UpdateGarageCapacities()
        {
            NativeArray<Entity> garages = garageQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < garages.Length; i++)
            {
                EntityManager.AddComponent<PathfindUpdated>(garages[i]);
            }
        }
    }
}
