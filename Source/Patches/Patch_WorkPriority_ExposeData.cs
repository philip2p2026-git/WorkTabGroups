using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups.Patches
{
    /// <summary>
    /// Work Tab uses Scribe_Defs.Look for WorkGiver refs; RimWorld logs Error before Work Tab's try/catch
    /// when a mod that added the WorkGiver was removed. Resolve def names silently on load instead.
    /// </summary>
    [HarmonyPatch(typeof(WorkPriority), nameof(WorkPriority.ExposeData))]
    public static class Patch_WorkPriority_ExposeData
    {
        private static readonly HashSet<string> LoggedMissingWorkGivers = new HashSet<string>();

        private static readonly AccessTools.FieldRef<WorkPriority, WorkGiverDef> WorkgiverField =
            AccessTools.FieldRefAccess<WorkPriority, WorkGiverDef>("workgiver");

        public static bool Prefix(WorkPriority __instance)
        {
            if (Scribe.mode != LoadSaveMode.LoadingVars)
            {
                if (Scribe.mode == LoadSaveMode.Inactive)
                {
                    LoggedMissingWorkGivers.Clear();
                }

                return true;
            }

            string workGiverDefName = null;
            Scribe_Values.Look(ref workGiverDefName, "Workgiver");

            WorkGiverDef workGiver = null;
            if (!workGiverDefName.NullOrEmpty())
            {
                workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(workGiverDefName);
                if (workGiver == null && Prefs.DevMode &&
                    LoggedMissingWorkGivers.Add(workGiverDefName))
                {
                    Log.Message(
                        "[WorkTabGroups] Skipped priority entries for removed WorkGiver: " + workGiverDefName);
                }
            }

            WorkgiverField(__instance) = workGiver;

            string prioritiesSource = string.Empty;
            Scribe_Values.Look(ref prioritiesSource, "Priorities");
            int[] priorities = string.IsNullOrEmpty(prioritiesSource)
                ? new int[0]
                : prioritiesSource.Select(c => int.Parse(c.ToString())).ToArray();

            AccessTools.Property(typeof(WorkPriority), "Priorities")
                .GetSetMethod(nonPublic: true)
                .Invoke(__instance, new object[] { priorities });

            return false;
        }
    }
}
