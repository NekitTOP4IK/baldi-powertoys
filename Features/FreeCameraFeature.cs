using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI.PlusExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using BaldiPowerToys.Utils;

namespace BaldiPowerToys.Features
{
    public class FreeCameraFeature : Feature
    {
        private const string FEATURE_ID = "free_camera";
        
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<float> _configSensitivity = null!;
        private static ConfigEntry<KeyCode> _configToggleKey = null!;

        private static bool _isCameraActive;

        private static readonly Color EnabledBarColor = new Color(0.2f, 0.6f, 1f);
        private static readonly Color DisabledBarColor = new Color(0.6f, 0.1f, 0.2f);
        private static readonly Color BackgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.95f);

        public override void Init(Harmony harmony)
        {
            _configIsEnabled = PowerToys.Config.Bind("FreeCamera", "Enabled", true, "Enable/disable the 3D camera feature.");
            _configSensitivity = PowerToys.Config.Bind("FreeCamera", "Sensitivity", 1f, "Mouse sensitivity for the 3D camera.");
            _configToggleKey = PowerToys.Config.Bind("FreeCamera", "ToggleKey", KeyCode.F,
                KeyCodeUtils.GetEssentialKeyCodeDescription("Клавиша для переключения свободной камеры"));

            _isCameraActive = false;

            harmony.PatchAll(typeof(CameraPatch));
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
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                if (_isCameraActive)
                {
                    _isCameraActive = false;
                }
                return;
            }

            if (!_configIsEnabled.Value)
            {
                if (_isCameraActive)
                {
                    _isCameraActive = false;
                    ShowNotification();
                }
                return;
            }

            if (Input.GetKeyDown(_configToggleKey.Value))
            {
                _isCameraActive = !_isCameraActive;
                ShowNotification();
            }
        }

        public void SetFreeCameraActive(bool active)
        {
            _isCameraActive = active;
        }
        
        public bool IsFreeCameraActive()
        {
            return _isCameraActive;
        }
        
        private void ShowNotification()
        {
            string cameraText = PowerToys.IsCyrillicPlusLoaded ? "3D Камера" : "3D Camera";
            string statusText = _isCameraActive 
                ? (PowerToys.IsCyrillicPlusLoaded ? "Включена" : "Enabled")
                : (PowerToys.IsCyrillicPlusLoaded ? "Выключена" : "Disabled");

            string status = _isCameraActive
                ? $"<color=#80D5FF><b>{statusText}</b></color>"
                : $"<color=#FF6060><b>{statusText}</b></color>";

            string keyHint = $"[{_configToggleKey.Value}]";
            
            string message = $"{cameraText} {status}\n<size=16><color=#888888>{keyHint}</color></size>";

            PowerToys.ShowNotification(
                message,
                duration: 1.0f,
                barColor: _isCameraActive ? EnabledBarColor : DisabledBarColor,
                backgroundColor: BackgroundColor,
                sourceId: FEATURE_ID
            );
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
                if (!_configIsEnabled.Value || !_isCameraActive || !__instance.Controllable) return;

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
