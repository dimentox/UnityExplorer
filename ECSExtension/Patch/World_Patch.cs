using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ECSExtension.Util;
using HarmonyLib;
using Unity.Entities;

namespace ECSExtension.Patch
{
    public static class World_Init_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = typeof(World);

            return type.GetConstructors(AccessTools.all)
                .Where(info =>
                {
                    return info.GetParameters().All(parameterInfo => parameterInfo.ParameterType != typeof(IntPtr));
                });
        }
        
        [HarmonyPostfix]
        public static void OnWorldInit(World __instance)
        {
            ECSUtil.WorldCreated?.Invoke(__instance);   
        }
    }

    public static class World_Dispose_Patch
    {
        [HarmonyPatch(typeof(World), nameof(World.Dispose))]
        [HarmonyPostfix]
        public static void OnWorldDispose(World __instance)
        {
            ECSUtil.WorldDestroyed?.Invoke(__instance);   
        }
    }
}