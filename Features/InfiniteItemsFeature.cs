using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BaldiPowerToys.Features
{
    public class InfiniteItemsFeature : Feature
    {
        private const string FEATURE_ID = "infinite_items";
        public static new ConfigEntry<bool> IsEnabled { get; private set; } = null!;
        private bool _isActive = false;
        private bool _wasEnabled = true;

        private static readonly Color EnabledBarColor = new Color(0.2f, 0.8f, 0.4f);
        private static readonly Color DisabledBarColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        void Awake()
        {
            IsEnabled = Plugin.PublicConfig.Bind("InfiniteItems", "Enabled", false, "Enable the Infinite Items feature.");
        }

        public override void Update()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
                return;
                
            if (_wasEnabled && !IsEnabled.Value)
            {
                _isActive = false;
            }
            _wasEnabled = IsEnabled.Value;

            if (!IsEnabled.Value) return;

            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                _isActive = !_isActive;
                string status = _isActive 
                    ? (PowerToys.IsCyrillicPlusLoaded ? "<color=#90FF90>ВКЛ</color>" : "<color=#90FF90>ON</color>")
                    : (PowerToys.IsCyrillicPlusLoaded ? "<color=#FF8080>ВЫКЛ</color>" : "<color=#FF8080>OFF</color>");

                string featureName = PowerToys.IsCyrillicPlusLoaded ? "Бесконечные предметы" : "Infinite Items";
                string message = $"{featureName}: {status}";

                PowerToys.ShowNotification(
                    message,
                    duration: 1.2f,
                    barColor: _isActive ? EnabledBarColor : DisabledBarColor,
                    backgroundColor: BackgroundColor,
                    sourceId: FEATURE_ID
                );
            }
        }

        [HarmonyPatch(typeof(ItemManager))]
        class ItemManager_UseItem_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("UseItem")]
            static bool Prefix(ItemManager __instance)
            {
                var feature = PowerToys.GetInstance<InfiniteItemsFeature>();
                if (feature != null && IsEnabled.Value && feature._isActive)
                {
                    var disabledField = typeof(ItemManager).GetField("disabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool disabled = (bool)(disabledField?.GetValue(__instance) ?? false);
                    
                    var currentItem = __instance.items[__instance.selectedItem];
                    
                    if ((!disabled || (currentItem.overrideDisabled && __instance.maxItem >= 0)))
                    {
                        var itemInstance = Object.Instantiate(currentItem.item);
                        if (itemInstance.Use(__instance.pm))
                        {}
                    }
                    
                    return false;
                }
                
                return true;
            }
        }

        [HarmonyPatch(typeof(ItemManager))]
        class ItemManager_Remove_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Remove")]
            static bool Prefix(ItemManager __instance, Items itemToRemove)
            {
                var feature = PowerToys.GetInstance<InfiniteItemsFeature>();
                if (feature != null && IsEnabled.Value && feature._isActive)
                {
                    return false;
                }
                
                return true;
            }
        }
    }
}