using System;
using HarmonyLib;
using UnityModManagerNet;

namespace UnfairBalance
{
    public static class Main
    {
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony HarmonyInstance;
        public static Settings Settings;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry) ?? new Settings();
            ModToggles.CopyFrom(Settings);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            try
            {
                HarmonyInstance.PatchAll(typeof(Main).Assembly);
                try
                {
                    SecondBackgroundButton.Apply(HarmonyInstance);
                }
                catch (Exception ex)
                {
                    Log("Second background button was not installed: " + ex);
                }

                Log("Unfair Balance loaded. Party scale follows the damage-to-party setting.");
            }
            catch (Exception ex)
            {
                Log("Harmony patch failed: " + ex);
                return false;
            }

            return true;
        }

        public static void Log(string message)
        {
            ModEntry?.Logger.Log(message);
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            var scale = PartyScale.Multiplier;
            UnityEngine.GUILayout.Label("Damage to party is " + scale.ToString("0.##") + ".");
            Settings.DoubleLife = UnityEngine.GUILayout.Toggle(Settings.DoubleLife, "2x Life");
            Settings.DoubleHeal = UnityEngine.GUILayout.Toggle(Settings.DoubleHeal, "2x Heal");
            Settings.DoubleBackground = UnityEngine.GUILayout.Toggle(Settings.DoubleBackground, "2x Background");
            ModToggles.CopyFrom(Settings);
            UnityEngine.GUILayout.Label("Life and heal use that multiplier while it is above 1. On Core and below they change nothing.");
            UnityEngine.GUILayout.Label("Life adds class hit dice, Constitution, favored class, Toughness and Mythic Toughness first, then multiplies that total.");
            UnityEngine.GUILayout.Label("Heal covers cure, channel, lay on hands, potions and fast healing, after the roll.");
            UnityEngine.GUILayout.Label("Background offers an optional second background during creation, mercenary hiring and respec.");
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings?.Save(modEntry);
        }
    }
}
