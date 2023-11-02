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
            Props.possibleGenes.AddRange(hc.Props.possibleGenes);
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
            || (double)this.parent.Severity < 1
            || Props.possibleGenes
                .Where(g => !pawn.genes.HasGene(g)).RandomElementWithFallback() is not { } gene) return;

        pawn.genes.AddGene(gene, true);
        shouldRemove = true;

        if (!PawnUtility.ShouldSendNotificationAbout(pawn))
            return;
        TaggedString label = "ScariaContaminationPatch_LetterLabelGeneGiver".Translate();
        TaggedString taggedString = "ScariaContaminationPatch_LetterTextGeneGiver".Translate(pawn.Named("PAWN"), (NamedArgument)gene.label);
        taggedString = taggedString.AdjustedFor(pawn);
        TaggedString text = taggedString.CapitalizeFirst();
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, (LookTargets)(Thing)pawn);
    }
}
