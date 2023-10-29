using RimWorld;
using Verse;

namespace ScariaContaminationPatch;

[DefOf]
public static class ScariaZombieDefOf
{
    public static GeneDef Taggerung_SCP_ScariaCarrier;

    static ScariaZombieDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ScariaZombieDefOf));
}