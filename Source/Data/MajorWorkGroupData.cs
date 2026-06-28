using System.Collections.Generic;
using Verse;

namespace WorkTabGroups
{
    public class MajorWorkGroupData : IExposable
    {
        public string defName;
        public string label;
        public string insertAfterAnchor;
        public bool expanded;
        public List<string> assignedWorkGiverDefNames = new List<string>();

        public MajorWorkGroupData()
        {
        }

        public MajorWorkGroupData(string defName, string label, string insertAfterAnchor)
        {
            this.defName = defName;
            this.label = label;
            this.insertAfterAnchor = insertAfterAnchor ?? string.Empty;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "defName");
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref insertAfterAnchor, "insertAfterAnchor", string.Empty);
            Scribe_Values.Look(ref expanded, "expanded", false);
            Scribe_Collections.Look(ref assignedWorkGiverDefNames, "assignedWorkGiverDefNames", LookMode.Value);
            if (assignedWorkGiverDefNames == null)
            {
                assignedWorkGiverDefNames = new List<string>();
            }
        }
    }
}
