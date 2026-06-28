using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
    public static class WorkTabGroupsColumnBuilder
    {
        /// <summary>
        /// Reorder columns without a full Work Tab rebuild (preserves native expand state).
        /// </summary>
        public static void RelayoutColumns()
        {
            Inject();
            WireExpandableColumns();
        }

        public static void Inject()
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return;
            }

            List<PawnColumnDef> columns = PawnTableDefOf.Work.columns;
            if (columns == null || columns.Count == 0)
            {
                return;
            }

            WorkGiverGroupLinks.Clear();

            Dictionary<string, bool> nativeExpandState = CaptureNativeExpandState(columns);

            HashSet<PawnColumnDef> reassignedColumns = new HashSet<PawnColumnDef>();
            foreach (MajorWorkGroupData group in manager.Groups)
            {
                foreach (string wgName in group.assignedWorkGiverDefNames)
                {
                    WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                    PawnColumnDef col = wg != null ? manager.GetWorkGiverColumn(wg) : null;
                    if (col != null)
                    {
                        reassignedColumns.Add(col);
                    }
                }
            }

            List<PawnColumnDef> newColumns = BuildNativeColumnLayout(columns, manager, reassignedColumns);

            List<MajorWorkGroupData> orderedGroups = manager.GetOrderedGroupsForInjection();
            foreach (MajorWorkGroupData group in orderedGroups)
            {
                PawnColumnDef groupCol = manager.GetColumnDefForGroup(group);
                if (groupCol == null)
                {
                    continue;
                }

                int insertIndex = ResolveInsertIndex(newColumns, group.insertAfterAnchor);
                insertIndex = Mathf.Clamp(insertIndex, 0, newColumns.Count);
                newColumns.Insert(insertIndex, groupCol);

                List<PawnColumnDef> wgCols = new List<PawnColumnDef>();
                foreach (string wgName in group.assignedWorkGiverDefNames)
                {
                    WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                    PawnColumnDef wgCol = wg != null ? manager.GetWorkGiverColumn(wg) : null;
                    if (wgCol != null)
                    {
                        wgCols.Add(wgCol);
                    }
                }

                newColumns.InsertRange(insertIndex + 1, wgCols);

                if (groupCol.Worker is PawnColumnWorker_MajorWorkGroup groupWorker)
                {
                    groupWorker.BindGroup(group);
                    groupWorker.CanExpand = wgCols.Count > 0;
                    groupWorker.Expanded = group.expanded;
                    CollapseIfCannotExpand(groupWorker);
                }
            }

            WireGroupedWorkGiverLinks(manager);
            UpdateNativeWorkTypeExpandState(newColumns);
            RestoreNativeExpandState(newColumns, nativeExpandState);
            WireNativeWorkTypeLinks(newColumns, manager);
            InvalidateReassignedWorkGiverCaches();
            PawnTableDefOf.Work.columns = newColumns;
            MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
        }

        public static void WireExpandableColumns()
        {
            List<PawnColumnDef> columns = PawnTableDefOf.Work.columns;
            if (columns == null)
            {
                return;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            WorkGiverGroupLinks.Clear();

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Worker is PawnColumnWorker_MajorWorkGroup groupWorker)
                {
                    MajorWorkGroupData group = ResolveGroupData(columns[i], groupWorker, manager);
                    if (group != null)
                    {
                        groupWorker.BindGroup(group);
                    }

                    int childCount = group?.assignedWorkGiverDefNames.Count ?? 0;
                    groupWorker.CanExpand = childCount > 0;
                    CollapseIfCannotExpand(groupWorker);

                    int j = i + 1;
                    while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
                    {
                        j++;
                    }

                    i = j - 1;
                }
            }

            WireGroupedWorkGiverLinks(manager);
            UpdateNativeWorkTypeExpandState(columns);
        }

        /// <summary>
        /// Wire grouped WorkGivers to custom groups using manager assignments (authoritative).
        /// Clears native WorkType parent links so collapse follows group Expanded state.
        /// </summary>
        public static void WireGroupedWorkGiverLinks(WorkTabGroupsManager manager, bool invalidateCaches = true)
        {
            if (manager == null)
            {
                return;
            }

            foreach (KeyValuePair<WorkGiverDef, MajorWorkGroupData> assignment in manager.GetAssignedWorkGiverAssignments())
            {
                WorkGiverDef workGiver = assignment.Key;
                MajorWorkGroupData groupData = assignment.Value;
                PawnColumnDef groupCol = manager.GetColumnDefForGroup(groupData);
                PawnColumnDef wgCol = manager.GetWorkGiverColumn(workGiver);
                if (groupCol?.Worker is PawnColumnWorker_MajorWorkGroup groupWorker &&
                    wgCol?.Worker is PawnColumnWorker_WorkGiver wgWorker)
                {
                    wgWorker.ColumnWorkerWorkType = null;
                    WorkGiverGroupLinks.MajorGroupByWorkGiver[workGiver] = groupWorker;
                    if (invalidateCaches)
                    {
                        wgWorker.InvalidateCache();
                    }
                }
            }
        }

        public static void InvalidateWorkTabColumnCaches()
        {
            List<PawnColumnDef> columns = PawnTableDefOf.Work.columns;
            if (columns == null)
            {
                return;
            }

            foreach (PawnColumnDef col in columns)
            {
                if (col.Worker is AbstractPawnColumnWorker abstractWorker)
                {
                    abstractWorker.InvalidateCache();
                }
            }
        }

        private static MajorWorkGroupData ResolveGroupData(
            PawnColumnDef column,
            PawnColumnWorker_MajorWorkGroup groupWorker,
            WorkTabGroupsManager manager)
        {
            if (column is PawnColumnDef_MajorWorkGroup majorCol && majorCol.majorWorkGroup?.data != null)
            {
                return majorCol.majorWorkGroup.data;
            }

            if (groupWorker.BoundGroup != null)
            {
                return groupWorker.BoundGroup;
            }

            return null;
        }

        private static void UpdateNativeWorkTypeExpandState(List<PawnColumnDef> columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Worker is PawnColumnWorker_WorkType workTypeWorker)
                {
                    int j = i + 1;
                    while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
                    {
                        j++;
                    }

                    workTypeWorker.CanExpand = j - (i + 1) > 0;
                    CollapseIfCannotExpand(workTypeWorker);
                    i = j - 1;
                }
            }
        }

        private static void CollapseIfCannotExpand(IExpandableColumn worker)
        {
            if (!worker.CanExpand && worker.Expanded)
            {
                worker.Expanded = false;
            }
        }

        private static Dictionary<string, bool> CaptureNativeExpandState(List<PawnColumnDef> columns)
        {
            var state = new Dictionary<string, bool>();
            if (columns == null)
            {
                return state;
            }

            foreach (PawnColumnDef col in columns)
            {
                if (col.Worker is PawnColumnWorker_WorkType workTypeWorker && col.workType != null)
                {
                    state[col.workType.defName] = workTypeWorker.Expanded;
                }
            }

            return state;
        }

        private static void RestoreNativeExpandState(List<PawnColumnDef> columns, Dictionary<string, bool> state)
        {
            if (columns == null || state == null || state.Count == 0)
            {
                return;
            }

            foreach (PawnColumnDef col in columns)
            {
                if (col.Worker is PawnColumnWorker_WorkType workTypeWorker && col.workType != null &&
                    state.TryGetValue(col.workType.defName, out bool expanded))
                {
                    workTypeWorker.Expanded = expanded;
                }
            }
        }

        private static List<PawnColumnDef> BuildNativeColumnLayout(
            List<PawnColumnDef> columns,
            WorkTabGroupsManager manager,
            HashSet<PawnColumnDef> reassignedColumns)
        {
            var result = new List<PawnColumnDef>();
            if (columns == null)
            {
                return result;
            }

            foreach (PawnColumnDef col in columns)
            {
                if (col.Worker is PawnColumnWorker_WorkGiver || col.Worker is PawnColumnWorker_MajorWorkGroup)
                {
                    continue;
                }

                result.Add(col);

                if (col.Worker is PawnColumnWorker_WorkType && col.workType != null)
                {
                    foreach (WorkGiverDef wg in col.workType.workGiversByPriority)
                    {
                        PawnColumnDef wgCol = manager.GetWorkGiverColumn(wg);
                        if (wgCol != null && !reassignedColumns.Contains(wgCol))
                        {
                            result.Add(wgCol);
                        }
                    }
                }
            }

            return result;
        }

        private static int ResolveInsertIndex(List<PawnColumnDef> columns, string anchor)
        {
            if (AnchorKeys.IsStart(anchor))
            {
                return FindFirstWorkTypeIndex(columns);
            }

            if (AnchorKeys.TryParseWorkType(anchor, out string workTypeName))
            {
                int wtIndex = FindWorkTypeIndex(columns, workTypeName);
                if (wtIndex < 0)
                {
                    return FindFirstWorkTypeIndex(columns);
                }

                return EndOfWorkTypeSectionIndex(columns, wtIndex);
            }

            return FindFirstWorkTypeIndex(columns);
        }

        private static int FindFirstWorkTypeIndex(List<PawnColumnDef> columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Worker is PawnColumnWorker_WorkType)
                {
                    return i;
                }
            }

            return columns.Count;
        }

        private static int FindWorkTypeIndex(List<PawnColumnDef> columns, string workTypeName)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                PawnColumnDef col = columns[i];
                if (col.workType != null &&
                    col.workType.defName == workTypeName &&
                    typeof(PawnColumnWorker_WorkType).IsAssignableFrom(col.workerClass))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Index after a WorkType header, its native WorkGivers, and any custom groups already placed after it.
        /// </summary>
        private static int EndOfWorkTypeSectionIndex(List<PawnColumnDef> columns, int workTypeHeaderIndex)
        {
            int j = workTypeHeaderIndex + 1;
            while (j < columns.Count)
            {
                if (columns[j].Worker is PawnColumnWorker_WorkGiver)
                {
                    j++;
                    continue;
                }

                if (columns[j].Worker is PawnColumnWorker_MajorWorkGroup)
                {
                    j++;
                    while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
                    {
                        j++;
                    }

                    continue;
                }

                break;
            }

            return j;
        }

        private static void WireNativeWorkTypeLinks(List<PawnColumnDef> columns, WorkTabGroupsManager manager)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Worker is PawnColumnWorker_WorkType workTypeWorker)
                {
                    int j = i + 1;
                    while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
                    {
                        if (columns[j].Worker is PawnColumnWorker_WorkGiver wgWorker &&
                            columns[j] is PawnColumnDef_WorkGiver wgDef &&
                            (manager == null || !manager.IsAssignedToCustomGroup(wgDef.workgiver)))
                        {
                            wgWorker.ColumnWorkerWorkType = workTypeWorker;
                        }

                        j++;
                    }

                    i = j - 1;
                }
            }
        }

        private static void InvalidateReassignedWorkGiverCaches()
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return;
            }

            foreach (WorkGiverDef workGiver in WorkGiverGroupLinks.MajorGroupByWorkGiver.Keys)
            {
                if (manager.GetWorkGiverColumn(workGiver)?.Worker is PawnColumnWorker_WorkGiver wgWorker)
                {
                    wgWorker.InvalidateCache();
                }
            }
        }
    }
}
