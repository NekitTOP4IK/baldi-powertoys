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
        
        private static System.Collections.Generic.HashSet<GameObject> _spawnedItems = new System.Collections.Generic.HashSet<GameObject>();

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
                    ? (PowerToys.IsRussian ? "<color=#90FF90>ВКЛ</color>" : "<color=#90FF90>ON</color>")
                    : (PowerToys.IsRussian ? "<color=#FF8080>ВЫКЛ</color>" : "<color=#FF8080>OFF</color>");

                string featureName = PowerToys.IsRussian ? "Бесконечные предметы" : "Infinite Items";
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
                        _spawnedItems.Add(itemInstance.gameObject);
                        
                        bool useResult = itemInstance.Use(__instance.pm);
                        
                        if (useResult)
                        {
                            var postUseMethod = itemInstance.GetType().GetMethod("PostUse", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            if (postUseMethod != null)
                            {
                                postUseMethod.Invoke(itemInstance, new object[] { __instance.pm });
                            }
                        }
                        else
                        {
                            _spawnedItems.Remove(itemInstance.gameObject);
                        }
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

        [HarmonyPatch(typeof(Object), "Destroy", new System.Type[] { typeof(Object) })]
        class Object_Destroy_Patch
        {
            [HarmonyPrefix]
            static bool Prefix(Object obj)
            {
                var feature = PowerToys.GetInstance<InfiniteItemsFeature>();
                if (feature != null && IsEnabled.Value && feature._isActive)
                {
                    if (obj is GameObject gameObject)
                    {
                        if (_spawnedItems.Contains(gameObject))
                        {
                            var item = gameObject.GetComponent<Item>();
                            if (item != null)
                            {
                                _spawnedItems.Remove(gameObject);
                                gameObject.SetActive(false);
                                feature.StartCoroutine(feature.ReactivateItem(gameObject));
                                return false;
                            }
                        }
                    }
                }
                
                return true;
            }
        }

        private System.Collections.IEnumerator ReactivateItem(GameObject item)
        {
            yield return null;
            if (item != null)
            {
                item.SetActive(true);
            }
        }
    }
}