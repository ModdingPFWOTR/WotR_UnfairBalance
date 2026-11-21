using System;
using System.Collections.Generic;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.CharGen;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace UnfairBalance
{
    static class SecondBackground
    {
        internal static IFeatureSelectionItem Chosen;
        internal static UnitEntityData PendingUnit;
        internal static bool Picking;
        internal static Action RefreshLabel;

        static FeatureSource source;
        static int level;
        static bool reverting;

        internal static bool Reverting
        {
            get { return reverting; }
        }

        internal static bool Enabled
        {
            get { return ModToggles.Background; }
        }

        internal static bool ShouldOffer(LevelUpState.CharBuildMode mode, int nextCharacterLevel, bool pet, bool respecSession, bool background)
        {
            if (!background || pet || nextCharacterLevel != 1)
            {
                return false;
            }

            if (mode == LevelUpState.CharBuildMode.CharGen || mode == LevelUpState.CharBuildMode.Respec)
            {
                return true;
            }

            return mode == LevelUpState.CharBuildMode.LevelUp && respecSession;
        }

        internal static bool IsBackground(FeatureSelectionState state)
        {
            var selection = state == null ? null : state.Selection as BlueprintFeatureSelection;
            if (selection == null)
            {
                return false;
            }

            return selection.Group == FeatureGroup.BackgroundSelection || selection.Group2 == FeatureGroup.BackgroundSelection;
        }

        internal static bool ShouldOffer(LevelUpController controller, FeatureSelectionState state)
        {
            if (!Enabled || controller == null || controller.State == null || controller.Unit == null || !IsBackground(state))
            {
                return false;
            }

            var pet = false;
            try
            {
                pet = controller.Unit.IsPet;
            }
            catch (Exception ex)
            {
                Main.Log("Could not tell whether this unit is a pet: " + ex.Message);
                pet = true;
            }

            return ShouldOffer(controller.State.Mode, controller.State.NextCharacterLevel, pet, InRespecSession(), true);
        }

        internal static BlueprintFeature ResolvePick(BlueprintFeature main, BlueprintFeature chosen, BlueprintFeature picked, bool picking, out bool swallow)
        {
            swallow = false;
            if (!picking)
            {
                if (chosen != null && chosen == picked)
                {
                    return null;
                }

                return chosen;
            }

            if (main == null)
            {
                return chosen;
            }

            swallow = true;
            if (picked == null || picked == main || picked == chosen)
            {
                return null;
            }

            return picked;
        }

        internal static void Remember(IFeatureSelectionItem item, FeatureSelectionState state, UnitEntityData unit)
        {
            Chosen = item;
            source = state == null ? default(FeatureSource) : state.Source;
            level = state == null ? 1 : state.Level;
            PendingUnit = item == null ? null : unit;
            Picking = false;
            RefreshLabel?.Invoke();
        }

        internal static void BeginRevert()
        {
            reverting = true;
        }

        internal static void EndRevert()
        {
            reverting = false;
        }

        internal static void Apply(LevelUpController controller)
        {
            if (controller == null || !controller.Committed || Chosen == null || PendingUnit != controller.Unit)
            {
                return;
            }

            if (!Enabled)
            {
                Clear();
                return;
            }

            var item = Chosen;
            var featureSource = source;
            var featureLevel = level;
            Clear();

            try
            {
                var feature = item.Feature;
                var descriptor = controller.Unit == null ? null : controller.Unit.Descriptor;
                if (feature == null || descriptor == null)
                {
                    return;
                }

                AddGranted(descriptor, feature, item.Param, featureSource, featureLevel, new HashSet<BlueprintFeature>());
                Main.Log("Second background added: " + feature.Name);
            }
            catch (Exception ex)
            {
                Main.Log("Second background was not added: " + ex.Message);
            }
        }

        internal static void OnStopped(LevelUpController controller)
        {
            if (controller == null || controller.Committed)
            {
                return;
            }

            if (controller.Unit == PendingUnit)
            {
                Clear();
            }
        }

        internal static void Clear()
        {
            Chosen = null;
            PendingUnit = null;
            Picking = false;
            RefreshLabel?.Invoke();
        }

        internal static string Label()
        {
            if (Picking)
            {
                return "Click a background";
            }

            var feature = Chosen == null ? null : Chosen.Feature;
            if (feature != null)
            {
                return feature.Name;
            }

            return "Second background";
        }

        static void AddGranted(UnitDescriptor descriptor, BlueprintFeature feature, FeatureParam param, FeatureSource featureSource, int featureLevel, HashSet<BlueprintFeature> seen)
        {
            if (feature == null || !seen.Add(feature))
            {
                return;
            }

            var fact = UnitHelper.AddFact<Feature>(descriptor, feature, null, param);
            if (fact == null)
            {
                Main.Log("Second background " + feature.Name + " was not added.");
                return;
            }

            fact.SetSource(featureSource, featureLevel);
            var components = feature.ComponentsArray;
            if (components == null)
            {
                return;
            }

            for (var i = 0; i < components.Length; i++)
            {
                var extra = components[i] as AddFeatureOnApply;
                if (extra != null)
                {
                    AddGranted(descriptor, extra.Feature, null, featureSource, featureLevel, seen);
                }
            }
        }

        static bool InRespecSession()
        {
            try
            {
                var window = RespecWindowVM.Instance;
                return window != null && window.IsRespec;
            }
            catch (Exception ex)
            {
                Main.Log("Respec window is not readable: " + ex.Message);
                return false;
            }
        }
    }
}
