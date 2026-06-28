using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
    public static class AnchorPickerUtility
    {
        public static List<AnchorOption> BuildAnchorOptions(string excludeGroupDefName = null)
        {
            var options = new List<AnchorOption>
            {
                new AnchorOption("WorkTabGroups.Anchor.Start".Translate(), string.Empty)
            };

            foreach (PawnColumnDef col in PawnTableDefOf.Work.columns)
            {
                if (col.workType != null)
                {
                    options.Add(new AnchorOption(
                        "WorkTabGroups.Anchor.AfterWorkType".Translate(col.workType.LabelCap),
                        AnchorKeys.ForWorkType(col.workType)));
                }
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager != null)
            {
                foreach (MajorWorkGroupData group in manager.Groups)
                {
                    if (group.defName == excludeGroupDefName)
                    {
                        continue;
                    }

                    options.Add(new AnchorOption(
                        "WorkTabGroups.Anchor.AfterGroup".Translate(group.label),
                        AnchorKeys.ForGroup(group.defName)));
                }
            }

            return options;
        }
    }

    public struct AnchorOption
    {
        public string label;
        public string anchor;

        public AnchorOption(string label, string anchor)
        {
            this.label = label;
            this.anchor = anchor;
        }
    }
}
