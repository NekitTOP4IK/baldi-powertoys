using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;
using BaldiPowerToys.UI;
using BaldiPowerToys.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BaldiPowerToys.Features
{
    public class QuickNextLevelFeature : MonoBehaviour
    {
        private ConfigEntry<bool> _isEnabled = null!;
        private ConfigEntry<KeyCode> _nextLevelKey = null!;

        private bool _confirmationPending;
        private float _confirmationTimer;
        private const float ConfirmationTimeout = 5f;
        private const string FEATURE_ID = "quick_next_level";
        
        private bool _isLoadingNextLevel;

        void Awake()
        {
            _isEnabled = Plugin.PublicConfig.Bind("QuickNextLevel", "Enabled", false, "Enable the Quick Next Level feature.");
            _nextLevelKey = PowerToys.Config.Bind("QuickNextLevel", "NextLevelKey", KeyCode.Quote,
                KeyCodeUtils.GetEssentialKeyCodeDescription("Клавиша для перехода на следующий уровень"));

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }

        private void OnSceneChanged(Scene current, Scene next)
        {
            if (next.name == "MainMenu")
            {
                _confirmationPending = false;
                _isLoadingNextLevel = false;
                PowerToysNotification.Instance.Hide(FEATURE_ID);
            }
            else
            {
                _isLoadingNextLevel = false;
            }
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
                return;
                
            if (!_isEnabled.Value || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused))
            {
                return;
            }

            if (Singleton<CoreGameManager>.Instance != null && !Singleton<CoreGameManager>.Instance.readyToStart)
            {
                return;
            }

            if (Input.GetKeyDown(_nextLevelKey.Value))
            {
                if (_isLoadingNextLevel)
                {
                    string message = PowerToys.IsRussian 
                        ? "<color=yellow><b>Загрузка...</b></color>\nПодождите, пока загрузится следующий уровень."
                        : "<color=yellow><b>Loading...</b></color>\nPlease wait until the next level loads.";
                    
                    PowerToys.ShowInfo(message, 2f, FEATURE_ID);
                    return;
                }

                if (IsSkippingDisallowed())
                {
                    ShowWarning();
                    _confirmationPending = false;
                    return;
                }

                if (_confirmationPending)
                {
                    _confirmationPending = false;
                    PowerToysNotification.Instance.Hide(FEATURE_ID);
                    SkipLevel();
                }
                else
                {
                    ShowConfirmation();
                }
            }
            
            if (_confirmationPending)
            {
                _confirmationTimer -= Time.deltaTime;
                if (_confirmationTimer <= 0)
                {
                    _confirmationPending = false;
                    PowerToysNotification.Instance.Hide(FEATURE_ID);
                }
            }
        }
        
        private void ShowConfirmation()
        {
            _confirmationPending = true;
            _confirmationTimer = ConfirmationTimeout;
            
            string keyName = GetLocalizedKeyName(_nextLevelKey.Value);
            string message = PowerToys.IsRussian 
                ? $"Нажмите <color=yellow>{keyName}</color> ещё раз, чтобы пропустить уровень\nЭто автоматически завершит уровень."
                : $"Press <color=yellow>{keyName}</color> again to skip level\nThis will autocomplete the level.";
            
            PowerToys.ShowConfirm(message, ConfirmationTimeout, FEATURE_ID);
        }
        
        private void ShowWarning()
        {
            string message = PowerToys.IsRussian 
                ? "<color=red><b>ВНИМАНИЕ!</b></color>\nНевозможно пропустить этот уровень или в это время."
                : "<color=red><b>WARNING!</b></color>\nCannot skip this level or at this time.";
            
            PowerToys.ShowError(message, 3f, FEATURE_ID);
        }
        
        private void SkipLevel()
        {
            _isLoadingNextLevel = true;
            
            string message = PowerToys.IsRussian 
                ? "<color=#4CFF4C><b>Уровень пропущен!</b></color>"
                : "<color=#4CFF4C><b>Level skipped!</b></color>";
            
            PowerToys.ShowSuccess(message, 0.2f, FEATURE_ID);
            
            Invoke("LoadNextLevel", 0.6f);
        }
        
        private void LoadNextLevel()
        {
            Singleton<BaseGameManager>.Instance.LoadNextLevel();
        }
        
        private bool IsSkippingDisallowed()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
                return true;

            var gm = Singleton<BaseGameManager>.Instance;
            if (gm == null)
                return true;

            if (gm is MainGameManager || gm is TutorialGameManager || gm is SpeedyChallengeManager || gm is GrappleChallengeManager || gm is StealthyChallengeManager || gm is PitstopGameManager)
            {
                var lb = FindObjectOfType<LevelBuilder>();
                if (lb != null && lb.levelAsset != null)
                {
                    string assetName = lb.levelAsset.name;
                    if ((assetName.Contains("YAY") && assetName.Contains("HideNSeek")) || assetName.Contains("PlaceholderEnding"))
                    {
                        return true;
                    }
                }
                return false;
            }

            return true;
        }

        private string GetLocalizedKeyName(KeyCode key)
        {
            if (PowerToys.IsRussian && RussianKeyMap.TryGetValue(key, out var localizedName))
            {
                return $"<color=yellow>{localizedName}</color>";
            }
            return $"<color=yellow>{key}</color>";
        }

        private static readonly Dictionary<KeyCode, string> RussianKeyMap = new Dictionary<KeyCode, string>
        {
            { KeyCode.Quote, "Кавычки" },
            { KeyCode.Return, "Ввод" },
            { KeyCode.KeypadEnter, "Ввод" },
            { KeyCode.Escape, "Escape" },
            { KeyCode.Space, "Пробел" },
            { KeyCode.LeftShift, "Левый Shift" },
            { KeyCode.RightShift, "Правый Shift" },
            { KeyCode.LeftAlt, "Левый Alt" },
            { KeyCode.RightAlt, "Правый Alt" },
            { KeyCode.LeftControl, "Левый Ctrl" },
            { KeyCode.RightControl, "Правый Ctrl" }
        };
    }
}