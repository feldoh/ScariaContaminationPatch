using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ScariaContaminationPatch.HarmonyPatches
{
    [HarmonyPatch(typeof(JobGiver_Berserk), "FindPawnTarget")]
    public class PatchJobGiver_Berserk
    {
        public static bool Prefix(ref Pawn __result, Pawn pawn)
        {
            __result = (Pawn)AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedReachable,
                x => x is Pawn pawn1 && pawn1.Spawned && !pawn1.Downed && !pawn1.IsInvisible() &&
                     !pawn1.health.hediffSet.HasHediff(HediffDefOf.Scaria),
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
            __result = (Pawn)AttackTargetFinder.BestAttackTarget(
                pawn,
                TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable,
                x => x is Pawn pawn1 && pawn1.def.race.intelligence >= Intelligence.ToolUser &&
                     !pawn1.health.hediffSet.HasHediff(HediffDefOf.Scaria),
                canBashDoors: true,
                canBashFences: canBashFences
            );

            return false;
        }
    }

    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "ExtraTargetValidator")]
    public class PatchJobGiver_AIFightEnemy
    {
        public static bool Prefix(ref bool __result, Pawn pawn, Thing target)
        {
            // Only check if the target is otherwise valid and attacker is not player controlled or does not have scaria.
            // Berserk pawns wont attack people with scaria so we only need to see about cancelling if the attacker has it.
            if (pawn.IsColonistPlayerControlled ||
                !(target is Pawn pawnTarget) ||
                pawnTarget.def.race.intelligence < Intelligence.ToolUser ||
                !(MentalStateDefOf.Berserk.Equals(pawnTarget.MentalStateDef) ||
                  MentalStateDefOf.Manhunter.Equals(pawnTarget.MentalStateDef) ||
                  MentalStateDefOf.ManhunterPermanent.Equals(pawnTarget.MentalStateDef)) ||
                !pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria))
                return true;

#if DEBUG
            Log.Message($"Marked {pawnTarget.Name} as invalid target for {pawn.Name} as they are not a threat to this pawn");
#endif
            // We know the pawn must be non-player, have scaria, and the target must be berserk or manhunter so this is an invalid target.
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public class PatchJobGiver_MentalStateHandler
    {
        public static bool Prefix(ref bool __result, Pawn ___pawn,
            MentalStateDef stateDef,
            string reason = null,
            bool forceWake = false,
            bool causedByMood = false,
            Pawn otherPawn = null,
            bool transitionSilently = false,
            bool causedByDamage = false,
            bool causedByPsycast = false)
        {
            // Prevent scaria ridden non-player pawns going berserk
            if (!MentalStateDefOf.Berserk.Equals(stateDef) ||
                ___pawn.IsColonist ||
                ___pawn.def.race.intelligence < Intelligence.ToolUser ||
                !___pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)) return true;

#if DEBUG
            Log.Message("Preventing pawn from running berserk as they are a non-colonist with scaria");
#endif
            __result = false;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyDamageToPart")]
    public class PatchDamageWorker_AddInjury
    {
        private static int lastTick;
        private static bool instantKillAllowed(Pawn pawn)
        {
            return Rand.Chance(ScariaContaminationPatch.Settings.InstantKillChance) &&
                   (pawn.kindDef.defName.ToLowerInvariant().Contains("zombie") ||
                    ScariaContaminationPatch.Settings.AllowInstantKillOfNonZombies) &&
                   ((!pawn.HostFaction?.IsPlayer ?? false) ||
                    ScariaContaminationPatch.Settings.AllowInstantKillOfGuests) &&
                   (!pawn.Faction.IsPlayerSafe() || ScariaContaminationPatch.Settings.AllowInstantKillOfPlayerFaction);
        }
        
        public static void Postfix(DamageInfo dinfo, Pawn pawn, DamageWorker.DamageResult result)
        {
#if DEBUG
            Log.Message($"Next headshot at {lastTick + ScariaContaminationPatch.Settings.CriticalHeadshotCooldown}, current {Find.TickManager.TicksGame} : {lastTick + ScariaContaminationPatch.Settings.CriticalHeadshotCooldown - Find.TickManager.TicksGame}");
#endif
            // Everyone knows zombies are weak to headshots.
            int ticks;
            if (!result.headshot ||
                (ticks = Find.TickManager.TicksGame) < lastTick + ScariaContaminationPatch.Settings.CriticalHeadshotCooldown ||
                !pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria) ||
                pawn.Destroyed ||
                pawn.Dead ||
                !(pawn.health?.hediffSet?.HasHead ?? false)) return;
            
            lastTick = ticks;
#if DEBUG
            Log.Message($"BOOM Headshot! on {pawn}");
#endif
            if (instantKillAllowed(pawn))
            {
                MoteMaker.ThrowText(new Vector3(pawn.Position.x + 1f, pawn.Position.y, pawn.Position.z + 1f), pawn.Map,
                    "ScariaContaminationPatch_Headshot".Translate(), Color.red);
                var hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
                var hediffMissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn);
                hediffMissingPart.lastInjury = hediffDefFromDamage;
                hediffMissingPart.Part = dinfo.HitPart;
                pawn.health.AddHediff(hediffMissingPart);
            }
            else
            {
                // Leave them a single hit point on the head, may not work if the injury has been merged but that's just luck.
                var damageTarget = pawn.health.hediffSet.GetPartHealth(dinfo.HitPart) - 1;
                var hediff = result.hediffs.Find(h => h.Part?.Equals(dinfo.HitPart) == true);
                if (hediff == null) return;
                hediff.Severity += damageTarget;
                result.totalDamageDealt += damageTarget;
            }
        }
    }
}
