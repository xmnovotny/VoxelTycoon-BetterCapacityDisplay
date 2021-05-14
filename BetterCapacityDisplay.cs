using HarmonyLib;
using System;
using UnityEngine;
using VoxelTycoon.Game.UI;
using VoxelTycoon.Tracks;
using VoxelTycoon.Modding;
using VoxelTycoon.Tracks.Rails;
using System.Runtime.CompilerServices;

namespace BetterCapacityDisplay
{
    // Main Class
    public class BetterCapacityDisplay : Mod
    {
        private static Harmony harmony;

        protected override void Initialize()
        {
            harmony = (Harmony)(object)new Harmony("cz.xmnovotny.bettercapacitydisplay.patch");
            harmony.PatchAll();
        }

        protected override void Deinitialize()
        {
            harmony.UnpatchAll();
            harmony = null;
        }

    }

    [HarmonyPatch(typeof(DepotWindowVehicleListItemStoragesView))]
    class VehicleDepotPatch
    {
        private static readonly ConditionalWeakTable<DepotWindowVehicleListItemStoragesView, StorageViewHelper> _instancesData = new ConditionalWeakTable<DepotWindowVehicleListItemStoragesView, StorageViewHelper>();

        [HarmonyPatch("Invalidate")]
        [HarmonyPrefix]
        private static bool PrefixInvalidate(DepotWindowVehicleListItemStoragesView __instance, Transform ____itemsContainer, Vehicle ____vehicle)
        {
            if (!(____vehicle is Train))
            {
                return true;
            }
            StorageViewHelper instanceData;
            if (!_instancesData.TryGetValue(__instance, out instanceData))
            {
                Type type = ____itemsContainer.GetType();
                var newInstanceData = new StorageViewHelper(__instance, ____vehicle);
                _instancesData.Add(__instance, newInstanceData);
            }
            return true;
        }


        [HarmonyPatch("Invalidate")]
        [HarmonyPostfix]
        private static void PostfixInvalidate(DepotWindowVehicleListItemStoragesView __instance, Transform ____itemsContainer, int? ____version)
        {
            StorageViewHelper instanceData;
            if (_instancesData.TryGetValue(__instance, out instanceData))
            {
                instanceData.Invalidate(____version);
            }
        }

        [HarmonyPatch("GetVersion")]
        [HarmonyPostfix]
        private static void PostfixGetVersion(Vehicle vehicle, ref int __result)
        {
            __result = __result * -0x5AAAAAD7 + vehicle.Name.GetHashCode();
        }
    }

    [HarmonyPatch(typeof(VehicleWindowHeaderView))]
    class VehicleWindowPatch
    {
        [HarmonyPatch("Initialize")]
        [HarmonyPrefix]
        private static bool PrefixInitialize(VehicleWindowHeaderView __instance, VehicleWindow window)
        {
            var titleContainer = __instance.transform.Find("Title");

            StorageViewHelper.InstantinateStoragesViewForVehicleWindow(titleContainer.transform, window.Vehicle);
            return true;
        }
    }
}
