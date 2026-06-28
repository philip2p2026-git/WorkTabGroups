using HarmonyLib;
using WorkTab;

namespace WorkTabGroups.Patches
{
    /// <summary>
    /// Work Tab gates both expand and collapse on CanExpand. Allow collapse when already expanded.
    /// </summary>
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), nameof(MainTabWindow_WorkTab.Expand))]
    public static class Patch_WorkTab_Expand
    {
        public static bool Prefix(IExpandableColumn expandableColumn, bool expand)
        {
            if (!expand && expandableColumn.Expanded)
            {
                expandableColumn.Expanded = false;
                return false;
            }

            return true;
        }
    }
}
