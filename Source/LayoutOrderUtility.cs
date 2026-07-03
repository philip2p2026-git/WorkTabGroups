using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
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
            var result = new List<WorkGiverDef>();
            if (workType?.workGiversByPriority == null)
            {
                return result;
            }

            foreach (WorkGiverDef wg in workType.workGiversByPriority.OrderByDescending(w => w.priorityInType))
            {
                if (manager == null || !manager.IsAssignedToCustomGroup(wg))
                {
                    result.Add(wg);
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

        public static void SyncWorkTypesInLayoutOrder(List<WorkLayoutEntry> layoutOrder, List<string> nativeWorkTypeOrder)
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
                WorkTabGroupsManager.Instance?.Groups.Select(g => g.defName) ?? Enumerable.Empty<string>());
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
    }
}
