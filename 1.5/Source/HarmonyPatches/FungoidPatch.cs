using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using ScariaContaminationPatch.Comps;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype))]
public class PatchSetXenotype
{
    public static HashSet<string> _fungalZombies = new(new List<string> { "VRE_Fungoid", "AG_Mycormorph" });

    [HarmonyPrefix]
    public static bool Prefix(XenotypeDef ___xenotype, Pawn ___pawn, XenotypeDef xenotype, out bool __state)
    {
        __state = false;
        if (!ModLister.CheckBiotech("Xenotypes")
            || Current.ProgramState != ProgramState.Playing
            || xenotype?.defName != "Baseliner"
            || !_fungalZombies.Contains(___xenotype?.defName)
            || PawnGenerator.IsBeingGenerated(___pawn)) return true;

        // Maybe grant immunity gene
        __state = Rand.Chance(ScariaContaminationPatch.Settings.ImmunityGeneChance);

        // If no Scaria to cure give points anyway
        if (___pawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Scaria) is not { } hediff)
        {
            if (!ModLister.IdeologyInstalled
                || ___pawn.HostileTo(Faction.OfPlayer)
                || Faction.OfPlayer.ideos.FluidIdeo is not { } ideo
                || !(ideo.development?.CanBeDevelopedNow ?? false)) return true;
            PreceptComp_CuringDevelopmentPoints bestPreceptComp = null;
            foreach (Precept p in ideo.PreceptsListForReading)
            {
                foreach (PreceptComp c in p.def.comps)
                {
                    if (c is PreceptComp_CuringDevelopmentPoints pc && pc.curePointsMultiplier > (bestPreceptComp?.curePointsMultiplier ?? 0))
                    {
                        bestPreceptComp = pc;
                    }
                }
            }

            if (bestPreceptComp is null) return true;
            int pointsOnRemoved = (int)(1 * bestPreceptComp.curePointsMultiplier);
            ideo.development.TryAddDevelopmentPoints(Mathf.Clamp(pointsOnRemoved, bestPreceptComp.minPoints, bestPreceptComp.maxPoints));
        }
        else
        {
            TaggedString text = HealthUtility.Cure(hediff);
            if (!PawnUtility.ShouldSendNotificationAbout(___pawn)) return true;
            Messages.Message(text, (Thing)___pawn, MessageTypeDefOf.PositiveEvent);
            return true;
        }

        return true;
    }

    [HarmonyPostfix]
    public static void Postfix(Pawn ___pawn, bool __state)
    {
        if (!__state) return;
        ___pawn.genes.AddGene(ScariaZombieDefOf.Taggerung_SCP_ScariaImmunity, true);
        if (!PawnUtility.ShouldSendNotificationAbout(___pawn)) return;
        Find.LetterStack.ReceiveLetter("ScariaContaminationPatch_GeneImmunityLetter".Translate(),
            "ScariaContaminationPatch_GeneImmunityLetterText".Translate(___pawn.NameShortColored, HediffDefOf.Scaria.LabelCap),
            LetterDefOf.PositiveEvent, (Thing)___pawn);
    }
}

[HarmonyPatch(typeof(Building_StylingStation), nameof(Building_StylingStation.GetFloatMenuOptions))]
public class PatchStylingStation
{
    [HarmonyPostfix]
    public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(IEnumerable<FloatMenuOption> __result, Pawn selPawn, Building_StylingStation __instance)
    {
        foreach (FloatMenuOption floatMenuOption in __result) yield return floatMenuOption;
        if (selPawn?.story?.hairDef is not { } hair || hair == HairDefOf.Bald) yield break;
        selPawn.style.nextHairDef = HairDefOf.Bald;
        yield return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("ScariaContaminationPatch_MakeBald".Translate().CapitalizeFirst(),
                () => selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.UseStylingStation, (LocalTargetInfo)__instance))), selPawn,
            (LocalTargetInfo)__instance);
    }
}
