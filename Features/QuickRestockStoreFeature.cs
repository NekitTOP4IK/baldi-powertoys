using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using BaldiPowerToys.Utils;

namespace BaldiPowerToys.Features
{
    /// <summary>
    /// Функция для быстрого обновления ассортимента магазина Джонни без затрат очков
    /// </summary>
    public class QuickRestockStoreFeature : Feature
    {
        private const string FEATURE_ID = "quick_restock_store";
        private static ConfigEntry<bool> _configIsEnabled = null!;
        private static ConfigEntry<KeyCode> _configRestockKey = null!;

        public override void Init(Harmony harmony)
        {
            _configIsEnabled = PowerToys.Config.Bind(
                "QuickRestockStore", 
                "Enabled", 
                true, 
                "Включить/выключить быстрое обновление магазина"
            );
            
            _configRestockKey = PowerToys.Config.Bind(
                "QuickRestockStore", 
                "RestockKey", 
                KeyCode.R,
                KeyCodeUtils.GetEssentialKeyCodeDescription("Клавиша для быстрого обновления магазина Джонни")
            );

            Debug.Log("[QuickRestockStore] Feature initialized.");
        }

        public override void Update()
        {
            if (!_configIsEnabled.Value) return;
            if (Singleton<CoreGameManager>.Instance == null) return;

            if (Input.GetKeyDown(_configRestockKey.Value))
            {
                TryRestockStore();
            }
        }

        private void TryRestockStore()
        {
            var player = Singleton<CoreGameManager>.Instance.GetPlayer(0);
            if (player == null)
            {
                ShowError("Игрок не найден");
                return;
            }

            var currentRoom = player.ec.CellFromPosition(player.transform.position).room;
            if (currentRoom == null)
            {
                ShowError("Вы не находитесь в комнате");
                return;
            }

            var storeFunction = currentRoom.functionObject?.GetComponent<StoreRoomFunction>();
            if (storeFunction == null)
            {
                ShowError("Вы не находитесь в магазине Джонни");
                return;
            }

            var openField = typeof(StoreRoomFunction).GetField("open", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (openField == null)
            {
                ShowError("Не удалось проверить статус магазина");
                Debug.LogError("[QuickRestockStore] Failed to find 'open' field in StoreRoomFunction");
                return;
            }

            bool isOpen = (bool)openField.GetValue(storeFunction)!;
            if (!isOpen)
            {
                ShowError("Магазин закрыт");
                return;
            }

            var restockMethod = typeof(StoreRoomFunction).GetMethod("Restock", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (restockMethod == null)
            {
                ShowError("Не удалось обновить магазин");
                Debug.LogError("[QuickRestockStore] Failed to find 'Restock' method in StoreRoomFunction");
                return;
            }

            restockMethod.Invoke(storeFunction, null);

            string message = PowerToys.IsRussian
                ? "<b>Магазин обновлён!</b>"
                : "<b>Store restocked!</b>";

            PowerToys.ShowSuccess(message, 1.5f, FEATURE_ID);
            Debug.Log("[QuickRestockStore] Store successfully restocked");
        }

        private void ShowError(string errorMessage)
        {
            string message = PowerToys.IsRussian
                ? $"<b>Ошибка:</b> {errorMessage}"
                : $"<b>Error:</b> {errorMessage}";

            PowerToys.ShowError(message, 2.0f, FEATURE_ID);
            Debug.LogWarning($"[QuickRestockStore] {errorMessage}");
        }
    }
}
