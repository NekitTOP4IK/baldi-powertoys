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
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<float> _configSpeedIncrement = null!;

        private static float _speedMultiplier = 1.0f;
        private bool _isShowingNotification;
        private float _animationProgress;
        private float _notificationTimer;
        private const float NotificationTimeout = 2f;

        private GUIStyle? _notificationStyle;
        private Texture2D? _backgroundTexture;
        private Texture2D? _borderTexture;
        private Texture2D? _timerBarBackgroundTexture;
        private Texture2D? _timerBarFillTexture;
        private const float BorderWidth = 2f;

        private static readonly ValueModifier _walkSpeedModifier = new ValueModifier(0f, 1f);
        private static readonly ValueModifier _runSpeedModifier = new ValueModifier(0f, 1f);
        private static bool _modifiersApplied;
        private static bool _levelReady;

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
            Debug.Log($"[AdjustPlayerSpeed] Modifiers flag and level ready flag reset. Current speed multiplier: {_speedMultiplier:F2}");

            if (next.name == "MainMenu")
            {
                _speedMultiplier = 1.0f;
                Debug.Log("[AdjustPlayerSpeed] Returned to MainMenu, speed multiplier reset to 1.0.");
            }
        }

        public override void Update()
        {
            if (!_configIsEnabled.Value)
            {
                RemoveSpeedModifiers();
                return;
            }

            var speedChanged = false;
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _speedMultiplier += _configSpeedIncrement.Value;
                speedChanged = true;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _speedMultiplier = Mathf.Max(0.1f, _speedMultiplier - _configSpeedIncrement.Value);
                speedChanged = true;
            }

            if (speedChanged)
            {
                Debug.Log($"[AdjustPlayerSpeed] Speed manually changed to: {_speedMultiplier:F2}x");
                ShowNotification();
            }

            if (_levelReady && _speedMultiplier != 1.0f)
            {
                ApplyOrUpdateSpeedModifiers();
            }
            else
            {
                RemoveSpeedModifiers();
            }

            UpdateNotificationAnimation();
        }

        public override void OnGUI()
        {
            if (!_isShowingNotification || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused)) return;

            if (_notificationStyle == null)
            {
                _notificationStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
            }

            if (_backgroundTexture == null || _borderTexture == null)
            {
                CreateTextures();
            }

            var text = PowerToys.IsRussian
                ? $"<color=#80ff80>Скорость</color> <color=yellow>{_speedMultiplier:F1}x</color>"
                : $"<color=#80ff80>Speed</color> <color=yellow>{_speedMultiplier:F1}x</color>";

            const float width = 280;
            const float height = 60;

            var onScreenY = Screen.height - height - 30;
            var offScreenY = Screen.height;

            var easedProgress = Mathf.SmoothStep(0f, 1f, _animationProgress);
            var currentY = Mathf.Lerp(offScreenY, onScreenY, easedProgress);

            var rect = new Rect((Screen.width - width) / 2, currentY, width, height);

            GuiUtils.DrawBoxWithBorder(rect, _backgroundTexture!, _borderTexture!);

            var textRect = rect;
            textRect.y -= (5f + BorderWidth) / 2f;

            GuiUtils.DrawTextWithShadow(textRect, text, _notificationStyle);

            if (!(_notificationTimer > 0)) return;
            
            var timerRect = new Rect(rect.x + BorderWidth, rect.yMax - BorderWidth - 5, rect.width - (BorderWidth * 2), 5);
            DrawTimerBar(timerRect, Mathf.Max(0f, _notificationTimer) / NotificationTimeout);
        }

        private void ShowNotification()
        {
            _isShowingNotification = true;
            _notificationTimer = NotificationTimeout;
        }

        private void UpdateNotificationAnimation()
        {
            if (_notificationTimer > 0)
            {
                _notificationTimer -= Time.deltaTime;
                _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * 4f);
            }
            else
            {
                _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * 4f);
                if (_animationProgress == 0f)
                {
                    _isShowingNotification = false;
                }
            }
        }

        private void CreateTextures()
        {
            _backgroundTexture = GuiUtils.CreateGradientTexture(128, new Color(0.2f, 0.2f, 0.2f, 0.95f), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            _borderTexture = GuiUtils.CreateSolidTexture(new Color(0.8f, 0.8f, 0.8f, 0.7f));
            _timerBarBackgroundTexture = GuiUtils.CreateSolidTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));
            _timerBarFillTexture = GuiUtils.CreateSolidTexture(new Color(1f, 0.9f, 0.2f));
        }

        private void DrawTimerBar(Rect rect, float progress)
        {
            if (_timerBarBackgroundTexture == null || _timerBarFillTexture == null) return;
            GUI.DrawTexture(rect, _timerBarBackgroundTexture);
            var fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            GUI.DrawTexture(fillRect, _timerBarFillTexture);
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
