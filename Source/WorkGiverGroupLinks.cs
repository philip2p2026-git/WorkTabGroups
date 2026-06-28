using System.Collections.Generic;
using RimWorld;
using WorkTab;

namespace WorkTabGroups
{
    public static class WorkGiverGroupLinks
    {
        public static readonly Dictionary<WorkGiverDef, PawnColumnWorker_MajorWorkGroup> MajorGroupByWorkGiver =
            new Dictionary<WorkGiverDef, PawnColumnWorker_MajorWorkGroup>();

        public static void Clear()
        {
            MajorGroupByWorkGiver.Clear();
        }
    }
}
