/// <summary>
/// Applies induced demand system to GarageLane for pathfinding graph
/// </summary>

using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Pathfind;
using Game.Tools;
using Colossal.Serialization.Entities;
using Colossal.Collections;

namespace RealisticParking
{
    public partial class GarageLanesModifiedSystem : GameSystemBase
    {
        // adapted from UpdateEdgeJob from LanesModifiedSystem and only kept lines for ConnectionLanes
        [BurstCompile]
        private struct UpdateTollEdgeJob : IJob
        {
            // custom code start
            [ReadOnly]
            public ComponentLookup<GarageCount> garageCountLookup;

            // make GarageLane unaccessible if vehicle count with demand applied exceeds capacity
            private void ApplyGarageDemand(ref PathSpecification pathSpec, Entity entity, GarageLane garageLane)
            {
                if (GetGarageCountWithDemand(entity, garageLane) >= garageLane.m_VehicleCapacity)
                {
                    pathSpec.m_MaxSpeed = 1f;
                    pathSpec.m_Density = 0f;
                }
            }
            private int GetGarageCountWithDemand(Entity entity, GarageLane garageLane)
            {
                if (garageCountLookup.TryGetComponent(entity, out GarageCount customCount))
                    return customCount.countWithDemand;
                else
                    return garageLane.m_VehicleCount;
            }
            // custom code end


            [ReadOnly]
            public NativeList<ArchetypeChunk> m_Chunks;

            [ReadOnly]
            public ComponentLookup<NetLaneData> m_NetLaneData;

            [ReadOnly]
            public ComponentLookup<PathfindConnectionData> m_ConnectionPathfindData;

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Owner> m_OwnerType;

            [ReadOnly]
            public ComponentTypeHandle<Lane> m_LaneType;

            [ReadOnly]
            public ComponentTypeHandle<Curve> m_CurveType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Net.ConnectionLane> m_ConnectionLaneType;

            [ReadOnly]
            public ComponentTypeHandle<GarageLane> m_GarageLaneType;

            [ReadOnly]
            public ComponentTypeHandle<Game.Net.OutsideConnection> m_OutsideConnectionType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

            [WriteOnly]
            public NativeArray<UpdateActionData> m_Actions;

            public void Execute()
            {
                int num = 0;
                for (int i = 0; i < m_Chunks.Length; i++)
                {
                    ArchetypeChunk archetypeChunk = m_Chunks[i];
                    NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
                    NativeArray<Lane> nativeArray2 = archetypeChunk.GetNativeArray(ref m_LaneType);
                    NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CurveType);
                    NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
                    NativeArray<Game.Net.ConnectionLane> nativeArray14 = archetypeChunk.GetNativeArray(ref m_ConnectionLaneType);
				    NativeArray<GarageLane> nativeArray15 = archetypeChunk.GetNativeArray(ref m_GarageLaneType);
                    NativeArray<Game.Net.OutsideConnection> nativeArray16 = archetypeChunk.GetNativeArray(ref m_OutsideConnectionType);
				    for (int num4 = 0; num4<nativeArray14.Length; num4++)
				    {
					    Lane lane6 = nativeArray2[num4];
                        Curve curveData4 = nativeArray3[num4];
                        Game.Net.ConnectionLane connectionLaneData = nativeArray14[num4];
                        PrefabRef prefabRef6 = nativeArray4[num4];
                        NetLaneData netLaneData6 = m_NetLaneData[prefabRef6.m_Prefab];
                        PathfindConnectionData connectionPathfindData = m_ConnectionPathfindData[netLaneData6.m_PathfindPrefab];
					    if (!CollectionUtils.TryGet(nativeArray15, num4, out var value14))
					    {
                            value14.m_VehicleCapacity = ushort.MaxValue;
					    }
                        CollectionUtils.TryGet(nativeArray16, num4, out var value15);
                        UpdateActionData value16 = default(UpdateActionData);
                        value16.m_Owner = nativeArray[num4];
					    value16.m_StartNode = lane6.m_StartNode;
					    value16.m_MiddleNode = lane6.m_MiddleNode;
					    value16.m_EndNode = lane6.m_EndNode;
					    if (lane6.m_StartNode.Equals(lane6.m_EndNode) && (connectionLaneData.m_Flags & ConnectionLaneFlags.SecondaryStart) != 0)
					    {
						    value16.m_SecondaryStartNode = value16.m_StartNode;
						    value16.m_SecondaryEndNode = value16.m_EndNode;
						    value16.m_SecondarySpecification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						    value16.m_SecondarySpecification.m_Flags |= EdgeFlags.SecondaryStart;
                            ApplyGarageDemand(ref value16.m_SecondarySpecification, nativeArray[num4], value14);
                        }
					    else
					    {
						    value16.m_Specification = PathUtils.GetSpecification(curveData4, connectionLaneData, value14, value15, connectionPathfindData);
						    if ((connectionLaneData.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0)
						    {
							    value16.m_SecondaryStartNode = value16.m_StartNode;
							    value16.m_SecondaryEndNode = value16.m_EndNode;
							    value16.m_SecondarySpecification = PathUtils.GetSecondarySpecification(curveData4, connectionLaneData, value15, connectionPathfindData);
						    }
                            ApplyGarageDemand(ref value16.m_Specification, nativeArray[num4], value14);
                        }
                        value16.m_Location = PathUtils.GetLocationSpecification(curveData4);
                        m_Actions[num++] = value16;
				    }
			    }
		    }
        }


