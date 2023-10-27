using RimWorld;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class CompProperties_UseEffect_RemoveHediff : CompProperties_UseEffect
{
    public HediffDef removesHediff;

    public CompProperties_UseEffect_RemoveHediff() => compClass = typeof(CompUseEffect_RemoveHediff);
}
