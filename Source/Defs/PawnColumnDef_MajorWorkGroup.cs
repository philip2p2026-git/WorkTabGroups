using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public class PawnColumnDef_MajorWorkGroup : PawnColumnDef
    {
        public MajorWorkGroupDef majorWorkGroup;

        public override TaggedString LabelCap => majorWorkGroup?.data?.label ?? base.LabelCap;
    }
}
