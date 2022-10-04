using HarmonyLib;
using Verse;

namespace ScariaContaminationPatch
{
    public class Mod: Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            new Harmony("Garethp.rimworld.ScariaContainmentPatch.main").PatchAll();
        }
    }
}