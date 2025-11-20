/// <summary>
/// Replaces PersonalCarAISystem with a custom PersonalCarTickJob
/// </summary>

using Game;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Simulation;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Game.Prefabs;

namespace RealisticParking
{
    public partial class NewPersonalCarAISystem : GameSystemBase
    {
        public partial class Actions : GameSystemBase
        {
            public JobHandle m_Dependency;

            public NativeQueue<MoneyTransfer> m_MoneyTransferQueue;

            protected override void OnUpdate()
            {
                JobHandle dependsOn = JobHandle.CombineDependencies(base.Dependency, m_Dependency);
                TransferMoneyJob jobData = default(TransferMoneyJob);
                jobData.m_Resources = SystemAPI.GetBufferLookup<Resources>();
                jobData.m_MoneyTransferQueue = m_MoneyTransferQueue;
                JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependsOn);
                m_MoneyTransferQueue.Dispose(jobHandle);
                base.Dependency = jobHandle;
            }
        }

        public struct MoneyTransfer
        {
            public Entity m_Payer;

            public Entity m_Recipient;

            public int m_Amount;
        }

        [BurstCompile]
        private struct TransferMoneyJob : IJob
        {
            public BufferLookup<Resources> m_Resources;

            public NativeQueue<MoneyTransfer> m_MoneyTransferQueue;

            public void Execute()
            {
                MoneyTransfer item;
                while (m_MoneyTransferQueue.TryDequeue(out item))
                {
                    if (m_Resources.HasBuffer(item.m_Payer) && m_Resources.HasBuffer(item.m_Recipient))
                    {
                        DynamicBuffer<Resources> resources = m_Resources[item.m_Payer];
                        DynamicBuffer<Resources> resources2 = m_Resources[item.m_Recipient];
                        EconomyUtils.AddResources(Resource.Money, -item.m_Amount, resources);
                        EconomyUtils.AddResources(Resource.Money, item.m_Amount, resources2);
                    }
                }
            }
        }

        private PersonalCarAISystem personalCarAISystem;
        private PersonalCarAISystem.Actions personalCarAIActions;
        private bool enableDemandSystem;
        private bool enableRerouteLimit;
        private int rerouteLimit;

        private EndFrameBarrier m_EndFrameBarrier;

        private SimulationSystem m_SimulationSystem;

        private PathfindSetupSystem m_PathfindSetupSystem;

        private CitySystem m_CitySystem;

        private TimeSystem m_TimeSystem;

        private ServiceFeeSystem m_ServiceFeeSystem;

        private Actions m_Actions;

        private EntityQuery m_VehicleQuery;

        private ComponentTypeSet m_MovingToParkedCarRemoveTypes;

        private ComponentTypeSet m_MovingToParkedCarAddTypes;

