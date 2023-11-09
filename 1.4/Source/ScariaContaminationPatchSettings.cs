using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch
{
    public class ScariaContaminationPatchSettings : ModSettings
    {
        private const float RowHeight = 22f;

        private readonly Listing_Standard _options = new();

        public bool AllowInfectedNPCBerserk;
        public bool AllowInstantKillOfNonZombies;
        public bool AllowInstantKillOfPlayerFaction;
        public bool AllowInstantKillOfGuests;
        public bool AllowAggressiveFoodHunt;
        public bool AllowInfectedHunting;
        public float DoorAttackChance;
        public float InstantKillChance;
        public float InfectedHungerFactor;
        public float BerserkRageMtb;
        public float UnstoppableHungerChance;
        public int CriticalHeadshotCooldown;
        public int SurvivalDays;
        private string _criticalHeadshotCooldownEditBuffer;
        private string _survivalDaysEditBuffer;

        private List<float> _hungerfactorCache;

        public static float BaseMtbDaysToCauseBerserk => HediffDefOf.Scaria?.CompProps<HediffCompProperties_CauseMentalState>()?.mtbDaysToCauseMentalState ?? 0f;
        public static int BaseSurvivalDays => HediffDefOf.Scaria?.CompProps<HediffCompProperties_KillAfterDays>()?.days ?? 0;

        public void DoWindowContents(Rect wrect)
        {
            _options.Begin(wrect);

            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowNonZombies".Translate(), ref AllowInstantKillOfNonZombies);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowPlayerPawns".Translate(), ref AllowInstantKillOfPlayerFaction);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowGuests".Translate(), ref AllowInstantKillOfGuests);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowAggressiveFoodHunt".Translate(), ref AllowAggressiveFoodHunt);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowHuntingInfected".Translate(), ref AllowInfectedHunting);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowInfectedNPCBerserk".Translate(), ref AllowInfectedNPCBerserk);
            _options.GapLine();
            _options.Gap();
            _options.Label("ScariaContaminationPatch_CriticalHeadshotCooldown".Translate());
            _options.IntEntry(ref CriticalHeadshotCooldown, ref _criticalHeadshotCooldownEditBuffer);
            _options.GapLine();
            _options.Gap();
            Rect instantKillChanceRect = _options.GetRect(RowHeight);
            string instantKillChanceLabel = "ScariaContaminationPatch_InstantKillChance".Translate(InstantKillChance.ToStringPercent());
            Widgets.HorizontalSlider(instantKillChanceRect, ref InstantKillChance, new FloatRange(0f, 1f), instantKillChanceLabel);
            _options.Gap();
            Rect doorAttackChanceRect = _options.GetRect(RowHeight);
            string doorAttackChanceLabel = "ScariaContaminationPatch_DoorAttackChance".Translate(DoorAttackChance.ToStringPercent());
            Widgets.HorizontalSlider(doorAttackChanceRect, ref DoorAttackChance, new FloatRange(0f, 1f), doorAttackChanceLabel);
            _options.Gap();
            Rect unstoppableHungerChanceRect = _options.GetRect(RowHeight);
            string unstoppableHungerChanceLabel = "ScariaContaminationPatch_UnstoppableHungerChance".Translate(UnstoppableHungerChance.ToStringPercent());
            Widgets.HorizontalSlider(unstoppableHungerChanceRect, ref UnstoppableHungerChance, new FloatRange(0f, 1f), unstoppableHungerChanceLabel);
            _options.Gap();
            Rect infectedHungerFactorRect = _options.GetRect(RowHeight);
            string infectedHungerFactorLabel = "ScariaContaminationPatch_InfectedHungerFactor".Translate(InfectedHungerFactor.ToStringDecimalIfSmall());
            float currentHungerFactor = InfectedHungerFactor;
            Widgets.HorizontalSlider(infectedHungerFactorRect, ref InfectedHungerFactor, new FloatRange(0f, 10f), infectedHungerFactorLabel);
            if (Math.Abs(InfectedHungerFactor - currentHungerFactor) > 0.005f) ApplyInfectedHungerFactor();
            _options.Gap();
            Rect berserkIntervalRect = _options.GetRect(RowHeight);
            string berserkIntervalLabel = "ScariaContaminationPatch_BerserkInterval".Translate(BerserkRageMtb.ToStringDecimalIfSmall());
            float currentBerserkRageMtb = BerserkRageMtb;
            Widgets.HorizontalSlider(berserkIntervalRect, ref BerserkRageMtb, new FloatRange(0f, 10f), berserkIntervalLabel);
            if (Math.Abs(currentBerserkRageMtb - BerserkRageMtb) > 0.005f) ApplyBerserkRageMtb();
            _options.Gap();
            _options.Label("ScariaContaminationPatch_SurvivalDays".Translate());
            _options.IntEntry(ref SurvivalDays, ref _survivalDaysEditBuffer);
            _options.End();
        }

        private void ApplyBerserkRageMtb()
        {
            if (BerserkRageMtb <= 0f) BerserkRageMtb = BaseMtbDaysToCauseBerserk;
            if (BerserkRageMtb > 0) HediffDefOf.Scaria.CompProps<HediffCompProperties_CauseMentalState>().mtbDaysToCauseMentalState = BerserkRageMtb;
        }

        private void ApplySurvivalDays()
        {
            if (SurvivalDays <= -1f) SurvivalDays = BaseSurvivalDays;

            if (SurvivalDays <= 0f && HediffDefOf.Scaria.CompProps<HediffCompProperties_KillAfterDays>() is { } compToRemove)
            {
                HediffDefOf.Scaria.comps.Remove(compToRemove);
                return;
            }

            if (SurvivalDays <= 0) return;
            if (HediffDefOf.Scaria.CompProps<HediffCompProperties_KillAfterDays>() is { } comp)
            {
                comp.days = SurvivalDays;
            }
            else
            {
                HediffDefOf.Scaria.comps.Add(new HediffCompProperties_KillAfterDays { days = SurvivalDays });
            }
        }

        public void ApplyInfectedHungerFactor()
        {
            if (_hungerfactorCache == null || _hungerfactorCache.Count == 0)
            {
                _hungerfactorCache = HediffDefOf.Scaria.stages.Select(s => s.hungerRateFactor).ToList();
            }

            for (int i = 0; i < HediffDefOf.Scaria.stages.Count; i++)
            {
                HediffDefOf.Scaria.stages[i].hungerRateFactor = _hungerfactorCache[i] * InfectedHungerFactor;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref BerserkRageMtb, "BerserkRageMtb", 0f);
            Scribe_Values.Look(ref InstantKillChance, "InstantKillChance", 1.0f);
            Scribe_Values.Look(ref DoorAttackChance, "DoorAttackChance", 0.4f);
            Scribe_Values.Look(ref InfectedHungerFactor, "InfectedHungerFactor", 1.0f);
            Scribe_Values.Look(ref UnstoppableHungerChance, "UnstoppableHungerChance", 0.8f);
            Scribe_Values.Look(ref AllowInfectedNPCBerserk, "AllowInfectedNPCBerserk", true);
            Scribe_Values.Look(ref AllowInstantKillOfNonZombies, "AllowInstantKillOfNonZombies", true);
            Scribe_Values.Look(ref AllowInstantKillOfPlayerFaction, "AllowInstantKillOfPlayerFaction", true);
            Scribe_Values.Look(ref AllowInstantKillOfGuests, "AllowInstantKillOfGuests", true);
            Scribe_Values.Look(ref AllowAggressiveFoodHunt, "AllowAggressiveFoodHunt", false);
            Scribe_Values.Look(ref AllowInfectedHunting, "AllowInfectedHunting", false);
            Scribe_Values.Look(ref CriticalHeadshotCooldown, "CriticalHeadshotCooldown");
            Scribe_Values.Look(ref SurvivalDays, "SurvivalDays", -1);
        }

        public void ApplySettings()
        {
            ApplyInfectedHungerFactor();
            ApplyBerserkRageMtb();
            ApplySurvivalDays();
        }
    }
}