        private PathfindQueueSystem pathfindQueueSystem;
        private EntityQuery updatedTollLanesQuery;
        private EntityQuery allGarageLanesQuery;
        private bool skipFirstFrame;
        private bool init;

        protected override void OnCreate()
        {
            base.OnCreate();
            pathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
            allGarageLanesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[2] {
                ComponentType.ReadOnly<Game.Net.ConnectionLane>(),
                ComponentType.ReadOnly<GarageLane>()
            },
                None = new ComponentType[3]
            {
                ComponentType.ReadOnly<Temp>(),
                ComponentType.ReadOnly<SlaveLane>(),
                ComponentType.ReadOnly<Deleted>()
            }
            });

            updatedTollLanesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Game.Net.ConnectionLane>(),
                    ComponentType.ReadOnly<GarageLane>()
                },
                None = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[3]
                {
                    ComponentType.ReadOnly<PathfindUpdated>(),
                    ComponentType.ReadOnly<Game.Net.ConnectionLane>(),
                    ComponentType.ReadOnly<GarageLane>()
                },
                None = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>()
                }
            });
        }

        protected override void OnUpdate()
        {
            // skip first frame to avoid conflicts with LanesModifiedSystem
            if (skipFirstFrame)
            {
                skipFirstFrame = false;
                return;
            }

            // set entityQuery to all lanes if initial frame
            EntityQuery entityQuery;
            if (init)
                entityQuery = allGarageLanesQuery;
            else
                entityQuery = updatedTollLanesQuery;
            int queryCount = entityQuery.CalculateEntityCount();
            if (queryCount == 0)
                return;
            init = false;

            // run update job
            JobHandle jobHandle = base.Dependency;
            UpdateAction action = new UpdateAction(queryCount, Allocator.Persistent);
            JobHandle outJobHandle;
            NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
            JobHandle jobHandle2 = IJobExtensions.Schedule(new UpdateTollEdgeJob
            {
                m_Chunks = chunks,
                m_NetLaneData = SystemAPI.GetComponentLookup<NetLaneData>(isReadOnly: true),
                m_ConnectionPathfindData = SystemAPI.GetComponentLookup<PathfindConnectionData>(isReadOnly: true),
                m_EntityType = SystemAPI.GetEntityTypeHandle(),
                m_OwnerType = SystemAPI.GetComponentTypeHandle<Owner>(isReadOnly: true),
                m_LaneType = SystemAPI.GetComponentTypeHandle<Lane>(isReadOnly: true),
                m_CurveType = SystemAPI.GetComponentTypeHandle<Curve>(isReadOnly: true),
                m_ConnectionLaneType = SystemAPI.GetComponentTypeHandle<Game.Net.ConnectionLane>(isReadOnly: true),
                m_GarageLaneType = SystemAPI.GetComponentTypeHandle<GarageLane>(isReadOnly: true),
                m_OutsideConnectionType = SystemAPI.GetComponentTypeHandle<Game.Net.OutsideConnection>(isReadOnly: true),
                m_PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
                m_Actions = action.m_UpdateData,
                garageCountLookup = SystemAPI.GetComponentLookup<GarageCount>(isReadOnly: true),
            }, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
            jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
            chunks.Dispose(jobHandle2);
            pathfindQueueSystem.Enqueue(action, jobHandle2);
            base.Dependency = jobHandle;
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            skipFirstFrame = true;
            init = true;
        }
    }
}
