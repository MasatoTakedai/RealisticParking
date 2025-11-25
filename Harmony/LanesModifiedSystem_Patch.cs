/// <summary>
/// patches LanesModifiedSystem's m_UpdatedLanesQuery to not include lanes with GarageLane component
/// </summary>

using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Tools;
using HarmonyLib;
using System.Reflection;
using Unity.Entities;
using System;
using static HarmonyLib.AccessTools;

namespace RealisticParking
{

    [HarmonyPatch(typeof(LanesModifiedSystem), "OnCreate")]
    public static class LanesModifiedSystem_Patch
    {
        private static readonly FieldRef<LanesModifiedSystem, EntityQuery> m_UpdatedLanesQueryRef =
            AccessTools.FieldRefAccess<LanesModifiedSystem, EntityQuery>("m_UpdatedLanesQuery");

        private static MethodInfo s_getEntityQueryMethod = typeof(ComponentSystemBase)
            .GetMethod("GetEntityQuery", BindingFlags.Instance | BindingFlags.NonPublic, null,
                       new Type[] { typeof(EntityQueryDesc[]) }, null);

        static void Postfix(LanesModifiedSystem __instance)
        {
            var method = AccessTools.Method(
                __instance.GetType(),
                "GetEntityQuery",
                new Type[] { typeof(EntityQueryDesc[]) });

            EntityQuery newQuery = (EntityQuery)s_getEntityQueryMethod.Invoke(__instance, new object[] { new EntityQueryDesc[] { new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Lane>()
                },
                Any = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Game.Net.CarLane>(),
                    ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                    ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
                    ComponentType.ReadOnly<Game.Net.ConnectionLane>()
                },
                None = new ComponentType[5]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>(),
                    ComponentType.ReadOnly<GarageLane>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Lane>()
                },
                Any = new ComponentType[1] { ComponentType.ReadOnly<Game.Net.TrackLane>() },
                None = new ComponentType[3]
                {
                    ComponentType.ReadOnly<Created>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            }, new EntityQueryDesc
            {
                All = new ComponentType[2]
                {
                    ComponentType.ReadOnly<PathfindUpdated>(),
                    ComponentType.ReadOnly<Lane>()
                },
                Any = new ComponentType[4]
                {
                    ComponentType.ReadOnly<Game.Net.CarLane>(),
                    ComponentType.ReadOnly<Game.Net.ParkingLane>(),
                    ComponentType.ReadOnly<Game.Net.PedestrianLane>(),
                    ComponentType.ReadOnly<Game.Net.ConnectionLane>()
                },
                None = new ComponentType[5]
                {
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<SlaveLane>(),
                    ComponentType.ReadOnly<GarageLane>()
                }
            } } });

            m_UpdatedLanesQueryRef(__instance) = newQuery;
        }
    }
}