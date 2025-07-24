using Verse;

namespace ScariaContaminationPatch;

[StaticConstructorOnStartup]
public static class ScariaContaminationPatchModInit
{
    static ScariaContaminationPatchModInit()
    {
        ScariaContaminationPatch.Settings?.ApplySettings();
    }
}
