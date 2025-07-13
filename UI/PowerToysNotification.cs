using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Bootstrap;
using System.Globalization;

namespace BaldiPowerToys.UI
{
    public class NotificationRequest
    {
        public string Message { get; set; }
        public float Duration { get; }
        public Color BarColor { get; }
        public Color BackgroundColor { get; }
        public string SourceId { get; }

        public NotificationRequest(string message, float duration, Color barColor, Color backgroundColor, string sourceId)
        {
            Message = message;
            Duration = duration;
            BarColor = barColor;
            BackgroundColor = backgroundColor;
            SourceId = sourceId;
        }
    }

    public class PowerToysNotification : MonoBehaviour
    {
        private enum UIState { Hidden, Showing, Exiting }
        
        private static PowerToysNotification? _instance;
        public static PowerToysNotification Instance => _instance!;

        private Queue<NotificationRequest> _notificationQueue = new Queue<NotificationRequest>();
        private NotificationRequest? _currentNotification;
        private string? _lastNotificationSource;

        private UIState _currentState = UIState.Hidden;
        private float _timer;
        private float _maxTime;
        private float _animationProgress;
        private const float AnimationSpeed = 4f;

        private GUIStyle? _textStyle;
        private GUIStyle? _boldStyle;
        private Texture2D? _bgTexture;
        private Texture2D? _barTexture;

        public static readonly Color SuccessColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        public static readonly Color ErrorColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        public static readonly Color InfoColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        public static readonly Color DefaultBackground = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            InitializeTextures();
        }

        private void InitializeTextures()
        {
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, DefaultBackground);
            _bgTexture.Apply();

