using System.Reflection;
using HarmonyLib;
using Kingmaker.EntitySystem.Stats;

namespace UnfairBalance
{
    // OnUpdate runs after hit dice, Constitution, favored class and feat modifiers are already summed.
    [HarmonyPatch(typeof(ModifiableValue), "OnUpdate")]
    static class HitPointScalePatch
    {
        static readonly FieldInfo ModifiedValue = AccessTools.Field(typeof(ModifiableValue), "<ModifiedValue>k__BackingField");
        static readonly FieldInfo ModifiedValueRaw = AccessTools.Field(typeof(ModifiableValue), "<ModifiedValueRaw>k__BackingField");
        static readonly FieldInfo PermanentValue = AccessTools.Field(typeof(ModifiableValue), "<PermanentValue>k__BackingField");

        static void Postfix(ModifiableValue __instance)
        {
            if (!(__instance is ModifiableValueHitPoints) || !PartyScale.LifeEnabled || !PartyScale.Scales || !PartyScale.IsMember(__instance.Owner))
            {
                return;
            }

            var floor = __instance.MinValue;
            Write(ModifiedValue, __instance, PartyScale.Apply(Read(ModifiedValue, __instance), floor));
            Write(ModifiedValueRaw, __instance, PartyScale.Apply(Read(ModifiedValueRaw, __instance), floor));
            Write(PermanentValue, __instance, PartyScale.Apply(Read(PermanentValue, __instance), floor));
        }

        static int Read(FieldInfo field, ModifiableValue stat)
        {
            return (int)field.GetValue(stat);
        }

        static void Write(FieldInfo field, ModifiableValue stat, int value)
        {
            field.SetValue(stat, value);
        }
    }
}
