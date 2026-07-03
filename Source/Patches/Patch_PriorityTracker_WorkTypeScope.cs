using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups.Patches
{
    /// <summary>
    /// WorkType priority bulk/display ops must ignore WorkGivers assigned to custom groups.
    /// </summary>
    [HarmonyPatch(typeof(PriorityTracker), nameof(PriorityTracker.GetPriority), typeof(WorkTypeDef), typeof(int))]
    public static class Patch_PriorityTracker_GetPriority_WorkType
    {
        public static void Postfix(PriorityTracker __instance, WorkTypeDef workType, int hour, ref int __result)
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null || workType == null || workType.workGiversByPriority == null ||
                !__instance.Pawn.AllowedToDo(workType))
            {
                return;
            }

            int min = int.MaxValue;
            bool any = false;
            foreach (WorkGiverDef wg in workType.workGiversByPriority)
            {
                if (manager.IsAssignedToCustomGroup(wg))
                {
                    continue;
                }

                int prio = __instance.GetPriority(wg, hour);
                if (prio > 0)
                {
                    min = System.Math.Min(min, prio);
                    any = true;
                }
            }

            __result = any ? min : 0;
        }
    }

    [HarmonyPatch(typeof(PriorityTracker), nameof(PriorityTracker.SetPriority), typeof(WorkTypeDef), typeof(int), typeof(int), typeof(bool))]
    public static class Patch_PriorityTracker_SetPriority_WorkType_Hour
    {
        public static bool Prefix(PriorityTracker __instance, WorkTypeDef worktype, int priority, int hour, bool recache)
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null || worktype == null || worktype.workGiversByPriority == null)
            {
                return true;
            }

            if (!__instance.Pawn.AllowedToDo(worktype))
            {
                return false;
            }

            foreach (WorkGiverDef wg in worktype.workGiversByPriority)
            {
                if (manager.IsAssignedToCustomGroup(wg))
                {
                    continue;
                }

                __instance.SetPriority(wg, priority, hour, recache: false);
            }

            if (recache)
            {
                __instance.InvalidateCache(worktype);
                AccessTools.Method(typeof(PriorityTracker), "OnChange").Invoke(__instance, null);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(PriorityTracker), nameof(PriorityTracker.SetPriority), typeof(WorkTypeDef), typeof(int), typeof(System.Collections.Generic.List<int>))]
    public static class Patch_PriorityTracker_SetPriority_WorkType_Hours
    {
        public static bool Prefix(PriorityTracker __instance, WorkTypeDef worktype, int priority, System.Collections.Generic.List<int> hours)
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null || worktype == null || worktype.workGiversByPriority == null)
            {
                return true;
            }

            if (!__instance.Pawn.AllowedToDo(worktype))
            {
                return false;
            }

            if (hours == null || hours.Count == 0)
            {
                hours = TimeUtilities.WholeDay;
            }

            foreach (WorkGiverDef wg in worktype.workGiversByPriority)
            {
                if (manager.IsAssignedToCustomGroup(wg))
                {
                    continue;
                }

                __instance.SetPriority(wg, priority, hours);
            }

            __instance.InvalidateCache(worktype);
            AccessTools.Method(typeof(PriorityTracker), "OnChange").Invoke(__instance, null);
            return false;
        }
    }
}
