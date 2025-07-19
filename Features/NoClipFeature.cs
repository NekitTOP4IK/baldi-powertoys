using System;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.PlusExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BaldiPowerToys.Features
{
    public class NoClipFeature : Feature
    {
        private const string FEATURE_ID = "noclip";
        
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<KeyCode> _configToggleKey = null!;
        private static ConfigEntry<float> _configSpeed = null!;
        
        internal static bool _isNoClipActive = false;
        private static bool _originalDetectCollisions = true;
        private static PlayerMovement _playerMovement = null!;
        private static CharacterController _characterController = null!;
        private static Entity _playerEntity = null!;
        private static FreeCameraFeature _freeCameraFeature = null!;
        
        private static readonly Color EnabledBarColor = new Color(0.2f, 1f, 0.2f);
        private static readonly Color DisabledBarColor = new Color(1f, 0.2f, 0.2f);
        private static readonly Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        public override void Init(Harmony harmony)
        {
            _configIsEnabled = PowerToys.Config.Bind("NoClip", "Enabled", false, "Enable/disable the NoClip feature.");
            _configToggleKey = PowerToys.Config.Bind("NoClip", "ToggleKey", KeyCode.N, "Key to toggle NoClip mode.");
            _configSpeed = PowerToys.Config.Bind("NoClip", "Speed", 10f, "Movement speed in NoClip mode.");
            
            harmony.PatchAll(typeof(NoClipPatches));
            
            SceneManager.activeSceneChanged += OnSceneChanged;
            
            _freeCameraFeature = PowerToys.GetInstance<FreeCameraFeature>();
            
            Debug.Log("[NoClip] Feature initialized.");
        }
        
        private void OnSceneChanged(Scene current, Scene next)
        {
            if (_isNoClipActive)
            {
                DisableNoClip();
            }
            
            _playerMovement = null!;
            _characterController = null!;
            _playerEntity = null!;
            
            Debug.Log($"[NoClip] Scene changed from '{current.name}' to '{next.name}', NoClip disabled.");
        }
        
        public override void Update()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
                return;
                
            if (!_configIsEnabled.Value)
            {
                if (_isNoClipActive)
                {
                    DisableNoClip();
                }
                return;
            }
            
            InitializePlayerReferences();
            
            if (_configToggleKey.Value != KeyCode.None && Input.GetKeyDown(_configToggleKey.Value))
            {
                ToggleNoClip();
                ShowNotification();
            }
            
            if (_isNoClipActive && _playerMovement != null)
            {
                HandleNoClipMovement();
            }
        }
        
        private void InitializePlayerReferences()
        {
            if (_playerMovement == null)
            {
                var playerManager = Singleton<CoreGameManager>.Instance?.GetPlayer(0);
                if (playerManager?.plm != null)
                {
                    _playerMovement = playerManager.plm;
                    _characterController = _playerMovement.cc;
                    _playerEntity = _playerMovement.Entity;
                }
            }
        }
        
        private void ToggleNoClip()
        {
            if (_isNoClipActive)
            {
                DisableNoClip();
            }
            else
            {
                EnableNoClip();
            }
        }
        
        private void EnableNoClip()
        {
            if (_characterController == null || _playerEntity == null)
                return;
                
            _isNoClipActive = true;
            
            _originalDetectCollisions = _characterController.detectCollisions;
            
            _characterController.detectCollisions = false;
            
            _playerEntity.SetFrozen(true);
            
            if (_freeCameraFeature != null)
            {
                _freeCameraFeature.SetFreeCameraActive(true);
            }
            
            Debug.Log("[NoClip] NoClip enabled - collisions disabled.");
        }
        
        private void DisableNoClip()
        {
            if (!_isNoClipActive)
                return;
                
            _isNoClipActive = false;
            
            if (_characterController != null)
            {
                _characterController.detectCollisions = _originalDetectCollisions;
            }
            
            if (_playerEntity != null)
            {
                _playerEntity.SetFrozen(false);
                
                Vector3 position = _playerEntity.transform.position;
                position.y = 5f; 
                _playerEntity.transform.position = position;
            }
            
            if (_freeCameraFeature != null)
            {
                _freeCameraFeature.SetFreeCameraActive(false);
            }
            
            Debug.Log("[NoClip] NoClip disabled - collisions restored.");
        }
        
        private void HandleNoClipMovement()
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            Vector3 up = Vector3.up; 
            
            Vector3 movementInput = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) movementInput += forward;
            if (Input.GetKey(KeyCode.S)) movementInput -= forward;
            if (Input.GetKey(KeyCode.A)) movementInput -= right;
            if (Input.GetKey(KeyCode.D)) movementInput += right;
            if (Input.GetKey(KeyCode.Space)) movementInput += up;     
            if (Input.GetKey(KeyCode.LeftShift)) movementInput -= up; 
            
            if (movementInput.magnitude > 1f)
                movementInput.Normalize();
            
            float baseSpeed = _configSpeed.Value;
            float speedMultiplier = AdjustPlayerSpeedFeature.SpeedMultiplier;
            float finalSpeed = baseSpeed * speedMultiplier;
            Vector3 movement = movementInput * finalSpeed * Time.deltaTime;
            
            if (movement.magnitude > 0.001f)
            {
                Vector3 newPosition = _playerMovement.transform.position + movement;
                _playerMovement.transform.position = newPosition;
                
                _playerEntity.transform.position = newPosition;
            }
        }
        
        private void ShowNotification()
        {
            string featureName = PowerToys.IsCyrillicPlusLoaded ? "NoClip" : "NoClip";
            string statusText = _isNoClipActive 
                ? (PowerToys.IsCyrillicPlusLoaded ? "Включен" : "Enabled")
                : (PowerToys.IsCyrillicPlusLoaded ? "Выключен" : "Disabled");
                
            string status = _isNoClipActive
                ? $"<color=#90FF90><b>{statusText}</b></color>"
                : $"<color=#FF8080><b>{statusText}</b></color>";
                
            string message = $"{featureName}: {status}";
            
            PowerToys.ShowNotification(
                message,
                duration: 1.5f,
                barColor: _isNoClipActive ? EnabledBarColor : DisabledBarColor,
                backgroundColor: BackgroundColor,
                sourceId: FEATURE_ID
            );
        }
        
        protected override void OnFeatureDisabled()
        {
            DisableNoClip();
        }
        
        protected override void OnCleanup()
        {
            DisableNoClip();
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
    }
    
    [HarmonyPatch]
    public static class NoClipPatches
    {
        [HarmonyPatch(typeof(Entity), "EntityUpdate")]
        [HarmonyPrefix]
        public static bool Entity_EntityUpdate_Prefix(Entity __instance)
        {
            if (NoClipFeature._isNoClipActive && __instance is PlayerEntity)
            {
                return false; 
            }
            return true; 
        }
        
        [HarmonyPatch(typeof(PlayerMovement), "PlayerMove")]
        [HarmonyPrefix]
        public static bool PlayerMovement_PlayerMove_Prefix(PlayerMovement __instance)
        {
            if (NoClipFeature._isNoClipActive)
            {
                return false; 
            }
            return true; 
        }
        
        [HarmonyPatch(typeof(InputManager), "GetDigitalInput", new Type[] { typeof(string), typeof(bool) })]
        [HarmonyPrefix]
        public static bool InputManager_GetDigitalInput_Prefix(string id, bool onDown, ref bool __result)
        {
            if (NoClipFeature._isNoClipActive && id == "LookBack")
            {
                __result = false;
                return false; 
            }
            return true; 
        }
    }
}