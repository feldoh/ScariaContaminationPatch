using HarmonyLib;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch
{
    public class ScariaContaminationPatch : Mod
    {
        public static ScariaContaminationPatchSettings Settings;

        public ScariaContaminationPatch(ModContentPack content) : base(content)
        {
            new Harmony("Garethp.RimWorld.ScariaContainmentPatch.main").PatchAll();
            Settings = GetSettings<ScariaContaminationPatchSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Scaria Zombies";
        }
    }
}
