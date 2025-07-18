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
using System.Collections.Generic;

namespace BaldiPowerToys
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigFile PublicConfig { get; private set; } = null!;
        public static Font? ComicSans { get; private set; }

        private static readonly WaitUntil WaitForAssetManager;
        private readonly List<Feature> _features = new List<Feature>();
        private GameObject? _featureHolder;

        static Plugin()
        {
            WaitForAssetManager = new WaitUntil(() => GetAssetManager() != null);
        }

        private void Awake()
        {
            PublicConfig = Config;
            InitializeFont();
            InitializeFeatures();
            PatchFeatures();

            StartCoroutine(WaitForAPI());

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void InitializeFont()
        {
            try
            {
                ComicSans = Font.CreateDynamicFontFromOSFont("Comic Sans MS", 28);
            }
            catch
            {
                ComicSans = null;
                Logger.LogWarning("Failed to load Comic Sans MS font");
            }
        }

        private void InitializeFeatures()
        {
            _featureHolder = new GameObject("BaldiPowerToys_Features");
            DontDestroyOnLoad(_featureHolder);

            PowerToys.Init(Config, false, _featureHolder);

            var featureTypes = new[] 
            {
                typeof(QuickNextLevelFeature),
                typeof(QuickFillMapFeature),
                typeof(QuickResultsFeature),
                typeof(GiveMoneyFeature),
                typeof(NoIncorrectAnswersFeature),
                typeof(AdjustPlayerSpeedFeature),
                typeof(InfiniteStaminaFeature),
                typeof(FreeCameraFeature),
                typeof(InfiniteItemsFeature)
            };

            foreach (var featureType in featureTypes)
            {
                if (_featureHolder.AddComponent(featureType) is Feature feature)
                {
                    _features.Add(feature);
                }
            }
        }

        private void PatchFeatures()
        {
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            foreach (var feature in _features)
            {
                feature.Init(harmony);
            }
            
            harmony.PatchAll();
        }

        private static object? GetAssetManager()
        {
            return typeof(MTM101BaldiDevAPI)
                .GetField("AssetMan", BindingFlags.NonPublic | BindingFlags.Static)?
                .GetValue(null);
        }

        private IEnumerator WaitForAPI()
        {
            yield return WaitForAssetManager;
            CustomOptionsCore.OnMenuInitialize += OnMenuInitialize;
        }

        private void OnMenuInitialize(OptionsMenu menu, CustomOptionsHandler handler)
        {
            handler.AddCategory<PowerToysSettingsCategory>("PowerToys");
        }

        private void OnDestroy()
        {
            if (_featureHolder != null)
            {
                foreach (var feature in _features)
                {
                    feature.OnPluginDestroy();
                }
                Destroy(_featureHolder);
            }
            
            CustomOptionsCore.OnMenuInitialize -= OnMenuInitialize;
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