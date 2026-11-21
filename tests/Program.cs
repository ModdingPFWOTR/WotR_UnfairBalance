using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;

namespace UnfairBalance.Tests
{
    public static class Program
    {
        internal static readonly HashSet<UnitDescriptor> Party = new HashSet<UnitDescriptor>();

        static readonly List<string> Failures = new List<string>();

        public static int Run()
        {
            var harmony = new Harmony("UnfairBalance.Tests");
            harmony.PatchAll(typeof(Main).Assembly);
            harmony.PatchAll(typeof(Program).Assembly);

            HealKeepsFinishedRollThenDoubles();
            HealCapsAtMissingHealth();
            HealStaysPutWhenSliderIsOne();
            EnemyHealIsNotScaled();
            EmpowerIsIncludedBeforeTheMultiplier();
            HealIsSubtractedFromDamage();
            HitPointsDoubleTheFinishedTotal();
            UpdatingAgainDoesNotStackTheMultiplier();
            EnemyHitPointsStayPut();
            HitPointsStayPutWhenSliderIsOne();
            FractionalSliderTruncates();
            OtherStatsAreLeftAlone();
            SecondBackgroundIsOfferedOnlyOnTheFirstCreationLevel();
            SecondBackgroundClickDoesNotReplaceTheMainBackground();
            EachToggleCanBeTurnedOffOnItsOwn();

            if (Failures.Count == 0)
            {
                Console.WriteLine("All checks passed.");
                return 0;
            }

            Console.WriteLine(Failures.Count + " failed:");
            foreach (var failure in Failures)
            {
                Console.WriteLine(" - " + failure);
            }

            return 1;
        }

        static void HealKeepsFinishedRollThenDoubles()
        {
            PartyScale.Override = 2f;
            var rule = Cast(party: true, missing: 100, bonus: 30);
            Check("party heal 30 becomes 60", rule.ValueWithoutReduction, 60);
            Check("party heal is not capped when health is missing", rule.Value, 60);
        }

        static void HealCapsAtMissingHealth()
        {
            PartyScale.Override = 2f;
            var rule = Cast(party: true, missing: 40, bonus: 30);
            Check("uncapped heal is the doubled roll", rule.ValueWithoutReduction, 60);
            Check("applied heal stops at missing health", rule.Value, 40);
        }

        static void HealStaysPutWhenSliderIsOne()
        {
            PartyScale.Override = 1f;
            var rule = Cast(party: true, missing: 100, bonus: 30);
            Check("slider 1 does not change the heal", rule.Value, 30);
        }

        static void EnemyHealIsNotScaled()
        {
            PartyScale.Override = 2f;
            var rule = Cast(party: false, missing: 100, bonus: 30);
            Check("enemy heal stays 30", rule.Value, 30);
        }

        static void EmpowerIsIncludedBeforeTheMultiplier()
        {
            PartyScale.Override = 2f;
            var rule = Cast(party: true, missing: 200, bonus: 30, empower: true);
            Check("empower then double is 90", rule.ValueWithoutReduction, 90);
        }

