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
        private string _criticalHeadshotCooldownEditBuffer;
        
        public void DoWindowContents(Rect wrect)
        {
            _options.Begin(wrect);
            
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowNonZombies".Translate(), ref AllowInstantKillOfNonZombies);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowPlayerPawns".Translate(), ref AllowInstantKillOfPlayerFaction);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowGuests".Translate(), ref AllowInstantKillOfGuests);
            _options.GapLine();
            _options.Gap();
            _options.Label("ScariaContaminationPatch_CriticalHeadshotCooldown".Translate());
            _options.IntEntry(ref CriticalHeadshotCooldown, ref _criticalHeadshotCooldownEditBuffer);
            _options.GapLine();
            _options.Gap();
            Rect instantKillChanceRect = _options.GetRect(RowHeight);
            string instantKillChanceLabel = "ScariaContaminationPatch_InstantKillChance".Translate(InstantKillChance * 100);
            Widgets.HorizontalSlider(instantKillChanceRect, ref InstantKillChance, new FloatRange(0f, 1f), instantKillChanceLabel);
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
