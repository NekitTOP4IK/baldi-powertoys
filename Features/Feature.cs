using HarmonyLib;
using UnityEngine;

namespace BaldiPowerToys.Features 
{
    public abstract class Feature : MonoBehaviour 
    {
        protected bool IsEnabled = true;
        protected bool IsInitialized;
        
        private bool _wasEnabledLastFrame;

        public virtual void Init(Harmony harmony) 
        { 
            IsInitialized = true;
            _wasEnabledLastFrame = IsEnabled;
        }

        protected virtual void OnEnable()
        {
            if (IsInitialized && !_wasEnabledLastFrame)
            {
                OnFeatureEnabled();
            }
            _wasEnabledLastFrame = true;
        }

        protected virtual void OnDisable()
        {
            if (IsInitialized && _wasEnabledLastFrame)
            {
                OnFeatureDisabled();
            }
            _wasEnabledLastFrame = false;
        }

        public virtual void OnPluginDestroy()
        {
            if (IsEnabled)
            {
                OnFeatureDisabled();
            }
            OnCleanup();
        }

        protected virtual void OnFeatureEnabled() { }
        protected virtual void OnFeatureDisabled() { }
        protected virtual void OnCleanup() { }

        public virtual void Update() { }
        public virtual void OnGUI() { }

        protected bool ShouldUpdate()
        {
            return IsEnabled && IsInitialized;
        }
    }
}
