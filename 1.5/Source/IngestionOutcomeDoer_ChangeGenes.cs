using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

public class IngestionOutcomeDoer_ChangeGenes : IngestionOutcomeDoer
{
    public List<GeneCategoryDef> includedGeneCategories = new();
    public List<GeneCategoryDef> excludedGeneCategories = new();
    public List<GeneDef> genesToPrioritise = new();
    public List<GeneDef> genesToPreserve = new();
    public int genesToRemove = 1;
    public XenotypeDef forcedXenotype = null;
    public bool allowArchiteGenes = true;
    public int maxPriorityOrder = 20;
    public int maxNonPriorityOrder = 100;
    public int priorityOnlyBeforeOrder = 5;

    protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested)
    {
        if (pawn.genes == null) return;
        if (forcedXenotype != null)
        {
            pawn.genes.SetXenotype(forcedXenotype);
            return;
        }

        Random rand = new();
        foreach (Gene targetGene in pawn.genes.Xenogenes
                     ?.Where(g => (includedGeneCategories.Count == 0 || includedGeneCategories.Contains(g.def.displayCategory))
                                  && (excludedGeneCategories.Count == 0 || !excludedGeneCategories.Contains(g.def.displayCategory))
                                  && !genesToPreserve.Contains(g.def) && (allowArchiteGenes || g.def.biostatArc < 1))
                     ?.OrderBy(g => genesToPrioritise.Contains(g.def) ? rand.Next(0, maxPriorityOrder) : rand.Next(priorityOnlyBeforeOrder, maxNonPriorityOrder))
                     ?.Take(genesToRemove) ?? new List<Gene>())
        {
            pawn.genes.RemoveGene(targetGene);
        }
    }
}
