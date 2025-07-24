using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class HediffCompProperties_GeneGiver : HediffCompProperties
{
    public List<XenotypeDef> xenoTypes = new();
    public List<GeneDef> possibleGenes = new();
    public int maxGenesToGive = 10;
    public int numGenesToGive = 1;
    public bool applyEntireXenotype = false;

    public HediffCompProperties_GeneGiver() => compClass = typeof(HediffComp_GeneGiver);

    public override void ResolveReferences(HediffDef parent)
    {
        base.ResolveReferences(parent);
        xenoTypes?.ForEach(x => possibleGenes.AddRange(x.genes));
    }
}
