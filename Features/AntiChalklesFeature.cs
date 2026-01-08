using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Reflection;

namespace BaldiPowerToys.Features
{
    public class AntiChalklesFeature : Feature
    {
        private const string FEATURE_ID = "anti_chalkles";
        public static new ConfigEntry<bool> IsEnabled { get; private set; } = null!;

        void Awake()
        {
            IsEnabled = Plugin.PublicConfig.Bind("AntiChalkles", "Enabled", false, "Enable the Anti-Chalkles feature to disable Chalkles completely.");
        }

        [HarmonyPatch]
        class ChalkFace_Initialize_Patch
        {
            static MethodBase TargetMethod()
            {
                var chalkFaceType = AccessTools.TypeByName("ChalkFace");
                return AccessTools.Method(chalkFaceType, "Initialize");
            }

            [HarmonyPostfix]
            static void Postfix(object __instance)
            {
                if (!IsEnabled.Value) return;

                if (__instance is MonoBehaviour chalkles)
                {
                    chalkles.gameObject.SetActive(false);
                    
                    string status = PowerToys.IsRussian ? "<color=#90FF90>ВКЛ</color>" : "<color=#90FF90>ON</color>";
                    string message = $"AntiChalkles: {status}";
                    PowerToys.ShowInfo(message, 2f, FEATURE_ID);
                }
            }
        }

        [HarmonyPatch]
        class ChalkFace_AdvanceTimer_Patch
        {
            static MethodBase TargetMethod()
            {
                var chalkFaceType = AccessTools.TypeByName("ChalkFace");
                return AccessTools.Method(chalkFaceType, "AdvanceTimer");
            }

            [HarmonyPrefix]
            static bool Prefix()
            {
                if (!IsEnabled.Value) return true;
                
                return false;
            }
        }

        [HarmonyPatch]
        class ChalkFace_Activate_Patch
        {
            static MethodBase TargetMethod()
            {
                var chalkFaceType = AccessTools.TypeByName("ChalkFace");
                return AccessTools.Method(chalkFaceType, "Activate");
            }

            [HarmonyPrefix]
            static bool Prefix()
            {
                if (!IsEnabled.Value) return true;
                
                return false;
            }
        }

        [HarmonyPatch]
        class Chalkboard_OnPlayerEnter_Patch
        {
            static MethodBase TargetMethod()
            {
                var chalkboardType = AccessTools.TypeByName("Chalkboard");
                return AccessTools.Method(chalkboardType, "OnPlayerEnter");
            }

            [HarmonyPrefix]
            static bool Prefix()
            {
                if (!IsEnabled.Value) return true;
                
                return false;
            }
        }

        [HarmonyPatch]
        class Chalkboard_OnActivityProgress_Patch
        {
            static MethodBase TargetMethod()
            {
                var chalkboardType = AccessTools.TypeByName("Chalkboard");
                return AccessTools.Method(chalkboardType, "OnActivityProgress");
            }

            [HarmonyPrefix]
            static bool Prefix()
            {
                if (!IsEnabled.Value) return true;
                
                return false;
            }
        }

        public static void DeactivateAllChalkles()
        {
            if (!IsEnabled.Value) return;

            try
            {
                var chalkFaceType = AccessTools.TypeByName("ChalkFace");
                if (chalkFaceType == null) return;

                var allChalkFaces = Object.FindObjectsOfType(chalkFaceType);
                
                foreach (var chalkFace in allChalkFaces)
                {
                    if (chalkFace == null) continue;

                    var cancelMethod = AccessTools.Method(chalkFaceType, "Cancel");
                    cancelMethod?.Invoke(chalkFace, null);

                    var chalkRendererField = AccessTools.Field(chalkFaceType, "chalkRenderer");
                    var flyingRendererField = AccessTools.Field(chalkFaceType, "flyingRenderer");
                    
                    if (chalkRendererField?.GetValue(chalkFace) is SpriteRenderer chalkRenderer)
                    {
                        chalkRenderer.gameObject.SetActive(false);
                    }
                    
                    if (flyingRendererField?.GetValue(chalkFace) is SpriteRenderer flyingRenderer)
                    {
                        flyingRenderer.gameObject.SetActive(false);
                    }

                    var audManField = AccessTools.Field(chalkFaceType, "audMan");
                    if (audManField?.GetValue(chalkFace) is AudioManager audMan)
                    {
                        audMan.FlushQueue(true);
                    }
                }

                string status = PowerToys.IsRussian ? "<color=#90FF90>ВКЛ</color>" : "<color=#90FF90>ON</color>";
                string message = $"AntiChalkles: {status}";
                PowerToys.ShowInfo(message, 2f, FEATURE_ID);
            }
            catch (System.Exception)
            {
            }
        }

        public static void ReactivateChalkles()
        {
            if (IsEnabled.Value) return;

            try
            {
                var chalkFaceType = AccessTools.TypeByName("ChalkFace");
                if (chalkFaceType == null) return;

                var allChalkFaces = Object.FindObjectsOfType(chalkFaceType);
                
                foreach (var chalkFace in allChalkFaces)
                {
                    if (chalkFace == null) continue;

                    var idleStateType = AccessTools.TypeByName("ChalkFace_Idle");
                    if (idleStateType != null)
                    {
                        var idleConstructor = AccessTools.Constructor(idleStateType, new[] { chalkFaceType });
                        var idleState = idleConstructor?.Invoke(new[] { chalkFace });
                        
                        if (idleState != null)
                        {
                            var stateField = AccessTools.Field(chalkFaceType, "state");
                            stateField?.SetValue(chalkFace, idleState);
                            
                            var behaviorStateMachineField = AccessTools.Field(chalkFaceType.BaseType, "behaviorStateMachine");
                            var behaviorStateMachine = behaviorStateMachineField?.GetValue(chalkFace);
                            
                            if (behaviorStateMachine != null)
                            {
                                var changeStateMethod = AccessTools.Method(behaviorStateMachine.GetType(), "ChangeState");
                                changeStateMethod?.Invoke(behaviorStateMachine, new[] { idleState });
                            }
                        }
                    }
                }

                string status = PowerToys.IsRussian ? "<color=#FF8080>ВЫКЛ</color>" : "<color=#FF8080>OFF</color>";
                string message = $"AntiChalkles: {status}";
                PowerToys.ShowInfo(message, 2f, FEATURE_ID);
            }
            catch (System.Exception)
            {
            }
        }

        private bool _lastEnabledState = false;

        public override void Update()
        {
            if (!IsInitialized) return;

            if (SceneManager.GetActiveScene().name == "MainMenu")
                return;

            if (Singleton<CoreGameManager>.Instance == null || !Singleton<CoreGameManager>.Instance.readyToStart)
                return;

            if (IsEnabled.Value != _lastEnabledState)
            {
                if (IsEnabled.Value)
                {
                    DeactivateAllChalkles();
                }
                else
                {
                    ReactivateChalkles();
                }
                _lastEnabledState = IsEnabled.Value;
            }
        }
    }
}