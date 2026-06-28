using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using WorkTab;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkGiver), "HeaderInteractions")]
    public static class Patch_WorkGiver_HeaderInteractions
    {
        public static bool Prefix(PawnColumnWorker_WorkGiver __instance, Rect headerRect, PawnTable table, bool clicked)
        {
            WorkGiverDef workGiver = __instance.WorkGiver;
            if (workGiver == null || !Mouse.IsOver(headerRect))
            {
                return true;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return true;
            }

            if (InteractionUtilities.Shift &&
                InteractionUtilities.Ctrl &&
                WorkTabGroupsColumnOrderUtility.TryGetScrollDirection(headerRect, clicked, out int direction))
            {
                MajorWorkGroupData group = manager.GetGroupForWorkGiver(workGiver);
                bool reordered = group != null
                    ? manager.ReorderWorkGiverInGroup(group, workGiver, direction)
                    : manager.ReorderNativeWorkGiver(workGiver.workType, workGiver, direction);

                if (reordered)
                {
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
                }

                return false;
            }

            if (InteractionUtilities.Shift)
            {
                return true;
            }

            bool rightClick = clicked ? InteractionUtilities.RightClicked() : InteractionUtilities.RightClicked(headerRect);
            if (!rightClick)
            {
                return true;
            }

            MajorWorkGroupData currentGroup = manager.GetGroupForWorkGiver(workGiver);
            var options = new System.Collections.Generic.List<FloatMenuOption>();

            foreach (MajorWorkGroupData groupOption in manager.Groups)
            {
                MajorWorkGroupData captured = groupOption;
                string label = groupOption.label;
                if (currentGroup != null && currentGroup.defName == groupOption.defName)
                {
                    label = "✓ " + label;
                }

                options.Add(new FloatMenuOption("WorkTabGroups.MoveToGroup".Translate(label), () =>
                {
                    manager.AssignWorkGiver(workGiver, captured.defName);
                }));
            }

            if (currentGroup != null)
            {
                options.Add(new FloatMenuOption("WorkTabGroups.RemoveFromGroup".Translate(), () =>
                {
                    manager.UnassignWorkGiver(workGiver);
                }));
            }

            options.Add(new FloatMenuOption("WorkTabGroups.NewGroup".Translate(), () =>
            {
                Find.WindowStack.Add(new Dialog_CreateMajorWorkGroup(workGiver));
            }));

            Find.WindowStack.Add(new FloatMenu(options));
            return false;
        }
    }
}
