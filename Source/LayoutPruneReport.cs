using System.Collections.Generic;

namespace WorkTabGroups
{
    public class LayoutPruneReport
    {
        public int PrunedWorkGiverCount;
        public int PrunedLayoutEntryCount;
        public readonly List<string> AffectedGroupLabels = new List<string>();
        public readonly List<string> RemovedWorkGiverDefNames = new List<string>();

        public bool HasChanges => PrunedWorkGiverCount > 0 || PrunedLayoutEntryCount > 0;
    }
}
