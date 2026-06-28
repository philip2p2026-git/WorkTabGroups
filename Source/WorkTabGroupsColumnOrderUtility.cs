using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
    public static class WorkTabGroupsColumnOrderUtility
    {
        public static bool TryGetScrollDirection(Rect headerRect, bool clicked, out int direction)
        {
            direction = 0;
            if (!InteractionUtilities.Shift || !InteractionUtilities.Ctrl)
            {
                return false;
            }

            if (!clicked)
            {
                if (InteractionUtilities.ScrolledUp(headerRect, stopPropagation: true))
                {
                    direction = -1;
                    return true;
                }

                if (InteractionUtilities.ScrolledDown(headerRect, stopPropagation: true))
                {
                    direction = 1;
                    return true;
                }

                return false;
            }

            if (InteractionUtilities.ScrolledUp())
            {
                direction = -1;
                return true;
            }

            if (InteractionUtilities.ScrolledDown())
            {
                direction = 1;
                return true;
            }

            return false;
        }

        public static bool TryGetCustomGroupSectionBounds(
            List<PawnColumnDef> columns,
            PawnColumnDef workGiverColumn,
            out int headerIndex,
            out int endIndexExclusive)
        {
            headerIndex = -1;
            endIndexExclusive = -1;
            if (columns == null || workGiverColumn == null)
            {
                return false;
            }

            int columnIndex = columns.IndexOf(workGiverColumn);
            if (columnIndex < 0)
            {
                return false;
            }

            for (int i = columnIndex - 1; i >= 0; i--)
            {
                if (columns[i].Worker is PawnColumnWorker_MajorWorkGroup)
                {
                    headerIndex = i;
                    break;
                }

                if (columns[i].Worker is PawnColumnWorker_WorkType)
                {
                    return false;
                }
            }

            if (headerIndex < 0)
            {
                return false;
            }

            endIndexExclusive = headerIndex + 1;
            while (endIndexExclusive < columns.Count && columns[endIndexExclusive].Worker is PawnColumnWorker_WorkGiver)
            {
                endIndexExclusive++;
            }

            return columnIndex >= headerIndex + 1 && columnIndex < endIndexExclusive;
        }

        public static bool TryGetNativeWorkTypeSectionBounds(
            List<PawnColumnDef> columns,
            WorkTypeDef workType,
            WorkTabGroupsManager manager,
            out int headerIndex,
            out int endIndexExclusive)
        {
            headerIndex = -1;
            endIndexExclusive = -1;
            if (columns == null || workType == null)
            {
                return false;
            }

            for (int i = 0; i < columns.Count; i++)
            {
                PawnColumnDef col = columns[i];
                if (col.workType == workType && col.Worker is PawnColumnWorker_WorkType)
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex < 0)
            {
                return false;
            }

            endIndexExclusive = headerIndex + 1;
            while (endIndexExclusive < columns.Count)
            {
                if (columns[endIndexExclusive].Worker is PawnColumnWorker_WorkGiver wgCol &&
                    columns[endIndexExclusive] is PawnColumnDef_WorkGiver wgDef &&
                    (manager == null || !manager.IsAssignedToCustomGroup(wgDef.workgiver)))
                {
                    endIndexExclusive++;
                    continue;
                }

                break;
            }

            return endIndexExclusive > headerIndex + 1;
        }

        public static List<WorkGiverDef> GetNativeUnassignedWorkGivers(WorkTypeDef workType, WorkTabGroupsManager manager)
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

        public static void SwapColumnsInList(List<PawnColumnDef> columns, int indexA, int indexB)
        {
            if (columns == null || indexA < 0 || indexB < 0 || indexA >= columns.Count || indexB >= columns.Count || indexA == indexB)
            {
                return;
            }

            PawnColumnDef temp = columns[indexA];
            columns[indexA] = columns[indexB];
            columns[indexB] = temp;
        }

        public static void SwapPriorityInType(WorkGiverDef a, WorkGiverDef b)
        {
            if (a == null || b == null)
            {
                return;
            }

            int temp = a.priorityInType;
            a.priorityInType = b.priorityInType;
            b.priorityInType = temp;
        }
    }
}
