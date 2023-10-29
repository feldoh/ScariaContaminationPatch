using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

public class GeneticInfectedGene : Gene
{
    public override void PostRemove()
    {
        if (GetHediff() is { } firstHediffOfDef)
            pawn.health.RemoveHediff(firstHediffOfDef);
        base.PostRemove();
    }

    public override void PostAdd()
    {
        base.PostAdd();
        ApplyHediff();
    }

    public override void Tick()
    {
        base.Tick();
        if (!pawn.Spawned || !pawn.IsHashIntervalTick(GenTicks.TickLongInterval) || Rand.Chance(0.99f) || GetHediff() != null) return;
        if (Rand.Chance(0.3f)) ApplyHediff();
    }

    public Hediff GetHediff() => pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Scaria);

    public void ApplyHediff()
    {
        if (GetHediff() != null) return;
        Hediff scariaHediff = HediffMaker.MakeHediff(HediffDefOf.Scaria, pawn);
        pawn.health.AddHediff(scariaHediff);
    }
}
