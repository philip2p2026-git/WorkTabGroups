using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(PawnColumnWorker_WorkGiver), "HeaderInteractions")]
    public static class Patch_WorkGiver_HeaderInteractions
    {
        public static bool Prefix(PawnColumnWorker_WorkGiver __instance, Rect headerRect, PawnTable table, bool clicked)
        {
            if (InteractionUtilities.Shift || !Mouse.IsOver(headerRect))
            {
                return true;
            }

            bool rightClick = clicked ? InteractionUtilities.RightClicked() : InteractionUtilities.RightClicked(headerRect);
            if (!rightClick)
            {
                return true;
            }

            WorkGiverDef workGiver = __instance.WorkGiver;
            if (workGiver == null)
            {
                return true;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return true;
            }

            MajorWorkGroupData currentGroup = manager.GetGroupForWorkGiver(workGiver);
            var options = new List<FloatMenuOption>();

            foreach (MajorWorkGroupData group in manager.Groups)
            {
                MajorWorkGroupData captured = group;
                string label = group.label;
                if (currentGroup != null && currentGroup.defName == group.defName)
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
