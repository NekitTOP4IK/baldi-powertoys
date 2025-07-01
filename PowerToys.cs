using BepInEx.Configuration;
using BaldiPowerToys.Features;
using UnityEngine;

namespace BaldiPowerToys {
    public static class PowerToys {
        public static ConfigFile Config { get; private set; } = null!;
        public static bool IsRussian { get; private set; }

        private static GameObject _featureHolder = null!;

        public static void Init(ConfigFile config, bool isRussian, GameObject featureHolder) {
            Config = config;
            IsRussian = isRussian;
            _featureHolder = featureHolder;
        }

        public static T GetInstance<T>() where T : Feature {
            return _featureHolder.GetComponent<T>();
        }
    }
}
