using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

[DefOf]
public static class ScariaZombieDefOf
{
    [MayRequireBiotech]
    public static GeneDef Taggerung_SCP_ScariaCarrier;
    [MayRequireBiotech]
    public static GeneDef Taggerung_SCP_ScariaImmunity;
    [MayRequireBiotech]
    public static GeneDef Taggerung_SCP_ScariaUnstoppableHunger;

    public static HediffDef Taggerung_SCP_ViralBuildup;

    static ScariaZombieDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ScariaZombieDefOf));
}
