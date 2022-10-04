using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ScariaContaminationPatch.HarmonyPatches
{
    [HarmonyPatch(typeof(JobGiver_Berserk), "FindPawnTarget")]
    public class PatchJobGiver_Berserk
    {
        public static bool Prefix(ref Pawn __result, Pawn pawn)
        {
            __result = (Pawn) AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedReachable,
                x => x is Pawn pawn1 && pawn1.Spawned && !pawn1.Downed && !pawn1.IsInvisible() && !pawn1.health.hediffSet.HasHediff(HediffDefOf.Scaria),
                maxDist: 40f,
                canBashDoors: true
            );

            return false;
        }
    }
    
    [HarmonyPatch(typeof(JobGiver_Manhunter), "FindPawnTarget")]
    public class PatchJobGiver_Manhunter
    {
        public static bool Prefix(ref Pawn __result, Pawn pawn, bool canBashFences)
        {
            __result = (Pawn) AttackTargetFinder.BestAttackTarget(
                pawn, 
                TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, 
                x => x is Pawn pawn1 && pawn1.def.race.intelligence >= Intelligence.ToolUser && !pawn1.health.hediffSet.HasHediff(HediffDefOf.Scaria), 
                canBashDoors: true, 
                canBashFences: canBashFences
            );

            return false;
        }
    }
}