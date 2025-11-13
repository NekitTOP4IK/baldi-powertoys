using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;
using BaldiPowerToys.Utils;

namespace BaldiPowerToys.Features
{
    public class GiveMoneyFeature : MonoBehaviour
    {
        private ConfigEntry<bool> _isEnabled = null!;
        private ConfigEntry<KeyCode> _moneyKey = null!;
        private ConfigEntry<int> _moneyAmount = null!;
        private ConfigEntry<int> _maxMoneyAmount = null!;
        private ConfigEntry<float> _initialDelay = null!;
        private ConfigEntry<float> _minDelay = null!;
        private ConfigEntry<float> _delayReductionRate = null!;

        private bool _isKeyDown;
        private float _currentDelay;
        private float _timeSinceLastAdd;

        private bool _showWarning;
        private float _warningTimer;
        private const float WarningDuration = 2.5f;

        private GUIStyle? _warningStyle;
        private Texture2D? _warningBackgroundTexture, _warningBorderTexture;
        private const float BorderWidth = 2f;

        private float _animationProgress;
        private const float AnimationSpeed = 4f;

        void Awake()
        {
            _isEnabled = Plugin.PublicConfig.Bind("GiveMoney", "Enabled", false, "Enable the Give Money feature.");
            _moneyKey = PowerToys.Config.Bind("GiveMoney", "MoneyKey", KeyCode.Semicolon,
                KeyCodeUtils.GetEssentialKeyCodeDescription("Клавиша для получения денег"));
            _moneyAmount = Plugin.PublicConfig.Bind("GiveMoney", "Amount", 100, "Amount of money to get per trigger.");
            _maxMoneyAmount = Plugin.PublicConfig.Bind("GiveMoney", "MaxAmount", 99999, "Maximum amount of money a player can have.");
            _initialDelay = Plugin.PublicConfig.Bind("GiveMoney.Holding", "InitialDelay", 0.5f, "Initial delay (seconds) before adding money when holding the key.");
            _minDelay = Plugin.PublicConfig.Bind("GiveMoney.Holding", "MinDelay", 0.05f, "Minimum delay between money additions when holding.");
            _delayReductionRate = Plugin.PublicConfig.Bind("GiveMoney.Holding", "DelayReductionRate", 0.1f, "How much the delay is reduced each time money is added.");

            CreateTextures();
        }

        private void CreateTextures()
        {
            _warningBackgroundTexture = CreateGradientTexture(128, new Color(0.7f, 0.1f, 0.1f, 0.9f), new Color(0.4f, 0.05f, 0.05f, 0.9f));
            _warningBorderTexture = CreateSolidTexture(new Color(1f, 0.5f, 0.5f, 0.7f));
        }

        private void Update()
        {
            if (!_isEnabled.Value || SceneManager.GetActiveScene().name == "MainMenu" || (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused))
            {
                return;
            }

            HandleInput();
            HandleWarning();
            HandleAnimation();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(_moneyKey.Value))
            {
                _isKeyDown = true;
                _currentDelay = _initialDelay.Value;
                _timeSinceLastAdd = 0f;
                AddMoney();
            }
            else if (Input.GetKey(_moneyKey.Value) && _isKeyDown)
            {
                _timeSinceLastAdd += Time.deltaTime;
                if (_timeSinceLastAdd >= _currentDelay)
                {
                    AddMoney();
                    _timeSinceLastAdd = 0f;
                    _currentDelay = Mathf.Max(_minDelay.Value, _currentDelay - _delayReductionRate.Value);
                }
            }
            else if (Input.GetKeyUp(_moneyKey.Value))
            {
                _isKeyDown = false;
            }
        }

        private void HandleWarning()
        {
            if (_showWarning)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0)
                {
                    _showWarning = false;
                }
            }
        }

        private void HandleAnimation()
        {
            if (_showWarning && _animationProgress < 1f)
            {
                _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * AnimationSpeed);
            }
            else if (!_showWarning && _animationProgress > 0f)
            {
                _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * AnimationSpeed);
            }
        }

        private void AddMoney()
        {
            if (Singleton<CoreGameManager>.Instance == null) return;

            if (Singleton<CoreGameManager>.Instance.GetPoints(0) >= _maxMoneyAmount.Value)
            {
                _showWarning = true;
                _warningTimer = WarningDuration;
            }
            else
            {
                Singleton<CoreGameManager>.Instance.AddPoints(_moneyAmount.Value, 0, true);
            }
        }

        private void OnGUI()
        {
            if (_animationProgress <= 0f) return;

            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            const float boxWidth = 400f;
            const float boxHeight = 60f;

            float easedProgress = Mathf.Sin(_animationProgress * Mathf.PI * 0.5f);
            float startY = Screen.height;
            float endY = Screen.height - boxHeight - 30f;
            float currentY = Mathf.Lerp(startY, endY, easedProgress);

            Rect boxRect = new Rect((Screen.width - boxWidth) / 2f, currentY, boxWidth, boxHeight);

            DrawBoxWithBorder(boxRect, _warningBackgroundTexture!, _warningBorderTexture!); 
            string message = PowerToys.IsCyrillicPlusLoaded ? "Превышен лимит денег" : "Money limit exceeded";
            DrawTextWithShadow(boxRect, message, _warningStyle);
        }

        private Texture2D CreateGradientTexture(int height, Color startColor, Color endColor)
        {
            var texture = new Texture2D(1, height);
            for (int y = 0; y < height; y++)
            {
                texture.SetPixel(0, y, Color.Lerp(startColor, endColor, (float)y / (height - 1)));
            }
            texture.Apply();
            return texture;
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
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
            GUIStyle shadowStyle = new GUIStyle(style) { normal = { textColor = new Color(0, 0, 0, 0.7f) } };
            Rect shadowRect = new Rect(rect.x + shadowDistance, rect.y + shadowDistance, rect.width, rect.height);
            GUI.Label(shadowRect, text, shadowStyle);
            GUI.Label(rect, text, style);
        }

        void OnDestroy()
        {
            Destroy(_warningBackgroundTexture);
            Destroy(_warningBorderTexture);
        }
    }
}
