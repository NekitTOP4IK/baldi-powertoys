using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;

namespace BaldiPowerToys.Features
{
    public class NoIncorrectAnswersFeature : MonoBehaviour
    {
        private static NoIncorrectAnswersFeature? _instance;

        private ConfigEntry<bool> _isEnabled = null!;

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

        void Awake()
        {
            _instance = this;
            _isEnabled = Plugin.PublicConfig.Bind("NoIncorrectAnswers", "Enabled", false, "Enable the No Incorrect Answers feature.");
            IsCyrillicPlusLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("blayms.tbb.baldiplus.cyrillic");

            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.9f));
            _bgTexture.Apply();

            _barTexture = new Texture2D(1, 1);
            _barTexture.SetPixel(0, 0, new Color(0.2f, 0.8f, 0.3f, 1f));
            _barTexture.Apply();
        }

        public void ShowMessage(string message, float duration)
        {
            _message = message;
            _timer = duration;
            _maxTime = duration;
            _currentState = UIState.Showing;
        }

        void Update()
        {
            if (_currentState == UIState.Hidden) return;
            HandleAnimations();
            HandleTimers();
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
            if (_currentState == UIState.Hidden || !_isEnabled.Value || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused)) return;

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

        [HarmonyPatch(typeof(MathMachine))]
        class MathMachine_Clicked_Patch
        {
            static readonly AccessTools.FieldRef<MathMachine, int[]> playerHoldingField = AccessTools.FieldRefAccess<MathMachine, int[]>("playerHolding");
            static readonly AccessTools.FieldRef<MathMachine, bool[]> playerIsHoldingField = AccessTools.FieldRefAccess<MathMachine, bool[]>("playerIsHolding");
            static readonly AccessTools.FieldRef<MathMachine, int> answerField = AccessTools.FieldRefAccess<MathMachine, int>("answer");
            static readonly AccessTools.FieldRef<Activity, bool> poweredField = AccessTools.FieldRefAccess<Activity, bool>("powered");

            [HarmonyPrefix]
            [HarmonyPatch("Clicked")]
            static void Prefix(MathMachine __instance, int player)
            {
                if (_instance == null || !_instance._isEnabled.Value) return;

                bool isPowered = poweredField(__instance);
                bool[] playerIsHolding = playerIsHoldingField(__instance);

                if (!isPowered || playerIsHolding == null || player >= playerIsHolding.Length || !playerIsHolding[player])
                {
                    return;
                }

                int correctAnswer = answerField(__instance);
                int[] playerAnswers = playerHoldingField(__instance);
                if (playerAnswers == null || player >= playerAnswers.Length) return;
                int playerAnswer = playerAnswers[player];

                if (playerAnswer != correctAnswer)
                {
                    playerAnswers[player] = correctAnswer;

                    string message = IsCyrillicPlusLoaded ? "Неверный ответ исправлен!" : "Incorrect answer corrected!";
                    _instance.ShowMessage(message, 2.5f);
                }
            }
        }
    }
}
