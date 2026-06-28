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

            List<PawnColumnDef> newColumns = columns.Where(c => !reassignedColumns.Contains(c)).ToList();

            List<MajorWorkGroupData> orderedGroups = manager.GetOrderedGroupsForInjection();
            foreach (MajorWorkGroupData group in orderedGroups)
            {
                PawnColumnDef groupCol = manager.GetColumnDefForGroup(group);
                if (groupCol == null)
                {
                    continue;
                }

                int insertIndex = ResolveInsertIndex(newColumns, group.insertAfterAnchor, manager);
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

                    foreach (PawnColumnDef wgCol in wgCols)
                    {
                        if (wgCol.Worker is PawnColumnWorker_WorkGiver wgWorker &&
                            wgCol is PawnColumnDef_WorkGiver wgDef)
                        {
                            wgWorker.ColumnWorkerWorkType = null;
                            WorkGiverGroupLinks.MajorGroupByWorkGiver[wgDef.workgiver] = groupWorker;
                        }
                    }
                }
            }

            UpdateNativeWorkTypeExpandState(newColumns);
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

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Worker is PawnColumnWorker_MajorWorkGroup groupWorker)
                {
                    int j = i + 1;
                    int childStart = j;
                    while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
                    {
                        j++;
                    }

                    int childCount = j - childStart;
                    groupWorker.CanExpand = childCount > 0;

                    for (int k = childStart; k < j; k++)
                    {
                        if (columns[k].Worker is PawnColumnWorker_WorkGiver wgWorker &&
                            columns[k] is PawnColumnDef_WorkGiver wgDef)
                        {
                            wgWorker.ColumnWorkerWorkType = null;
                            WorkGiverGroupLinks.MajorGroupByWorkGiver[wgDef.workgiver] = groupWorker;
                        }
                    }

                    i = j - 1;
                }
            }

            UpdateNativeWorkTypeExpandState(columns);
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
                    i = j - 1;
                }
            }
        }

        private static int ResolveInsertIndex(List<PawnColumnDef> columns, string anchor, WorkTabGroupsManager manager)
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

                return EndOfBlockIndex(columns, wtIndex);
            }

            if (AnchorKeys.TryParseGroup(anchor, out string groupDefName))
            {
                int groupIndex = FindMajorGroupIndex(columns, groupDefName, manager);
                if (groupIndex < 0)
                {
                    return columns.Count;
                }

                return EndOfBlockIndex(columns, groupIndex);
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
                if (columns[i].workType != null && columns[i].workType.defName == workTypeName)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindMajorGroupIndex(List<PawnColumnDef> columns, string groupDefName, WorkTabGroupsManager manager)
        {
            PawnColumnDef target = manager.GetColumnDefForGroup(manager.GetGroup(groupDefName));
            if (target == null)
            {
                return -1;
            }

            return columns.IndexOf(target);
        }

        private static int EndOfBlockIndex(List<PawnColumnDef> columns, int headerIndex)
        {
            int j = headerIndex + 1;
            while (j < columns.Count && columns[j].Worker is PawnColumnWorker_WorkGiver)
            {
                j++;
            }

            return j;
        }
    }
}
