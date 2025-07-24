using RimWorld;
using Verse;

namespace ScariaContaminationPatch.Comps;

public class CompUseEffect_RemoveHediff : CompUseEffect
{
    public CompProperties_UseEffect_RemoveHediff Props => (CompProperties_UseEffect_RemoveHediff)this.props;

    public override float OrderPriority => 10.0f;

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);
        if (Props.removesHediff is not { } hediffDef || usedBy.health.hediffSet.GetFirstHediffOfDef(hediffDef) is not { } hediff) return;
        TaggedString text = HealthUtility.Cure(hediff);
        if (!PawnUtility.ShouldSendNotificationAbout(usedBy)) return;
        Messages.Message(text, (Thing)usedBy, MessageTypeDefOf.PositiveEvent);
    }
}
