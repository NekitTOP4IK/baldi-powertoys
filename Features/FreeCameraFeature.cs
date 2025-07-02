using BaldiPowerToys.Utils;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI.PlusExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaldiPowerToys.Features
{
    public class FreeCameraFeature : Feature
    {
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<float> _configSensitivity = null!;
        private static ConfigEntry<KeyCode> _configToggleKey = null!;

        private static bool _isFeatureEnabled;

        private bool _isShowingNotification;
        private float _animationProgress;
        private float _notificationTimer;
        private const float NotificationTimeout = 2f;

        private GUIStyle? _notificationStyle;
        private Texture2D? _backgroundTexture;
        private Texture2D? _borderTexture;

        public override void Init(Harmony harmony)
        {
            _configIsEnabled = PowerToys.Config.Bind("FreeCamera", "Enabled", true, "Enable/disable the 3D camera feature.");
            _configSensitivity = PowerToys.Config.Bind("FreeCamera", "Sensitivity", 1f, "Mouse sensitivity for the 3D camera.");
            _configToggleKey = PowerToys.Config.Bind("FreeCamera", "ToggleKey", KeyCode.F1, "Key to toggle the 3D camera.");

            _isFeatureEnabled = _configIsEnabled.Value;

            harmony.PatchAll(typeof(CameraPatch));

            _configIsEnabled.SettingChanged += (_, __) =>
            {
                _isFeatureEnabled = _configIsEnabled.Value;
            };

            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(Scene current, Scene next)
        {
            if (next.name == "MainMenu" || next.name == "Game")
            {
                CameraPatch.ResetRotation();
            }
        }

        public override void Update()
        {
            if (!_configIsEnabled.Value)
            {
                _isFeatureEnabled = false;
                return;
            }

            if (Input.GetKeyDown(_configToggleKey.Value))
            {
                _isFeatureEnabled = !_isFeatureEnabled;
                ShowNotification();
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
                    normal = { textColor = Color.white }
                };
                CreateTextures();
            }

            string enabledText = PowerToys.IsRussian ? "Включена" : "Enabled";
            string disabledText = PowerToys.IsRussian ? "Выключена" : "Disabled";
            string featureName = PowerToys.IsRussian ? "3D Камера" : "3D Camera";
            string color = _isFeatureEnabled ? "green" : "red";
            string statusText = _isFeatureEnabled ? enabledText : disabledText;
            string text = $"{featureName}: <color={color}>{statusText}</color>";

            Vector2 textSize = _notificationStyle.CalcSize(new GUIContent(text));
            float width = textSize.x + 40f;
            float height = 60f;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            float startY = screenHeight;
            float endY = screenHeight - height - 20f;
            float currentY = Mathf.Lerp(startY, endY, _animationProgress);

            Rect rect = new Rect((screenWidth - width) / 2f, currentY, width, height);

            if (_borderTexture != null) GUI.DrawTexture(rect, _borderTexture);
            if (_backgroundTexture != null) GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f), _backgroundTexture);

            Rect textRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            GuiUtils.DrawTextWithShadow(textRect, text, _notificationStyle);
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
        }

        [HarmonyPatch(typeof(GameCamera))]
        private class CameraPatch
        {
            private static float xRotation;
            private static PlayerMovement? playerMovement;

            public static void ResetRotation()
            {
                xRotation = 0f;
            }

            [HarmonyPostfix]
            [HarmonyPatch("LateUpdate")]
            private static void LateUpdate_Postfix(GameCamera __instance)
            {
                if (!_isFeatureEnabled || !__instance.Controllable) return;

                if (playerMovement == null)
                {
                    playerMovement = Object.FindObjectOfType<PlayerMovement>();
                    if (playerMovement == null) return;
                }

                Singleton<InputManager>.Instance.GetAnalogInput(playerMovement.cameraAnalogData, out _, out Vector2 deltaVector, 0.1f);

                float sensitivity = _configSensitivity.Value * Singleton<PlayerFileManager>.Instance.mouseCameraSensitivity;
                float mouseY = deltaVector.y * sensitivity;

                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -89f, 89f);

                __instance.transform.rotation = Quaternion.Euler(xRotation, __instance.transform.eulerAngles.y, 0f);

                if (__instance.listenerTra == null) return;
                
                bool audioReversed = Traverse.Create(__instance).Field<bool>("audioReversed").Value;
                if (!audioReversed)
                {
                    __instance.listenerTra.transform.localEulerAngles = __instance.transform.eulerAngles;
                }
                else
                {
                    __instance.listenerTra.transform.localEulerAngles = __instance.transform.eulerAngles + Vector3.forward * 180f;
                }
            }
        }
    }
}
