using System;
using HarmonyLib;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.FeatureSelector;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.FeatureSelector;
using Kingmaker.UnitLogic.Class.LevelUp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnfairBalance
{
    static class SecondBackgroundButton
    {
        const string ButtonName = "UnfairBalanceSecondBackground";

        static CharGenFeatureSelectorPhaseDetailedPCView owner;
        static TextMeshProUGUI label;

        internal static void Apply(Harmony harmony)
        {
            var view = typeof(CharGenFeatureSelectorPhaseDetailedPCView);
            harmony.Patch(
                AccessTools.Method(view, "BindViewImplementation"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(SecondBackgroundButton), "Bind")));
            harmony.Patch(
                AccessTools.Method(view, "DestroyViewImplementation"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(SecondBackgroundButton), "Destroy")));
        }

        static void Bind(CharGenFeatureSelectorPhaseDetailedPCView __instance)
        {
            try
            {
                Attach(__instance);
            }
            catch (Exception ex)
            {
                Main.Log("Second background button failed: " + ex.Message);
            }
        }

        static void Destroy(CharGenFeatureSelectorPhaseDetailedPCView __instance)
        {
            if (owner == __instance)
            {
                Release();
            }
        }

        static void Attach(CharGenFeatureSelectorPhaseDetailedPCView view)
        {
            Release();
            var phase = Traverse.Create(view).Property("ViewModel").GetValue<CharGenFeatureSelectorPhaseVM>();
            var state = phase == null ? null : phase.FeatureSelectorStateVM;
            var controller = state == null ? null : Traverse.Create(state).Field("m_LevelUpController").GetValue<LevelUpController>();
            if (state == null || !SecondBackground.ShouldOffer(controller, state.SelectionState))
            {
                return;
            }

            var header = Traverse.Create(view).Field("m_SelectorHeader").GetValue<TextMeshProUGUI>();
            if (header == null || header.transform.parent == null)
            {
                Main.Log("Second background button has nowhere to sit on the background screen.");
                return;
            }

            owner = view;
            SecondBackground.RefreshLabel = Refresh;

            var parent = header.transform.parent;
            var existing = parent.Find(ButtonName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var buttonObject = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.transform.SetSiblingIndex(header.transform.GetSiblingIndex() + 1);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.13f, 0.09f, 0.95f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Toggle(view));

            var textObject = new GameObject("Label", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = header.font;
            label.fontSharedMaterial = header.fontSharedMaterial;
            label.fontSize = header.fontSize > 1f ? header.fontSize : 18f;
            label.color = header.color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var headerRect = header.rectTransform;
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = headerRect.anchorMin;
            rect.anchorMax = headerRect.anchorMax;
            rect.pivot = headerRect.pivot;
            var width = headerRect.sizeDelta.x > 1f ? headerRect.sizeDelta.x : 360f;
            var height = headerRect.sizeDelta.y > 1f ? headerRect.sizeDelta.y : 46f;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = headerRect.anchoredPosition + new Vector2(0f, -(height + 8f));
            Refresh();
        }

        static void Toggle(CharGenFeatureSelectorPhaseDetailedPCView view)
        {
            var phase = Traverse.Create(view).Property("ViewModel").GetValue<CharGenFeatureSelectorPhaseVM>();
            var state = phase == null ? null : phase.FeatureSelectorStateVM;
            var controller = state == null ? null : Traverse.Create(state).Field("m_LevelUpController").GetValue<LevelUpController>();
            if (state == null || !SecondBackground.ShouldOffer(controller, state.SelectionState))
            {
                return;
            }

            SecondBackground.Picking = !SecondBackground.Picking;
            Refresh();
        }

        static void Refresh()
        {
            if (label != null)
            {
                label.text = SecondBackground.Label();
            }
        }

        static void Release()
        {
            if (owner != null)
            {
                var header = Traverse.Create(owner).Field("m_SelectorHeader").GetValue<TextMeshProUGUI>();
                if (header != null && header.transform.parent != null)
                {
                    var existing = header.transform.parent.Find(ButtonName);
                    if (existing != null)
                    {
                        UnityEngine.Object.Destroy(existing.gameObject);
                    }
                }
            }

            owner = null;
            label = null;
            SecondBackground.RefreshLabel = null;
        }
    }
}
