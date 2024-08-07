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
                Mod.log.Info("creating action job");
                JobHandle dependsOn = JobHandle.CombineDependencies(base.Dependency, m_Dependency);
                SystemAPI.GetBufferLookup<Resources>().Update(ref base.CheckedStateRef);
                TransferMoneyJob jobData = default(TransferMoneyJob);
                jobData.m_Resources = SystemAPI.GetBufferLookup<Resources>();
                jobData.m_MoneyTransferQueue = m_MoneyTransferQueue;
                JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependsOn);
                m_MoneyTransferQueue.Dispose(jobHandle);
                base.Dependency = jobHandle;
                Mod.log.Info("created and scheduled action job");
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

        private EndFrameBarrier m_EndFrameBarrier;

        private SimulationSystem m_SimulationSystem;

        private PathfindSetupSystem m_PathfindSetupSystem;

        private CitySystem m_CitySystem;

        private CityStatisticsSystem m_CityStatisticsSystem;

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

            m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
            m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
            m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
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
            Mod.log.Info("creating jobs");
            uint index = m_SimulationSystem.frameIndex % 16;
            m_VehicleQuery.ResetFilter();
            m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
            m_Actions.m_MoneyTransferQueue = new NativeQueue<MoneyTransfer>(Allocator.TempJob);
            PersonalCarTickJob jobData = default(PersonalCarTickJob);
            jobData.m_EntityType = SystemAPI.GetEntityTypeHandle();
            jobData.m_UnspawnedType = SystemAPI.GetComponentTypeHandle<Unspawned>();
            jobData.m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>();
            jobData.m_LayoutElementType = SystemAPI.GetBufferTypeHandle<LayoutElement>();
            jobData.m_PersonalCarType = SystemAPI.GetComponentTypeHandle<Game.Vehicles.PersonalCar>();
            jobData.m_CarType = SystemAPI.GetComponentTypeHandle<Car>();
            jobData.m_CurrentLaneType = SystemAPI.GetComponentTypeHandle<CarCurrentLane>();
            jobData.m_CarNavigationLaneType = SystemAPI.GetBufferTypeHandle<CarNavigationLane>();
            jobData.m_EntityLookup = SystemAPI.GetEntityStorageInfoLookup();
            jobData.m_ParkedCarData = SystemAPI.GetComponentLookup<ParkedCar>();
            jobData.m_OwnerData = SystemAPI.GetComponentLookup<Owner>();
            jobData.m_SpawnLocationData = SystemAPI.GetComponentLookup<Game.Objects.SpawnLocation>();
            jobData.m_UnspawnedData = SystemAPI.GetComponentLookup<Unspawned>();
            jobData.m_PrefabCarData = SystemAPI.GetComponentLookup<CarData>();
            jobData.m_PrefabRefData = SystemAPI.GetComponentLookup<PrefabRef>();
            jobData.m_PrefabParkingLaneData = SystemAPI.GetComponentLookup<ParkingLaneData>();
            jobData.m_PrefabObjectGeometryData = SystemAPI.GetComponentLookup<ObjectGeometryData>();
            jobData.m_PrefabCreatureData = SystemAPI.GetComponentLookup<CreatureData>();
            jobData.m_PrefabHumanData = SystemAPI.GetComponentLookup<HumanData>();
            jobData.m_PrefabSpawnLocationData = SystemAPI.GetComponentLookup<SpawnLocationData>();
            jobData.m_PropertyRenterData = SystemAPI.GetComponentLookup<PropertyRenter>();
            jobData.m_CarLaneData = SystemAPI.GetComponentLookup<Game.Net.CarLane>();
            jobData.m_ParkingLaneData = SystemAPI.GetComponentLookup<Game.Net.ParkingLane>();
            jobData.m_GarageLaneData = SystemAPI.GetComponentLookup<GarageLane>();
            jobData.m_ConnectionLaneData = SystemAPI.GetComponentLookup<Game.Net.ConnectionLane>();
            jobData.m_CurveData = SystemAPI.GetComponentLookup<Curve>();
            jobData.m_SlaveLaneData = SystemAPI.GetComponentLookup<SlaveLane>();
            jobData.m_ResidentData = SystemAPI.GetComponentLookup<Game.Creatures.Resident>();
            jobData.m_DivertData = SystemAPI.GetComponentLookup<Divert>();
            jobData.m_CurrentVehicleData = SystemAPI.GetComponentLookup<CurrentVehicle>();
            jobData.m_CitizenData = SystemAPI.GetComponentLookup<Citizen>();
            jobData.m_HouseholdMemberData = SystemAPI.GetComponentLookup<HouseholdMember>();
            jobData.m_HouseholdData = SystemAPI.GetComponentLookup<Household>();
            jobData.m_WorkerData = SystemAPI.GetComponentLookup<Worker>();
            jobData.m_TravelPurposeData = SystemAPI.GetComponentLookup<TravelPurpose>();
            jobData.m_MovingAwayData = SystemAPI.GetComponentLookup<MovingAway>();
            jobData.m_Passengers = SystemAPI.GetBufferLookup<Passenger>(isReadOnly: true);
            jobData.m_LaneObjects = SystemAPI.GetBufferLookup<LaneObject>(isReadOnly: true);
            jobData.m_LaneOverlaps = SystemAPI.GetBufferLookup<LaneOverlap>(isReadOnly: true);
            jobData.m_SubLanes = SystemAPI.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
            jobData.m_HouseholdCitizens = SystemAPI.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
            jobData.m_TargetData = SystemAPI.GetComponentLookup<Target>(isReadOnly: true);
            jobData.m_PathOwnerData = SystemAPI.GetComponentLookup<PathOwner>(isReadOnly: true);
            jobData.m_PathElements = SystemAPI.GetBufferLookup<PathElement>(isReadOnly: false);
            jobData.m_RandomSeed = RandomSeed.Next();
            jobData.m_City = m_CitySystem.City;
            jobData.m_TimeOfDay = m_TimeSystem.normalizedTime;
            jobData.m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes;
            jobData.m_MovingToParkedCarAddTypes = m_MovingToParkedCarAddTypes;
            jobData.m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter();
            jobData.m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter();
            jobData.m_MoneyTransferQueue = m_Actions.m_MoneyTransferQueue.AsParallelWriter();
            jobData.m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out var deps).AsParallelWriter();
            jobData.m_FeeQueue = m_ServiceFeeSystem.GetFeeQueue(out var deps2).AsParallelWriter();
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, deps, deps2));
            m_PathfindSetupSystem.AddQueueWriter(jobHandle);
            m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
            m_CityStatisticsSystem.AddWriter(jobHandle);
            m_ServiceFeeSystem.AddQueueWriter(jobHandle);
            m_Actions.m_Dependency = jobHandle;
            base.Dependency = jobHandle;
            Mod.log.Info("created and scheduled jobs");
        }
    }
}
