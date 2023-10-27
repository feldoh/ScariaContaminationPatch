using Verse;

namespace ScariaContaminationPatch.Comps;

public class HediffCompProperties_DevelopmentPointsOnRemoved : HediffCompProperties
{
    public int basePointsOnRemoved = 1;
    public bool mustBeAlive = true;
    public bool playerFactionOnly = false;
    public bool nonHostileOnly = true;

    public HediffCompProperties_DevelopmentPointsOnRemoved() => compClass = typeof(HediffComp_DevelopmentPointsOnRemoved);
}
