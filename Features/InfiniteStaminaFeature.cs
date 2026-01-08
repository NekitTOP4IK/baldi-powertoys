using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using UnityEngine;

namespace BaldiPowerToys.Features
{
    public class InfiniteStaminaFeature : Feature
    {
        private const string FEATURE_ID = "infinite_stamina";
        public static new ConfigEntry<bool> IsEnabled { get; private set; } = null!;
        private bool _isActive = false;
        private bool _wasEnabled = true;

        private static readonly Color EnabledBarColor = new Color(0.4f, 0.8f, 0.2f);
        private static readonly Color DisabledBarColor = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        void Awake()
        {
            IsEnabled = Plugin.PublicConfig.Bind("InfiniteStamina", "Enabled", false, "Enable the Infinite Stamina feature.");
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

            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                _isActive = !_isActive;
                string status = _isActive 
                    ? (PowerToys.IsRussian ? "<color=#90FF90>ВКЛ</color>" : "<color=#90FF90>ON</color>")
                    : (PowerToys.IsRussian ? "<color=#FF8080>ВЫКЛ</color>" : "<color=#FF8080>OFF</color>");

                string featureName = PowerToys.IsRussian ? "Бесконечная стамина" : "Infinite Stamina";
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

        [HarmonyPatch(typeof(PlayerMovement))]
        class PlayerMovement_StaminaUpdate_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch("StaminaUpdate")]
            static void Postfix(PlayerMovement __instance)
            {
                var feature = PowerToys.GetInstance<InfiniteStaminaFeature>();
                if (feature != null && IsEnabled.Value && feature._isActive)
                {
                    __instance.stamina = __instance.staminaMax;
                    Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).SetStaminaValue(1f);
                }
            }
        }
    }
}
