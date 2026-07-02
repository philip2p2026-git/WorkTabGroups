using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public static class LayoutOrderMigration
    {
        public static List<WorkLayoutEntry> MigrateFromAnchors(
            IEnumerable<MajorWorkGroupData> groups,
            List<string> nativeWorkTypeOrder = null)
        {
            var order = LayoutOrderUtility.BuildDefaultLayoutOrder(nativeWorkTypeOrder);
            var nextInsertIndex = new Dictionary<string, int>();

            foreach (MajorWorkGroupData group in groups)
            {
                string anchor = group.insertAfterAnchor ?? string.Empty;
                if (!nextInsertIndex.TryGetValue(anchor, out int insertIndex))
                {
                    insertIndex = ComputeBaseInsertIndex(order, anchor);
                    nextInsertIndex[anchor] = insertIndex;
                }

                insertIndex = Mathf.Clamp(insertIndex, 0, order.Count);
                order.Insert(insertIndex, WorkLayoutEntry.ForCustomGroup(group.defName));
                nextInsertIndex[anchor] = insertIndex + 1;
            }

            return order;
        }

        public static List<WorkLayoutEntry> MigrateLayoutPreset(
            LayoutPreset preset,
            List<string> nativeWorkTypeOrder = null)
        {
            if (preset?.layoutOrder != null && preset.layoutOrder.Count > 0)
            {
                return new List<WorkLayoutEntry>(preset.layoutOrder);
            }

            if (preset?.groups == null || preset.groups.Count == 0)
            {
                return LayoutOrderUtility.BuildDefaultLayoutOrder(nativeWorkTypeOrder);
            }

            var pseudoGroups = new List<MajorWorkGroupData>();
            int id = 0;
            foreach (LayoutGroupEntry entry in preset.groups)
            {
                string defName = "MajorWorkGroup_" + id++;
                var data = new MajorWorkGroupData(defName, entry.groupLabel, entry.insertAfterAnchor ?? string.Empty);
                if (entry.assignedWorkGiverDefNames != null)
                {
                    data.assignedWorkGiverDefNames.AddRange(entry.assignedWorkGiverDefNames);
                }

                pseudoGroups.Add(data);
            }

            return MigrateFromAnchors(pseudoGroups, nativeWorkTypeOrder);
        }

        private static int ComputeBaseInsertIndex(List<WorkLayoutEntry> order, string anchor)
        {
            if (AnchorKeys.IsStart(anchor))
            {
                return 0;
            }

            if (AnchorKeys.TryParseWorkType(anchor, out string workTypeName))
            {
                for (int i = 0; i < order.Count; i++)
                {
                    if (order[i].kind == WorkLayoutEntryKind.WorkType && order[i].key == workTypeName)
                    {
                        return i + 1;
                    }
                }
            }

            return 0;
        }
    }
}
