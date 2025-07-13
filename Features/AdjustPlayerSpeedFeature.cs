using BaldiPowerToys.Utils;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.PlusExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaldiPowerToys.Features
{
    public class AdjustPlayerSpeedFeature : Feature
    {
        private const string FEATURE_ID = "adjust_speed";
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<float> _configSpeedIncrement = null!;
        private static float _speedMultiplier = 1.0f;

        private static readonly ValueModifier _walkSpeedModifier = new ValueModifier(0f, 1f);
        private static readonly ValueModifier _runSpeedModifier = new ValueModifier(0f, 1f);
        private static bool _modifiersApplied;
        private static bool _levelReady;

        private static readonly Color SpeedBarColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color SpeedBgColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        public override void Init(Harmony harmony)
        {
            _configIsEnabled = PowerToys.Config.Bind("AdjustPlayerSpeed", "Enabled", true, "Enable/disable the player speed adjustment feature.");
            _configSpeedIncrement = PowerToys.Config.Bind("AdjustPlayerSpeed", "SpeedIncrement", 0.1f, "The amount to increase/decrease speed by.");
            SceneManager.activeSceneChanged += OnSceneChanged;
            Debug.Log("[AdjustPlayerSpeed] Feature initialized.");
        }

        private void OnSceneChanged(Scene current, Scene next)
        {
            Debug.Log($"[AdjustPlayerSpeed] Scene changed from '{current.name}' to '{next.name}'.");
            _modifiersApplied = false;
            _levelReady = false;

            PowerToys.ClearNotifications();
            
            if (next.name == "MainMenu")
            {
                _speedMultiplier = 1.0f;
                RemoveSpeedModifiers();
                PowerToys.ClearNotifications();
                Debug.Log("[AdjustPlayerSpeed] Returned to MainMenu, speed reset to 1.0.");
            }
            
            Debug.Log($"[AdjustPlayerSpeed] Modifiers flag and level ready flag reset. Current speed multiplier: {_speedMultiplier:F2}");
        }

        public override void Update()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
                return;

            if (!_configIsEnabled.Value)
            {
                RemoveSpeedModifiers();
                return;
            }

            var speedChanged = false;
            var increased = false;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _speedMultiplier += _configSpeedIncrement.Value;
                speedChanged = true;
                increased = true;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _speedMultiplier = Mathf.Max(0.1f, _speedMultiplier - _configSpeedIncrement.Value);
                speedChanged = true;
                increased = false;
            }

            if (speedChanged)
            {
                Debug.Log($"[AdjustPlayerSpeed] Speed manually changed to: {_speedMultiplier:F2}x");
                
                string speedValue = $"<color=#FFE065><b>{_speedMultiplier:F1}×</b></color>";
                string arrow = increased ? "↑" : "↓";
                
                string message = PowerToys.IsCyrillicPlusLoaded
                    ? $"<b>Скорость {arrow}</b>\n{speedValue}"
                    : $"<b>Speed {arrow}</b>\n{speedValue}";
                
                PowerToys.ShowNotification(
                    message,
                    duration: 1.0f,
                    barColor: SpeedBarColor,
                    backgroundColor: SpeedBgColor,
                    sourceId: FEATURE_ID
                );
            }

            if (_levelReady && _speedMultiplier != 1.0f)
            {
                ApplyOrUpdateSpeedModifiers();
            }
            else
            {
                RemoveSpeedModifiers();
            }
        }

        private void ApplyOrUpdateSpeedModifiers()
        {
            if (Singleton<CoreGameManager>.Instance == null) return;
            var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (player == null) return;

            var statModifier = player.GetMovementStatModifier();

            _walkSpeedModifier.multiplier = _speedMultiplier;
            _runSpeedModifier.multiplier = _speedMultiplier;

            if (_modifiersApplied) return;
            
            Debug.Log($"[AdjustPlayerSpeed] Applying speed modifiers. Multiplier: {_speedMultiplier:F2}");
            statModifier.AddModifier("walkSpeed", _walkSpeedModifier);
            statModifier.AddModifier("runSpeed", _runSpeedModifier);
            _modifiersApplied = true;
            Debug.Log("[AdjustPlayerSpeed] Modifiers applied successfully.");
        }

        public static void OnLevelReady()
        {
            _levelReady = true;
            Debug.Log("[AdjustPlayerSpeed] Level is ready! Speed modifiers can now be applied.");
        }

        private void RemoveSpeedModifiers()
        {
            if (!_modifiersApplied) return;
            if (Singleton<CoreGameManager>.Instance == null) return;

            var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (player == null)
            {
                _modifiersApplied = false;
                Debug.LogWarning("[AdjustPlayerSpeed] Could not get Player to remove modifiers, but flagging as removed anyway.");
                return;
            }

            Debug.Log("[AdjustPlayerSpeed] Removing speed modifiers.");
            var statModifier = player.GetMovementStatModifier();

            statModifier.RemoveModifier(_walkSpeedModifier);
            statModifier.RemoveModifier(_runSpeedModifier);
            _modifiersApplied = false;
            Debug.Log("[AdjustPlayerSpeed] Modifiers removed successfully.");
        }
    }
}
