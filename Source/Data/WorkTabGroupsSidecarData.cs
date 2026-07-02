using System.Collections.Generic;
using Verse;

namespace WorkTabGroups
{
    public class WorkTabGroupsSidecarData : IExposable
    {
        public List<MajorWorkGroupData> groups = new List<MajorWorkGroupData>();
        public List<WorkLayoutEntry> workLayoutOrder = new List<WorkLayoutEntry>();
        public int nextGroupId;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
            Scribe_Collections.Look(ref workLayoutOrder, "workLayoutOrder", LookMode.Deep);
            Scribe_Values.Look(ref nextGroupId, "nextGroupId", 0);

            if (groups == null)
            {
                groups = new List<MajorWorkGroupData>();
            }

            if (workLayoutOrder == null)
            {
                workLayoutOrder = new List<WorkLayoutEntry>();
            }
        }
    }
}
