using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ScariaContaminationPatch.HarmonyPatches;

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
            target is not Pawn pawnTarget ||
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
        bool isBerserk = MentalStateDefOf.Berserk.Equals(stateDef);

        // Prevent scaria attacks causing berserk
        if (isBerserk
            && HediffSet_AddDirectPatch.PawnBlockedScaria == ___pawn
            && (___pawn.genes.HasGene(ScariaZombieDefOf.Taggerung_SCP_ScariaImmunity)
                || ___pawn.health.hediffSet.HasHediff(ScariaZombieDefOf.Taggerung_SCP_ImmunixHigh)))
        {
            HediffSet_AddDirectPatch.PawnBlockedScaria = null;
            return false;
        }

        // Prevent scaria ridden non-player pawns going berserk
        {
            if (ScariaContaminationPatch.Settings.AllowInfectedNPCBerserk ||
                !isBerserk ||
                ___pawn.IsColonist ||
                ___pawn.def.race.intelligence < Intelligence.ToolUser ||
                !___pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)) return true;
        }

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
            HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(dinfo.Def, pawn, dinfo.HitPart);
            Hediff_MissingPart hediffMissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn);
            hediffMissingPart.lastInjury = hediffDefFromDamage;
            hediffMissingPart.Part = dinfo.HitPart;
            pawn.health.AddHediff(hediffMissingPart);
        }
        else
        {
            // Leave them a single hit point on the head, may not work if the injury has been merged but that's just luck.
            float damageTarget = pawn.health.hediffSet.GetPartHealth(dinfo.HitPart) - 1;
            Hediff hediff = result.hediffs.Find(h => h.Part?.Equals(dinfo.HitPart) == true);
            if (hediff == null) return;
            hediff.Severity += damageTarget;
            result.totalDamageDealt += damageTarget;
        }
    }
}

[HarmonyPatch(typeof(FoodUtility), "IsAcceptablePreyFor")]
public static class IsAcceptablePreyForPatch
{
    [HarmonyPostfix]
    public static bool IsAcceptablePreyFor(bool __result, Pawn predator, Pawn prey)
    {
        return __result
               && (ScariaContaminationPatch.Settings.AllowInfectedHunting
                   || !(prey.health?.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false)
                   || !(predator.health?.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false));
    }
}

[HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect))]
public static class HediffSet_AddDirectPatch
{
    [CanBeNull] public static Pawn PawnBlockedScaria = null;

    [HarmonyPrefix]
    public static bool AddDirect(HediffSet __instance, Hediff hediff)
    {
        if (hediff.def != HediffDefOf.Scaria) return true;
        PawnBlockedScaria = __instance.pawn.genes.HasGene(ScariaZombieDefOf.Taggerung_SCP_ScariaImmunity) ||
                            __instance.pawn.health.hediffSet.HasHediff(ScariaZombieDefOf.Taggerung_SCP_ImmunixHigh)
            ? __instance.pawn
            : null;
        return PawnBlockedScaria == null;
    }
}

[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
public static class JobGiver_GetFoodPatch
{
    [HarmonyPostfix]
    public static Job TryGiveJob(Job __result, Pawn pawn)
    {
        if (!ScariaContaminationPatch.Settings.AllowAggressiveFoodHunt
            || !(pawn.needs?.food?.Starving ?? false)
            || !pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria)
            || (__result?.def == JobDefOf.PredatorHunt
                && __result?.targetA.Pawn is { } p
                && !p.health.hediffSet.HasHediff(HediffDefOf.Scaria))
            || (pawn.MentalState?.def == MentalStateDefOf.Manhunter && pawn.mindState.lastEngageTargetTick > Find.TickManager.TicksGame - 3000)
            || pawn.meleeVerbs.TryGetMeleeVerb((Thing)null) == null) return __result;

        if (pawn.MentalState?.def == MentalStateDefOf.Manhunter) pawn.mindState.mentalStateHandler.Reset();

        Pawn prey = pawn.Map.mapPawns.AllPawnsSpawned
            .Where(mapPawn =>
                !(mapPawn.health?.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false) &&
                pawn.CanReach((LocalTargetInfo)(Thing)mapPawn, PathEndMode.ClosestTouch, Danger.Deadly) && !mapPawn.IsForbidden(pawn))
            .OrderByDescending(prey => FoodUtility.GetPreyScoreFor(pawn, prey))
            .FirstOrDefault();
        if (prey != null)
        {
            Log.Message(prey.LabelShort);
            TaggedString reason = "LetterPredatorHuntingColonist".Translate(pawn.Named("PREDATOR"), prey.Named("PREY"));
            pawn.mindState?
                .mentalStateHandler?
                .TryStartMentalState(MentalStateDefOf.Manhunter, reason);

            if (prey.Spawned && prey.Faction == Faction.OfPlayer && prey.RaceProps.Humanlike)
            {
                pawn?.mindState?.Notify_PredatorHuntingPlayerNotification();
            }

            return __result;
        }

        if (!(pawn.genes?.HasGene(ScariaZombieDefOf.Taggerung_SCP_ScariaUnstoppableHunger) ?? false) &&
            !Rand.ChanceSeeded(ScariaContaminationPatch.Settings.UnstoppableHungerChance, pawn.thingIDNumber)) return __result;

        prey = pawn.Map.mapPawns.AllPawnsSpawned
            .Where(mapPawn =>
                !(mapPawn.health?.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false) &&
                pawn.CanReach((LocalTargetInfo)(Thing)mapPawn, PathEndMode.ClosestTouch, Danger.Deadly, canBashFences: true, canBashDoors: true,
                    mode: TraverseMode.PassAllDestroyableThingsNotWater) && !mapPawn.IsForbidden(pawn))
            .OrderByDescending(preyPawn => FoodUtility.GetPreyScoreFor(pawn, preyPawn))
            .FirstOrDefault();


        Job gotoJob = MakeGotoUnstoppable(pawn, prey);

        return gotoJob ?? __result;
    }

    public static Job MakeGotoUnstoppable(Pawn pawn, LocalTargetInfo target)
    {
        if (!target.IsValid) return null;
        bool canReach = false;

        // Maybe go for the doors, or maybe don't...
        if (Rand.Chance(ScariaContaminationPatch.Settings.DoorAttackChance))
        {
            using PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, target,
                TraverseParms.For(pawn, mode: TraverseMode.PassDoors));
            canReach = path.Found;
            Thing blocker = path.FirstBlockingBuilding(out IntVec3 cellBefore, pawn);
            if (blocker != null) return DigUtility.PassBlockerJob(pawn, blocker, cellBefore, true, true);
        }

        if (!canReach)
        {
            using PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, target,
                TraverseParms.For(pawn, mode: TraverseMode.PassAllDestroyableThings));
            Thing blocker = path.FirstBlockingBuilding(out IntVec3 cellBefore, pawn);
            if (blocker != null) return DigUtility.PassBlockerJob(pawn, blocker, cellBefore, true, true);
        }

        Job jobGoto = JobMaker.MakeJob(JobDefOf.Goto, target);
        jobGoto.expiryInterval = 10000;
        jobGoto.locomotionUrgency = LocomotionUrgency.Walk;
        jobGoto.canBashDoors = true;
        jobGoto.canBashFences = true;
        jobGoto.attackDoorIfTargetLost = true;
        return jobGoto;
    }
}
