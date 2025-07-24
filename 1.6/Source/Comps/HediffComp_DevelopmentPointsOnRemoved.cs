using RimWorld;
using UnityEngine;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class HediffComp_DevelopmentPointsOnRemoved : HediffComp
{
    public HediffCompProperties_DevelopmentPointsOnRemoved Props => (HediffCompProperties_DevelopmentPointsOnRemoved)props;

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        ApplyDevelopmentPointsIfRequired(Pawn, Props.playerFactionOnly, Props.mustBeAlive, Props.nonHostileOnly, Props.basePointsOnRemoved);
    }

    public static void ApplyDevelopmentPointsIfRequired(Pawn pawn, bool scariaPlayerFactionOnly, bool scariaMustBeAlive, bool scariaNonHostileOnly, int scariaBasePointsOnRemoved)
    {
        if (!ModsConfig.IdeologyActive
            || (scariaPlayerFactionOnly && !pawn.Faction.IsPlayer)
            || (scariaMustBeAlive && pawn.health.ShouldBeDead())
            || (scariaNonHostileOnly && pawn.HostileTo(Faction.OfPlayer))
            || Faction.OfPlayer.ideos.FluidIdeo is not { } ideo
            || !(ideo.development?.CanBeDevelopedNow ?? false)) return;
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
        if (bestPreceptComp is null) return;
        int pointsOnRemoved = (int)(scariaBasePointsOnRemoved * bestPreceptComp.curePointsMultiplier);
        ideo.development.TryAddDevelopmentPoints(Mathf.Clamp(pointsOnRemoved, bestPreceptComp.minPoints, bestPreceptComp.maxPoints));
    }
}
