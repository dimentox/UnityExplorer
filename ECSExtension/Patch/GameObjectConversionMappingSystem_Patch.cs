using HarmonyLib;
using Unity.Entities;
using Unity.Entities.Conversion;

namespace ECSExtension.Patch
{
    public class GameObjectConversionMappingSystem_Patch
    {
        [HarmonyPatch(typeof(GameObjectConversionMappingSystem), nameof(GameObjectConversionMappingSystem.InitArchetypes))]
        [HarmonyPostfix]
        public static void OnCreate(GameObjectConversionMappingSystem __instance)
        {
            __instance._Settings_k__BackingField.ConversionFlags |= GameObjectConversionUtility.ConversionFlags.AssignName;
        }
    }
}