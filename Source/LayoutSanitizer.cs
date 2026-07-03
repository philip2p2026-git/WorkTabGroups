using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public static class LayoutSanitizer
    {
        public static void PruneInvalidReferences(WorkTabGroupsManager manager)
        {
            if (manager == null)
            {
                return;
            }

            PruneLayoutData(manager.GroupsMutable, manager.WorkLayoutOrderMutable);
        }

        public static void PruneLayoutData(List<MajorWorkGroupData> groups, List<WorkLayoutEntry> layoutOrder)
        {
            PruneLayoutDataWithReport(groups, layoutOrder);
        }

        public static LayoutPruneReport PruneLayoutDataWithReport(
            List<MajorWorkGroupData> groups,
            List<WorkLayoutEntry> layoutOrder)
        {
            var report = new LayoutPruneReport();
            if (groups == null)
            {
                return report;
            }

            report.PrunedWorkGiverCount = PruneInvalidWorkGivers(groups, report);
            report.PrunedLayoutEntryCount = PruneInvalidLayoutEntries(layoutOrder, defName => FindGroup(groups, defName));

            if (Prefs.DevMode && report.HasChanges)
            {
                Log.Message(
                    $"[WorkTabGroups] Pruned invalid references: {report.PrunedWorkGiverCount} WorkGiver(s), " +
                    $"{report.PrunedLayoutEntryCount} layout entry(ies).");
            }

            return report;
        }

        private static MajorWorkGroupData FindGroup(List<MajorWorkGroupData> groups, string defName)
        {
            if (groups == null || defName.NullOrEmpty())
            {
                return null;
            }

            foreach (MajorWorkGroupData group in groups)
            {
                if (group?.defName == defName)
                {
                    return group;
                }
            }

            return null;
        }

        private static bool IsResolvableWorkGiver(string wgName)
        {
            if (wgName.NullOrEmpty())
            {
                return false;
            }

            WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
            if (wg == null)
            {
                return false;
            }

            if (wg.workType == null)
            {
                return false;
            }

            return DefDatabase<WorkTypeDef>.GetNamedSilentFail(wg.workType.defName) != null;
        }

        private static int PruneInvalidWorkGivers(IEnumerable<MajorWorkGroupData> groups, LayoutPruneReport report = null)
        {
            int pruned = 0;
            if (groups == null)
            {
                return pruned;
            }

            foreach (MajorWorkGroupData group in groups)
            {
                if (group?.assignedWorkGiverDefNames == null)
                {
                    continue;
                }

                for (int i = group.assignedWorkGiverDefNames.Count - 1; i >= 0; i--)
                {
                    string wgName = group.assignedWorkGiverDefNames[i];
                    if (!IsResolvableWorkGiver(wgName))
                    {
                        group.assignedWorkGiverDefNames.RemoveAt(i);
                        pruned++;
                        if (report != null)
                        {
                            report.RemovedWorkGiverDefNames.Add(wgName);
                            if (!group.label.NullOrEmpty() && !report.AffectedGroupLabels.Contains(group.label))
                            {
                                report.AffectedGroupLabels.Add(group.label);
                            }
                        }
                    }
                }
            }

            return pruned;
        }

        private static int PruneInvalidLayoutEntries(
            List<WorkLayoutEntry> layoutOrder,
            Func<string, MajorWorkGroupData> getGroup)
        {
            if (layoutOrder == null)
            {
                return 0;
            }

            int before = layoutOrder.Count;
            layoutOrder.RemoveAll(entry =>
            {
                if (entry.kind == WorkLayoutEntryKind.WorkType)
                {
                    return DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key) == null;
                }

                if (entry.kind == WorkLayoutEntryKind.CustomGroup)
                {
                    return getGroup(entry.key) == null;
                }

                return false;
            });

            return before - layoutOrder.Count;
        }
    }
}
