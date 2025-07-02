using BepInEx;
using BaldiPowerToys.Features;
using BaldiPowerToys.Settings;
using MTM101BaldAPI.OptionsAPI;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections;
using MTM101BaldAPI;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace BaldiPowerToys
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigFile PublicConfig { get; private set; } = null!;
        public static bool IsCyrillicPlusLoaded { get; private set; }
        public static Font? ComicSans { get; private set; }

        void Awake()
        {
            PublicConfig = Config;
            IsCyrillicPlusLoaded = Chainloader.PluginInfos.ContainsKey("blayms.tbb.baldiplus.cyrillic");

            try
            {
                ComicSans = Font.CreateDynamicFontFromOSFont("Comic Sans MS", 28);
            }
            catch
            {
                ComicSans = null;
            }

            GameObject featureHolder = new GameObject("BaldiPowerToys_Features");
            DontDestroyOnLoad(featureHolder);

            PowerToys.Init(Config, IsCyrillicPlusLoaded, featureHolder);

            featureHolder.AddComponent<QuickNextLevelFeature>();
            featureHolder.AddComponent<QuickFillMapFeature>();
            featureHolder.AddComponent<QuickResultsFeature>();
            featureHolder.AddComponent<GiveMoneyFeature>();
            featureHolder.AddComponent<NoIncorrectAnswersFeature>();
            featureHolder.AddComponent<AdjustPlayerSpeedFeature>();
            featureHolder.AddComponent<InfiniteStaminaFeature>();
            featureHolder.AddComponent<FreeCameraFeature>();

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            foreach (var feature in featureHolder.GetComponents<Feature>())
            {
                feature.Init(harmony);
            }
            
            harmony.PatchAll();

            StartCoroutine(WaitForAPI());

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        private static object? GetAssetManager()
        {
            FieldInfo field = typeof(MTM101BaldiDevAPI).GetField("AssetMan", BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                return field.GetValue(null);
            }
            return null;
        }

        private IEnumerator WaitForAPI()
        {
            yield return new WaitUntil(() => GetAssetManager() != null);
            CustomOptionsCore.OnMenuInitialize += OnMenuInitialize;
        }

        void OnMenuInitialize(OptionsMenu menu, CustomOptionsHandler handler)
        {
            handler.AddCategory<PowerToysSettingsCategory>("PowerToys");
        }
    }

    [HarmonyPatch(typeof(Map), "Initialize")]
    class Map_Initialize_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            QuickFillMapFeature.MapWasFilled = false;
        }
    }
}