            _barTexture = new Texture2D(1, 1);
            _barTexture.Apply();
        }

        public void ShowNotification(string message, float duration = 1.2f, Color? barColor = null, Color? backgroundColor = null, string sourceId = "default")
        {
            var request = new NotificationRequest(
                message,
                duration,
                barColor ?? InfoColor,
                backgroundColor ?? DefaultBackground,
                sourceId
            );

            if (_currentNotification != null && sourceId == _lastNotificationSource && (_liveConfig == null || _liveConfig.SourceId != sourceId))
            {
                if (_liveConfig != null)
                {
                    _liveConfig = null;
                }

                _currentNotification = request;
                _timer = duration;
                _maxTime = duration;
                _currentState = UIState.Showing;
                _animationProgress = 1f;
                UpdateTextures(request);
            }
            else
            {
                if (_liveConfig?.SourceId == sourceId)
                {
                    _liveConfig = null;
                }

                _notificationQueue.Enqueue(request);

                if (_currentState == UIState.Hidden)
                {
                    ShowNextNotification();
                }
            }
        }

        private void ShowNextNotification()
        {
            if (_notificationQueue.Count == 0) return;

            _currentNotification = _notificationQueue.Dequeue();
            _lastNotificationSource = _currentNotification.SourceId;
            _timer = _currentNotification.Duration;
            _maxTime = _currentNotification.Duration;
            _currentState = UIState.Showing;
            _animationProgress = 0f;
            
            if (_liveConfig != null && _liveConfig.SourceId != _currentNotification.SourceId)
            {
                _liveConfig = null;
            }
            
            UpdateTextures(_currentNotification);
        }

        private void UpdateTextures(NotificationRequest notification)
        {
            _bgTexture!.SetPixel(0, 0, notification.BackgroundColor);
            _bgTexture.Apply();

            _barTexture!.SetPixel(0, 0, notification.BarColor);
            _barTexture.Apply();
        }

        private void Update()
        {
            if (Singleton<CoreGameManager>.Instance != null && 
                Singleton<CoreGameManager>.Instance.Paused) return;

            HandleTimers();
            HandleAnimations();
            UpdateLiveNotification();
        }

        private void HandleTimers()
        {
            if (_currentState == UIState.Showing && _currentNotification != null)
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
                _animationProgress = Mathf.Min(1f, _animationProgress + Time.deltaTime * AnimationSpeed * 2f);
            }
            else if (!shouldBeVisible && _animationProgress > 0f)
            {
                _animationProgress = Mathf.Max(0f, _animationProgress - Time.deltaTime * AnimationSpeed);
                if (_animationProgress <= 0f)
                {
                    _currentState = UIState.Hidden;
                    ShowNextNotification();
                }
            }
        }

        private void OnGUI()
        {
            if (_currentState == UIState.Hidden || _currentNotification == null || 
                (Singleton<CoreGameManager>.Instance != null && Singleton<CoreGameManager>.Instance.Paused)) 
                return;
                
            if (Singleton<ElevatorScreen>.Instance != null && Singleton<ElevatorScreen>.Instance.gameObject.activeSelf)
                return;

            GUI.depth = 0;

            if (_textStyle == null || _boldStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    font = Plugin.ComicSans ?? GUI.skin.font,
                    normal = { textColor = Color.white },
                    richText = true,
                    wordWrap = true,
                    fontStyle = FontStyle.Normal
                };

                _boldStyle = new GUIStyle(_textStyle)
                {
                    fontSize = 24,
                    richText = true
                };
            }

            var content = new GUIContent(_currentNotification.Message);
            var textSize = _boldStyle.CalcSize(content);
            
            float boxWidth = Mathf.Min(textSize.x + 40, Screen.width - 40);
            float boxHeight = Mathf.Max(70, textSize.y + 30);

            float easedProgress = _animationProgress * _animationProgress * (3f - 2f * _animationProgress);

            float startY = Screen.height;
            float endY = Screen.height - boxHeight - 20;
            float currentY = Mathf.Lerp(startY, endY, easedProgress);

            var boxRect = new Rect(Screen.width / 2f - boxWidth / 2f, currentY, boxWidth, boxHeight);

            GUI.DrawTexture(boxRect, _bgTexture!, ScaleMode.StretchToFill);
            GUI.Label(new Rect(boxRect.x + 20, boxRect.y, boxRect.width - 40, boxRect.height - 10), 
                     _currentNotification.Message, _boldStyle);

            float timerPercentage = Mathf.Clamp01(_timer / _maxTime);
            float barWidth = boxWidth * timerPercentage;
            var barRect = new Rect(boxRect.x, boxRect.y + boxRect.height - 5, barWidth, 5);

            GUI.DrawTexture(barRect, _barTexture!, ScaleMode.StretchToFill);
        }

        public void ClearNotifications()
        {
            _notificationQueue.Clear();
            _currentNotification = null;
            _lastNotificationSource = null;
            _currentState = UIState.Hidden;
            _animationProgress = 0f;
        }

        public void ShowSuccess(string sourceId, string message, float duration = 2.5f)
        {
            ShowNotification(message, duration, SuccessColor, DefaultBackground, sourceId);
        }

        public void ShowError(string sourceId, string message, float duration = 2.5f)
        {
            ShowNotification(message, duration, ErrorColor, DefaultBackground, sourceId);
        }

        public void ShowConfirm(string sourceId, string message, float duration = 5f)
        {
            var config = new LiveNotificationConfig(
                sourceId,
                duration,
                InfoColor,
                DefaultBackground,
                messageGenerator: time => $"{message} ({(time > 0 ? time : 0):F1}s)"
            );
            ShowLiveNotification(config);
        }

        public void Hide(string sourceId)
        {
            if (_currentNotification?.SourceId == sourceId)
            {
                _currentState = UIState.Exiting;
                if (_liveConfig?.SourceId == sourceId)
                {
                    _liveConfig = null;
                }
            }
            _notificationQueue = new Queue<NotificationRequest>(_notificationQueue.Where(n => n.SourceId != sourceId));
        }

        public class LiveNotificationConfig
        {
            public string SourceId { get; }
            public float Duration { get; }
            public Color BarColor { get; }
            public Color BackgroundColor { get; }
            public Func<float, string> MessageGenerator { get; }

            public LiveNotificationConfig(
                string sourceId,
                float duration,
                Color? barColor = null,
                Color? backgroundColor = null,
                Func<float, string>? messageGenerator = null)
            {
                SourceId = sourceId;
                Duration = duration;
                BarColor = barColor ?? InfoColor;
                BackgroundColor = backgroundColor ?? DefaultBackground;
                MessageGenerator = messageGenerator ?? ((time) => "");
            }
        }

        private LiveNotificationConfig? _liveConfig;

        public void ShowLiveNotification(LiveNotificationConfig config)
        {
            _liveConfig = config;
            var initialMessage = config.MessageGenerator != null ? config.MessageGenerator(config.Duration) : "Live notification";
            
            _currentNotification = new NotificationRequest(
                initialMessage,
                config.Duration,
                config.BarColor,
                config.BackgroundColor,
                config.SourceId
            );

            _lastNotificationSource = config.SourceId;
            _timer = config.Duration;
            _maxTime = config.Duration;
            _currentState = UIState.Showing;
            _animationProgress = 0f;
            
            UpdateTextures(_currentNotification);
        }

        private void UpdateLiveNotification()
        {
            if (_liveConfig != null && _currentState == UIState.Showing && _currentNotification?.SourceId == _liveConfig.SourceId)
            {
                if (_liveConfig.MessageGenerator != null && _currentNotification != null)
                {
                    float remainingTime = Mathf.Max(0, _timer);
                    _currentNotification.Message = _liveConfig.MessageGenerator(remainingTime);
                }
            }
        }

        private void OnDestroy()
        {
            if (_bgTexture != null) Destroy(_bgTexture);
            if (_barTexture != null) Destroy(_barTexture);
            _instance = null;
        }
    }
}