        static void HealIsSubtractedFromDamage()
        {
            PartyScale.Override = 2f;
            var target = MakeUnit(party: true, missing: 100);
            try
            {
                Cast(target, bonus: 30, fake: false);
            }
            catch (Exception ex)
            {
                Fail("applying the heal threw " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            Check("100 missing health becomes 40 after a doubled 30 heal", target.Damage, 40);
        }

        static void HitPointsDoubleTheFinishedTotal()
        {
            PartyScale.Override = 2f;
            var hp = MakeHitPoints(party: true, total: 18, permanent: 15);
            Recalculate(hp);
            Check("finished 18 hit points, toughness included, become 36", hp.ModifiedValue, 36);
            Check("permanent hit points scale on their own", hp.PermanentValue, 30);
        }

        static void UpdatingAgainDoesNotStackTheMultiplier()
        {
            PartyScale.Override = 2f;
            var hp = MakeHitPoints(party: true, total: 18, permanent: 15);
            Recalculate(hp);
            Set(hp, "<ModifiedValue>k__BackingField", 18);
            Set(hp, "<ModifiedValueRaw>k__BackingField", 18);
            Set(hp, "<PermanentValue>k__BackingField", 15);
            Recalculate(hp);
            Check("a fresh total of 18 doubles once", hp.ModifiedValue, 36);
        }

        static void EnemyHitPointsStayPut()
        {
            PartyScale.Override = 2f;
            var hp = MakeHitPoints(party: false, total: 18, permanent: 15);
            Recalculate(hp);
            Check("enemy hit points stay 18", hp.ModifiedValue, 18);
        }

        static void HitPointsStayPutWhenSliderIsOne()
        {
            PartyScale.Override = 1f;
            var hp = MakeHitPoints(party: true, total: 18, permanent: 15);
            Recalculate(hp);
            Check("slider 1 leaves hit points at 18", hp.ModifiedValue, 18);
        }

        static void FractionalSliderTruncates()
        {
            PartyScale.Override = 1.5f;
            var hp = MakeHitPoints(party: true, total: 47, permanent: 47);
            Recalculate(hp);
            Check("47 times 1.5 truncates to 70", hp.ModifiedValue, 70);
        }

        static void OtherStatsAreLeftAlone()
        {
            PartyScale.Override = 2f;
            var owner = MakeDescriptor(party: true, missing: 0);
            var stats = (CharacterStats)FormatterServices.GetUninitializedObject(typeof(CharacterStats));
            Set(stats, "<Owner>k__BackingField", owner);
            var speed = (ModifiableValue)FormatterServices.GetUninitializedObject(typeof(ModifiableValue));
            Set(speed, "m_Stats", stats);
            Set(speed, "<ModifiedValue>k__BackingField", 30);
            Recalculate(speed);
            Check("a non hit-point stat stays 30", speed.ModifiedValue, 30);
        }

        static RuleHealDamage Cast(bool party, int missing, int bonus, bool empower = false)
        {
            return Cast(MakeUnit(party, missing), bonus, true, empower);
        }

        static RuleHealDamage Cast(UnitEntityData target, int bonus, bool fake, bool empower = false)
        {
            var rule = new RuleHealDamage(target, target, new DiceFormula(0, DiceType.D6), bonus);
            rule.CalculationType.Set(DamageCalculationType.Maximized, Metamagic.Maximize);
            rule.IsFake = fake;
            if (empower)
            {
                rule.Empower.Set(true, Metamagic.Empower);
            }

            rule.OnTrigger(null);
            return rule;
        }

        static UnitEntityData MakeUnit(bool party, int missing)
        {
            var descriptor = MakeDescriptor(party, missing);
            var unit = (UnitEntityData)FormatterServices.GetUninitializedObject(typeof(UnitEntityData));
            Set(unit, "<Descriptor>k__BackingField", descriptor);
            Set(unit, "Parts", new EntityPartsManager(unit));
            return unit;
        }

        static UnitDescriptor MakeDescriptor(bool party, int missing)
        {
            var descriptor = (UnitDescriptor)FormatterServices.GetUninitializedObject(typeof(UnitDescriptor));
            Set(descriptor, "m_Damage", missing);
            if (party)
            {
                Party.Add(descriptor);
            }

            return descriptor;
        }

        static ModifiableValueHitPoints MakeHitPoints(bool party, int total, int permanent)
        {
            var owner = MakeDescriptor(party, 0);
            var stats = (CharacterStats)FormatterServices.GetUninitializedObject(typeof(CharacterStats));
            Set(stats, "<Owner>k__BackingField", owner);
            var hp = (ModifiableValueHitPoints)FormatterServices.GetUninitializedObject(typeof(ModifiableValueHitPoints));
            Set(hp, "m_Stats", stats);
            Set(hp, "<ModifiedValue>k__BackingField", total);
            Set(hp, "<ModifiedValueRaw>k__BackingField", total);
            Set(hp, "<PermanentValue>k__BackingField", permanent);
            return hp;
        }

        static void Recalculate(ModifiableValue stat)
        {
            typeof(ModifiableValue)
                .GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stat, null);
        }

        static void Set(object target, string name, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(target.GetType().FullName, name);
        }

        static void SecondBackgroundIsOfferedOnlyOnTheFirstCreationLevel()
        {
            var creation = LevelUpState.CharBuildMode.CharGen;
            Check("creation offers a second background", SecondBackground.ShouldOffer(creation, 1, false, false, true));
            Check("respec offers a second background", SecondBackground.ShouldOffer(LevelUpState.CharBuildMode.Respec, 1, false, false, true));
            Check("ally respec offers a second background", SecondBackground.ShouldOffer(LevelUpState.CharBuildMode.LevelUp, 1, false, true, true));
            Check("a normal level up does not offer it", !SecondBackground.ShouldOffer(LevelUpState.CharBuildMode.LevelUp, 1, false, false, true));
            Check("mythic does not offer it", !SecondBackground.ShouldOffer(LevelUpState.CharBuildMode.Mythic, 1, false, false, true));
            Check("a pet does not get it", !SecondBackground.ShouldOffer(creation, 1, true, false, true));
            Check("later levels do not offer it", !SecondBackground.ShouldOffer(creation, 2, false, false, true));
            Check("other feature screens do not offer it", !SecondBackground.ShouldOffer(creation, 1, false, false, false));
        }

        static void SecondBackgroundClickDoesNotReplaceTheMainBackground()
        {
            var main = Background();
            var other = Background();
            var third = Background();
            bool swallow;

            var stored = SecondBackground.ResolvePick(main, null, other, true, out swallow);
            Check("a different background is stored", stored == other && swallow);

            stored = SecondBackground.ResolvePick(main, other, main, true, out swallow);
            Check("clicking the main background clears the second", stored == null && swallow);

            stored = SecondBackground.ResolvePick(main, other, other, true, out swallow);
            Check("clicking the second background again clears it", stored == null && swallow);

            stored = SecondBackground.ResolvePick(main, other, third, true, out swallow);
            Check("a new second background replaces the previous one", stored == third && swallow);

            stored = SecondBackground.ResolvePick(null, null, other, true, out swallow);
            Check("the first background click stays the main background", stored == null && !swallow);

            stored = SecondBackground.ResolvePick(main, other, third, false, out swallow);
            Check("changing the main background keeps the second", stored == other && !swallow);

            stored = SecondBackground.ResolvePick(main, other, other, false, out swallow);
            Check("taking the second background as the main one clears it", stored == null && !swallow);
        }

        static void EachToggleCanBeTurnedOffOnItsOwn()
        {
            PartyScale.Override = 2f;
            try
            {
                ModToggles.Heal = false;
                var heal = Cast(party: true, missing: 100, bonus: 30);
                Check("heal stays 30 while 2x Heal is off", heal.Value, 30);

                ModToggles.Heal = true;
                ModToggles.Life = false;
                var hp = MakeHitPoints(party: true, total: 18, permanent: 15);
                Recalculate(hp);
                Check("hit points stay 18 while 2x Life is off", Read(hp, "<ModifiedValue>k__BackingField"), 18);

                ModToggles.Life = true;
                var restored = MakeHitPoints(party: true, total: 18, permanent: 15);
                Recalculate(restored);
                Check("hit points double again when 2x Life is on", Read(restored, "<ModifiedValue>k__BackingField"), 36);
                var doubled = Cast(party: true, missing: 100, bonus: 30);
                Check("heal doubles again when 2x Heal is on", doubled.Value, 60);
                Check("a background screen is hidden while 2x Background is off", !OfferedWhenBackgroundToggleIs(false));
                Check("a background screen is offered while 2x Background is on", OfferedWhenBackgroundToggleIs(true));
            }
            finally
            {
                ModToggles.Life = true;
                ModToggles.Heal = true;
                ModToggles.Background = true;
                PartyScale.Override = null;
            }
        }

        static bool OfferedWhenBackgroundToggleIs(bool enabled)
        {
            ModToggles.Background = enabled;
            return SecondBackground.ShouldOffer(LevelUpState.CharBuildMode.CharGen, 1, false, false, true)
                && SecondBackground.Enabled;
        }

        static int Read(object target, string name)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return (int)field.GetValue(target);
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(target.GetType().FullName, name);
        }

