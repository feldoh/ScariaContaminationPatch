using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using ScariaContaminationPatch.Comps;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch.HarmonyPatches;

public class FungoidPatch
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype))]
    public class PatchSetXenotype
    {
        public static HashSet<string> _fungalZombies = new(new List<string> {"VRE_Fungoid", "AG_Mycormorph"});
        [HarmonyPrefix]
        public static bool Prefix(XenotypeDef ___xenotype, Pawn ___pawn, XenotypeDef xenotype)
        {
            if (!ModLister.CheckBiotech("Xenotypes")
                || Current.ProgramState != ProgramState.Playing
                || xenotype?.defName != "Baseliner"
                || !_fungalZombies.Contains(___xenotype?.defName)
                || PawnGenerator.IsBeingGenerated(___pawn)) return true;
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
    }
}
