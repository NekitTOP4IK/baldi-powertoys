using HarmonyLib;
using BaldiPowerToys.Features;

namespace BaldiPowerToys.Patches
{
    [HarmonyPatch(typeof(CoreGameManager), "PlayBegins")]
    internal class CoreGameManager_PlayBegins_Patch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            AdjustPlayerSpeedFeature.OnLevelReady();
        }
    }
}
