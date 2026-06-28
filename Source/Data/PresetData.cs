using System.Collections.Generic;
using Verse;

namespace WorkTabGroups
{
    public class GroupPreset : IExposable
    {
        public string presetName;
        public string groupLabel;
        public List<string> assignedWorkGiverDefNames = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref presetName, "presetName");
            Scribe_Values.Look(ref groupLabel, "groupLabel");
            Scribe_Collections.Look(ref assignedWorkGiverDefNames, "assignedWorkGiverDefNames", LookMode.Value);
            if (assignedWorkGiverDefNames == null)
            {
                assignedWorkGiverDefNames = new List<string>();
            }
        }
    }

    public class LayoutGroupEntry : IExposable
    {
        public string groupLabel;
        public string presetGroupId;
        public string insertAfterAnchor;
        public List<string> assignedWorkGiverDefNames = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref groupLabel, "groupLabel");
            Scribe_Values.Look(ref presetGroupId, "presetGroupId");
            Scribe_Values.Look(ref insertAfterAnchor, "insertAfterAnchor", string.Empty);
            Scribe_Collections.Look(ref assignedWorkGiverDefNames, "assignedWorkGiverDefNames", LookMode.Value);
            if (assignedWorkGiverDefNames == null)
            {
                assignedWorkGiverDefNames = new List<string>();
            }
        }
    }

    public class LayoutPreset : IExposable
    {
        public string presetName;
        public List<LayoutGroupEntry> groups = new List<LayoutGroupEntry>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref presetName, "presetName");
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
            if (groups == null)
            {
                groups = new List<LayoutGroupEntry>();
            }
        }
    }
}
