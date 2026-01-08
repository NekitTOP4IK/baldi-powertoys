using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using BaldiPowerToys.Utils;
using BepInEx.Logging;

namespace BaldiPowerToys.Features
{
    public class SkipDetentionFeature : Feature
    {
        private ConfigEntry<bool> _configIsEnabled = null!;
        private ConfigEntry<KeyCode> _configSkipKey = null!;
        
        private static SkipDetentionFeature _instance = null!;
        public static SkipDetentionFeature Instance => _instance;
        
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("SkipDetention");

        public override void Init(Harmony harmony)
        {
            base.Init(harmony);
            _instance = this;
            
            _configIsEnabled = Plugin.PublicConfig.Bind("SkipDetention", "Enabled", false,
                "Enable the Skip Detention feature to instantly skip detention with a key press.");

            _configSkipKey = PowerToys.Config.Bind("SkipDetention", "SkipKey", KeyCode.End,
                KeyCodeUtils.GetEssentialKeyCodeDescription("Клавиша для пропуска detention"));
        }

        public override void Update()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
                return;

            if (!_configIsEnabled.Value)
                return;

            if (Input.GetKeyDown(_configSkipKey.Value))
            {
                TrySkipDetention();
            }
        }

        private void TrySkipDetention()
        {
            try
            {
                var coreGameManager = Singleton<CoreGameManager>.Instance;
                if (coreGameManager?.GetPlayer(0)?.ec == null)
                    return;

                var ec = coreGameManager.GetPlayer(0).ec;
                
                foreach (var office in ec.offices)
                {
                    var detentionFunction = office.functionObject?.GetComponent<DetentionRoomFunction>();
                    if (detentionFunction != null && IsDetentionActive(detentionFunction))
                    {
                        SkipDetentionTimer(detentionFunction);
                        PowerToys.ShowSuccess("Наказание пропущено!", 2f, "SkipDetention");
                        return;
                    }
                }
                
                PowerToys.ShowError("Вы не наказаны!", 2f, "SkipDetention");
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Ошибка при попытке пропустить detention: {ex.Message}");
            }
        }

        private bool IsDetentionActive(DetentionRoomFunction detentionFunction)
        {
            try
            {
                var activeField = typeof(DetentionRoomFunction).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
                return activeField != null && (bool)activeField.GetValue(detentionFunction);
            }
            catch
            {
                return false;
            }
        }

        private void SkipDetentionTimer(DetentionRoomFunction detentionFunction)
        {
            try
            {
                var timeField = typeof(DetentionRoomFunction).GetField("time", BindingFlags.NonPublic | BindingFlags.Instance);
                if (timeField != null)
                {
                    timeField.SetValue(detentionFunction, 0f);
                }

                UnlockOfficeDoors(detentionFunction);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Ошибка при установке времени detention: {ex.Message}");
            }
        }

        private void UnlockOfficeDoors(DetentionRoomFunction detentionFunction)
        {
            try
            {
                var roomField = typeof(RoomFunction).GetField("room", BindingFlags.NonPublic | BindingFlags.Instance);
                if (roomField != null)
                {
                    var room = roomField.GetValue(detentionFunction) as RoomController;
                    if (room?.doors != null)
                    {
                        foreach (var door in room.doors)
                        {
                            door.Unlock();
                        }
                        Logger.LogInfo($"Разблокировано {room.doors.Count} дверей офиса");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Ошибка при разблокировке дверей офиса: {ex.Message}");
            }
        }


    }
}