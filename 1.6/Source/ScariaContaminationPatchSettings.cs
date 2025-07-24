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

        public bool AllowInfectedNPCBerserk = true;
        public bool AllowInstantKillOfNonZombies = true;
        public bool AllowInstantKillOfPlayerFaction = true;
        public bool AllowInstantKillOfGuests = true;
        public bool AllowAggressiveFoodHunt = true;
        public bool AllowInfectedHunting = false;
        public bool ScariaMustBeAlive = true;
        public bool ScariaPlayerFactionOnly = false;
        public bool ScariaNonHostileOnly = true;
        public float DoorAttackChance = 0.4f;
        public float InstantKillChance = 0.6f;
        public float InfectedHungerFactor = 1;
        public float UnstoppableHungerChance = 0.8f;
        public float ImmunityGeneChance = 0.01f;
        public int CriticalHeadshotCooldown = 100;
        public int ScariaBasePointsOnRemoved = 1;
        private string _criticalHeadshotCooldownEditBuffer;
        private string _scariaBasePointsOnRemovedBuffer;

        private List<float> _hungerfactorCache;

        public void DoWindowContents(Rect wrect)
        {
            _options.Begin(wrect);

            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowNonZombies".Translate(), ref AllowInstantKillOfNonZombies);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowPlayerPawns".Translate(), ref AllowInstantKillOfPlayerFaction);
            _options.CheckboxLabeled("ScariaContaminationPatch_InstantKillAllowGuests".Translate(), ref AllowInstantKillOfGuests);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowAggressiveFoodHunt".Translate(), ref AllowAggressiveFoodHunt);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowHuntingInfected".Translate(), ref AllowInfectedHunting);
            _options.CheckboxLabeled("ScariaContaminationPatch_AllowInfectedNPCBerserk".Translate(), ref AllowInfectedNPCBerserk);
            if (ModsConfig.IdeologyActive)
            {
                _options.CheckboxLabeled("ScariaContaminationPatch_ScariaMustBeAlive".Translate(), ref ScariaMustBeAlive);
                _options.CheckboxLabeled("ScariaContaminationPatch_ScariaPlayerFactionOnly".Translate(), ref ScariaPlayerFactionOnly);
                _options.CheckboxLabeled("ScariaContaminationPatch_ScariaNonHostileOnly".Translate(), ref ScariaNonHostileOnly);
            }
            _options.Gap();
            _options.Label("ScariaContaminationPatch_ScariaBasePointsOnRemoved".Translate());
            _options.IntEntry(ref ScariaBasePointsOnRemoved, ref _scariaBasePointsOnRemovedBuffer);
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
            Rect immunityGeneChanceRect = _options.GetRect(RowHeight);
            string immunityGeneChanceLabel = "ScariaContaminationPatch_ImmunityGeneChance".Translate(ImmunityGeneChance.ToStringPercent());
            Widgets.HorizontalSlider(immunityGeneChanceRect, ref ImmunityGeneChance, new FloatRange(0f, 1f), immunityGeneChanceLabel);
            _options.Gap();
            Rect infectedHungerFactorRect = _options.GetRect(RowHeight);
            string infectedHungerFactorLabel = "ScariaContaminationPatch_InfectedHungerFactor".Translate(InfectedHungerFactor.ToStringDecimalIfSmall());
            float currentHungerFactor = InfectedHungerFactor;
            Widgets.HorizontalSlider(infectedHungerFactorRect, ref InfectedHungerFactor, new FloatRange(0f, 10f), infectedHungerFactorLabel);
            if (Math.Abs(InfectedHungerFactor - currentHungerFactor) > 0.005f) ApplyInfectedHungerFactor();
            _options.End();
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
            Scribe_Values.Look(ref InstantKillChance, "InstantKillChance", 1.0f);
            Scribe_Values.Look(ref DoorAttackChance, "DoorAttackChance", 0.4f);
            Scribe_Values.Look(ref ImmunityGeneChance, "ImmunityGeneChance", 0.01f);
            Scribe_Values.Look(ref InfectedHungerFactor, "InfectedHungerFactor", 1.0f);
            Scribe_Values.Look(ref UnstoppableHungerChance, "UnstoppableHungerChance", 0.8f);
            Scribe_Values.Look(ref AllowInfectedNPCBerserk, "AllowInfectedNPCBerserk", true);
            Scribe_Values.Look(ref AllowInstantKillOfNonZombies, "AllowInstantKillOfNonZombies", true);
            Scribe_Values.Look(ref AllowInstantKillOfPlayerFaction, "AllowInstantKillOfPlayerFaction", true);
            Scribe_Values.Look(ref AllowInstantKillOfGuests, "AllowInstantKillOfGuests", true);
            Scribe_Values.Look(ref AllowAggressiveFoodHunt, "AllowAggressiveFoodHunt", false);
            Scribe_Values.Look(ref AllowInfectedHunting, "AllowInfectedHunting", false);
            Scribe_Values.Look(ref CriticalHeadshotCooldown, "CriticalHeadshotCooldown");
            Scribe_Values.Look(ref ScariaBasePointsOnRemoved, "ScariaBasePointsOnRemoved", 1);
            Scribe_Values.Look(ref ScariaMustBeAlive, "ScariaMustBeAlive", true);
            Scribe_Values.Look(ref ScariaPlayerFactionOnly, "ScariaPlayerFactionOnly", false);
            Scribe_Values.Look(ref ScariaNonHostileOnly, "ScariaNonHostileOnly", true);
        }

        public void ApplySettings()
        {
            ApplyInfectedHungerFactor();
        }
    }
}
