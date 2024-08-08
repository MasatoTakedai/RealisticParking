using Game;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Entities;

using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Jobs;

namespace RealisticParking
{
    public struct UpdateParkingDemandJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        [ReadOnly] public EntityTypeHandle entityType;
        [ReadOnly] public ComponentLookup<CarQueued> carQueuedLookup;
        [ReadOnly] public ComponentLookup<CarRerouted> carReroutedLookup;
        [ReadOnly] public ComponentLookup<ParkingDemand> parkingDemand;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (carQueuedLookup.HasComponent(entity))
                {
                    if (parkingDemand.TryGetComponent(entity, out ParkingDemand demandData))
                    {
                        short newDemand = (short)(demandData.demand + 1);
                        commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand((newDemand)));
                        if (newDemand > 6 && newDemand % 3 == 0)
                        {
                            commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                        }
                    }
                    else
                    {
                        commandBuffer.AddComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                        commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand(1));
                    }
                    commandBuffer.RemoveComponent<CarQueued>(unfilteredChunkIndex, entity);
                }

                if (carReroutedLookup.HasComponent(entity))
                {
                    if (parkingDemand.TryGetComponent(entity, out ParkingDemand demandData) && demandData.demand != 0)
                    {
                        commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand(0));
                        commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                    }
                    else
                    {
                        commandBuffer.AddComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                    }
                    commandBuffer.RemoveComponent<CarRerouted>(unfilteredChunkIndex, entity);
                }
            }
        }
    }

    public partial class ParkingDemandSystem : GameSystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery updatedParkingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystemManaged<ModificationBarrier1>();

            Mod.INSTANCE.settings.onSettingsApplied += settings =>
            {
                if (settings.GetType() == typeof(Setting))
                {
                    //this.UpdateSettings((Setting)settings);
                }
            };
            //this.UpdateSettings(Mod.INSTANCE.settings);

            updatedParkingQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.ParkingLane>(), },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<CarQueued>(),
                ComponentType.ReadOnly<CarRerouted>()
                },
                None = new ComponentType[2]
                {
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[1] { ComponentType.ReadOnly<GarageLane>() },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<CarQueued>(),
                ComponentType.ReadOnly<CarRerouted>()
                },
                None = new ComponentType[3]
                {
                ComponentType.ReadOnly<Updated>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.ReadOnly<Temp>()
                }
            });

            RequireForUpdate(updatedParkingQuery);
        }

        protected override void OnUpdate()
        {
            UpdateParkingDemandJob parkingDemandJob = new UpdateParkingDemandJob();
            parkingDemandJob.entityType = SystemAPI.GetEntityTypeHandle();
            parkingDemandJob.parkingDemand = SystemAPI.GetComponentLookup<ParkingDemand>(isReadOnly: true);
            parkingDemandJob.carQueuedLookup = SystemAPI.GetComponentLookup<CarQueued>(isReadOnly: true);
            parkingDemandJob.carReroutedLookup = SystemAPI.GetComponentLookup<CarRerouted>(isReadOnly: true);
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            parkingDemandJob.commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(parkingDemandJob, updatedParkingQuery, base.Dependency);
            base.Dependency = jobHandle;
        }
    }
}
