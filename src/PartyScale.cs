using System;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Settings;
using Kingmaker.UnitLogic;

namespace UnfairBalance
{
    static class PartyScale
    {
        // Tests set this. The game always leaves it empty and reads the difficulty slider.
        internal static float? Override;

        static bool loggedFailure;

        internal static float Multiplier
        {
            get
            {
                if (Override.HasValue)
                {
                    return Override.Value;
                }

                try
                {
                    var damageToParty = SettingsRoot.Difficulty?.DamageToParty;
                    if (damageToParty == null)
                    {
                        return 1f;
                    }

                    return damageToParty.GetValue();
                }
                catch (Exception ex)
                {
                    if (!loggedFailure)
                    {
                        loggedFailure = true;
                        Main.Log("Damage to party is not readable yet: " + ex.Message);
                    }

                    return 1f;
                }
            }
        }

        internal static bool Scales => Multiplier > 1f;

        internal static bool LifeEnabled => ModToggles.Life;

        internal static bool HealEnabled => ModToggles.Heal;

        internal static int Apply(int value, int floor = 0)
        {
            var multiplier = Multiplier;
            if (value <= 0 || multiplier <= 1f)
            {
                return value;
            }

            var scaled = (int)(value * multiplier);
            return scaled < floor ? floor : scaled;
        }

        internal static bool IsMember(UnitEntityData unit)
        {
            if (unit == null)
            {
                return false;
            }

            try
            {
                return IsMember(unit.Descriptor);
            }
            catch (Exception ex)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    Main.Log("Could not read unit faction: " + ex.Message);
                }

                return false;
            }
        }

        internal static bool IsMember(UnitDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return false;
            }

            try
            {
                return descriptor.IsPlayerFaction;
            }
            catch (Exception ex)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    Main.Log("Could not read unit faction: " + ex.Message);
                }

                return false;
            }
        }
    }
}
