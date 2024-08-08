/* copied from ParkingLaneDataSystem with some custom modifications */

using Colossal.Mathematics;
using Colossal.Collections;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Entities;
using Unity.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using UnityEngine;
using static Colossal.IO.AssetDatabase.AtlasFrame;

namespace RealisticParking
{
    [BurstCompile]
    public struct UpdateLaneDataJob : IJobChunk
    {
        // custom code start
        public int garageSpotsMultiplier;
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        [ReadOnly] public ComponentLookup<CarQueued> carQueuedLookup;
        [ReadOnly] public ComponentLookup<ParkingDemand> parkingDemand;
        [ReadOnly] public bool enableDemandSystem;
        [ReadOnly] public int demandTolerance;
        [ReadOnly] public float demandSizePerSpot;

        private float CalculateCustomFreeSpace(Entity entity, int unfilteredChunkIndex, Curve curve, Game.Net.ParkingLane parkingLane, ParkingLaneData parkingLaneData, DynamicBuffer<LaneObject> laneObjects, DynamicBuffer<LaneOverlap> laneOverlaps, Bounds1 blockedRange)
        {
            float freeSpace = CalculateFreeSpace(curve, parkingLane, parkingLaneData, laneObjects, laneOverlaps, blockedRange);
            float newFreeSpace = freeSpace;
            if (enableDemandSystem && parkingDemand.TryGetComponent(entity, out ParkingDemand limit))
            {
                // set new free space value
                if (limit.demand > demandTolerance)
                    newFreeSpace -= math.min(freeSpace - 0.01f, math.floor((limit.demand - demandTolerance) / demandSizePerSpot) * 6);

                // remove parking demand component if no viable spots left
                if (freeSpace < 2)
                {
                    newFreeSpace = freeSpace;
                    commandBuffer.RemoveComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                }
            }
            return newFreeSpace;
        }

        private int ApplyCustomGarageCapacity(int vanillaCapacity) { return garageSpotsMultiplier * vanillaCapacity; }
        // custom code end


        [ReadOnly]
        public EntityTypeHandle m_EntityType;

        [ReadOnly]
        public ComponentTypeHandle<Curve> m_CurveType;

        [ReadOnly]
        public ComponentTypeHandle<Owner> m_OwnerType;

        [ReadOnly]
        public ComponentTypeHandle<Lane> m_LaneType;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

        [ReadOnly]
        public BufferTypeHandle<LaneOverlap> m_LaneOverlapType;

        public ComponentTypeHandle<Game.Net.ParkingLane> m_ParkingLaneType;

        public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

        public ComponentTypeHandle<GarageLane> m_GarageLaneType;

        public BufferTypeHandle<LaneObject> m_LaneObjectType;

        [ReadOnly]
        public ComponentLookup<Owner> m_OwnerData;

        [ReadOnly]
        public ComponentLookup<Lane> m_LaneData;

        [ReadOnly]
        public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

        [ReadOnly]
        public ComponentLookup<Road> m_RoadData;

        [ReadOnly]
        public ComponentLookup<ParkedCar> m_ParkedCarData;

        [ReadOnly]
        public ComponentLookup<BorderDistrict> m_BorderDistrictData;

        [ReadOnly]
        public ComponentLookup<District> m_DistrictData;

        [ReadOnly]
        public ComponentLookup<Building> m_BuildingData;

        [ReadOnly]
        public ComponentLookup<Game.Buildings.ParkingFacility> m_ParkingFacilityData;

        [ReadOnly]
        public ComponentLookup<Game.City.City> m_CityData;

        [ReadOnly]
        public ComponentLookup<Game.Objects.Transform> m_TransformData;

        [ReadOnly]
        public ComponentLookup<Unspawned> m_UnspawnedData;

        [ReadOnly]
        public ComponentLookup<Attachment> m_AttachmentData;

        [ReadOnly]
        public ComponentLookup<PrefabRef> m_PrefabRefData;

