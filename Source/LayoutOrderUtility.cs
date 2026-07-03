using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
    public class CapturedLayoutData
    {
        public List<MajorWorkGroupData> groups = new List<MajorWorkGroupData>();
        public List<WorkLayoutEntry> workLayoutOrder = new List<WorkLayoutEntry>();
        public int nextGroupId;
    }

    public static class LayoutOrderUtility
    {
        public static List<string> GetNativeWorkTypeOrder()
        {
            var result = new List<string>();
            List<PawnColumnDef> columns = PawnTableDefOf.Work?.columns;
            if (columns != null)
            {
                foreach (PawnColumnDef col in columns)
                {
                    if (col.workType != null && col.Worker is PawnColumnWorker_WorkType &&
                        WorkTypeDefExists(col.workType.defName) &&
                        !result.Contains(col.workType.defName))
                    {
                        result.Add(col.workType.defName);
                    }
                }
            }

            if (result.Count == 0)
            {
                foreach (PawnColumnDef col in DefDatabase<PawnColumnDef>.AllDefsListForReading
                    .Where(c => c.workType != null && c.workerClass == typeof(PawnColumnWorker_WorkType))
                    .OrderByDescending(c => c.workType.naturalPriority))
                {
                    if (WorkTypeDefExists(col.workType.defName) &&
                        !result.Contains(col.workType.defName))
                    {
                        result.Add(col.workType.defName);
                    }
                }
            }

            return result;
        }

        public static List<WorkLayoutEntry> BuildDefaultLayoutOrder(IEnumerable<string> workTypeOrder = null)
        {
            var order = new List<WorkLayoutEntry>();
            foreach (string workTypeName in workTypeOrder ?? GetNativeWorkTypeOrder())
            {
                if (WorkTypeDefExists(workTypeName))
                {
                    order.Add(WorkLayoutEntry.ForWorkType(workTypeName));
                }
            }

            return order;
        }

        public static List<WorkGiverDef> GetUnassignedWorkGivers(WorkTypeDef workType, WorkTabGroupsManager manager)
        {
            return GetUnassignedWorkGivers(workType, wg => manager != null && manager.IsAssignedToCustomGroup(wg));
        }

        public static List<WorkGiverDef> GetUnassignedWorkGivers(
            WorkTypeDef workType,
            Func<WorkGiverDef, bool> isAssignedToCustomGroup)
        {
            var result = new List<WorkGiverDef>();
            if (workType?.workGiversByPriority == null)
            {
                return result;
            }

            foreach (WorkGiverDef wg in workType.workGiversByPriority.OrderByDescending(w => w.priorityInType))
            {
                if (isAssignedToCustomGroup == null || !isAssignedToCustomGroup(wg))
                {
                    result.Add(wg);
                }
            }

            return result;
        }

        public static CapturedLayoutData CaptureFromDisplayedColumns()
        {
            List<PawnColumnDef> columns = PawnTableDefOf.Work?.columns;
            if (columns == null || columns.Count == 0)
            {
                return null;
            }

            var data = new CapturedLayoutData();
            var groupsByDefName = new Dictionary<string, MajorWorkGroupData>();
            MajorWorkGroupData currentGroup = null;
            bool hasWorkColumns = false;

            foreach (PawnColumnDef col in columns)
            {
                if (col.Worker is PawnColumnWorker_WorkType && col.workType != null)
                {
                    hasWorkColumns = true;
                    currentGroup = null;
                    data.workLayoutOrder.Add(WorkLayoutEntry.ForWorkType(col.workType.defName));
                }
                else if (col.Worker is PawnColumnWorker_MajorWorkGroup groupWorker)
                {
                    hasWorkColumns = true;
                    MajorWorkGroupData groupData = ResolveGroupDataFromColumn(col, groupWorker);
                    if (groupData == null)
                    {
                        continue;
                    }

                    if (!groupsByDefName.TryGetValue(groupData.defName, out MajorWorkGroupData tracked))
                    {
                        tracked = CloneGroup(groupData);
                        tracked.assignedWorkGiverDefNames.Clear();
                        groupsByDefName[tracked.defName] = tracked;
                        data.groups.Add(tracked);
                    }

                    if (groupWorker.Expanded)
                    {
                        tracked.expanded = true;
                    }

                    currentGroup = tracked;
                    if (!data.workLayoutOrder.Any(e =>
                            e.kind == WorkLayoutEntryKind.CustomGroup && e.key == tracked.defName))
                    {
                        data.workLayoutOrder.Add(WorkLayoutEntry.ForCustomGroup(tracked.defName));
                    }
                }
                else if (col.Worker is PawnColumnWorker_WorkGiver)
                {
                    hasWorkColumns = true;
                    WorkGiverDef workGiver = GetWorkGiverFromColumn(col);
                    if (workGiver == null)
                    {
                        continue;
                    }

                    if (currentGroup != null &&
                        !currentGroup.assignedWorkGiverDefNames.Contains(workGiver.defName))
                    {
                        currentGroup.assignedWorkGiverDefNames.Add(workGiver.defName);
                    }
                }
            }

            if (!hasWorkColumns)
            {
                return null;
            }

            data.nextGroupId = ComputeNextGroupId(data.groups);
            LayoutSanitizer.PruneLayoutData(data.groups, data.workLayoutOrder);
            return data;
        }

        public static MajorWorkGroupData CloneGroup(MajorWorkGroupData source)
        {
            if (source == null)
            {
                return null;
            }

            var copy = new MajorWorkGroupData(source.defName, source.label, string.Empty)
            {
                expanded = source.expanded
            };
            copy.assignedWorkGiverDefNames.AddRange(source.assignedWorkGiverDefNames);
            return copy;
        }

        public static List<WorkLayoutEntry> CloneWorkLayoutOrder(IEnumerable<WorkLayoutEntry> source)
        {
            var result = new List<WorkLayoutEntry>();
            if (source == null)
            {
                return result;
            }

            foreach (WorkLayoutEntry entry in source)
            {
                result.Add(new WorkLayoutEntry(entry.kind, entry.key));
            }

            return result;
        }

        public static List<MajorWorkGroupData> CloneGroups(IEnumerable<MajorWorkGroupData> source)
        {
            var result = new List<MajorWorkGroupData>();
            if (source == null)
            {
                return result;
            }

            foreach (MajorWorkGroupData group in source)
            {
                MajorWorkGroupData copy = CloneGroup(group);
                if (copy != null)
                {
                    result.Add(copy);
                }
            }

            return result;
        }

        public static PawnColumnDef FindWorkTypeColumn(List<PawnColumnDef> columns, string workTypeDefName)
        {
            if (columns == null || workTypeDefName.NullOrEmpty())
            {
                return null;
            }

            foreach (PawnColumnDef col in columns)
            {
                if (col.workType != null &&
                    col.workType.defName == workTypeDefName &&
                    col.Worker is PawnColumnWorker_WorkType)
                {
                    return col;
                }
            }

            return DefDatabase<PawnColumnDef>.AllDefsListForReading.FirstOrDefault(c =>
                c.workType?.defName == workTypeDefName &&
                c.workerClass == typeof(PawnColumnWorker_WorkType));
        }

        public static void SyncWorkTypesInLayoutOrder(
            List<WorkLayoutEntry> layoutOrder,
            List<string> nativeWorkTypeOrder,
            WorkTabGroupsManager manager = null)
        {
            SyncWorkTypesInLayoutOrder(
                layoutOrder,
                nativeWorkTypeOrder,
                manager?.Groups);
        }

        public static void SyncWorkTypesInLayoutOrder(
            List<WorkLayoutEntry> layoutOrder,
            List<string> nativeWorkTypeOrder,
            IEnumerable<MajorWorkGroupData> groups)
        {
            if (layoutOrder == null)
            {
                return;
            }

            nativeWorkTypeOrder = FilterExistingWorkTypes(nativeWorkTypeOrder ?? GetNativeWorkTypeOrder());

            if (nativeWorkTypeOrder.Count > 0)
            {
                layoutOrder.RemoveAll(e =>
                    e.kind == WorkLayoutEntryKind.WorkType &&
                    (!WorkTypeDefExists(e.key) || !nativeWorkTypeOrder.Contains(e.key)));
            }

            var groupDefNames = new HashSet<string>(
                groups?.Select(g => g.defName) ?? Enumerable.Empty<string>());
            layoutOrder.RemoveAll(e =>
                e.kind == WorkLayoutEntryKind.CustomGroup &&
                !groupDefNames.Contains(e.key));

            foreach (string workTypeName in nativeWorkTypeOrder)
            {
                if (layoutOrder.Any(e => e.kind == WorkLayoutEntryKind.WorkType && e.key == workTypeName))
                {
                    continue;
                }

                int vanillaIndex = nativeWorkTypeOrder.IndexOf(workTypeName);
                int insertIndex = 0;
                if (vanillaIndex > 0)
                {
                    string previousWorkType = nativeWorkTypeOrder[vanillaIndex - 1];
                    for (int j = 0; j < layoutOrder.Count; j++)
                    {
                        if (layoutOrder[j].kind == WorkLayoutEntryKind.WorkType &&
                            layoutOrder[j].key == previousWorkType)
                        {
                            insertIndex = j + 1;
                            while (insertIndex < layoutOrder.Count &&
                                   layoutOrder[insertIndex].kind == WorkLayoutEntryKind.CustomGroup)
                            {
                                insertIndex++;
                            }

                            break;
                        }
                    }
                }

                layoutOrder.Insert(insertIndex, WorkLayoutEntry.ForWorkType(workTypeName));
            }

            SyncCustomGroupsInLayoutOrder(layoutOrder, groupDefNames);
        }

        public static int ComputeNextGroupId(IEnumerable<MajorWorkGroupData> groups, int minimum = 0)
        {
            int max = minimum;
            foreach (MajorWorkGroupData group in groups ?? Enumerable.Empty<MajorWorkGroupData>())
            {
                if (group?.defName != null && group.defName.StartsWith("MajorWorkGroup_") &&
                    int.TryParse(group.defName.Substring("MajorWorkGroup_".Length), out int id))
                {
                    max = Math.Max(max, id + 1);
                }
            }

            return max;
        }

        public static void SyncCustomGroupsInLayoutOrder(
            List<WorkLayoutEntry> layoutOrder,
            IEnumerable<MajorWorkGroupData> groups)
        {
            if (layoutOrder == null || groups == null)
            {
                return;
            }

            foreach (MajorWorkGroupData group in groups)
            {
                if (group == null || group.defName.NullOrEmpty())
                {
                    continue;
                }

                if (layoutOrder.Any(e =>
                        e.kind == WorkLayoutEntryKind.CustomGroup && e.key == group.defName))
                {
                    continue;
                }

                layoutOrder.Insert(0, WorkLayoutEntry.ForCustomGroup(group.defName));
            }
        }

        public static void SyncCustomGroupsInLayoutOrder(
            List<WorkLayoutEntry> layoutOrder,
            HashSet<string> groupDefNames)
        {
            if (layoutOrder == null || groupDefNames == null)
            {
                return;
            }

            foreach (string defName in groupDefNames)
            {
                if (defName.NullOrEmpty())
                {
                    continue;
                }

                if (layoutOrder.Any(e =>
                        e.kind == WorkLayoutEntryKind.CustomGroup && e.key == defName))
                {
                    continue;
                }

                layoutOrder.Insert(0, WorkLayoutEntry.ForCustomGroup(defName));
            }
        }

        private static bool WorkTypeDefExists(string workTypeDefName)
        {
            return !workTypeDefName.NullOrEmpty() &&
                   DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName) != null;
        }

        private static List<string> FilterExistingWorkTypes(List<string> workTypeNames)
        {
            var result = new List<string>();
            if (workTypeNames == null)
            {
                return result;
            }

            foreach (string workTypeName in workTypeNames)
            {
                if (WorkTypeDefExists(workTypeName) && !result.Contains(workTypeName))
                {
                    result.Add(workTypeName);
                }
            }

            return result;
        }

        private static MajorWorkGroupData ResolveGroupDataFromColumn(
            PawnColumnDef column,
            PawnColumnWorker_MajorWorkGroup groupWorker)
        {
            if (column is PawnColumnDef_MajorWorkGroup majorCol && majorCol.majorWorkGroup?.data != null)
            {
                return majorCol.majorWorkGroup.data;
            }

            return groupWorker.BoundGroup;
        }

        private static WorkGiverDef GetWorkGiverFromColumn(PawnColumnDef column)
        {
            if (column is PawnColumnDef_WorkGiver workGiverCol)
            {
                return workGiverCol.workgiver;
            }

            return null;
        }
    }
}
