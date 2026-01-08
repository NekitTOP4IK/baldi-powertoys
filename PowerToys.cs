using BepInEx.Configuration;
using BaldiPowerToys.Features;
using BaldiPowerToys.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Bootstrap;

namespace BaldiPowerToys 
{
    public static class PowerToys 
    {
        public static ConfigFile Config { get; private set; } = null!;
        public static bool IsRussian => IsCyrillicPlusLoaded && !ForceEnglish;
        public static bool IsCyrillicPlusLoaded { get; private set; }
        public static bool ForceEnglish { get; set; }

        private static GameObject _featureHolder = null!;
        private static readonly Dictionary<System.Type, Feature> _featureCache = new Dictionary<System.Type, Feature>();
        
        private static PowerToysNotification? _notifications;
        
        public static void Init(ConfigFile config, bool isRussian, GameObject featureHolder) 
        {
            Config = config;
            _featureHolder = featureHolder;
            _featureCache.Clear();
            
            IsCyrillicPlusLoaded = Chainloader.PluginInfos.Any(x => x.Value.Metadata.GUID == "blayms.tbb.baldiplus.cyrillic");
            
            var forceEnglishConfig = config.Bind("General", "ForceEnglish", false, "Принудительно использовать английский язык (если установлена кириллица)");
            ForceEnglish = forceEnglishConfig.Value;
            
            _notifications = featureHolder.AddComponent<PowerToysNotification>();
        }

        public static T GetInstance<T>() where T : Feature 
        {
            var type = typeof(T);
            Feature? feature = null;
            
            if (!_featureCache.TryGetValue(type, out feature))
            {
                feature = _featureHolder.GetComponent<T>();
                if (feature != null)
                {
                    _featureCache[type] = feature;
                }
            }
            
            return feature != null ? (T)feature : null!;
        }

        public static void ShowNotification(string message, float duration = 1.2f, Color? barColor = null, Color? backgroundColor = null, string sourceId = "default")
        {
            _notifications?.ShowNotification(
                message,
                duration,
                barColor,
                backgroundColor,
                sourceId
            );
        }

        public static void ShowSuccess(string message, float duration = 1.2f, string sourceId = "default")
        {
            ShowNotification(message, duration, PowerToysNotification.SuccessColor, null, sourceId);
        }

        public static void ShowError(string message, float duration = 1.2f, string sourceId = "default")
        {
            ShowNotification(message, duration, PowerToysNotification.ErrorColor, null, sourceId);
        }

        public static void ShowInfo(string message, float duration = 1.2f, string sourceId = "default")
        {
            ShowNotification(message, duration, PowerToysNotification.InfoColor, null, sourceId);
        }

        public static void ShowConfirm(string message, float duration = 5f, string sourceId = "default")
        {
            if (_notifications == null) return;
            
            var config = new PowerToysNotification.LiveNotificationConfig(
                sourceId,
                duration,
                PowerToysNotification.InfoColor,
                PowerToysNotification.DefaultBackground,
                time => {
                    string timeText = IsRussian 
                        ? $"<color=#80c8ff>({time:F1}с)</color>" 
                        : $"<color=#80c8ff>({time:F1}s)</color>";
                    return $"{message} {timeText}";
                }
            );
            
            _notifications.ShowLiveNotification(config);
        }

        public static void ClearNotifications()
        {
            _notifications?.ClearNotifications();
        }

        public static void ClearCache()
        {
            _featureCache.Clear();
        }
    }
}
