using BepInEx.Configuration; 
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BaldiPowerToys.Features
{
    public class QuickFillMapFeature : MonoBehaviour
    {
        private ConfigEntry<bool> _isEnabled = null!;
        private ConfigEntry<KeyCode> _fillMapKey = null!;

        private enum UIState { Hidden, Confirming, Executing, Error, Exiting }
        private UIState _currentState = UIState.Hidden;
        private UIState _lastVisibleState;

        private float _timer;
        private const float ConfirmationTimeout = 5f;
        private const float MessageTimeout = 2.5f;

        private GUIStyle? _titleStyle, _bodyStyle, _statusStyle;

        private Texture2D? _confirmBackgroundTexture, _confirmBorderTexture;
        private Texture2D? _successBackgroundTexture, _successBorderTexture;
        private Texture2D? _errorBackgroundTexture, _errorBorderTexture;
        private Texture2D? _timerBarBackgroundTexture, _timerBarFillTexture;

        private const float BorderWidth = 2f;
        private float _animationProgress;
        private const float AnimationSpeed = 4f;

        public static bool MapWasFilled; 
        private bool _wasMapLockedByPlugin;



        private void CreateTextures()
        {
            // Blue/Cyan theme for QuickFillMap
            _confirmBackgroundTexture = CreateGradientTexture(128, new Color(0.1f, 0.2f, 0.4f, 0.95f), new Color(0.05f, 0.1f, 0.2f, 0.95f));
            _confirmBorderTexture = CreateSolidTexture(new Color(0.4f, 0.7f, 1f, 0.8f));

            _successBackgroundTexture = CreateGradientTexture(128, new Color(0.1f, 0.4f, 0.4f, 0.95f), new Color(0.05f, 0.2f, 0.2f, 0.95f));
            _successBorderTexture = CreateSolidTexture(new Color(0.5f, 1f, 1f, 0.8f));

            _errorBackgroundTexture = CreateGradientTexture(128, new Color(0.6f, 0.1f, 0.2f, 0.95f), new Color(0.3f, 0.05f, 0.1f, 0.95f));
            _errorBorderTexture = CreateSolidTexture(new Color(1f, 0.5f, 0.6f, 0.8f));

            _timerBarBackgroundTexture = CreateSolidTexture(new Color(0.05f, 0.1f, 0.2f, 0.7f));
            _timerBarFillTexture = CreateSolidTexture(new Color(1f, 0.8f, 0.4f));
        }

        void Awake()
        {
            _isEnabled = Plugin.PublicConfig.Bind("QuickFillMap", "Enabled", false, "Enable the Quick Fill Map feature.");
            _fillMapKey = Plugin.PublicConfig.Bind("QuickFillMap", "FillMapKey", KeyCode.U, "Key to trigger the map fill.");
            CreateTextures();
        }

        private void OnDestroy()
        {
            Destroy(_confirmBackgroundTexture);
            Destroy(_confirmBorderTexture);
            Destroy(_successBackgroundTexture);
            Destroy(_successBorderTexture);
            Destroy(_errorBackgroundTexture);
            Destroy(_errorBorderTexture);
            Destroy(_timerBarBackgroundTexture);
            Destroy(_timerBarFillTexture);
        }

        void Update()
        {
            if (!_isEnabled.Value) return;

            if (Singleton<CoreGameManager>.Instance == null) return;

            if (Singleton<CoreGameManager>.Instance.Paused || Singleton<CoreGameManager>.Instance.MapOpen)
            {
                if (_currentState != UIState.Hidden) TransitionToState(UIState.Exiting);
                return;
            }

            if (Input.GetKeyDown(_fillMapKey.Value))
            {
                if (_currentState == UIState.Confirming)
                {
                    TransitionToState(UIState.Executing);
                }
                else if (_currentState == UIState.Hidden)
                {
                    if (MapWasFilled)
                    {
                        TransitionToState(UIState.Error);
                    }
                    else
                    {
                        TransitionToState(UIState.Confirming);
                    }
                }
            }

            HandleTimers();
            HandleAnimations();
            HandleMapLock();
        }

        private void TransitionToState(UIState newState)
        {
            if (_currentState == newState) return;

            if (newState == UIState.Exiting)
            {
                _lastVisibleState = _currentState;
            }

            _currentState = newState;
            switch (newState)
            {
                case UIState.Confirming:
                    _timer = ConfirmationTimeout;
                    break;
                case UIState.Executing:
                case UIState.Error:
                    _timer = MessageTimeout;
                    break;
            }
        }

        private void HandleTimers()
        {
            if (_currentState != UIState.Hidden && _currentState != UIState.Exiting)
            {
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    if (_currentState == UIState.Executing) FillMap();
                    TransitionToState(UIState.Exiting);
                }
            }
        }

        private void HandleAnimations()
        {
            bool shouldBeVisible = _currentState != UIState.Hidden && _currentState != UIState.Exiting;

            if (shouldBeVisible && _animationProgress < 1f)
            {
                _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * AnimationSpeed);
            }
            else if (!shouldBeVisible && _animationProgress > 0f)
            {
                _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * AnimationSpeed);
                if (_animationProgress <= 0f && _currentState == UIState.Exiting)
                {
                    _currentState = UIState.Hidden;
                }
            }
        }

        void OnGUI()
        {
            if (_animationProgress <= 0f) return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 28, font = Plugin.ComicSans ?? GUI.skin.font, normal = { textColor = new Color(0.9f, 0.2f, 0.2f) } };
                _bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, font = Plugin.ComicSans ?? GUI.skin.font, normal = { textColor = Color.white }, richText = true, wordWrap = true };
                _statusStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32, font = Plugin.ComicSans ?? GUI.skin.font, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }, richText = true };
            }

            const float boxWidth = 560f, boxHeight = 160f;
            float easedProgress = Mathf.Sin(_animationProgress * Mathf.PI * 0.5f);
            float finalX = Screen.width - boxWidth - 20;
            float currentX = finalX + (boxWidth + 20) * (1 - easedProgress);
            float yPos = (Screen.height - boxHeight) / 2f;
            Rect boxRect = new Rect(currentX, yPos, boxWidth, boxHeight);

            DrawUI(boxRect);
        }

        private void DrawUI(Rect boxRect)
        {
            var stateToDraw = _currentState == UIState.Exiting ? _lastVisibleState : _currentState;

            switch (stateToDraw)
            {
                case UIState.Confirming:
                    DrawBoxWithBorder(boxRect, _confirmBackgroundTexture!, _confirmBorderTexture!);
                    DrawConfirmationDialog(boxRect);
                    break;
                case UIState.Executing:
                    DrawBoxWithBorder(boxRect, _successBackgroundTexture!, _successBorderTexture!);
                    string executingMsg = Plugin.IsCyrillicPlusLoaded ? "<color=#aaffaa>Заполняю карту...</color>" : "<color=#aaffaa>Filling map...</color>";
                    DrawTextWithShadow(boxRect, executingMsg, _statusStyle!);
                    break;
                case UIState.Error:
                    DrawBoxWithBorder(boxRect, _errorBackgroundTexture!, _errorBorderTexture!);
                    string errorMsg = Plugin.IsCyrillicPlusLoaded ? "<color=#ffaaaa>Карта уже заполнена!</color>" : "<color=#ffaaaa>The map is already full!</color>";
                    DrawTextWithShadow(boxRect, errorMsg, _statusStyle!);
                    break;
            }
        }

        private void DrawConfirmationDialog(Rect boxRect)
        {
            _titleStyle!.normal.textColor = new Color(0.8f, 0.8f, 1f);
            string titleText = Plugin.IsCyrillicPlusLoaded ? "Заполнить карту?" : "Fill the map?";
            DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 15, boxRect.width, 40), titleText, _titleStyle);

            string timerValue = Mathf.Max(0f, _timer).ToString("F1", CultureInfo.InvariantCulture);
            string keyName = _fillMapKey?.Value.ToString() ?? "U";
            string bodyText = Plugin.IsCyrillicPlusLoaded
                ? $"Нажмите <color=yellow>{keyName}</color> снова, чтобы заполнить (<color=#80c8ff>{timerValue}с</color>)."
                : $"Press <color=yellow>{keyName}</color> again to fill (<color=#80c8ff>{timerValue}s</color>).";
            DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 65, boxRect.width, 60), bodyText, _bodyStyle!);

            if (_currentState == UIState.Confirming)
            {
                Rect timerRect = new Rect(boxRect.x + BorderWidth, boxRect.yMax - BorderWidth - 5, boxRect.width - (BorderWidth * 2), 5);
                DrawTimerBar(timerRect, _timer / ConfirmationTimeout);
            }
        }

        private void FillMap()
        {
            var map = FindObjectOfType<Map>();
            if (map != null && !MapWasFilled)
            {
                map.CompleteMap();
                MapWasFilled = true;
            }
        }

        private void DrawTimerBar(Rect rect, float progress)
        {
            GUI.DrawTexture(rect, _timerBarBackgroundTexture);
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            GUI.DrawTexture(fillRect, _timerBarFillTexture);
        }

        private Texture2D CreateGradientTexture(int height, Color startColor, Color endColor)
        {
            Texture2D texture = new Texture2D(1, height);
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(0, y, Color.Lerp(startColor, endColor, (float)y / (height - 1)));
            }
            texture.Apply();
            return texture;
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void DrawBoxWithBorder(Rect rect, Texture2D background, Texture2D border)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, rect.height), border);
            GUI.DrawTexture(new Rect(rect.x + BorderWidth, rect.y + BorderWidth, rect.width - (BorderWidth * 2), rect.height - (BorderWidth * 2)), background);
        }

        private void DrawTextWithShadow(Rect rect, string text, GUIStyle style, float shadowDistance = 2f)
        {
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0, 0, 0, 0.7f);
            string shadowText = Regex.Replace(text, "<.*?>", string.Empty);
            Rect shadowRect = new Rect(rect.x + shadowDistance, rect.y + shadowDistance, rect.width, rect.height);
            GUI.Label(shadowRect, shadowText, shadowStyle);
            GUI.Label(rect, text, style);
        }

        private bool IsWindowVisible() => _currentState != UIState.Hidden && _currentState != UIState.Exiting;

        private void HandleMapLock()
        {
            if (IsWindowVisible())
            {
                if (!Singleton<CoreGameManager>.Instance.disablePause)
                {
                    Singleton<CoreGameManager>.Instance.disablePause = true;
                    _wasMapLockedByPlugin = true;
                }
            }
            else
            {
                if (_wasMapLockedByPlugin)
                {
                    Singleton<CoreGameManager>.Instance.disablePause = false;
                    _wasMapLockedByPlugin = false;
                }
            }
        }
    }
}
