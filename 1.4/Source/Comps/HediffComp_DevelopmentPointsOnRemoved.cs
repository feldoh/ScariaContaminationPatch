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
        if (!ModLister.IdeologyInstalled
            || (Props.playerFactionOnly && !this.Pawn.Faction.IsPlayer)
            || (Props.mustBeAlive && this.Pawn.health.ShouldBeDead())
            || (Props.nonHostileOnly && this.Pawn.HostileTo(Faction.OfPlayer))
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
        int pointsOnRemoved = (int)(Props.basePointsOnRemoved * bestPreceptComp.curePointsMultiplier);
        ideo.development.TryAddDevelopmentPoints(Mathf.Clamp(pointsOnRemoved, bestPreceptComp.minPoints, bestPreceptComp.maxPoints));
    }
}
