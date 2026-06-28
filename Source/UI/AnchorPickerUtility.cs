using System.Collections.Generic;
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

            // Anchor positions are relative to native WorkType (major work type) headers only.
            foreach (PawnColumnDef col in PawnTableDefOf.Work.columns)
            {
                if (col.workType != null && typeof(PawnColumnWorker_WorkType).IsAssignableFrom(col.workerClass))
                {
                    options.Add(new AnchorOption(
                        "WorkTabGroups.Anchor.AfterWorkType".Translate(col.workType.LabelCap),
                        AnchorKeys.ForWorkType(col.workType)));
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
