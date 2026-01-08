using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;
using System.Collections.Generic;
using System.Reflection;

namespace BaldiPowerToys.Features
{
    public class NoIncorrectAnswersFeature : Feature
    {
        private const string FEATURE_ID = "no_incorrect";
        public static new ConfigEntry<bool> IsEnabled { get; private set; } = null!;

        private static readonly Color CORRECT_COLOR = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color BACKGROUND_COLOR = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        void Awake()
        {
            IsEnabled = Plugin.PublicConfig.Bind("NoIncorrectAnswers", "Enabled", false, "Enable the No Incorrect Answers feature.");
        }

        [HarmonyPatch(typeof(MathMachine))]
        class MathMachine_Clicked_Patch
        {
            static readonly AccessTools.FieldRef<MathMachine, int[]> playerHoldingField = AccessTools.FieldRefAccess<MathMachine, int[]>("playerHolding");
            static readonly AccessTools.FieldRef<MathMachine, bool[]> playerIsHoldingField = AccessTools.FieldRefAccess<MathMachine, bool[]>("playerIsHolding");
            static readonly AccessTools.FieldRef<MathMachine, int> answerField = AccessTools.FieldRefAccess<MathMachine, int>("answer");
            static readonly AccessTools.FieldRef<Activity, bool> poweredField = AccessTools.FieldRefAccess<Activity, bool>("powered");

            [HarmonyPrefix]
            [HarmonyPatch("Clicked")]
            static void Prefix(MathMachine __instance, int player)
            {
                if (!IsEnabled.Value) return;

                bool isPowered = poweredField(__instance);
                bool[] playerIsHolding = playerIsHoldingField(__instance);

                if (!isPowered || playerIsHolding == null || player >= playerIsHolding.Length || !playerIsHolding[player])
                {
                    return;
                }

                int correctAnswer = answerField(__instance);
                int[] playerAnswers = playerHoldingField(__instance);
                if (playerAnswers == null || player >= playerAnswers.Length) return;
                int playerAnswer = playerAnswers[player];

                if (playerAnswer != correctAnswer)
                {
                    playerAnswers[player] = correctAnswer;

                    string message = PowerToys.IsRussian 
                        ? "Неверный ответ исправлен!" 
                        : "Incorrect answer corrected!";

                    PowerToys.ShowNotification(
                        message,
                        duration: 1.2f,
                        barColor: CORRECT_COLOR,
                        backgroundColor: BACKGROUND_COLOR,
                        sourceId: FEATURE_ID
                    );
                }
            }
        }



        [HarmonyPatch]
        class BalloonBuster_SubmitAnswer_Patch
        {
            static MethodBase TargetMethod()
            {
                var balloonBusterType = AccessTools.TypeByName("BalloonBuster");
                return AccessTools.Method(balloonBusterType, "SubmitAnswer");
            }

            [HarmonyPrefix]
            static bool Prefix(object __instance)
            {
                if (!IsEnabled.Value) return true;

                try
                {
                    var poweredField = AccessTools.Field(__instance.GetType().BaseType, "powered");
                    var completedField = AccessTools.Field(__instance.GetType().BaseType, "completed");
                    var countingField = AccessTools.Field(__instance.GetType(), "counting");
                    
                    bool isPowered = (bool)poweredField.GetValue(__instance);
                    bool isCompleted = (bool)completedField.GetValue(__instance);
                    bool isCounting = (bool)countingField.GetValue(__instance);
                    
                    if (!isPowered || isCompleted || isCounting) return true;

                    var balloonBusterType = __instance.GetType();
                    var solutionField = AccessTools.Field(balloonBusterType, "solution");
                    var balloonField = AccessTools.Field(balloonBusterType, "balloon");
                    var startingTotalField = AccessTools.Field(balloonBusterType, "startingTotal");

                    if (solutionField == null || balloonField == null || startingTotalField == null)
                        return true;

                    int solution = (int)solutionField.GetValue(__instance);
                    var balloons = (System.Array)balloonField.GetValue(__instance);
                    int startingTotal = (int)startingTotalField.GetValue(__instance);

                    
                    int currentUnpoppedCount = 0;
                    for (int i = 0; i < startingTotal; i++)
                    {
                        var balloon = balloons.GetValue(i);
                        if (balloon != null)
                        {
                            var poppedProperty = AccessTools.Property(balloon.GetType(), "popped");
                            bool isPopped = (bool)poppedProperty.GetValue(balloon);
                            if (!isPopped) currentUnpoppedCount++;
                        }
                    }

                   
                    if (currentUnpoppedCount != solution)
                    {
                        
                        if (currentUnpoppedCount > solution)
                        {
                            int balloonsToPopCount = currentUnpoppedCount - solution;
                            int poppedCount = 0;

                            for (int i = 0; i < startingTotal && poppedCount < balloonsToPopCount; i++)
                            {
                                var balloon = balloons.GetValue(i);
                                if (balloon != null)
                                {
                                    var poppedProperty = AccessTools.Property(balloon.GetType(), "popped");
                                    bool isPopped = (bool)poppedProperty.GetValue(balloon);
                                    if (!isPopped)
                                    {
                                        var popMethod = AccessTools.Method(balloon.GetType(), "Pop", new[] { typeof(bool) });
                                        popMethod.Invoke(balloon, new object[] { true });
                                        poppedCount++;
                                    }
                                }
                            }
                        }
                        
                        else if (currentUnpoppedCount < solution)
                        {
                            int balloonsToRestoreCount = solution - currentUnpoppedCount;
                            int restoredCount = 0;

                            for (int i = 0; i < startingTotal && restoredCount < balloonsToRestoreCount; i++)
                            {
                                var balloon = balloons.GetValue(i);
                                if (balloon != null)
                                {
                                    var poppedProperty = AccessTools.Property(balloon.GetType(), "popped");
                                    bool isPopped = (bool)poppedProperty.GetValue(balloon);
                                    if (isPopped)
                                    {
                                        
                                        poppedProperty.SetValue(balloon, false);
                                        restoredCount++;
                                    }
                                }
                            }
                        }

                        string message = PowerToys.IsRussian 
                            ? "Неправильный ответ исправлен!" 
                            : "Incorrect answer corrected!";

                        PowerToys.ShowNotification(
                            message,
                            duration: 1.2f,
                            barColor: CORRECT_COLOR,
                            backgroundColor: BACKGROUND_COLOR,
                            sourceId: FEATURE_ID
                        );
                    }
                }
                catch (System.Exception)
                {
                    
                }
                
                
                return true;
            }
        }


