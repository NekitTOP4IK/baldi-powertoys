using BepInEx.Configuration; 
using UnityEngine;
using UnityEngine.SceneManagement;
using BaldiPowerToys.UI;
using BaldiPowerToys.Utils;
using HarmonyLib;

namespace BaldiPowerToys.Features
{
    public class QuickFillMapFeature : MonoBehaviour
    {
        private ConfigEntry<bool> _isEnabled = null!;
        private ConfigEntry<KeyCode> _fillMapKey = null!;
        
        private const float ConfirmationTimeout = 5f;
        private float _confirmationTimer;
        private bool _isConfirming;
        private const string FEATURE_ID = "map_fill";
        
        public static bool MapWasFilled;
        private Map? _currentMap;

        void Awake()
        {
            _isEnabled = Plugin.PublicConfig.Bind("QuickFillMap", "Enabled", false, "Enable the Quick Fill Map feature.");
            _fillMapKey = Plugin.PublicConfig.Bind("QuickFillMap", "FillMapKey", KeyCode.U, "Key to trigger the map fill.");
            
            SceneManager.activeSceneChanged += OnSceneChanged;
        }
        
        void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
        
        private void OnSceneChanged(Scene current, Scene next)
        {
            MapWasFilled = false;
            _currentMap = null;
            _isConfirming = false;
            PowerToysNotification.Instance.Hide(FEATURE_ID);
        }

        void Update()
        {
            if (!_isEnabled.Value) return;

            var manager = Singleton<CoreGameManager>.Instance;
            if (manager == null || manager.Paused || manager.MapOpen)
            {
                CancelConfirmation();
                return;
            }

            if (_isConfirming)
            {
                _confirmationTimer -= Time.deltaTime;
                if (_confirmationTimer <= 0)
                {
                    CancelConfirmation();
                }
            }

            if (Input.GetKeyDown(_fillMapKey.Value))
            {
                if (_isConfirming)
                {
                    _isConfirming = false;
                    FillMap();
                }
                else
                {
                    if (MapWasFilled)
                    {
                        PowerToys.ShowError(
                            PowerToys.IsCyrillicPlusLoaded ? "Карта уже заполнена!" : "Map is already filled!",
                            1.0f,
                            FEATURE_ID
                        );
                    }
                    else
                    {
                        StartConfirmation();
                    }
                }
            }
        }

        private void StartConfirmation()
        {
            _isConfirming = true;
            _confirmationTimer = ConfirmationTimeout;
            
            string keyName = _fillMapKey.Value.ToString();
            string message = PowerToys.IsCyrillicPlusLoaded
                ? $"Нажмите <color=yellow>{keyName}</color> снова чтобы заполнить карту"
                : $"Press <color=yellow>{keyName}</color> again to fill the map";
                
            PowerToys.ShowConfirm(message, ConfirmationTimeout, FEATURE_ID);
        }

        private void CancelConfirmation()
        {
            if (_isConfirming)
            {
                _isConfirming = false;
                PowerToysNotification.Instance.Hide(FEATURE_ID);
            }
        }

        private void FillMap()
        {
            if (!MapWasFilled)
            {
                _currentMap ??= FindObjectOfType<Map>();
                if (_currentMap != null)
                {
                    _currentMap.CompleteMap();
                    MapWasFilled = true;
                    
                    PowerToysNotification.Instance.Hide(FEATURE_ID);
                    
                    PowerToys.ShowSuccess(
                        PowerToys.IsCyrillicPlusLoaded ? "Карта заполнена!" : "Map filled!",
                        1.0f,
                        FEATURE_ID
                    );
                }
            }
        }
    }
}
