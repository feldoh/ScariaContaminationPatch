using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class HediffComp_GeneGiver : HediffComp
{
    public HediffCompProperties_GeneGiver Props => (HediffCompProperties_GeneGiver)props;
    public bool shouldRemove;

    public override bool CompShouldRemove => shouldRemove;

    public override void CompPostMake()
    {
        Pawn pawn = this.parent.pawn;
        if (pawn.genes != null && !Props.possibleGenes.All(g => pawn.genes.HasGene(g))) return;
        this.shouldRemove = true;
    }

    public override void CompPostMerged(Hediff other)
    {
        if (other.TryGetComp<HediffComp_GeneGiver>() is { } hc)
        {
            Props.possibleGenes.AddRange(hc.Props.possibleGenes.Except(Props.possibleGenes));
            Props.xenoTypes.AddRange(hc.Props.xenoTypes.Except(Props.xenoTypes));
            Props.maxGenesToGive = Math.Max(Props.maxGenesToGive, hc.Props.maxGenesToGive);
            Props.numGenesToGive = Math.Min(Props.numGenesToGive + hc.Props.numGenesToGive, Props.maxGenesToGive);
            Props.applyEntireXenotype = Props.applyEntireXenotype || hc.Props.applyEntireXenotype;
        }

        base.CompPostMerged(other);
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (CompShouldRemove)
        {
            this.parent.pawn.health.RemoveHediff(this.parent);
            return;
        }

        base.CompPostTick(ref severityAdjustment);
        Pawn pawn = this.parent.pawn;

        if (!pawn.Spawned
            || (double)this.parent.Severity < 1) return;

        int genesToGive = Math.Min(Props.numGenesToGive, Props.maxGenesToGive);
        string description = "";
        if (Props.applyEntireXenotype)
        {
            Stack<GeneDef> viableGenes = new(Props.possibleGenes
                .Distinct()
                .Except(pawn.genes.GenesListForReading.Select(g => g.def))
                .InRandomOrder());
            while (genesToGive > 0 && !viableGenes.EnumerableNullOrEmpty())
            {
                GeneDef gene = viableGenes.Pop();
                pawn.genes.AddGene(gene, true);
                description += $"\n\t- {gene.LabelCap}";
            }
        }
        else
        {
            XenotypeDef xenotype = Props.xenoTypes.RandomElement();
            pawn.genes.SetXenotype(xenotype);
            description = xenotype.LabelCap;
        }

        shouldRemove = true;

        if (!PawnUtility.ShouldSendNotificationAbout(pawn))
            return;
        TaggedString label = "ScariaContaminationPatch_LetterLabelGeneGiver".Translate();
        string letterText = Props.applyEntireXenotype
            ? "ScariaContaminationPatch_LetterTextGeneGiverFullXenotype"
            : "ScariaContaminationPatch_LetterTextGeneGiver";
        TaggedString taggedString = letterText.Translate(pawn.Named("PAWN"), (NamedArgument)description);
        taggedString = taggedString.AdjustedFor(pawn);
        TaggedString text = taggedString.CapitalizeFirst();
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, (LookTargets)(Thing)pawn);
    }
}
