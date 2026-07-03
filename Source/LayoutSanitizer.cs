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

            int prunedWorkGivers = PruneMissingWorkGivers(manager.Groups);
            int prunedLayoutEntries = PruneInvalidLayoutEntries(manager.WorkLayoutOrderMutable, manager.GetGroup);

            if (Prefs.DevMode && (prunedWorkGivers > 0 || prunedLayoutEntries > 0))
            {
                Log.Message(
                    $"[WorkTabGroups] Pruned invalid references: {prunedWorkGivers} WorkGiver(s), {prunedLayoutEntries} layout entry(ies).");
            }
        }

        private static int PruneMissingWorkGivers(IEnumerable<MajorWorkGroupData> groups)
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
                    if (DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName) == null)
                    {
                        group.assignedWorkGiverDefNames.RemoveAt(i);
                        pruned++;
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
