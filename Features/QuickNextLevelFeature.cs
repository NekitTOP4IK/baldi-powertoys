using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;
using BepInEx.Bootstrap;
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

        private GUIStyle? _titleStyle, _bodyStyle, _skippingStyle;

        private Texture2D? _confirmBackgroundTexture, _confirmBorderTexture;
        private Texture2D? _warningBackgroundTexture, _warningBorderTexture;
        private Texture2D? _timerBarBackgroundTexture, _timerBarFillTexture;

        private const float BorderWidth = 2f;

        private float _animationProgress;
        private const float AnimationSpeed = 4f;
        private bool _isExiting;
        private bool _warningIsExiting;
        private bool _confirmationIsTimingOut;

        private bool _showWarning;
        private float _warningTimer;
        private const float WarningDuration = 3f;

        void Awake()
        {
            _isEnabled = Plugin.PublicConfig.Bind("QuickNextLevel", "Enabled", false, "Enable the Quick Next Level feature.");
            _nextLevelKey = Plugin.PublicConfig.Bind("QuickNextLevel", "NextLevelKey", KeyCode.Quote, "Key to trigger the next level load.");

            SceneManager.activeSceneChanged += OnSceneChanged;
            CreateTextures();
        }

        private void CreateTextures()
        {
            _confirmBackgroundTexture = CreateGradientTexture(128, new Color(0.2f, 0.2f, 0.2f, 0.95f), new Color(0.1f, 0.1f, 0.1f, 0.95f));
            _confirmBorderTexture = CreateSolidTexture(new Color(0.8f, 0.8f, 0.8f, 0.7f));
            _warningBackgroundTexture = CreateGradientTexture(128, new Color(0.9f, 0.6f, 0.0f, 0.95f), new Color(0.7f, 0.4f, 0.0f, 0.95f));
            _warningBorderTexture = CreateSolidTexture(new Color(1f, 0.8f, 0.5f, 0.7f));
            _timerBarBackgroundTexture = CreateSolidTexture(new Color(0.1f, 0.1f, 0.1f, 0.5f));
            _timerBarFillTexture = CreateSolidTexture(new Color(1f, 0.9f, 0.2f));
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            Destroy(_confirmBackgroundTexture);
            Destroy(_confirmBorderTexture);
            Destroy(_warningBackgroundTexture);
            Destroy(_warningBorderTexture);
            Destroy(_timerBarBackgroundTexture);
            Destroy(_timerBarFillTexture);
        }

        private void OnSceneChanged(Scene current, Scene next)
        {
            if (next.name == "MainMenu")
            {
                _animationProgress = 0f;
                _confirmationPending = false;
                _showWarning = false;
                _isExiting = false;
                _warningIsExiting = false;
            }
        }

        private void Update()
        {
            if (!_isEnabled.Value || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused))
            {
                return;
            }

            if (Input.GetKeyDown(_nextLevelKey.Value))
            {
                if (IsSkippingDisallowed())
                {
                    _showWarning = true;
                    _warningTimer = WarningDuration;
                    _confirmationPending = false;
                    _warningIsExiting = false;
                    return;
                }

                if (_confirmationPending)
                {
                    _confirmationPending = false;
                    _isExiting = true;
                }
                else
                {
                    if (_animationProgress <= 0f && !_showWarning)
                    {
                        _confirmationPending = true;
                        _confirmationTimer = ConfirmationTimeout;
                    }
                }
            }
            
            if (_showWarning)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0)
                {
                    _showWarning = false;
                    _warningIsExiting = true;
                }
            }

            if (_confirmationPending)
            {
                _confirmationTimer -= Time.deltaTime;
                if (_confirmationTimer <= 0)
                {
                    _confirmationPending = false;
                    _confirmationIsTimingOut = true;
                }
            }
            
            if (_confirmationPending || _showWarning)
            {
                if (_animationProgress < 1f)
                {
                    _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * AnimationSpeed);
                }
            }
            else
            {
                if (_animationProgress > 0f)
                {
                    _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * AnimationSpeed);
                }
                else
                {
                    if (_isExiting)
                    {
                        _isExiting = false;
                        Singleton<BaseGameManager>.Instance.LoadNextLevel();
                    }
                    else if (_warningIsExiting)
                    {
                        _warningIsExiting = false;
                    }
                    else if (_confirmationIsTimingOut)
                    {
                        _confirmationIsTimingOut = false;
                    }
                }
            }
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

        private void OnGUI()
        {
            if (!_isEnabled.Value || _animationProgress <= 0f)
            {
                return;
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 28,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    normal = { textColor = new Color(0.9f, 0.2f, 0.2f) }
                };
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    normal = { textColor = Color.white },
                    richText = true,
                    wordWrap = true
                };
                _skippingStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 40,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.4f, 1f, 0.4f) }
                };
            }

            const float boxWidth = 560f;
            const float boxHeight = 160;

            float easedProgress = Mathf.Sin(_animationProgress * Mathf.PI * 0.5f);
            float finalX = Screen.width - boxWidth - 20;
            float currentX = finalX + (boxWidth + 20) * (1 - easedProgress);
            float yPos = (Screen.height - boxHeight) / 2f;

            Rect boxRect = new Rect(currentX, yPos, boxWidth, boxHeight);

            if (_isExiting)
            {
                DrawBoxWithBorder(boxRect, _confirmBackgroundTexture!, _confirmBorderTexture!);
                DrawTextWithShadow(boxRect, Plugin.IsCyrillicPlusLoaded ? "Пропускаем уровень..." : "Skipping level...", _skippingStyle!);
            }
            else if (_showWarning || _warningIsExiting)
            {
                DrawBoxWithBorder(boxRect, _warningBackgroundTexture!, _warningBorderTexture!);
                DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 15, boxRect.width, 40), Plugin.IsCyrillicPlusLoaded ? "ВНИМАНИЕ!" : "WARNING!", _titleStyle!);
                DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 55, boxRect.width, 60), Plugin.IsCyrillicPlusLoaded ? "Невозможно пропустить этот уровень или в это время." : "Cannot skip this level or at this time.", _bodyStyle!);
                if (_showWarning)
                {
                    Rect timerRect = new Rect(boxRect.x + BorderWidth, boxRect.yMax - BorderWidth - 5, boxRect.width - (BorderWidth * 2), 5);
                    DrawTimerBar(timerRect, _warningTimer / WarningDuration);
                }
            }
            else if (_confirmationPending || _confirmationIsTimingOut)
            {
                DrawBoxWithBorder(boxRect, _confirmBackgroundTexture!, _confirmBorderTexture!);
                DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 15, boxRect.width, 40), Plugin.IsCyrillicPlusLoaded ? "Подтвердите пропуск уровня" : "Confirm Level Skip", _titleStyle!);
                
                string keyName = GetLocalizedKeyName(_nextLevelKey.Value);
                string timerValue = Mathf.Max(0f, _confirmationTimer).ToString("F1", CultureInfo.InvariantCulture);
                
                string line1 = Plugin.IsCyrillicPlusLoaded 
                    ? $"Нажмите {keyName} ещё раз, чтобы пропустить (<color=yellow>{timerValue}с</color>)."
                    : $"Press {keyName} again to skip (<color=yellow>{timerValue}s</color>).";
                string line2 = Plugin.IsCyrillicPlusLoaded 
                    ? "Это автоматически завершит уровень."
                    : "This will autocomplete the level.";

                DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 55, boxRect.width, 30), line1, _bodyStyle!);
                DrawTextWithShadow(new Rect(boxRect.x, boxRect.y + 85, boxRect.width, 30), line2, _bodyStyle!);
                
                if (_confirmationPending)
                {
                    Rect timerRect = new Rect(boxRect.x + BorderWidth, boxRect.yMax - BorderWidth - 5, boxRect.width - (BorderWidth * 2), 5);
                    DrawTimerBar(timerRect, _confirmationTimer / ConfirmationTimeout);
                }
            }
        }

        private string GetLocalizedKeyName(KeyCode key)
        {
            if (Plugin.IsCyrillicPlusLoaded && RussianKeyMap.TryGetValue(key, out var localizedName))
            {
                return localizedName;
            }
            return key.ToString();
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

        private void DrawTimerBar(Rect rect, float progress)
        {
            GUI.DrawTexture(rect, _timerBarBackgroundTexture);
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            GUI.DrawTexture(fillRect, _timerBarFillTexture);
        }

        private Texture2D CreateGradientTexture(int height, Color startColor, Color endColor)
        {
            int width = 1;
            Texture2D texture = new Texture2D(width, height);
            for (int y = 0; y < height; y++)
            {
                float normalY = (float)y / (height - 1);
                texture.SetPixel(0, y, Color.Lerp(startColor, endColor, normalY));
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
    }
} 