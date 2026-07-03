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
            if (groups == null)
            {
                return;
            }

            int prunedWorkGivers = PruneInvalidWorkGivers(groups);
            int prunedEmptyGroups = PruneEmptyGroups(groups, layoutOrder);
            int prunedLayoutEntries = PruneInvalidLayoutEntries(layoutOrder, defName => FindGroup(groups, defName));

            if (Prefs.DevMode && (prunedWorkGivers > 0 || prunedEmptyGroups > 0 || prunedLayoutEntries > 0))
            {
                Log.Message(
                    $"[WorkTabGroups] Pruned invalid references: {prunedWorkGivers} WorkGiver(s), " +
                    $"{prunedEmptyGroups} empty group(s), {prunedLayoutEntries} layout entry(ies).");
            }
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

        private static int PruneInvalidWorkGivers(IEnumerable<MajorWorkGroupData> groups)
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
                    if (!IsResolvableWorkGiver(group.assignedWorkGiverDefNames[i]))
                    {
                        group.assignedWorkGiverDefNames.RemoveAt(i);
                        pruned++;
                    }
                }
            }

            return pruned;
        }

        private static int PruneEmptyGroups(List<MajorWorkGroupData> groups, List<WorkLayoutEntry> layoutOrder)
        {
            if (groups == null || groups.Count == 0)
            {
                return 0;
            }

            var emptyDefNames = new HashSet<string>();
            foreach (MajorWorkGroupData group in groups)
            {
                if (group?.assignedWorkGiverDefNames == null || group.assignedWorkGiverDefNames.Count == 0)
                {
                    emptyDefNames.Add(group.defName);
                }
            }

            if (emptyDefNames.Count == 0)
            {
                return 0;
            }

            groups.RemoveAll(g => g != null && emptyDefNames.Contains(g.defName));

            if (layoutOrder != null)
            {
                layoutOrder.RemoveAll(e =>
                    e.kind == WorkLayoutEntryKind.CustomGroup && emptyDefNames.Contains(e.key));
            }

            return emptyDefNames.Count;
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
