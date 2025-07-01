using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BaldiPowerToys.Features
{
    public class QuickResultsFeature : MonoBehaviour
    {
        public static ConfigEntry<bool> IsEnabled = null!;

        void Awake()
        {
            IsEnabled = Plugin.PublicConfig.Bind("QuickResults", "Enabled", false, "Enable the Quick Results feature.");
        }
    }

    [HarmonyPatch(typeof(ElevatorScreen))]
    class ElevatorScreen_Patch
    {
        static readonly AccessTools.FieldRef<ElevatorScreen, bool> busyField = AccessTools.FieldRefAccess<ElevatorScreen, bool>("busy");
        static readonly AccessTools.FieldRef<ElevatorScreen, int> ytpMultiplierField = AccessTools.FieldRefAccess<ElevatorScreen, int>("ytpMultiplier");
        static readonly AccessTools.FieldRef<ElevatorScreen, BigScreen> bigScreenField = AccessTools.FieldRefAccess<ElevatorScreen, BigScreen>("bigScreen");

        [HarmonyPrefix]
        [HarmonyPatch("Results")]
        static bool Results_Prefix(ref IEnumerator __result, ElevatorScreen __instance, float gameTime, int timeBonus)
        {
            if (!QuickResultsFeature.IsEnabled.Value)
            {
                return true; // Run original method
            }

            __result = CustomResults(__instance, gameTime, timeBonus);
            return false; // Skip original method
        }

        static IEnumerator CustomResults(ElevatorScreen instance, float gameTime, int timeBonus)
        {
            yield return null;

            int ytpMultiplier = ytpMultiplierField(instance);
            BigScreen bigScreen = bigScreenField(instance);

            busyField(instance) = true;

            bigScreen.animator.speed = 10f;

            bigScreen.animator.Play("SwingDown", -1, 0f);
            float time = 0.1f;
            while (time > 0f)
            {
                time -= Time.unscaledDeltaTime;
                yield return null;
            }
            bigScreen.resultsText.SetActive(value: true);
            TMP_Text toFill = bigScreen.time;
            string value = "";
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 4:
                        {
                            bigScreen.timeText.SetActive(value: true);
                            bigScreen.time.gameObject.SetActive(value: true);
                            toFill = bigScreen.time;
                            string text = Mathf.Floor(gameTime % 60f).ToString();
                            if (Mathf.Floor(gameTime % 60f) < 10f)
                            {
                                text = "0" + Mathf.Floor(gameTime % 60f);
                            }
                            value = Mathf.Floor(gameTime / 60f) + ":" + text;
                            break;
                        }
                    case 0:
                        bigScreen.pointsText.SetActive(value: true);
                        bigScreen.points.gameObject.SetActive(value: true);
                        toFill = bigScreen.points;
                        value = Singleton<CoreGameManager>.Instance.GetPointsThisLevel(0).ToString();
                        break;
                    case 1:
                        bigScreen.multiplierText.SetActive(value: true);
                        bigScreen.multiplier.gameObject.SetActive(value: true);
                        toFill = bigScreen.multiplier;
                        value = ytpMultiplier + "X";
                        break;
                    case 2:
                        bigScreen.totalText.SetActive(value: true);
                        bigScreen.total.gameObject.SetActive(value: true);
                        toFill = bigScreen.total;
                        value = (Singleton<CoreGameManager>.Instance.GetPointsThisLevel(0) * ytpMultiplier).ToString();
                        break;
                    case 3:
                        bigScreen.gradeText.SetActive(value: true);
                        bigScreen.grade.gameObject.SetActive(value: true);
                        toFill = bigScreen.grade;
                        value = Singleton<CoreGameManager>.Instance.Grade;
                        break;
                }
                time = 0.05f;
                while (time > 0f)
                {
                    switch (i)
                    {
                        case 4: toFill.text = Random.Range(0, 9999).ToString(); break;
                        case 0: toFill.text = Random.Range(0, 9999).ToString(); break;
                        case 1: toFill.text = Random.Range(1, 4) + "X"; break;
                        case 2: toFill.text = Random.Range(0, 9999).ToString(); break;
                        case 3: toFill.text = bigScreen.grades[Random.Range(0, bigScreen.grades.Length)]; break;
                    }
                    time -= Time.unscaledDeltaTime;
                    yield return null;
                }
                toFill.text = value;
                switch (i)
                {
                    case 4:
                        if (timeBonus > 0)
                        {
                            bigScreen.timeBonusText.SetActive(value: true);
                            bigScreen.timeBonus.gameObject.SetActive(value: true);
                            bigScreen.timeBonus.text = "+" + timeBonus + " YTPs";
                        }
                        break;
                    case 3:
                        if (CoreGameManager.gradeBonusVal[Singleton<CoreGameManager>.Instance.GradeVal] != 0)
                        {
                            bigScreen.gradeBonusText.SetActive(value: true);
                            bigScreen.gradeBonus.gameObject.SetActive(value: true);
                            bigScreen.gradeBonus.text = "+" + CoreGameManager.gradeBonusVal[Singleton<CoreGameManager>.Instance.GradeVal] + " YTPs";
                        }
                        break;
                }
                time = 0.05f;
                while (time > 0f)
                {
                    time -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            time = 0.1f;
            while (time > 0f)
            {
                time -= Time.unscaledDeltaTime;
                yield return null;
            }
            bigScreen.resultsText.SetActive(value: false);
            bigScreen.time.gameObject.SetActive(value: false);
            bigScreen.points.gameObject.SetActive(value: false);
            bigScreen.total.gameObject.SetActive(value: false);
            bigScreen.grade.gameObject.SetActive(value: false);
            bigScreen.timeText.SetActive(value: false);
            bigScreen.pointsText.SetActive(value: false);
            bigScreen.gradeText.SetActive(value: false);
            bigScreen.totalText.SetActive(value: false);
            bigScreen.gradeBonusText.SetActive(value: false);
            bigScreen.timeBonusText.SetActive(value: false);
            bigScreen.timeBonus.gameObject.SetActive(value: false);
            bigScreen.gradeBonus.gameObject.SetActive(value: false);
            bigScreen.multiplierText.gameObject.SetActive(value: false);
            bigScreen.multiplier.gameObject.SetActive(value: false);
            bigScreen.animator.Play("SwingUp", -1, 0f);
            time = 0.1f;
            while (time > 0f)
            {
                time -= Time.unscaledDeltaTime;
                yield return null;
            }

            bigScreen.animator.speed = 1f;
            busyField(instance) = false;
        }
    }
}
