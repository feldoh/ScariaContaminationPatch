using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

[DefOf]
public static class ScariaZombieDefOf
{
    [MayRequireBiotech]
    public static GeneDef Taggerung_SCP_ScariaCarrier;

    static ScariaZombieDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ScariaZombieDefOf));
}
