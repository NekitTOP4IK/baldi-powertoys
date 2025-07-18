using BepInEx.Configuration;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaldiPowerToys.Settings
{
    public class PowerToysSettingsCategory : CustomOptionsCategory
    {
        private int _currentPage;
        private readonly List<GameObject> _pages = new List<GameObject>();
        private TextMeshProUGUI? _pageText;

        public override void Build()
        {
            var versionText = CreateText("VersionText", $"\n<color=#888888>v{PluginInfo.PLUGIN_VERSION}</color>", Vector3.zero, BaldiFonts.ComicSans18, TextAlignmentOptions.Center, new Vector2(300, 30), Color.gray, false);
            versionText.transform.SetParent(transform, false);
            (versionText.transform as RectTransform)!.anchoredPosition = new Vector2(0f, 100f);

            var pagesContainer = new GameObject("PagesContainer", typeof(RectTransform));
            pagesContainer.transform.SetParent(transform, false);
            (pagesContainer.transform as RectTransform)!.localPosition = new Vector3(139f, 4f, 0f);

            void SetupToggleLayout(MenuToggle toggle)
            {
                var text = toggle.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.alignment = TextAlignmentOptions.Left;

                var button = toggle.GetComponentInChildren<StandardMenuButton>(true);
                if (button != null && button.transform is RectTransform buttonRect) buttonRect.anchoredPosition += new Vector2(-150f, 0);
            }

            var page1 = CreatePage("Page1", pagesContainer.transform);
            _pages.Add(page1);

            var qnlEnabled = Plugin.PublicConfig.Bind("QuickNextLevel", "Enabled", false, "Enable the Quick Next Level feature.");
            MenuToggle qnlToggle = CreateToggle("QNLToggle", "Quick Next Level", qnlEnabled.Value, Vector3.zero, 300f);
            qnlToggle.transform.SetParent(page1.transform, false);
            SetupToggleLayout(qnlToggle);
            qnlToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { qnlEnabled.Value = !qnlEnabled.Value; });

            var qfmEnabled = Plugin.PublicConfig.Bind("QuickFillMap", "Enabled", false, "Enable the Quick Fill Map feature.");
            MenuToggle qfmToggle = CreateToggle("QFMToggle", "Quick Fill Map", qfmEnabled.Value, Vector3.zero, 300f);
            qfmToggle.transform.SetParent(page1.transform, false);
            SetupToggleLayout(qfmToggle);
            qfmToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { qfmEnabled.Value = !qfmEnabled.Value; });

            var qrEnabled = Plugin.PublicConfig.Bind("QuickResults", "Enabled", false, "Enable the Quick Results feature.");
            MenuToggle qrToggle = CreateToggle("QRToggle", "Quick Results", qrEnabled.Value, Vector3.zero, 300f);
            qrToggle.transform.SetParent(page1.transform, false);
            SetupToggleLayout(qrToggle);
            qrToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { qrEnabled.Value = !qrEnabled.Value; });

            var gmEnabled = Plugin.PublicConfig.Bind("GiveMoney", "Enabled", false, "Enable the Give Money feature.");
            MenuToggle gmToggle = CreateToggle("GMToggle", "Give Money", gmEnabled.Value, Vector3.zero, 300f);
            gmToggle.transform.SetParent(page1.transform, false);
            SetupToggleLayout(gmToggle);
            gmToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { gmEnabled.Value = !gmEnabled.Value; });

            var page2 = CreatePage("Page2", pagesContainer.transform);
            _pages.Add(page2);

            var niaEnabled = Plugin.PublicConfig.Bind("NoIncorrectAnswers", "Enabled", false, "Enable the No Incorrect Answers feature.");
            MenuToggle niaToggle = CreateToggle("NIAToggle", "No Incorrect Answers", niaEnabled.Value, Vector3.zero, 300f);
            niaToggle.transform.SetParent(page2.transform, false);
            SetupToggleLayout(niaToggle);
            niaToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { niaEnabled.Value = !niaEnabled.Value; });

            var apsEnabled = Plugin.PublicConfig.Bind("AdjustPlayerSpeed", "Enabled", true, "Enable/disable the player speed adjustment feature.");
            MenuToggle apsToggle = CreateToggle("APSToggle", "Adjust Player Speed", apsEnabled.Value, Vector3.zero, 300f);
            apsToggle.transform.SetParent(page2.transform, false);
            SetupToggleLayout(apsToggle);
            apsToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { apsEnabled.Value = !apsEnabled.Value; });

            var isEnabled = Plugin.PublicConfig.Bind("InfiniteStamina", "Enabled", false, "Enable the Infinite Stamina feature.");
            MenuToggle isToggle = CreateToggle("ISToggle", "Infinite Stamina", isEnabled.Value, Vector3.zero, 300f);
            isToggle.transform.SetParent(page2.transform, false);
            SetupToggleLayout(isToggle);
            isToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { isEnabled.Value = !isEnabled.Value; });

            var fcEnabled = Plugin.PublicConfig.Bind("FreeCamera", "Enabled", true, "Enable/disable the 3D camera feature.");
            MenuToggle fcToggle = CreateToggle("FCToggle", "3D Camera", fcEnabled.Value, Vector3.zero, 300f);
            fcToggle.transform.SetParent(page2.transform, false);
            SetupToggleLayout(fcToggle);
            fcToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { fcEnabled.Value = !fcEnabled.Value; });


            var page3 = CreatePage("Page3", pagesContainer.transform);
            _pages.Add(page3);

            var iiEnabled = Plugin.PublicConfig.Bind("InfiniteItems", "Enabled", false, "Enable the Infinite Items feature.");
            MenuToggle iiToggle = CreateToggle("IIToggle", "Infinite Items", iiEnabled.Value, Vector3.zero, 300f);
            iiToggle.transform.SetParent(page3.transform, false);
            SetupToggleLayout(iiToggle);
            iiToggle.GetComponentInChildren<StandardMenuButton>(true).OnPress.AddListener(() => { iiEnabled.Value = !iiEnabled.Value; });

            var paginationContainer = new GameObject("Pagination", typeof(RectTransform));
            paginationContainer.transform.SetParent(transform, false);
            var containerRect = paginationContainer.transform as RectTransform;
            if (containerRect != null)
            {
                containerRect.anchorMin = new Vector2(0.5f, 0f);
                containerRect.anchorMax = new Vector2(0.5f, 0f);
                containerRect.pivot = new Vector2(0.5f, 0f);
                containerRect.anchoredPosition = new Vector2(0f, -130f);
                containerRect.sizeDelta = new Vector2(250f, 40f);
            }

            var prevButton = CreateButton(() => ChangePage(-1), menuArrowLeft, menuArrowLeftHighlight, "PrevPageButton", Vector3.zero);
            prevButton.transform.SetParent(paginationContainer.transform, false);
            (prevButton.transform as RectTransform)!.anchoredPosition = new Vector2(-54.6667f, 0f);

            _pageText = CreateText("PageIndicator", "1/3", Vector3.zero, BaldiFonts.ComicSans24, TextAlignmentOptions.Center, new Vector2(80, 30), Color.black, false);
            _pageText.transform.SetParent(paginationContainer.transform, false);
            (_pageText.transform as RectTransform)!.anchoredPosition = Vector2.zero;

            var nextButton = CreateButton(() => ChangePage(1), menuArrowRight, menuArrowRightHighlight, "NextPageButton", Vector3.zero);
            nextButton.transform.SetParent(paginationContainer.transform, false);
            (nextButton.transform as RectTransform)!.anchoredPosition = new Vector2(54.6667f, 0f);

            UpdatePageVisibility();
        }

        private GameObject CreatePage(string name, Transform parent)
        {
            var pageObject = new GameObject(name, typeof(RectTransform));
            pageObject.transform.SetParent(parent, false);

            var vLayout = pageObject.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 15f;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandHeight = false;

            return pageObject;
        }

        private void ChangePage(int direction)
        {
            _currentPage = (_currentPage + direction + _pages.Count) % _pages.Count;
            UpdatePageVisibility();
        }

        private void UpdatePageVisibility()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].SetActive(i == _currentPage);
            }

            if (_pageText != null)
            {
                _pageText.text = $"{_currentPage + 1}/{_pages.Count}";
            }
        }
    }
}