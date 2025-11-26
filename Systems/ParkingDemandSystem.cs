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
using Unity.Burst;
using Unity.Mathematics;

namespace RealisticParking
{
    [BurstCompile]
    public struct UpdateParkingDemandJob : IJobChunk
    {
        private const int COOLDOWN_LENGTH = 1000;
        private const float COOLDOWN_FACTOR = 0.1f;
        public EntityCommandBuffer.ParallelWriter commandBuffer;
        [ReadOnly] public EntityTypeHandle entityType;
        [ReadOnly] public ComponentLookup<CarQueued> carQueuedLookup;
        [ReadOnly] public ComponentLookup<CarParked> carParkedLookup;
        [ReadOnly] public ComponentLookup<ParkingDemand> parkingDemand;
        [ReadOnly] public uint currentFrameIndex;
        [ReadOnly] public bool enableDemandSystem;
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
                        commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                    }
                    if (carQueuedLookup.HasComponent(entity))
                        commandBuffer.RemoveComponent<CarQueued>(unfilteredChunkIndex, entity);
                    if (carParkedLookup.HasComponent(entity))
                        commandBuffer.RemoveComponent<CarParked>(unfilteredChunkIndex, entity);

                    continue;
                }

                // initialize parking demand component and variables
                ParkingDemand oldDemand = new ParkingDemand(0, currentFrameIndex);
                if (parkingDemand.HasComponent(entity))
                    oldDemand = parkingDemand[entity];
                short newDemandVal = oldDemand.demand;
                bool resetCooldown = false;

                // increment and decrement demand based on what tag components are present
                if (carQueuedLookup.HasComponent(entity))
                {
                    newDemandVal++;
                    commandBuffer.RemoveComponent<CarQueued>(unfilteredChunkIndex, entity);
                    resetCooldown = true;
                }
                if (carParkedLookup.HasComponent(entity))
                {
                    newDemandVal--;
                    commandBuffer.RemoveComponent<CarParked>(unfilteredChunkIndex, entity);
                }

                // decrease demand if cooldown reached
                if (oldDemand.demand != 0 && currentFrameIndex >= oldDemand.cooldownStartFrame + COOLDOWN_LENGTH)
                {
                    newDemandVal -= (short)(newDemandVal * COOLDOWN_FACTOR);
                    newDemandVal--;
                    resetCooldown = true;
                }
                newDemandVal = (short)math.max(0, newDemandVal);

                // update demand component and set PathfindUpdated flag
                if (newDemandVal != oldDemand.demand)
                {
                    if (!parkingDemand.HasComponent(entity))
                        commandBuffer.AddComponent<ParkingDemand>(unfilteredChunkIndex, entity);
                    commandBuffer.SetComponent(unfilteredChunkIndex, entity, new ParkingDemand(newDemandVal, resetCooldown ? currentFrameIndex : oldDemand.cooldownStartFrame));

                    if (math.ceil((oldDemand.demand - demandTolerance) / demandSizePerSpot) != math.ceil((newDemandVal - demandTolerance) / demandSizePerSpot))
                    {
                        commandBuffer.AddComponent<PathfindUpdated>(unfilteredChunkIndex, entity);
                    }
                }
            }
        }
    }

    public partial class ParkingDemandSystem : GameSystemBase
    {
        private ModificationBarrier5 barrier;
        private SimulationSystem simulationSystem;
        private bool enableDemandSystem;
        private int demandTolerance;
        private float demandSizePerSpot;
        EntityQuery updatedParkingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            barrier = World.GetOrCreateSystemManaged<ModificationBarrier5>();
            simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();

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
                Any = new ComponentType[3]
                {
                    ComponentType.ReadOnly<CarQueued>(),
                    ComponentType.ReadOnly<CarParked>(),
                    ComponentType.ReadOnly<ParkingDemand>(),
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
            parkingDemandJob.carParkedLookup = SystemAPI.GetComponentLookup<CarParked>(isReadOnly: true);
            parkingDemandJob.commandBuffer = barrier.CreateCommandBuffer().AsParallelWriter();
            parkingDemandJob.currentFrameIndex = simulationSystem.frameIndex;
            parkingDemandJob.enableDemandSystem = this.enableDemandSystem;
            parkingDemandJob.demandTolerance = this.demandTolerance;
            parkingDemandJob.demandSizePerSpot = this.demandSizePerSpot;
            JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(parkingDemandJob, updatedParkingQuery, base.Dependency);
            base.Dependency = jobHandle;
            barrier.AddJobHandleForProducer(jobHandle);
        }

        private void UpdateSettings(Setting settings)
        {
            this.enableDemandSystem = settings.EnableInducedDemand;
            this.demandTolerance = settings.InducedDemandInitialTolerance;
            this.demandSizePerSpot = settings.InducedDemandQueueSizePerSpot;
        }
    }
}
