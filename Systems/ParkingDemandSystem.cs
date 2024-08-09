/// <summary>
/// Processes parking demand and sets tag to update pathfinding for parking
/// </summary>

using Game;
using Game.Common;
using Game.Net;
using Game.Tools;
using Unity.Entities;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Jobs;
using Game.Simulation;

namespace RealisticParking
{
    public struct UpdateParkingDemandJob : IJobChunk
    {
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        [ReadOnly] public EntityTypeHandle entityType;
        [ReadOnly] public ComponentLookup<CarQueued> carQueuedLookup;
        [ReadOnly] public ComponentLookup<ParkingDemand> parkingDemand;
        [ReadOnly] public uint frameIndex;
        [ReadOnly] public bool enableDemandSystem;
        [ReadOnly] public uint cooldownLength;
        [ReadOnly] public int demandTolerance;
        [ReadOnly] public float demandSizePerSpot;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];

                // remove components if disabling system
                if (!enableDemandSystem)
                {
                    if (parkingDemand.HasComponent(entity))
                    {
                        commandBuffer.RemoveComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                        commandBuffer.RemoveComponent<GarageCount>(unfilteredChunkIndex, entity);
                        commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                    }
                    if (carQueuedLookup.HasComponent(entity))
                        commandBuffer.RemoveComponent<CarQueued>(unfilteredChunkIndex, entity);

                    continue;
                }

                // if a new car is queued add to parking demand data and add flag to update pathfinding if needed
                if (carQueuedLookup.HasComponent(entity))
                {
                    if (parkingDemand.TryGetComponent(entity, out ParkingDemand demandData))
                    {
                        short newDemand = (short)(demandData.demand + 1);
                        commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand(newDemand, frameIndex));

                        if (newDemand > demandTolerance && (newDemand - demandTolerance) % demandSizePerSpot == 0)
                        {
                            commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                        }
                    }
                    else
                    {
                        commandBuffer.AddComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                        commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand(1, frameIndex));

                        if (demandTolerance == 0 && demandSizePerSpot == 1)
                        {
                            commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                        }
                    }

                    commandBuffer.RemoveComponent<CarQueued>(unfilteredChunkIndex, entity);

                }
                // remove demand and garage count component if cooldown reached
                else if (parkingDemand.TryGetComponent(entity, out ParkingDemand demandData) && frameIndex >= demandData.cooldownStartFrame + cooldownLength)
                {
                    commandBuffer.RemoveComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                    commandBuffer.RemoveComponent<GarageCount>(unfilteredChunkIndex, entity);
                    commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                }
            }
        }
    }

    public partial class ParkingDemandSystem : GameSystemBase
    {
        private EntityCommandBufferSystem entityCommandBufferSystem;
        private SimulationSystem simulationSystem;
        private bool enableDemandSystem;
        private uint cooldownLength;
        private int demandTolerance;
        private int demandSizePerSpot;
        EntityQuery updatedParkingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetExistingSystemManaged<ModificationBarrier1>();
            simulationSystem = World.GetExistingSystemManaged<SimulationSystem>();

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
                All = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.ParkingLane>(), },
                Any = new ComponentType[2]
                {
                ComponentType.ReadOnly<CarQueued>(),
                ComponentType.ReadOnly<ParkingDemand>()
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
                ComponentType.ReadOnly<ParkingDemand>()
                },
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
            UpdateParkingDemandJob parkingDemandJob = new UpdateParkingDemandJob();
            parkingDemandJob.entityType = SystemAPI.GetEntityTypeHandle();
            parkingDemandJob.parkingDemand = SystemAPI.GetComponentLookup<ParkingDemand>(isReadOnly: true);
            parkingDemandJob.carQueuedLookup = SystemAPI.GetComponentLookup<CarQueued>(isReadOnly: true);
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            parkingDemandJob.commandBuffer = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            parkingDemandJob.frameIndex = simulationSystem.frameIndex;
            parkingDemandJob.enableDemandSystem = this.enableDemandSystem;
            parkingDemandJob.cooldownLength = this.cooldownLength;
            parkingDemandJob.demandTolerance = this.demandTolerance;
            parkingDemandJob.demandSizePerSpot = this.demandSizePerSpot;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(parkingDemandJob, updatedParkingQuery, base.Dependency);
            base.Dependency = jobHandle;
        }

        private void UpdateSettings(Setting settings)
        {
            this.enableDemandSystem = settings.InducedDemandEnable;
            this.cooldownLength = (uint)settings.InducedDemandCooldown;
            this.demandTolerance = settings.InducedDemandInitialTolerance;
            this.demandSizePerSpot = settings.InducedDemandQueueSizePerSpot;
        }
    }
}
