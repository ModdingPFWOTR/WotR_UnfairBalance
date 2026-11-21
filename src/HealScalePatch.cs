using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Kingmaker.RuleSystem.Rules.Damage;

namespace UnfairBalance
{
    // ValueWithoutReduction is the finished heal, after dice, bonus, empower and resistance.
    // Scaling it here happens before the missing-HP cap, the combat log and the mirrored companion heal.
    [HarmonyPatch(typeof(RuleHealDamage), nameof(RuleHealDamage.OnTrigger))]
    static class HealScalePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setter = AccessTools.PropertySetter(typeof(RuleHealDamage), nameof(RuleHealDamage.ValueWithoutReduction));
            var scale = AccessTools.Method(typeof(HealScalePatch), nameof(ScaleHeal));
            var rewritten = new List<CodeInstruction>();
            var hits = 0;

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(setter))
                {
                    hits++;
                    rewritten.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    rewritten.Add(new CodeInstruction(OpCodes.Call, scale));
                }

                rewritten.Add(instruction);
            }

            if (hits != 1)
            {
                throw new System.InvalidOperationException(
                    "RuleHealDamage.OnTrigger changed: expected one ValueWithoutReduction assignment, found " + hits + ".");
            }

            return rewritten;
        }

        public static int ScaleHeal(int value, RuleHealDamage rule)
        {
            if (rule == null || !PartyScale.HealEnabled || !PartyScale.IsMember(rule.Target))
            {
                return value;
            }

            return PartyScale.Apply(value);
        }
    }
}