        [ReadOnly]
        public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

        [ReadOnly]
        public ComponentLookup<ParkingLaneData> m_ParkingLaneData;

        [ReadOnly]
        public ComponentLookup<ParkingFacilityData> m_PrefabParkingFacilityData;

        [ReadOnly]
        public ComponentLookup<BuildingData> m_PrefabBuildingData;

        [ReadOnly]
        public ComponentLookup<BuildingPropertyData> m_PrefabBuildingPropertyData;

        [ReadOnly]
        public ComponentLookup<WorkplaceData> m_PrefabWorkplaceData;

        [ReadOnly]
        public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

        [ReadOnly]
        public BufferLookup<DistrictModifier> m_DistrictModifiers;

        [ReadOnly]
        public BufferLookup<BuildingModifier> m_BuildingModifiers;

        [ReadOnly]
        public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

        [ReadOnly]
        public BufferLookup<Game.Net.SubLane> m_SubLanes;

        [ReadOnly]
        public BufferLookup<CityModifier> m_CityModifiers;

        [ReadOnly]
        public BufferLookup<ActivityLocationElement> m_ActivityLocations;

        [ReadOnly]
        public Entity m_City;

        [ReadOnly]
        public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Curve> nativeArray = chunk.GetNativeArray(ref m_CurveType);
            NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
            NativeArray<Game.Net.ParkingLane> nativeArray3 = chunk.GetNativeArray(ref m_ParkingLaneType);
            NativeArray<Entity> nativeArray6 = chunk.GetNativeArray(m_EntityType);
            if (nativeArray3.Length != 0)
            {
                NativeArray<Lane> nativeArray4 = chunk.GetNativeArray(ref m_LaneType);
                NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
                BufferAccessor<LaneObject> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneObjectType);
                BufferAccessor<LaneOverlap> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LaneOverlapType);
                ushort taxiFee = 0;
                if (m_City != Entity.Null && CityUtils.CheckOption(m_CityData[m_City], CityOption.PaidTaxiStart))
                {
                    float value = 0f;
                    CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.TaxiStartingFee);
                    taxiFee = (ushort)math.clamp(Mathf.RoundToInt(value), 0, 65535);
                }
                for (int i = 0; i < nativeArray3.Length; i++)
                {
                    Curve curve = nativeArray[i];
                    Owner owner = nativeArray2[i];
                    Lane laneData = nativeArray4[i];
                    Game.Net.ParkingLane parkingLane = nativeArray3[i];
                    PrefabRef prefabRef = nativeArray5[i];
                    DynamicBuffer<LaneObject> laneObjects = bufferAccessor[i];
                    DynamicBuffer<LaneOverlap> laneOverlaps = bufferAccessor2[i];
                    ParkingLaneData parkingLaneData = m_ParkingLaneData[prefabRef.m_Prefab];
                    Bounds1 blockedRange = GetBlockedRange(owner, laneData);
                    parkingLane.m_Flags &= ~(ParkingLaneFlags.ParkingDisabled | ParkingLaneFlags.AllowEnter | ParkingLaneFlags.AllowExit);
                    laneObjects.AsNativeArray().Sort();
                    parkingLane.m_FreeSpace = CalculateCustomFreeSpace(nativeArray6[i], unfilteredChunkIndex, curve, parkingLane, parkingLaneData, laneObjects, laneOverlaps, blockedRange);
                    GetParkingStats(owner, parkingLane, out parkingLane.m_AccessRestriction, out var _, out parkingLane.m_ParkingFee, out parkingLane.m_ComfortFactor, out var disabled, out var allowEnter, out var allowExit);
                    parkingLane.m_TaxiFee = taxiFee;
                    if (disabled)
                    {
                        parkingLane.m_Flags |= ParkingLaneFlags.ParkingDisabled;
                    }
                    if (allowEnter)
                    {
                        parkingLane.m_Flags |= ParkingLaneFlags.AllowEnter;
                    }
                    if (allowExit)
                    {
                        parkingLane.m_Flags |= ParkingLaneFlags.AllowExit;
                    }
                    nativeArray3[i] = parkingLane;
                }
                return;
            }
            NativeArray<GarageLane> nativeArray7 = chunk.GetNativeArray(ref m_GarageLaneType);
            NativeArray<Game.Net.ConnectionLane> nativeArray8 = chunk.GetNativeArray(ref m_ConnectionLaneType);
            for (int j = 0; j < nativeArray7.Length; j++)
            {
                Entity entity = nativeArray6[j];
                Curve curve2 = nativeArray[j];
                Owner owner2 = nativeArray2[j];
                GarageLane value2 = nativeArray7[j];
                Game.Net.ConnectionLane connectionLane = nativeArray8[j];
                connectionLane.m_Flags &= ~(ConnectionLaneFlags.Disabled | ConnectionLaneFlags.AllowEnter | ConnectionLaneFlags.AllowExit);
                GetParkingStats(owner2, default(Game.Net.ParkingLane), out connectionLane.m_AccessRestriction, out value2.m_VehicleCapacity, out value2.m_ParkingFee, out value2.m_ComfortFactor, out var disabled2, out var allowEnter2, out var allowExit2);
                value2.m_VehicleCount = CountVehicles(entity, owner2, curve2, connectionLane);
                if (disabled2)
                {
                    connectionLane.m_Flags |= ConnectionLaneFlags.Disabled;
                }
                if (allowEnter2)
                {
                    connectionLane.m_Flags |= ConnectionLaneFlags.AllowEnter;
                }
                if (allowExit2)
                {
                    connectionLane.m_Flags |= ConnectionLaneFlags.AllowExit;
                }
                nativeArray7[j] = value2;
                nativeArray8[j] = connectionLane;
            }
        }

        private Bounds1 GetBlockedRange(Owner owner, Lane laneData)
        {
            Bounds1 result = new Bounds1(2f, -1f);
            if (m_SubLanes.HasBuffer(owner.m_Owner))
            {
                DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner.m_Owner];
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    Entity subLane = dynamicBuffer[i].m_SubLane;
                    Lane lane = m_LaneData[subLane];
                    if (laneData.m_StartNode.EqualsIgnoreCurvePos(lane.m_MiddleNode) && m_CarLaneData.HasComponent(subLane))
                    {
                        Game.Net.CarLane carLane = m_CarLaneData[subLane];
                        if (carLane.m_BlockageEnd >= carLane.m_BlockageStart)
                        {
                            Bounds1 blockageBounds = carLane.blockageBounds;
                            blockageBounds.min = math.select(blockageBounds.min - 0.01f, 0f, blockageBounds.min <= 0.51f);
                            blockageBounds.max = math.select(blockageBounds.max + 0.01f, 1f, blockageBounds.max >= 0.49f);
                            result |= blockageBounds;
                        }
                    }
                }
            }
            return result;
        }

        private void GetParkingStats(Owner owner, Game.Net.ParkingLane parkingLane, out Entity restriction, out ushort garageCapacity, out ushort fee, out ushort comfort, out bool disabled, out bool allowEnter, out bool allowExit)
        {
            restriction = Entity.Null;
            garageCapacity = 0;
            fee = 0;
            comfort = 0;
            disabled = false;
            allowEnter = false;
            allowExit = false;
            Owner owner2 = owner;
            Owner componentData;
            while (m_OwnerData.TryGetComponent(owner2.m_Owner, out componentData))
            {
                owner2 = componentData;
            }
            if (m_BuildingData.HasComponent(owner2.m_Owner))
            {
                ParkingFacilityData parkingFacilityData = default(ParkingFacilityData);
                bool flag = false;
                PrefabRef prefabRef = m_PrefabRefData[owner2.m_Owner];
                if (m_PrefabParkingFacilityData.HasComponent(prefabRef.m_Prefab))
                {
                    parkingFacilityData = m_PrefabParkingFacilityData[prefabRef.m_Prefab];
                    flag = true;
                }
                if (m_InstalledUpgrades.TryGetBuffer(owner2.m_Owner, out var bufferData))
                {
                    for (int i = 0; i < bufferData.Length; i++)
                    {
                        InstalledUpgrade installedUpgrade = bufferData[i];
                        if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
                        {
                            PrefabRef prefabRef2 = m_PrefabRefData[installedUpgrade.m_Upgrade];
                            if (m_PrefabParkingFacilityData.HasComponent(prefabRef2.m_Prefab))
                            {
                                parkingFacilityData.Combine(m_PrefabParkingFacilityData[prefabRef2.m_Prefab]);
                                flag = true;
                            }
                        }
                    }
                }
                Entity entity = owner2.m_Owner;
                if (m_AttachmentData.TryGetComponent(entity, out var componentData2) && componentData2.m_Attached != Entity.Null)
                {
                    entity = componentData2.m_Attached;
                }
                if (m_PrefabRefData.TryGetComponent(entity, out var componentData3) && m_PrefabBuildingData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
                {
                    if (m_RoadData.HasComponent(owner.m_Owner))
                    {
                        componentData4.m_Flags &= ~(Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar);
                    }
                    restriction = entity;
                    allowEnter = (componentData4.m_Flags & (Game.Prefabs.BuildingFlags.RestrictedPedestrian | Game.Prefabs.BuildingFlags.RestrictedCar)) == 0;
                    allowExit = (componentData4.m_Flags & Game.Prefabs.BuildingFlags.RestrictedParking) == 0;
                }
                if (m_PrefabRefData.TryGetComponent(owner.m_Owner, out var componentData5) && m_PrefabGeometryData.TryGetComponent(componentData5.m_Prefab, out var componentData6) && (componentData6.m_Flags & Game.Net.GeometryFlags.SubOwner) != 0)
                {
                    restriction = Entity.Null;
                    allowEnter = false;
                    allowExit = false;
                }
                if (!flag)
                {
                    parkingFacilityData.m_GarageMarkerCapacity = 2;
                    parkingFacilityData.m_ComfortFactor = 0.8f;
                    WorkplaceData componentData8;
                    if (m_PrefabBuildingPropertyData.TryGetComponent(prefabRef.m_Prefab, out var componentData7))
                    {
                        parkingFacilityData.m_GarageMarkerCapacity = math.max(1, ApplyCustomGarageCapacity(Mathf.RoundToInt(componentData7.m_SpaceMultiplier)));
                    }
                    else if (m_PrefabWorkplaceData.TryGetComponent(prefabRef.m_Prefab, out componentData8))
                    {
                        parkingFacilityData.m_GarageMarkerCapacity = math.max(2, componentData8.m_MaxWorkers / 20);
                    }
                }
                if (m_ParkingFacilityData.TryGetComponent(owner2.m_Owner, out var componentData9))
                {
                    disabled = (componentData9.m_Flags & ParkingFacilityFlags.ParkingSpacesActive) == 0 && (parkingLane.m_Flags & ParkingLaneFlags.VirtualLane) == 0;
                    parkingFacilityData.m_ComfortFactor = componentData9.m_ComfortFactor;
                }
                garageCapacity = (ushort)math.clamp(parkingFacilityData.m_GarageMarkerCapacity, 0, 65535);
                comfort = (ushort)math.clamp(Mathf.RoundToInt(parkingFacilityData.m_ComfortFactor * 65535f), 0, 65535);
                fee = GetBuildingParkingFee(owner2.m_Owner);
            }
            else if (m_BorderDistrictData.HasComponent(owner.m_Owner))
            {
                BorderDistrict borderDistrict = m_BorderDistrictData[owner.m_Owner];
                if ((parkingLane.m_Flags & ParkingLaneFlags.RightSide) != 0)
                {
                    fee = GetDistrictParkingFee(borderDistrict.m_Right);
                }
                else
                {
                    fee = GetDistrictParkingFee(borderDistrict.m_Left);
                }
            }
        }

        private ushort GetDistrictParkingFee(Entity district)
        {
            if (m_DistrictData.HasComponent(district) && AreaUtils.CheckOption(m_DistrictData[district], DistrictOption.PaidParking))
            {
                float value = 0f;
                DynamicBuffer<DistrictModifier> modifiers = m_DistrictModifiers[district];
                AreaUtils.ApplyModifier(ref value, modifiers, DistrictModifierType.ParkingFee);
                return (ushort)math.clamp(Mathf.RoundToInt(value), 0, 65535);
            }
            return 0;
        }

        private ushort GetBuildingParkingFee(Entity building)
        {
            if (m_BuildingData.HasComponent(building) && BuildingUtils.CheckOption(m_BuildingData[building], BuildingOption.PaidParking))
            {
                float value = 0f;
                DynamicBuffer<BuildingModifier> modifiers = m_BuildingModifiers[building];
                BuildingUtils.ApplyModifier(ref value, modifiers, BuildingModifierType.ParkingFee);
                return (ushort)math.clamp(Mathf.RoundToInt(value), 0, 65535);
            }
            return 0;
        }

        private ushort CountVehicles(Entity entity, Owner owner, Curve curve, Game.Net.ConnectionLane connectionLane)
        {
            CountVehiclesIterator countVehiclesIterator = default(CountVehiclesIterator);
            countVehiclesIterator.m_Lane = entity;
            countVehiclesIterator.m_Bounds = VehicleUtils.GetConnectionParkingBounds(connectionLane, curve.m_Bezier);
            countVehiclesIterator.m_ParkedCarData = m_ParkedCarData;
            CountVehiclesIterator iterator = countVehiclesIterator;
            Owner owner2 = owner;
            while (m_OwnerData.HasComponent(owner2.m_Owner))
            {
                owner2 = m_OwnerData[owner2.m_Owner];
            }
            if (m_BuildingData.HasComponent(owner2.m_Owner))
            {
                PrefabRef prefabRef = m_PrefabRefData[owner2.m_Owner];
                if (m_ActivityLocations.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
                {
                    Game.Objects.Transform transform = m_TransformData[owner2.m_Owner];
                    ActivityMask activityMask = new ActivityMask(ActivityType.GarageSpot);
                    for (int i = 0; i < bufferData.Length; i++)
                    {
                        ActivityLocationElement activityLocationElement = bufferData[i];
                        if ((activityLocationElement.m_ActivityMask.m_Mask & activityMask.m_Mask) != 0)
                        {
                            float3 @float = ObjectUtils.LocalToWorld(transform, activityLocationElement.m_Position);
                            iterator.m_Bounds.min = math.min(iterator.m_Bounds.min, @float - 1f);
                            iterator.m_Bounds.max = math.max(iterator.m_Bounds.max, @float + 1f);
                        }
                    }
                }
            }
            m_MovingObjectSearchTree.Iterate(ref iterator);
            return (ushort)math.clamp(iterator.m_Result, 0, 65535);
        }

        private float CalculateFreeSpace(Curve curve, Game.Net.ParkingLane parkingLane, ParkingLaneData parkingLaneData, DynamicBuffer<LaneObject> laneObjects, DynamicBuffer<LaneOverlap> laneOverlaps, Bounds1 blockedRange)
        {
            if ((parkingLane.m_Flags & ParkingLaneFlags.VirtualLane) != 0)
            {
                return 0f;
            }
            if (parkingLaneData.m_SlotInterval != 0f)
            {
                int num = (int)math.floor((curve.m_Length + 0.01f) / parkingLaneData.m_SlotInterval);
                float3 x = curve.m_Bezier.a;
                float2 @float = 0f;
                float num2 = 0f;
                float num3 = math.max((parkingLane.m_Flags & (ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane)) switch
                {
                    ParkingLaneFlags.StartingLane => curve.m_Length - (float)num * parkingLaneData.m_SlotInterval,
                    ParkingLaneFlags.EndingLane => 0f,
                    _ => (curve.m_Length - (float)num * parkingLaneData.m_SlotInterval) * 0.5f,
                }, 0f);
                int i = -1;
                float num4 = 2f;
                int num5 = 0;
                while (num5 < laneObjects.Length)
                {
                    LaneObject laneObject = laneObjects[num5++];
                    if (m_ParkedCarData.HasComponent(laneObject.m_LaneObject) && !m_UnspawnedData.HasComponent(laneObject.m_LaneObject))
                    {
                        num4 = laneObject.m_CurvePosition.x;
                        break;
                    }
                }
                float2 float2 = 2f;
                int num6 = 0;
                if (num6 < laneOverlaps.Length)
                {
                    LaneOverlap laneOverlap = laneOverlaps[num6++];
                    float2 = new float2((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd) * 0.003921569f;
                }
                for (int j = 1; j <= 16; j++)
                {
                    float num7 = (float)j * 0.0625f;
                    float3 float3 = MathUtils.Position(curve.m_Bezier, num7);
                    for (num2 += math.distance(x, float3); num2 >= num3 || (j == 16 && i < num); i++)
                    {
                        @float.y = math.select(num7, math.lerp(@float.x, num7, num3 / num2), num3 < num2);
                        bool flag = false;
                        if (num4 <= @float.y)
                        {
                            num4 = 2f;
                            flag = true;
                            while (num5 < laneObjects.Length)
                            {
                                LaneObject laneObject2 = laneObjects[num5++];
                                if (m_ParkedCarData.HasComponent(laneObject2.m_LaneObject) && !m_UnspawnedData.HasComponent(laneObject2.m_LaneObject) && laneObject2.m_CurvePosition.x > @float.y)
                                {
                                    num4 = laneObject2.m_CurvePosition.x;
                                    break;
                                }
                            }
                        }
                        if (float2.x < @float.y)
                        {
                            flag = true;
                            if (float2.y <= @float.y)
                            {
                                float2 = 2f;
                                while (num6 < laneOverlaps.Length)
                                {
                                    LaneOverlap laneOverlap2 = laneOverlaps[num6++];
                                    float2 float4 = new float2((int)laneOverlap2.m_ThisStart, (int)laneOverlap2.m_ThisEnd) * 0.003921569f;
                                    if (float4.y > @float.y)
                                    {
                                        float2 = float4;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!flag && i >= 0 && i < num && (@float.x > blockedRange.max || @float.y < blockedRange.min))
                        {
                            return parkingLaneData.m_MaxCarLength;
                        }
                        num2 -= num3;
                        @float.x = @float.y;
                        num3 = parkingLaneData.m_SlotInterval;
                    }
                    x = float3;
                }
                return 0f;
            }
            float x2 = 0f;
            float2 x3 = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.StartingLane) == 0);
            float3 x4 = curve.m_Bezier.a;
            float num8 = 2f;
            float2 float5 = 0f;
            int num9 = 0;
            while (num9 < laneObjects.Length)
            {
                LaneObject laneObject3 = laneObjects[num9++];
                if (m_ParkedCarData.HasComponent(laneObject3.m_LaneObject) && !m_UnspawnedData.HasComponent(laneObject3.m_LaneObject))
                {
                    num8 = laneObject3.m_CurvePosition.x;
                    float5 = VehicleUtils.GetParkingOffsets(laneObject3.m_LaneObject, ref m_PrefabRefData, ref m_ObjectGeometryData) + 1f;
                    break;
                }
            }
            float2 float6 = 2f;
            int num10 = 0;
            if (num10 < laneOverlaps.Length)
            {
                LaneOverlap laneOverlap3 = laneOverlaps[num10++];
                float6 = new float2((int)laneOverlap3.m_ThisStart, (int)laneOverlap3.m_ThisEnd) * 0.003921569f;
            }
            float3 y = default(float3);
            float3 float7 = default(float3);
            if (blockedRange.max >= blockedRange.min)
            {
                y = MathUtils.Position(curve.m_Bezier, MathUtils.Center(blockedRange));
                float7.x = math.distance(MathUtils.Position(curve.m_Bezier, blockedRange.min), y);
                float7.y = math.distance(MathUtils.Position(curve.m_Bezier, blockedRange.max), y);
            }
            float num11;
            while (num8 != 2f || float6.x != 2f)
            {
                float2 float8;
                float x5;
                if (num8 <= float6.x)
                {
                    float8 = num8;
                    x3.y = float5.x;
                    x5 = float5.y;
                    num8 = 2f;
                    while (num9 < laneObjects.Length)
                    {
                        LaneObject laneObject4 = laneObjects[num9++];
                        if (m_ParkedCarData.HasComponent(laneObject4.m_LaneObject) && !m_UnspawnedData.HasComponent(laneObject4.m_LaneObject))
                        {
                            num8 = laneObject4.m_CurvePosition.x;
                            float5 = VehicleUtils.GetParkingOffsets(laneObject4.m_LaneObject, ref m_PrefabRefData, ref m_ObjectGeometryData) + 1f;
                            break;
                        }
                    }
                }
                else
                {
                    float8 = float6;
                    x3.y = 0.5f;
                    x5 = 0.5f;
                    float6 = 2f;
                    while (num10 < laneOverlaps.Length)
                    {
                        LaneOverlap laneOverlap4 = laneOverlaps[num10++];
                        float2 float9 = new float2((int)laneOverlap4.m_ThisStart, (int)laneOverlap4.m_ThisEnd) * 0.003921569f;
                        if (float9.x <= float8.y)
                        {
                            float8.y = math.max(float8.y, float9.y);
                            continue;
                        }
                        float6 = float9;
                        break;
                    }
                }
                float3 float10 = MathUtils.Position(curve.m_Bezier, float8.x);
                num11 = math.distance(x4, float10) - math.csum(x3);
                if (blockedRange.max >= blockedRange.min)
                {
                    float x6 = math.distance(x4, y) - x3.x - float7.x;
                    float y2 = math.distance(float10, y) - x3.y - float7.y;
                    num11 = math.min(num11, math.max(x6, y2));
                }
                x2 = math.max(x2, num11);
                x3.x = x5;
                x4 = MathUtils.Position(curve.m_Bezier, float8.y);
            }
            x3.y = math.select(0f, 0.5f, (parkingLane.m_Flags & ParkingLaneFlags.EndingLane) == 0);
            num11 = math.distance(x4, curve.m_Bezier.d) - math.csum(x3);
            if (blockedRange.max >= blockedRange.min)
            {
                float x7 = math.distance(x4, y) - x3.x - float7.x;
                float y3 = math.distance(curve.m_Bezier.d, y) - x3.y - float7.y;
                num11 = math.min(num11, math.max(x7, y3));
            }
            x2 = math.max(x2, num11);
            return math.select(x2, math.min(x2, parkingLaneData.m_MaxCarLength), parkingLaneData.m_MaxCarLength != 0f);
        }


        private struct CountVehiclesIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public Entity m_Lane;

            public Bounds3 m_Bounds;

            public int m_Result;

            public ComponentLookup<ParkedCar> m_ParkedCarData;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_ParkedCarData.TryGetComponent(entity, out var componentData) && componentData.m_Lane == m_Lane)
                {
                    m_Result++;
                }
            }
        }
    }
}