        [HarmonyPatch]
        class MatchActivity_BalloonRevealed_Patch
        {
            static MethodBase TargetMethod()
            {
                var matchActivityType = AccessTools.TypeByName("MatchActivity");
                return AccessTools.Method(matchActivityType, "BalloonRevealed", new[] { AccessTools.TypeByName("MatchActivityBalloon") });
            }

            [HarmonyPrefix]
            static void Prefix(object __instance, object revealedBalloon)
            {
                if (!IsEnabled.Value) return;

                try
                {
                    
                    var poweredField = AccessTools.Field(__instance.GetType().BaseType, "powered");
                    var completedField = AccessTools.Field(__instance.GetType().BaseType, "completed");
                    
                    bool isPowered = (bool)poweredField.GetValue(__instance);
                    bool isCompleted = (bool)completedField.GetValue(__instance);
                    
                    if (!isPowered || isCompleted) return;

                    
                    var revealedBalloonsField = AccessTools.Field(__instance.GetType(), "revealedBalloons");
                    var revealedBalloons = revealedBalloonsField.GetValue(__instance);
                    var countProperty = AccessTools.Property(revealedBalloons.GetType(), "Count");
                    int revealedCount = (int)countProperty.GetValue(revealedBalloons);

                    
                    if (revealedCount == 1)
                    {
                        var indexerProperty = AccessTools.Property(revealedBalloons.GetType(), "Item");
                        var firstBalloon = indexerProperty.GetValue(revealedBalloons, new object[] { 0 });

                        
                        var matchingBalloonProperty = AccessTools.Property(firstBalloon.GetType(), "MatchingBalloon");
                        var correctPair = matchingBalloonProperty.GetValue(firstBalloon);

                        
                        if (!revealedBalloon.Equals(correctPair))
                        {
                            
                            var matchingBalloonField = AccessTools.Field(revealedBalloon.GetType(), "matchingBalloon");
                            var firstBalloonMatchingField = AccessTools.Field(firstBalloon.GetType(), "matchingBalloon");
                            
                            matchingBalloonField.SetValue(revealedBalloon, firstBalloon);
                            firstBalloonMatchingField.SetValue(firstBalloon, revealedBalloon);

                            string message = PowerToys.IsRussian 
                                ? "Пара исправлена!" 
                                : "Pair corrected!";

                            PowerToys.ShowNotification(
                                message,
                                duration: 1.2f,
                                barColor: CORRECT_COLOR,
                                backgroundColor: BACKGROUND_COLOR,
                                sourceId: FEATURE_ID
                            );
                        }
                    }
                }
                catch (System.Exception)
                {
                    
                }
            }
        }

        [HarmonyPatch]
        class MatchActivity_Fail_Patch
        {
            static MethodBase TargetMethod()
            {
                var matchActivityType = AccessTools.TypeByName("MatchActivity");
                return AccessTools.Method(matchActivityType, "Fail");
            }

            [HarmonyPrefix]
            static bool Prefix()
            {
                if (!IsEnabled.Value) return true;
                
                
                string message = PowerToys.IsRussian 
                    ? "Ошибка предотвращена!" 
                    : "Failure prevented!";

                PowerToys.ShowNotification(
                    message,
                    duration: 1.2f,
                    barColor: CORRECT_COLOR,
                    backgroundColor: BACKGROUND_COLOR,
                    sourceId: FEATURE_ID
                );
                
                return false;
            }
        }


    }
}
