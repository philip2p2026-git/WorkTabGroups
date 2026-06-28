using HarmonyLib;
using WorkTab;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkGiver), nameof(PawnColumnWorker_WorkGiver.VisibleCurrently), MethodType.Getter)]
    public static class Patch_WorkGiver_VisibleCurrently
    {
        public static void Postfix(PawnColumnWorker_WorkGiver __instance, ref bool __result)
        {
            if (__instance.WorkGiver != null &&
                WorkGiverGroupLinks.MajorGroupByWorkGiver.TryGetValue(__instance.WorkGiver, out PawnColumnWorker_MajorWorkGroup groupWorker))
            {
                __result = groupWorker.Expanded;
            }
        }
    }
}
