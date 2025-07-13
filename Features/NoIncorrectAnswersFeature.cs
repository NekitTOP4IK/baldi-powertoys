using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;

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

                    string message = PowerToys.IsCyrillicPlusLoaded 
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
    }
}
