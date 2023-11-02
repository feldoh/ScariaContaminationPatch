using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch.HarmonyPatches;

[HarmonyPatch(typeof(GameCondition_NoxiousHaze), nameof(GameCondition_NoxiousHaze.GameConditionTick))]
public static class AcidSmogPatch
{
    [HarmonyPostfix]
    public static void GameConditionTick(GameCondition_NoxiousHaze __instance)
    {
        List<Map> affectedMaps = __instance.AffectedMaps;
        if (Find.TickManager.TicksGame % 3152 != 0) return;
        foreach (Map t in affectedMaps) DoPawnsToxicDamage(t);
    }

    private static void DoPawnsToxicDamage(Map map)
    {
        List<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;
        foreach (Pawn t in allPawnsSpawned) DoPawnToxicDamage(t);
    }

    /**
     * More or less copied from GameCondition_ToxicFallout
     */
    public static void DoPawnToxicDamage(Pawn p, float extraFactor = 1f)
    {
        if (p.Spawned && p.Position.Roofed(p.Map))
            return;
        float resistanceFactor = 0.023f * Mathf.Max(1f - p.GetStatValue(StatDefOf.ToxicResistance), 0.0f) * extraFactor;
        if (ModsConfig.BiotechActive)
            resistanceFactor *= Mathf.Max(1f - p.GetStatValue(StatDefOf.ToxicEnvironmentResistance), 0.0f);
        if (resistanceFactor == 0.0) return;
        float baseSeverityOffset = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(p.thingIDNumber ^ 74374237));
        float sevOffset = resistanceFactor * baseSeverityOffset;
        HealthUtility.AdjustSeverity(p, ScariaZombieDefOf.Taggerung_SCP_ViralBuildup, sevOffset);
    }
}
