using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using WorkTab;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(DefGenerator_GenerateImpliedDefs_PreResolve), nameof(DefGenerator_GenerateImpliedDefs_PreResolve.ReBuildWorkTabColumns))]
    public static class Patch_ReBuildWorkTabColumns
    {
        public static void Postfix()
        {
            WorkTabGroupsColumnBuilder.Inject();
        }
    }

    [HarmonyPatch(typeof(DefGenerator_GenerateImpliedDefs_PreResolve), nameof(DefGenerator_GenerateImpliedDefs_PreResolve.InitializeExpandableColumns))]
    public static class Patch_InitializeExpandableColumns
    {
        public static void Postfix()
        {
            WorkTabGroupsColumnBuilder.WireExpandableColumns();
        }
    }
}
