using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

public class Recipe_SuppressScaria : Recipe_RemoveHediff
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        return thing is Pawn pawn
               && (pawn.genes?.HasGene(ScariaZombieDefOf.Taggerung_SCP_ScariaCarrier) ?? false)
               && base.AvailableOnNow(thing, part);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        if (!(!pawn.health.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false)
            || pawn.genes.GetGene(ScariaZombieDefOf.Taggerung_SCP_ScariaCarrier) is not { } carrierGene) return;
        pawn.genes.RemoveGene(carrierGene);
        if (Rand.Chance(ScariaContaminationPatch.Settings.ImmunityGeneChance))
        {
            pawn.genes.AddGene(ScariaZombieDefOf.Taggerung_SCP_ScariaImmunity, true);
        }
    }
}
