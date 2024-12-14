/// <summary>
/// Copied from original PersonalCarTickJob.  Processes CarQueued assignment and modifies personal car navigation code.
/// </summary>

using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Vehicles;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticParking
{
    [BurstCompile]
    public struct PersonalCarTickJob : IJobChunk
    {
        // custom code start
        [ReadOnly] public ComponentLookup<GarageCount> garageCountLookup;   
        [ReadOnly] public bool enableDemandSystem;
        [ReadOnly] public bool enableRerouteLimit;
        [ReadOnly] public int rerouteLimit;

        // add custom ParkingTarget and CarQueued components with a new destination
        private void SetCustomParkingComponents(Entity entity, int jobIndex, PathElement pathElement)
        {
            if (!enableDemandSystem)
                return;
            
            /*if (!parkingTargetLookup.HasComponent(entity))
                m_CommandBuffer.AddComponent<ParkingTarget>(jobIndex, entity);

            m_CommandBuffer.SetComponent(jobIndex, entity, new ParkingTarget(pathElement.m_Target));*/
            m_CommandBuffer.AddComponent<CarQueued>(jobIndex, pathElement.m_Target);
        }

        // return vanilla limit of 4000 if disabled
        private int GetRerouteLimit() 
        {
            if (enableRerouteLimit)
                return rerouteLimit;
            else
                return 4000;
        }

        private int GetActualGarageCount(Entity entity, GarageLane garageLane)
        {
            if (garageCountLookup.TryGetComponent(entity, out GarageCount customCount))
                return customCount.actualCount;
            else
                return garageLane.m_VehicleCount;
        }
        // custom code end



        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Unspawned> m_UnspawnedType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public BufferTypeHandle<LayoutElement> m_LayoutElementType;

        public ComponentTypeHandle<Game.Vehicles.PersonalCar> m_PersonalCarType;

        public ComponentTypeHandle<Car> m_CarType;

        public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

        public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

        [ReadOnly]
        public EntityStorageInfoLookup m_EntityLookup;

        [ReadOnly]
        public ComponentLookup<ParkedCar> m_ParkedCarData;

        [ReadOnly]
        public ComponentLookup<Owner> m_OwnerData;

        [ReadOnly]
        public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

        [ReadOnly]
        public ComponentLookup<Unspawned> m_UnspawnedData;

        [ReadOnly]
        public ComponentLookup<CarData> m_PrefabCarData;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<CreatureData> m_PrefabCreatureData;

        [ReadOnly]
        public ComponentLookup<HumanData> m_PrefabHumanData;

        [ReadOnly]
        public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

        [ReadOnly]
        public ComponentLookup<PropertyRenter> m_PropertyRenterData;

        [ReadOnly]
        public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

        [ReadOnly]
        public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

        [ReadOnly]
        public ComponentLookup<GarageLane> m_GarageLaneData;

        [ReadOnly]
        public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

        [ReadOnly]
        public ComponentLookup<Curve> m_CurveData;

        [ReadOnly]
        public ComponentLookup<SlaveLane> m_SlaveLaneData;

        [ReadOnly]
        public ComponentLookup<Game.Creatures.Resident> m_ResidentData;

        [ReadOnly]
        public ComponentLookup<Divert> m_DivertData;

        [ReadOnly]
        public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

        [ReadOnly]
        public ComponentLookup<Citizen> m_CitizenData;

        [ReadOnly]
        public ComponentLookup<HouseholdMember> m_HouseholdMemberData;

        [ReadOnly]
        public ComponentLookup<Household> m_HouseholdData;

        [ReadOnly]
        public ComponentLookup<Worker> m_WorkerData;

        [ReadOnly]
        public ComponentLookup<TravelPurpose> m_TravelPurposeData;

        [ReadOnly]
        public ComponentLookup<MovingAway> m_MovingAwayData;

        [ReadOnly]
        public BufferLookup<Passenger> m_Passengers;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> m_SubLanes;

        [ReadOnly]
        public BufferLookup<LaneObject> m_LaneObjects;

        [ReadOnly]
        public BufferLookup<LaneOverlap> m_LaneOverlaps;

        [ReadOnly]
        public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<Target> m_TargetData;

        [NativeDisableParallelForRestriction]
        public ComponentLookup<PathOwner> m_PathOwnerData;

        [NativeDisableParallelForRestriction]
        public BufferLookup<PathElement> m_PathElements;

        [ReadOnly]
        public RandomSeed m_RandomSeed;

        [ReadOnly]
        public Entity m_City;

        [ReadOnly]
        public float m_TimeOfDay;

        [ReadOnly]
        public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

        [ReadOnly]
        public ComponentTypeSet m_MovingToParkedCarAddTypes;

        public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

        public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

        public NativeQueue<NewPersonalCarAISystem.MoneyTransfer>.ParallelWriter m_MoneyTransferQueue;

        public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

        public NativeQueue<ServiceFeeSystem.FeeEvent>.ParallelWriter m_FeeQueue;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
            NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
            NativeArray<CarCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
            NativeArray<Game.Vehicles.PersonalCar> nativeArray4 = chunk.GetNativeArray(ref m_PersonalCarType);
            NativeArray<Car> nativeArray5 = chunk.GetNativeArray(ref m_CarType);
            BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
            BufferAccessor<CarNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
            bool isUnspawned = chunk.Has(ref m_UnspawnedType);
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity entity = nativeArray[i];
                PrefabRef prefabRef = nativeArray2[i];
                Game.Vehicles.PersonalCar personalCar = nativeArray4[i];
                Car car = nativeArray5[i];
                CarCurrentLane currentLane = nativeArray3[i];
                DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor2[i];
                Target target = m_TargetData[entity];
                PathOwner pathOwner = m_PathOwnerData[entity];
                DynamicBuffer<LayoutElement> layout = default(DynamicBuffer<LayoutElement>);
                if (bufferAccessor.Length != 0)
                {
                    layout = bufferAccessor[i];
                }
                VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
                Tick(unfilteredChunkIndex, entity, prefabRef, layout, navigationLanes, ref personalCar, ref car, ref currentLane, ref pathOwner, ref target);
                m_TargetData[entity] = target;
                m_PathOwnerData[entity] = pathOwner;
                nativeArray4[i] = personalCar;
                nativeArray5[i] = car;
                nativeArray3[i] = currentLane;
            }
        }

        private void Tick(int jobIndex, Entity entity, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, DynamicBuffer<CarNavigationLane> navigationLanes, ref Game.Vehicles.PersonalCar personalCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
        {
            Random random = m_RandomSeed.GetRandom(entity.Index);
            if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
            {
                ResetPath(entity, jobIndex, ref random, ref personalCar, ref car, ref currentLane, ref pathOwner);
            }
            if (((personalCar.m_State & (PersonalCarFlags.Transporting | PersonalCarFlags.Boarding | PersonalCarFlags.Disembarking)) == 0 && !m_EntityLookup.Exists(target.m_Target)) || VehicleUtils.PathfindFailed(pathOwner))
            {
                RemovePath(entity, ref pathOwner);
                if ((personalCar.m_State & PersonalCarFlags.Disembarking) != 0)
                {
                    if (StopDisembarking(entity, layout, ref personalCar, ref pathOwner))
                    {
                        ParkCar(jobIndex, entity, layout, resetLocation: true, ref personalCar, ref currentLane);
                    }
                    return;
                }
                if ((personalCar.m_State & PersonalCarFlags.Transporting) != 0)
                {
                    if (!StartDisembarking(jobIndex, entity, layout, ref personalCar, ref currentLane))
                    {
                        ParkCar(jobIndex, entity, layout, resetLocation: true, ref personalCar, ref currentLane);
                    }
                    return;
                }
                if ((personalCar.m_State & PersonalCarFlags.Boarding) == 0)
                {
                    ParkCar(jobIndex, entity, layout, resetLocation: false, ref personalCar, ref currentLane);
                    return;
                }
                if (!StopBoarding(entity, layout, navigationLanes, ref personalCar, ref currentLane, ref pathOwner, ref target))
                {
                    return;
                }
                if ((personalCar.m_State & PersonalCarFlags.Transporting) == 0)
                {
                    ParkCar(jobIndex, entity, layout, resetLocation: false, ref personalCar, ref currentLane);
                    return;
                }
            }
            else
            {
                if (VehicleUtils.PathEndReached(currentLane))
                {
                    VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity, layout);
                    return;
                }
                if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner) || (personalCar.m_State & (PersonalCarFlags.Boarding | PersonalCarFlags.Disembarking)) != 0)
                {
                    if ((personalCar.m_State & PersonalCarFlags.Disembarking) != 0)
                    {
                        if (StopDisembarking(entity, layout, ref personalCar, ref pathOwner))
                        {
                            ParkCar(jobIndex, entity, layout, resetLocation: false, ref personalCar, ref currentLane);
                        }
                        return;
                    }
                    if ((personalCar.m_State & PersonalCarFlags.Transporting) != 0)
                    {
                        if (!StartDisembarking(jobIndex, entity, layout, ref personalCar, ref currentLane))
                        {
                            ParkCar(jobIndex, entity, layout, resetLocation: false, ref personalCar, ref currentLane);
                        }
                        return;
                    }
                    if ((personalCar.m_State & PersonalCarFlags.Boarding) == 0)
                    {
                        if (!StartBoarding(entity, ref personalCar, ref car, ref target))
                        {
                            VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity, layout);
                        }
                        return;
                    }
                    if (!StopBoarding(entity, layout, navigationLanes, ref personalCar, ref currentLane, ref pathOwner, ref target))
                    {
                        return;
                    }
                    if ((personalCar.m_State & PersonalCarFlags.Transporting) == 0)
                    {
                        ParkCar(jobIndex, entity, layout, resetLocation: false, ref personalCar, ref currentLane);
                        return;
                    }
                }
                else if (VehicleUtils.WaypointReached(currentLane))
                {
                    currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
                    pathOwner.m_State &= ~PathFlags.Failed;
                    pathOwner.m_State |= PathFlags.Obsolete;
                }
            }
            if ((personalCar.m_State & PersonalCarFlags.Disembarking) == 0)
            {
                if (VehicleUtils.RequireNewPath(pathOwner))
                {
                    FindNewPath(entity, prefabRef, layout, ref personalCar, ref currentLane, ref pathOwner, ref target);
                }
                else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
                {
                    CheckParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes);
                }
            }
        }

        private void CheckParkingSpace(Entity entity, ref Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
        {
            DynamicBuffer<PathElement> path = m_PathElements[entity];
            ComponentLookup<Blocker> blockerData = default(ComponentLookup<Blocker>);
            if (VehicleUtils.ValidateParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes, path, ref m_ParkedCarData, ref blockerData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_GarageLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: true, boardingOnly: false) != Entity.Null)
            {
                return;
            }
            int num = math.min(GetRerouteLimit(), path.Length - pathOwner.m_ElementIndex);
            if (num <= 0 || navigationLanes.Length <= 0)
            {
                return;
            }
            int num2 = random.NextInt(num) * (random.NextInt(num) + 1) / num;
            PathElement pathElement = path[pathOwner.m_ElementIndex + num2];
            if (m_ParkingLaneData.HasComponent(pathElement.m_Target))
            {
                float minT;
                if (num2 == 0)
                {
                    CarNavigationLane carNavigationLane = navigationLanes[navigationLanes.Length - 1];
                    minT = (((carNavigationLane.m_Flags & Game.Vehicles.CarLaneFlags.Reserved) == 0) ? carNavigationLane.m_CurvePosition.x : carNavigationLane.m_CurvePosition.y);
                }
                else
                {
                    minT = path[pathOwner.m_ElementIndex + num2 - 1].m_TargetDelta.x;
                }
                float offset;
                float y = VehicleUtils.GetParkingSize(entity, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, out offset).y;
                float curvePos = pathElement.m_TargetDelta.x;
                if (VehicleUtils.FindFreeParkingSpace(ref random, pathElement.m_Target, minT, y, offset, ref curvePos, ref m_ParkedCarData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: true))
                {
                    return;
                }
            }
            else
            {
                if (!m_GarageLaneData.HasComponent(pathElement.m_Target))
                {
                    return;
                }
                GarageLane garageLane = m_GarageLaneData[pathElement.m_Target];
                Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[pathElement.m_Target];
                if (GetActualGarageCount(pathElement.m_Target, garageLane) < garageLane.m_VehicleCapacity && (connectionLane.m_Flags & ConnectionLaneFlags.Disabled) == 0)
                {
                    return;
                }
            }
            for (int i = 0; i < num2; i++)
            {
                if (IsParkingLane(path[pathOwner.m_ElementIndex + i].m_Target))
                {
                    return;
                }
            }
            pathOwner.m_State |= PathFlags.Obsolete;
        }

        private void ResetPath(Entity entity, int jobIndex, ref Random random, ref Game.Vehicles.PersonalCar personalCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
        {
            if ((personalCar.m_State & PersonalCarFlags.Transporting) != 0)
            {
                car.m_Flags |= CarFlags.StayOnRoad;
            }
            else
            {
                car.m_Flags &= ~CarFlags.StayOnRoad;
            }
            DynamicBuffer<PathElement> path = m_PathElements[entity];
            PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
            VehicleUtils.ResetParkingLaneStatus(entity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
            int i = VehicleUtils.SetParkingCurvePos(entity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
            if (i != path.Length)
            {
                SetCustomParkingComponents(entity, jobIndex, path[i]);
            }
        }

        private void RemovePath(Entity entity, ref PathOwner pathOwner)
        {
            m_PathElements[entity].Clear();
            pathOwner.m_ElementIndex = 0;
        }

        private bool IsParkingLane(Entity lane)
        {
            if (m_ParkingLaneData.HasComponent(lane))
            {
                return true;
            }
            if (m_ConnectionLaneData.TryGetComponent(lane, out var componentData))
            {
                return (componentData.m_Flags & ConnectionLaneFlags.Parking) != 0;
            }
            return false;
        }

        private bool IsCarLane(Entity lane)
        {
            if (m_CarLaneData.HasComponent(lane))
            {
                return true;
            }
            if (m_ConnectionLaneData.TryGetComponent(lane, out var componentData))
            {
                return (componentData.m_Flags & ConnectionLaneFlags.Road) != 0;
            }
            if (m_SpawnLocationData.HasComponent(lane))
            {
                PrefabRef prefabRef = m_PrefabRefData[lane];
                if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
                {
                    if (componentData2.m_ConnectionType != RouteConnectionType.Road)
                    {
                        return componentData2.m_ConnectionType == RouteConnectionType.Parking;
                    }
                    return true;
                }
            }
            return false;
        }

        private bool StartBoarding(Entity vehicleEntity, ref Game.Vehicles.PersonalCar personalCar, ref Car car, ref Target target)
        {
            if ((personalCar.m_State & PersonalCarFlags.DummyTraffic) == 0)
            {
                personalCar.m_State |= PersonalCarFlags.Boarding;
                return true;
            }
            return false;
        }

        private bool HasPassengers(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout)
        {
            if (layout.IsCreated && layout.Length != 0)
            {
                for (int i = 0; i < layout.Length; i++)
                {
                    if (m_Passengers[layout[i].m_Vehicle].Length != 0)
                    {
                        return true;
                    }
                }
            }
            else if (m_Passengers[vehicleEntity].Length != 0)
            {
                return true;
            }
            return false;
        }

        private Entity FindLeader(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout)
        {
            if (layout.IsCreated && layout.Length != 0)
            {
                for (int i = 0; i < layout.Length; i++)
                {
                    Entity entity = FindLeader(m_Passengers[layout[i].m_Vehicle]);
                    if (entity != Entity.Null)
                    {
                        return entity;
                    }
                }
                return Entity.Null;
            }
            return FindLeader(m_Passengers[vehicleEntity]);
        }

        private Entity FindLeader(DynamicBuffer<Passenger> passengers)
        {
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].m_Passenger;
                if (m_CurrentVehicleData.HasComponent(passenger) && (m_CurrentVehicleData[passenger].m_Flags & CreatureVehicleFlags.Leader) != 0)
                {
                    return passenger;
                }
            }
            return Entity.Null;
        }

        private bool PassengersReady(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout, out Entity leader)
        {
            leader = Entity.Null;
            if (layout.IsCreated && layout.Length != 0)
            {
                for (int i = 0; i < layout.Length; i++)
                {
                    if (!PassengersReady(m_Passengers[layout[i].m_Vehicle], ref leader))
                    {
                        return false;
                    }
                }
                return true;
            }
            return PassengersReady(m_Passengers[vehicleEntity], ref leader);
        }

        private bool PassengersReady(DynamicBuffer<Passenger> passengers, ref Entity leader)
        {
            for (int i = 0; i < passengers.Length; i++)
            {
                Entity passenger = passengers[i].m_Passenger;
                if (m_CurrentVehicleData.HasComponent(passenger))
                {
                    CurrentVehicle currentVehicle = m_CurrentVehicleData[passenger];
                    if ((currentVehicle.m_Flags & CreatureVehicleFlags.Ready) == 0)
                    {
                        return false;
                    }
                    if ((currentVehicle.m_Flags & CreatureVehicleFlags.Leader) != 0)
                    {
                        leader = passenger;
                    }
                }
            }
            return true;
        }

        private bool StopBoarding(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout, DynamicBuffer<CarNavigationLane> navigationLanes, ref Game.Vehicles.PersonalCar personalCar, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
        {
            if (!PassengersReady(vehicleEntity, layout, out var leader))
            {
                return false;
            }
            if (leader == Entity.Null)
            {
                personalCar.m_State &= ~PersonalCarFlags.Boarding;
                return true;
            }
            DynamicBuffer<PathElement> targetElements = m_PathElements[vehicleEntity];
            DynamicBuffer<PathElement> sourceElements = m_PathElements[leader];
            PathOwner sourceOwner = m_PathOwnerData[leader];
            Target target2 = m_TargetData[leader];
            PathUtils.CopyPath(sourceElements, sourceOwner, 0, targetElements);
            pathOwner.m_ElementIndex = 0;
            pathOwner.m_State |= PathFlags.Updated;
            personalCar.m_State &= ~PersonalCarFlags.Boarding;
            personalCar.m_State |= PersonalCarFlags.Transporting;
            bool flag = false;
            target.m_Target = target2.m_Target;
            if (m_HouseholdMemberData.HasComponent(leader))
            {
                Entity household = m_HouseholdMemberData[leader].m_Household;
                if (m_PropertyRenterData.HasComponent(household))
                {
                    Entity property = m_PropertyRenterData[household].m_Property;
                    flag |= property == target.m_Target;
                }
            }
            if (m_DivertData.HasComponent(leader))
            {
                flag &= m_DivertData[leader].m_Purpose == Purpose.None;
            }
            if (flag)
            {
                personalCar.m_State |= PersonalCarFlags.HomeTarget;
            }
            else
            {
                personalCar.m_State &= ~PersonalCarFlags.HomeTarget;
            }
            VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
            return true;
        }

        private bool StartDisembarking(int jobIndex, Entity vehicleEntity, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.PersonalCar personalCar, ref CarCurrentLane currentLane)
        {
            if (!HasPassengers(vehicleEntity, layout))
            {
                return false;
            }
            personalCar.m_State &= ~PersonalCarFlags.Transporting;
            personalCar.m_State |= PersonalCarFlags.Disembarking;
            if (m_ParkingLaneData.HasComponent(currentLane.m_Lane))
            {
                Game.Net.ParkingLane parkingLane = m_ParkingLaneData[currentLane.m_Lane];
                if (parkingLane.m_ParkingFee > 0)
                {
                    Entity entity = FindLeader(vehicleEntity, layout);
                    if (m_ResidentData.HasComponent(entity))
                    {
                        Game.Creatures.Resident resident = m_ResidentData[entity];
                        if (m_HouseholdMemberData.HasComponent(resident.m_Citizen))
                        {
                            HouseholdMember householdMember = m_HouseholdMemberData[resident.m_Citizen];
                            m_MoneyTransferQueue.Enqueue(new NewPersonalCarAISystem.MoneyTransfer
                            {
                                m_Payer = householdMember.m_Household,
                                m_Recipient = m_City,
                                m_Amount = parkingLane.m_ParkingFee
                            });
                            m_StatisticsEventQueue.Enqueue(new StatisticsEvent
                            {
                                m_Statistic = StatisticType.Income,
                                m_Change = (int)parkingLane.m_ParkingFee,
                                m_Parameter = 9
                            });
                            m_FeeQueue.Enqueue(new ServiceFeeSystem.FeeEvent
                            {
                                m_Amount = 1f,
                                m_Cost = (int)parkingLane.m_ParkingFee,
                                m_Resource = PlayerResource.Parking,
                                m_Outside = false
                            });
                        }
                    }
                }
            }
            return true;
        }

        private bool StopDisembarking(Entity vehicleEntity, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.PersonalCar personalCar, ref PathOwner pathOwner)
        {
            if (HasPassengers(vehicleEntity, layout))
            {
                return false;
            }
            m_PathElements[vehicleEntity].Clear();
            pathOwner.m_ElementIndex = 0;
            personalCar.m_State &= ~PersonalCarFlags.Disembarking;
            return true;
        }

        private void ParkCar(int jobIndex, Entity entity, DynamicBuffer<LayoutElement> layout, bool resetLocation, ref Game.Vehicles.PersonalCar personalCar, ref CarCurrentLane currentLane)
        {
            personalCar.m_State &= ~(PersonalCarFlags.Transporting | PersonalCarFlags.Boarding | PersonalCarFlags.Disembarking);
            if (layout.IsCreated)
            {
                for (int i = 0; i < layout.Length; i++)
                {
                    Entity vehicle = layout[i].m_Vehicle;
                    if (!(vehicle == entity))
                    {
                        m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
                    }
                }
                m_CommandBuffer.RemoveComponent<LayoutElement>(jobIndex, entity);
            }
            m_CommandBuffer.RemoveComponent(jobIndex, entity, in m_MovingToParkedCarRemoveTypes);
            m_CommandBuffer.AddComponent(jobIndex, entity, in m_MovingToParkedCarAddTypes);
            m_CommandBuffer.SetComponent(jobIndex, entity, new ParkedCar(currentLane.m_Lane, currentLane.m_CurvePosition.x));
            if (resetLocation)
            {
                Entity resetLocation2 = Entity.Null;
                if (m_HouseholdMemberData.TryGetComponent(personalCar.m_Keeper, out var componentData) && m_PropertyRenterData.TryGetComponent(componentData.m_Household, out var componentData2) && (m_HouseholdData[componentData.m_Household].m_Flags & HouseholdFlags.MovedIn) != 0 && !m_MovingAwayData.HasComponent(componentData.m_Household))
                {
                    resetLocation2 = componentData2.m_Property;
                }
                m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, resetLocation2));
            }
            else if (m_ParkingLaneData.HasComponent(currentLane.m_Lane) && currentLane.m_ChangeLane == Entity.Null)
            {
                m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
            }
            else if (m_GarageLaneData.HasComponent(currentLane.m_Lane))
            {
                m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
                m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
            }
            else
            {
                m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
            }
        }

        private void FindNewPath(Entity entity, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, ref Game.Vehicles.PersonalCar personalCar, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
        {
            CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
            pathOwner.m_State &= ~(PathFlags.AddDestination | PathFlags.Divert);
            bool flag = false;
            PathfindParameters parameters;
            SetupQueueTarget origin;
            SetupQueueTarget destination;
            if ((personalCar.m_State & PersonalCarFlags.Transporting) != 0)
            {
                PathfindParameters pathfindParameters = default(PathfindParameters);
                pathfindParameters.m_MaxSpeed = new float2(carData.m_MaxSpeed, 277.77777f);
                pathfindParameters.m_WalkSpeed = 5.555556f;
                pathfindParameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
                pathfindParameters.m_Methods = PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking;
                pathfindParameters.m_ParkingTarget = VehicleUtils.GetParkingSource(entity, currentLane, ref m_ParkingLaneData, ref m_ConnectionLaneData);
                pathfindParameters.m_ParkingDelta = currentLane.m_CurvePosition.z;
                pathfindParameters.m_ParkingSize = VehicleUtils.GetParkingSize(entity, ref m_PrefabRefData, ref m_PrefabObjectGeometryData);
                pathfindParameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
                pathfindParameters.m_SecondaryIgnoredRules = VehicleUtils.GetIgnoredPathfindRulesTaxiDefaults();
                parameters = pathfindParameters;
                SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.Parking;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                origin = setupQueueTarget;
                setupQueueTarget = default(SetupQueueTarget);
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Pedestrian;
                setupQueueTarget.m_Entity = target.m_Target;
                setupQueueTarget.m_RandomCost = 30f;
                destination = setupQueueTarget;
                Entity entity2 = FindLeader(entity, layout);
                if (m_ResidentData.HasComponent(entity2))
                {
                    PrefabRef prefabRef2 = m_PrefabRefData[entity2];
                    Game.Creatures.Resident resident = m_ResidentData[entity2];
                    CreatureData creatureData = m_PrefabCreatureData[prefabRef2.m_Prefab];
                    HumanData humanData = m_PrefabHumanData[prefabRef2.m_Prefab];
                    parameters.m_MaxCost = CitizenBehaviorSystem.kMaxPathfindCost;
                    parameters.m_WalkSpeed = humanData.m_WalkSpeed;
                    parameters.m_Methods |= RouteUtils.GetTaxiMethods(resident) | RouteUtils.GetPublicTransportMethods(resident, m_TimeOfDay);
                    destination.m_ActivityMask = creatureData.m_SupportedActivities;
                    if (m_HouseholdMemberData.HasComponent(resident.m_Citizen))
                    {
                        Entity household = m_HouseholdMemberData[resident.m_Citizen].m_Household;
                        if (m_PropertyRenterData.HasComponent(household))
                        {
                            flag |= (parameters.m_Authorization1 = m_PropertyRenterData[household].m_Property) == target.m_Target;
                        }
                    }
                    if (m_WorkerData.HasComponent(resident.m_Citizen))
                    {
                        Worker worker = m_WorkerData[resident.m_Citizen];
                        if (m_PropertyRenterData.HasComponent(worker.m_Workplace))
                        {
                            parameters.m_Authorization2 = m_PropertyRenterData[worker.m_Workplace].m_Property;
                        }
                        else
                        {
                            parameters.m_Authorization2 = worker.m_Workplace;
                        }
                    }
                    if (m_CitizenData.HasComponent(resident.m_Citizen))
                    {
                        Citizen citizen = m_CitizenData[resident.m_Citizen];
                        Entity household2 = m_HouseholdMemberData[resident.m_Citizen].m_Household;
                        Household household3 = m_HouseholdData[household2];
                        parameters.m_Weights = CitizenUtils.GetPathfindWeights(citizen, household3, m_HouseholdCitizens[household2].Length);
                    }
                    if (m_TravelPurposeData.TryGetComponent(resident.m_Citizen, out var componentData))
                    {
                        switch (componentData.m_Purpose)
                        {
                            case Purpose.EmergencyShelter:
                                parameters.m_Weights = new PathfindWeights(1f, 0.2f, 0f, 0.1f);
                                break;
                            case Purpose.MovingAway:
                                parameters.m_MaxCost = CitizenBehaviorSystem.kMaxMovingAwayCost;
                                break;
                        }
                    }
                    if (m_DivertData.HasComponent(entity2))
                    {
                        Divert divert = m_DivertData[entity2];
                        CreatureUtils.DivertDestination(ref destination, ref pathOwner, divert);
                        flag &= divert.m_Purpose == Purpose.None;
                    }
                }
            }
            else
            {
                PathfindParameters pathfindParameters = default(PathfindParameters);
                pathfindParameters.m_MaxSpeed = carData.m_MaxSpeed;
                pathfindParameters.m_WalkSpeed = 5.555556f;
                pathfindParameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
                pathfindParameters.m_Methods = PathMethod.Road;
                pathfindParameters.m_ParkingTarget = VehicleUtils.GetParkingSource(entity, currentLane, ref m_ParkingLaneData, ref m_ConnectionLaneData);
                pathfindParameters.m_ParkingDelta = currentLane.m_CurvePosition.z;
                pathfindParameters.m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData);
                parameters = pathfindParameters;
                SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road | PathMethod.Parking;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                origin = setupQueueTarget;
                setupQueueTarget = default(SetupQueueTarget);
                setupQueueTarget.m_Type = SetupTargetType.CurrentLocation;
                setupQueueTarget.m_Methods = PathMethod.Road;
                setupQueueTarget.m_RoadTypes = RoadTypes.Car;
                setupQueueTarget.m_Entity = target.m_Target;
                destination = setupQueueTarget;
            }
            if (flag)
            {
                personalCar.m_State |= PersonalCarFlags.HomeTarget;
            }
            else
            {
                personalCar.m_State &= ~PersonalCarFlags.HomeTarget;
            }
            VehicleUtils.SetupPathfind(item: new SetupQueueItem(entity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
        }

        void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
        }
    }
}
