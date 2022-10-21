using UnityEngine;
using Verse;

namespace ScariaContaminationPatch
{
    public class ScariaContaminationPatchSettings: ModSettings
    {
        private const float RowHeight = 22f;
        
        private readonly Listing_Standard _options = new();

        public bool AllowInstantKillOfNonZombies;
        public bool AllowInstantKillOfPlayerFaction;
        public bool AllowInstantKillOfGuests;
        public float InstantKillChance;
        public int CriticalHeadshotCooldown;
        public string CriticalHeadshotCooldownEditBuffer;
        
        public void DoWindowContents(Rect wrect)
        {
            _options.Begin(wrect);
            
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowNonZombies".Translate(), ref AllowInstantKillOfNonZombies);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowPlayerPawns".Translate(), ref AllowInstantKillOfPlayerFaction);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowGuests".Translate(), ref AllowInstantKillOfGuests);
            _options.GapLine();
            _options.Gap();
            _options.Label("ScariaContaminationPatch_CriticalHeadshotCooldown".Translate());
            _options.IntEntry(ref CriticalHeadshotCooldown, ref CriticalHeadshotCooldownEditBuffer);
            _options.GapLine();
            _options.Gap();
            var instantKillChanceRect = _options.GetRect(RowHeight);
            var instantKillChanceLabel = "ScariaContaminationPatch_InstantKillChance".Translate(InstantKillChance * 100);
            InstantKillChance = Widgets.HorizontalSlider(instantKillChanceRect, InstantKillChance * 100, 0f, 100.0f, true,
                instantKillChanceLabel, "0", "100", 0.5f) / 100f;
            _options.Gap();
            _options.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref InstantKillChance, "InstantKillChance", 1.0f);
            Scribe_Values.Look(ref AllowInstantKillOfNonZombies, "AllowInstantKillOfNonZombies", true);
            Scribe_Values.Look(ref AllowInstantKillOfPlayerFaction, "AllowInstantKillOfPlayerFaction", true);
            Scribe_Values.Look(ref AllowInstantKillOfGuests, "AllowInstantKillOfGuests", true);
            Scribe_Values.Look(ref CriticalHeadshotCooldown, "CriticalHeadshotCooldown");
        }
    }
}
