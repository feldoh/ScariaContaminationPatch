using System.Collections.Generic;
using System.Linq;
using ScariaContaminationPatch.Comps;
using Verse;

namespace ScariaContaminationPatch;

public class HediffGiver_RandomGene : HediffGiver_Random
{
    private List<GeneDef> possibleGenes = new();

    public override float ChanceFactor(Pawn pawn)
    {
        return pawn.genes == null || possibleGenes.All(g => pawn.genes.HasGene(g)) ? 0 : 1;
    }

    public override bool OnHediffAdded(Pawn pawn, Hediff hediff)
    {
        if (hediff is HediffWithComps hwc)
        {
            HediffCompProperties_GeneGiver giverProps = new()
            {
                possibleGenes = possibleGenes
            };
            HediffComp_GeneGiver geneGiver = new()
            {
                props = giverProps
            };
            hwc.comps.Add(geneGiver);
        }

        return base.OnHediffAdded(pawn, hediff);
    }
}
