using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

public class Recipe_SuppressScaria : Recipe_RemoveHediff
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
    {
        return thing is Pawn pawn
               && (pawn.genes?.HasActiveGene(ScariaZombieDefOf.Taggerung_SCP_ScariaCarrier) ?? false)
               && base.AvailableOnNow(thing, part);
    }

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        if (!(!pawn.health.hediffSet?.HasHediff(HediffDefOf.Scaria) ?? false)
            || pawn.genes.GetGene(ScariaZombieDefOf.Taggerung_SCP_ScariaCarrier) is not { } carrierGene) return;
        pawn.genes.RemoveGene(carrierGene);
        if (!Rand.Chance(ScariaContaminationPatch.Settings.ImmunityGeneChance)) return;
        pawn.genes.AddGene(ScariaZombieDefOf.Taggerung_SCP_ScariaImmunity, true);
        if (!PawnUtility.ShouldSendNotificationAbout(pawn)) return;
        Find.LetterStack.ReceiveLetter("ScariaContaminationPatch_GeneImmunityLetter".Translate(),
            "ScariaContaminationPatch_GeneImmunityLetterText".Translate(pawn.NameShortColored, HediffDefOf.Scaria.LabelCap),
            LetterDefOf.PositiveEvent, (Thing)pawn);
    }
}
