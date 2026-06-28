using System.Collections.Generic;
using Verse;

namespace WorkTabGroups
{
    public class WorkTabGroupsSidecarData : IExposable
    {
        public List<MajorWorkGroupData> groups = new List<MajorWorkGroupData>();
        public int nextGroupId;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
            Scribe_Values.Look(ref nextGroupId, "nextGroupId", 0);

            if (groups == null)
            {
                groups = new List<MajorWorkGroupData>();
            }
        }
    }
}
