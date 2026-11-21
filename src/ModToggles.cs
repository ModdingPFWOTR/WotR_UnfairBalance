namespace UnfairBalance
{
    static class ModToggles
    {
        internal static bool Life = true;
        internal static bool Heal = true;
        internal static bool Background = true;

        internal static void CopyFrom(Settings settings)
        {
            if (settings == null)
            {
                return;
            }

            Life = settings.DoubleLife;
            Heal = settings.DoubleHeal;
            Background = settings.DoubleBackground;
        }
    }
}
