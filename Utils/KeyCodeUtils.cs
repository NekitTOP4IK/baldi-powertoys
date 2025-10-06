using System;
using System.Linq;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx;

namespace BaldiPowerToys.Utils
{
    public static class KeyCodeUtils
    {
        /// <summary>
        /// Получает конфигурацию для KeyCode без джойстиков
        /// </summary>
        public static ConfigDescription GetFilteredKeyCodeDescription(string description)
        {
            return new ConfigDescription(description);
        }
        
        /// <summary>
        /// Получает конфигурацию для основных клавиш
        /// </summary>
        public static ConfigDescription GetEssentialKeyCodeDescription(string description)
        {
            return new ConfigDescription(description);
        }
        
        /// <summary>
        /// Проверяет, является ли клавиша джойстиком
        /// </summary>
        public static bool IsJoystickKey(KeyCode keyCode)
        {
            return keyCode.ToString().StartsWith("Joystick");
        }
        
        /// <summary>
        /// Получает отображаемое имя клавиши без префикса Joystick
        /// </summary>
        public static string GetDisplayName(KeyCode keyCode)
        {
            string name = keyCode.ToString();
            if (name.StartsWith("Joystick"))
            {
                // Убираем префикс Joystick для более короткого отображения
                return name.Replace("Joystick", "J").Replace("Button", "B");
            }
            return name;
        }
    }
}