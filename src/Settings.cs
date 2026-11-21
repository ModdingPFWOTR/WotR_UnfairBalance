using UnityModManagerNet;

namespace UnfairBalance
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool DoubleLife = true;
        public bool DoubleHeal = true;
        public bool DoubleBackground = true;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