        static BlueprintFeature Background()
        {
            return (BlueprintFeature)FormatterServices.GetUninitializedObject(typeof(BlueprintFeature));
        }

        static void Check(string name, bool condition)
        {
            if (!condition)
            {
                Fail(name);
            }
            else
            {
                Console.WriteLine("ok  " + name);
            }
        }

        static void Check(string name, int actual, int expected)
        {
            if (actual != expected)
            {
                Fail(name + " (expected " + expected + ", got " + actual + ")");
            }
            else
            {
                Console.WriteLine("ok  " + name);
            }
        }

        static void Fail(string message)
        {
            Failures.Add(message);
            Console.WriteLine("FAIL " + message);
        }
    }

    [HarmonyPatch(typeof(UnitDescriptor), "get_IsPlayerFaction")]
    static class FactionStub
    {
        static bool Prefix(UnitDescriptor __instance, ref bool __result)
        {
            __result = Program.Party.Contains(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(ShadowSpellLogicExtension), nameof(ShadowSpellLogicExtension.SkipBecauseOfShadow))]
    static class ShadowStub
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(UnitPartDualCompanion), nameof(UnitPartDualCompanion.HandleHealing))]
    static class MirrorHealStub
    {
        static bool Prefix()
        {
            return false;
        }
    }
}
