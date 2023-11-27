using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

public class IngestionOutcomeDoer_ChangeGenes : IngestionOutcomeDoer
{
    public List<GeneCategoryDef> includedGeneCategories = new();
    public List<GeneCategoryDef> excludedGeneCategories = new();
    public int genesToRemove = 1;
    public XenotypeDef forcedXenotype = null;

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
    {
        if (pawn.genes == null) return;
        if (forcedXenotype != null)
        {
            pawn.genes.SetXenotype(forcedXenotype);
            return;
        }

        foreach (Gene targetGene in pawn.genes.Xenogenes
                     ?.Where(g => (includedGeneCategories.Count == 0 || includedGeneCategories.Contains(g.def.displayCategory))
                                  && (excludedGeneCategories.Count == 0 || !excludedGeneCategories.Contains(g.def.displayCategory)))
                     ?.InRandomOrder()
                     ?.Take(genesToRemove) ?? new List<Gene>())
        {
            pawn.genes.RemoveGene(targetGene);
        }
    }
}
