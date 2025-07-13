using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;

namespace BaldiPowerToys.Features
{
    public class InfiniteStaminaFeature : MonoBehaviour
    {
        private static InfiniteStaminaFeature? _instance;

        private ConfigEntry<bool> _isEnabled = null!;
        internal bool _isActive = false;
        private bool _wasEnabled = true;

        internal static bool IsCyrillicPlusLoaded { get; private set; }

        private enum UIState { Hidden, Showing, Exiting }
        private UIState _currentState = UIState.Hidden;
        private float _timer;
        private float _maxTime;
        private string _message = "";
        private GUIStyle? _textStyle;
        private Texture2D? _bgTexture;
        private Texture2D? _barTexture;
        private float _animationProgress;
        private const float AnimationSpeed = 4f;

        private readonly Color _greenColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        private readonly Color _redColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        void Awake()
        {
            _instance = this;
            _isEnabled = Plugin.PublicConfig.Bind("InfiniteStamina", "Enabled", false, "Enable the Infinite Stamina feature.");
            IsCyrillicPlusLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("blayms.tbb.baldiplus.cyrillic");

            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.9f));
            _bgTexture.Apply();

            _barTexture = new Texture2D(1, 1);
            _barTexture.Apply();
        }

        public void ShowMessage(string message, float duration, Color barColor)
        {
            _message = message;
            _timer = duration;
            _maxTime = duration;
            _currentState = UIState.Showing;
            _barTexture!.SetPixel(0, 0, barColor);
            _barTexture.Apply();
        }

        void Update()
        {
            if (_wasEnabled && !_isEnabled.Value)
            {
                _isActive = false;
            }
            _wasEnabled = _isEnabled.Value;

            if (!_isEnabled.Value)
            {
                if (_currentState != UIState.Hidden)
                {
                    _currentState = UIState.Exiting;
                }
                return;
            }

            HandleAnimations();
            HandleTimers();

            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                _isActive = !_isActive;
                if (_isActive)
                {
                    string message = IsCyrillicPlusLoaded ? "Бесконечная стамина <color=#80ff80>включена</color>" : "Infinite Stamina <color=#80ff80>Enabled</color>";
                    ShowMessage(message, 1.2f, _greenColor);
                }
                else
                {
                    string message = IsCyrillicPlusLoaded ? "Бесконечная стамина <color=red>выключена</color>" : "Infinite Stamina <color=red>Disabled</color>";
                    ShowMessage(message, 1.2f, _redColor);
                }
            }
        }

        private void HandleTimers()
        {
            if (_currentState == UIState.Showing)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _currentState = UIState.Exiting;
                }
            }
        }

        private void HandleAnimations()
        {
            bool shouldBeVisible = _currentState == UIState.Showing;

            if (shouldBeVisible && _animationProgress < 1f)
            {
                _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * AnimationSpeed);
            }
            else if (!shouldBeVisible && _animationProgress > 0f)
            {
                _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * AnimationSpeed);
                if (_animationProgress <= 0f)
                {
                    _currentState = UIState.Hidden;
                }
            }
        }

        void OnGUI()
        {
            if (_currentState == UIState.Hidden || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused)) return;

            GUI.depth = 2;

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    font = Plugin.ComicSans,
                    normal = { textColor = Color.white },
                    richText = true
                };
            }

            const float boxWidth = 350, boxHeight = 70;
            float easedProgress = 1 - Mathf.Pow(1 - _animationProgress, 3);

            float startY = Screen.height;
            float endY = Screen.height - boxHeight - 20;
            float currentY = Mathf.Lerp(startY, endY, easedProgress);

            var boxRect = new Rect(Screen.width / 2f - boxWidth / 2f, currentY, boxWidth, boxHeight);

            GUI.DrawTexture(boxRect, _bgTexture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(boxRect.x, boxRect.y, boxRect.width, boxRect.height - 10), _message, _textStyle);

            float timerPercentage = Mathf.Clamp01(_timer / _maxTime);
            float barWidth = boxWidth * timerPercentage;
            var barRect = new Rect(boxRect.x, boxRect.y + boxRect.height - 5, barWidth, 5);

            GUI.DrawTexture(barRect, _barTexture, ScaleMode.StretchToFill);
        }

        [HarmonyPatch(typeof(PlayerMovement))]
        class PlayerMovement_StaminaUpdate_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch("StaminaUpdate")]
            static void Postfix(PlayerMovement __instance)
            {
                if (_instance != null && _instance._isEnabled.Value && _instance._isActive)
                {
                    __instance.stamina = __instance.staminaMax;
                    Singleton<CoreGameManager>.Instance.GetHud(__instance.pm.playerNumber).SetStaminaValue(1f);
                }
            }
        }
    }
}