        protected override void OnCreate()
        {
            base.OnCreate();
            personalCarAISystem = base.World.GetOrCreateSystemManaged<PersonalCarAISystem>();
            personalCarAISystem.Enabled = false;
            personalCarAIActions = base.World.GetOrCreateSystemManaged<PersonalCarAISystem.Actions>();
            personalCarAIActions.Enabled = false;

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    this.UpdateSettings((Setting)settings);
                }
            };
            this.UpdateSettings(Mod.INSTANCE.settings);

            m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
            m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
            m_Actions = base.World.GetOrCreateSystemManaged<Actions>();
            m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Vehicles.PersonalCar>(), ComponentType.ReadOnly<CarCurrentLane>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>(), ComponentType.Exclude<Destroyed>());
            m_MovingToParkedCarRemoveTypes = new ComponentTypeSet(new ComponentType[11]
            {
            ComponentType.ReadWrite<Moving>(),
            ComponentType.ReadWrite<TransformFrame>(),
            ComponentType.ReadWrite<InterpolatedTransform>(),
            ComponentType.ReadWrite<Swaying>(),
            ComponentType.ReadWrite<CarNavigation>(),
            ComponentType.ReadWrite<CarNavigationLane>(),
            ComponentType.ReadWrite<CarCurrentLane>(),
            ComponentType.ReadWrite<PathOwner>(),
            ComponentType.ReadWrite<Target>(),
            ComponentType.ReadWrite<Blocker>(),
            ComponentType.ReadWrite<PathElement>()
            });
            m_MovingToParkedCarAddTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>(), ComponentType.ReadWrite<Updated>());
        }

        protected override void OnUpdate()
        {
            uint index = m_SimulationSystem.frameIndex % 16;
            m_VehicleQuery.ResetFilter();
            m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
            m_Actions.m_MoneyTransferQueue = new NativeQueue<MoneyTransfer>(Allocator.TempJob);
            JobHandle deps;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PersonalCarTickJob
            {
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_UnspawnedType = SystemAPI.GetComponentTypeHandle<Unspawned>(isReadOnly: true),
                m_BicycleType = SystemAPI.GetComponentTypeHandle<Bicycle>(isReadOnly: true),
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_LayoutElementType = SystemAPI.GetBufferTypeHandle<LayoutElement>(isReadOnly: true),
                m_PersonalCarType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.PersonalCar>(isReadOnly: false),
                m_CarType = SystemAPI.GetComponentTypeHandle<Car>(isReadOnly: false),
                m_CurrentLaneType = SystemAPI.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: false),
                m_CarNavigationLaneType = SystemAPI.GetBufferTypeHandle<CarNavigationLane>(isReadOnly: false),
                m_EntityLookup = SystemAPI.GetEntityStorageInfoLookup(),
                m_ParkedCarData = SystemAPI.GetComponentLookup<ParkedCar>(isReadOnly: true),
                m_OwnerData = SystemAPI.GetComponentLookup<Owner>(isReadOnly: true),
                m_SpawnLocationData = SystemAPI.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true),
                m_UnspawnedData = SystemAPI.GetComponentLookup<Unspawned>(isReadOnly: true),
                m_PrefabCarData = SystemAPI.GetComponentLookup<CarData>(isReadOnly: true),
                m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true),
                m_PrefabParkingLaneData = SystemAPI.GetComponentLookup<ParkingLaneData>(isReadOnly: true),
                m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>(isReadOnly: true),
                m_PrefabCreatureData = SystemAPI.GetComponentLookup<CreatureData>(isReadOnly: true),
                m_PrefabHumanData = SystemAPI.GetComponentLookup<HumanData>(isReadOnly: true),
                m_PrefabSpawnLocationData = SystemAPI.GetComponentLookup<SpawnLocationData>(isReadOnly: true),
                m_PropertyRenterData = SystemAPI.GetComponentLookup<PropertyRenter>(isReadOnly: true),
                m_PedestrianLaneData = SystemAPI.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true),
                m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true),
                m_ParkingLaneData = SystemAPI.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true),
                m_GarageLaneData = SystemAPI.GetComponentLookup<GarageLane>(isReadOnly: true),
                m_ConnectionLaneData = SystemAPI.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true),
                m_CurveData = SystemAPI.GetComponentLookup<Curve>(isReadOnly: true),
                m_SlaveLaneData = SystemAPI.GetComponentLookup<SlaveLane>(isReadOnly: true),
                m_ResidentData = SystemAPI.GetComponentLookup<Game.Creatures.Resident>(isReadOnly: true),
                m_DivertData = SystemAPI.GetComponentLookup<Divert>(isReadOnly: true),
                m_CurrentVehicleData = SystemAPI.GetComponentLookup<CurrentVehicle>(isReadOnly: true),
                m_CitizenData = SystemAPI.GetComponentLookup<Citizen>(isReadOnly: true),
                m_HouseholdMemberData = SystemAPI.GetComponentLookup<HouseholdMember>(isReadOnly: true),
                m_HouseholdData = SystemAPI.GetComponentLookup<Household>(isReadOnly: true),
                m_WorkerData = SystemAPI.GetComponentLookup<Worker>(isReadOnly: true),
                m_TravelPurposeData = SystemAPI.GetComponentLookup<TravelPurpose>(isReadOnly: true),
                m_MovingAwayData = SystemAPI.GetComponentLookup<MovingAway>(isReadOnly: true),
                m_Passengers = SystemAPI.GetBufferLookup<Passenger>(isReadOnly: true),
                m_LaneObjects = SystemAPI.GetBufferLookup<LaneObject>(isReadOnly: true),
                m_LaneOverlaps = SystemAPI.GetBufferLookup<LaneOverlap>(isReadOnly: true),
                m_SubLanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true),
                m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(isReadOnly: true),
                m_TargetData = SystemAPI.GetComponentLookup<Target>(isReadOnly: true),
                m_PathOwnerData = SystemAPI.GetComponentLookup<PathOwner>(isReadOnly: true),
                m_PathElements = SystemAPI.GetBufferLookup<PathElement>(isReadOnly: false),
                m_RandomSeed = RandomSeed.Next(),
                m_City = m_CitySystem.City,
                m_TimeOfDay = m_TimeSystem.normalizedTime,
                m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
                m_MovingToParkedCarAddTypes = m_MovingToParkedCarAddTypes,
                m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
                m_MoneyTransferQueue = m_Actions.m_MoneyTransferQueue.AsParallelWriter(),
                m_FeeQueue = m_ServiceFeeSystem.GetFeeQueue(out deps).AsParallelWriter(),
                garageCountLookup = SystemAPI.GetComponentLookup<GarageCount>(isReadOnly: true),
                enableRerouteLimit = this.enableRerouteLimit,
                rerouteLimit = this.rerouteLimit,
                enableDemandSystem = this.enableDemandSystem
            }, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, deps));
            m_PathfindSetupSystem.AddQueueWriter(jobHandle);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            m_ServiceFeeSystem.AddQueueWriter(jobHandle);
            m_Actions.m_Dependency = jobHandle;
            base.Dependency = jobHandle;
        }

        private void UpdateSettings(Setting settings)
        {
            this.enableDemandSystem = settings.EnableInducedDemand;
            this.enableRerouteLimit = settings.EnableRerouteDistance;
            this.rerouteLimit = settings.RerouteDistance;
        }

    }
}
