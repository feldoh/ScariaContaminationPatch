using System.Collections.Generic;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class HediffCompProperties_GeneGiver : HediffCompProperties
{
    public List<GeneDef> possibleGenes = new();
    public HediffCompProperties_GeneGiver() => compClass = typeof(HediffComp_GeneGiver);
}
