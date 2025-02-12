﻿/// <summary>
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
        private float garageSpotsPerResProp;
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
                    this.UpdateSettings((Setting)settings);
                }
            };
            this.UpdateSettings(Mod.INSTANCE.settings);

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

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (enableParkingMinimums)
                UpdateGarageCapacities();
        }

        protected override void OnUpdate()
        {
            UpdateLaneDataJob updateLaneJob = default(UpdateLaneDataJob);
            updateLaneJob.m_EntityType = SystemAPI.GetEntityTypeHandle();
            updateLaneJob.m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(isReadOnly: true);
            updateLaneJob.m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true);
            updateLaneJob.m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(isReadOnly: true);
            updateLaneJob.m_TransformType =  SystemAPI.GetComponentTypeHandle<Transform>(isReadOnly: true);
            updateLaneJob.m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
            updateLaneJob.m_LaneOverlapType = SystemAPI.GetBufferTypeHandle<LaneOverlap>(isReadOnly: true);
            updateLaneJob.m_ParkingLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ParkingLane>(isReadOnly: false);
            updateLaneJob.m_ConnectionLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: false);
            updateLaneJob.m_GarageLaneType = SystemAPI.GetComponentTypeHandle<GarageLane>(isReadOnly: false);
            updateLaneJob.m_SpawnLocationType = SystemAPI.GetComponentTypeHandle<Game.Objects.SpawnLocation>(isReadOnly: false);
            updateLaneJob.m_LaneObjectType = SystemAPI.GetBufferTypeHandle<LaneObject>(isReadOnly: false);
            updateLaneJob.m_OwnerData = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true);
            updateLaneJob.m_LaneData = SystemAPI.GetComponentLookup<Lane>(isReadOnly: true);
            updateLaneJob.m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
            updateLaneJob.m_RoadData = SystemAPI.GetComponentLookup<Road>(isReadOnly: true);
            updateLaneJob.m_ParkedCarData = SystemAPI.GetComponentLookup<ParkedCar>(isReadOnly: true);
            updateLaneJob.m_ParkedTrainData = SystemAPI.GetComponentLookup<ParkedTrain>(isReadOnly: true);
            updateLaneJob.m_ControllerData = SystemAPI.GetComponentLookup<Controller>(isReadOnly: true);
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
            updateLaneJob.m_PrefabSpawnLocationData = SystemAPI.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
            updateLaneJob.m_DistrictModifiers = SystemAPI.GetBufferLookup<DistrictModifier>(isReadOnly: true);
            updateLaneJob.m_BuildingModifiers = SystemAPI.GetBufferLookup<BuildingModifier>(isReadOnly: true);
            updateLaneJob.m_InstalledUpgrades = SystemAPI.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
            updateLaneJob.m_SubLanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
            updateLaneJob.m_LaneObjects = SystemAPI.GetBufferLookup<LaneObject>(isReadOnly: true);
            updateLaneJob.m_CityModifiers = SystemAPI.GetBufferLookup<CityModifier>(isReadOnly: true);
            updateLaneJob.m_ActivityLocations = SystemAPI.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
            updateLaneJob.m_City = m_CitySystem.City;
            updateLaneJob.m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out var dependencies);
            updateLaneJob.garageSpotsPerResident = garageSpotsPerResProp;
            updateLaneJob.garageSpotsPerWorker = garageSpotsPerWorker;
            updateLaneJob.renterLookup = SystemAPI.GetBufferLookup<Renter>(isReadOnly: true);
            updateLaneJob.workProviderLookup = SystemAPI.GetComponentLookup<WorkProvider>(isReadOnly: true);
            updateLaneJob.parkingDemandLookup = SystemAPI.GetComponentLookup<ParkingDemand>(isReadOnly: true);
            updateLaneJob.carQueuedLookup = SystemAPI.GetComponentLookup<CarQueued>(isReadOnly: true);
            updateLaneJob.garageCountLookup = SystemAPI.GetComponentLookup<GarageCount>(isReadOnly: true);
            updateLaneJob.commandBuffer = modificationEndBarrier.CreateCommandBuffer().AsParallelWriter();
            updateLaneJob.enableDemandSystem = this.enableDemandSystem;
            updateLaneJob.enableParkingMinimums = this.enableParkingMinimums;
            updateLaneJob.demandTolerance = this.demandTolerance;
            updateLaneJob.demandSizePerSpot = this.demandSizePerSpot;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(updateLaneJob, updatedParkingQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
            m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
            modificationEndBarrier.AddJobHandleForProducer(jobHandle);

            base.Dependency = jobHandle;
        }

        private void UpdateSettings(Setting settings)
        {
            this.garageSpotsPerResProp = settings.GarageSpotsPerResProp;
            this.garageSpotsPerWorker = settings.GarageSpotsPerWorker;
            this.enableDemandSystem = settings.EnableInducedDemand;
            this.enableParkingMinimums = settings.EnableParkingMins;
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
