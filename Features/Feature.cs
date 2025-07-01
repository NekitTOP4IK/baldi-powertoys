using HarmonyLib;
using UnityEngine;

namespace BaldiPowerToys.Features {
    public abstract class Feature : MonoBehaviour {
        public virtual void Init(Harmony harmony) { }
        public virtual void Update() { }
        public virtual void OnGUI() { }
    }
}
