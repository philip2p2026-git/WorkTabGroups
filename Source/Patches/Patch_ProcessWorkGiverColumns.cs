using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups.Patches
{
  /// <summary>
  /// Work Tab drops non-WorkType WorkPriority columns during ProcessWorkGiverColumns.
  /// Preserve custom group headers so column order stays stable between rebuilds.
  /// </summary>
    [HarmonyPatch(typeof(DefGenerator_GenerateImpliedDefs_PreResolve), "ProcessWorkGiverColumns")]
    public static class Patch_ProcessWorkGiverColumns
    {
        public static void Prefix(List<PawnColumnDef> workTableColumns, ref List<PawnColumnDef> __state)
        {
            __state = new List<PawnColumnDef>();
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            foreach (PawnColumnDef col in workTableColumns)
            {
                if (col.workerClass != typeof(PawnColumnWorker_MajorWorkGroup))
                {
                    continue;
                }

                if (manager == null)
                {
                    continue;
                }

                if (col is PawnColumnDef_MajorWorkGroup majorCol &&
                    majorCol.majorWorkGroup?.data != null &&
                    manager.GetGroup(majorCol.majorWorkGroup.data.defName) != null)
                {
                    __state.Add(col);
                }
            }
        }

        public static void Postfix(List<PawnColumnDef> __result, List<PawnColumnDef> __state)
        {
            if (__state == null || __state.Count == 0)
            {
                return;
            }

            foreach (PawnColumnDef col in __state)
            {
                if (!__result.Contains(col))
                {
                    __result.Add(col);
                }
            }
        }
    }
}
