using HarmonyLib;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.FeatureSelector;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace UnfairBalance
{
    [HarmonyPatch(typeof(CharGenFeatureSelectorStateVM), "SetFeature")]
    static class SecondBackgroundPickPatch
    {
        static bool Prefix(CharGenFeatureSelectorStateVM __instance, CharGenFeatureSelectorItemVM itemVM)
        {
            if (SecondBackground.Reverting || __instance == null)
            {
                return true;
            }

            if (!SecondBackground.Enabled)
            {
                if (SecondBackground.Picking)
                {
                    SecondBackground.Picking = false;
                    SecondBackground.RefreshLabel?.Invoke();
                }

                return true;
            }

            var state = __instance.SelectionState;
            var pickedItem = itemVM == null ? null : itemVM.Feature;
            var picked = pickedItem == null ? null : pickedItem.Feature;
            var chosen = SecondBackground.Chosen == null ? null : SecondBackground.Chosen.Feature;

            if (!SecondBackground.Picking || !SecondBackground.IsBackground(state))
            {
                if (SecondBackground.Picking)
                {
                    SecondBackground.Picking = false;
                    SecondBackground.RefreshLabel?.Invoke();
                }

                if (picked != null && picked == chosen)
                {
                    SecondBackground.Clear();
                }

                return true;
            }

            var mainItem = state.SelectedItem;
            var main = mainItem == null ? null : mainItem.Feature;
            var stored = SecondBackground.ResolvePick(main, chosen, picked, true, out var swallow);
            if (!swallow)
            {
                SecondBackground.Picking = false;
                SecondBackground.RefreshLabel?.Invoke();
                return true;
            }

            if (stored == null)
            {
                SecondBackground.Clear();
            }
            else
            {
                var controller = Traverse.Create(__instance).Field("m_LevelUpController").GetValue<LevelUpController>();
                SecondBackground.Remember(pickedItem, state, controller == null ? null : controller.Unit);
            }

            SnapBack(__instance, mainItem);
            return false;
        }

        static void SnapBack(CharGenFeatureSelectorStateVM vm, IFeatureSelectionItem main)
        {
            if (vm == null || vm.SelectedItemVM == null || main == null)
            {
                return;
            }

            var current = vm.SelectedItemVM.Value;
            if (current != null && current.Feature != null && current.Feature.Feature == main.Feature)
            {
                return;
            }

            var entities = vm.ExtractNestedEntities();
            if (entities == null)
            {
                return;
            }

            SecondBackground.BeginRevert();
            try
            {
                for (var i = 0; i < entities.Count; i++)
                {
                    var item = entities[i] as CharGenFeatureSelectorItemVM;
                    if (item != null && item.Feature != null && item.Feature.Feature == main.Feature)
                    {
                        vm.SelectedItemVM.Value = item;
                        return;
                    }
                }
            }
            finally
            {
                SecondBackground.EndRevert();
            }
        }
    }

    [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
    static class SecondBackgroundCommitPatch
    {
        static void Postfix(LevelUpController __instance)
        {
            SecondBackground.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Stop))]
    static class SecondBackgroundStopPatch
    {
        static void Postfix(LevelUpController __instance)
        {
            SecondBackground.OnStopped(__instance);
        }
    }
}
