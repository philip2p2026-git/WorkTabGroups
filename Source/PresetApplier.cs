using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public static class PresetApplier
    {
        public static void ApplyGroupPreset(GroupPreset preset, int layoutIndex, WorkTabGroupsManager manager = null)
        {
            manager ??= WorkTabGroupsManager.EnsureRegistered();
            if (manager == null || preset == null)
            {
                return;
            }

            string error = manager.CreateGroup(preset.groupLabel, layoutIndex);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            MajorWorkGroupData group = manager.Groups.LastOrDefault();
            if (group == null)
            {
                return;
            }

            foreach (string wgName in preset.assignedWorkGiverDefNames)
            {
                WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                if (wg != null)
                {
                    manager.AssignWorkGiver(wg, group.defName);
                }
                else if (Prefs.DevMode)
                {
                    Log.Message("[WorkTabGroups] Skipped missing WorkGiver def: " + wgName);
                }
            }
        }

        public static void ApplyGroupPreset(GroupPreset preset, string insertAfterAnchor, WorkTabGroupsManager manager = null)
        {
            manager ??= WorkTabGroupsManager.EnsureRegistered();
            if (manager == null)
            {
                return;
            }

            ApplyGroupPreset(preset, manager.ResolveLayoutIndexFromAnchor(insertAfterAnchor), manager);
        }

        public static void ApplyLayout(LayoutPreset preset, WorkTabGroupsManager manager = null, bool skipConfirm = false)
        {
            manager ??= WorkTabGroupsManager.EnsureRegistered();
            if (manager == null || preset == null)
            {
                return;
            }

            if (!skipConfirm && manager.Groups.Count > 0)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WorkTabGroups.ConfirmReplaceLayout".Translate(),
                    () => ApplyLayout(preset, manager, skipConfirm: true)));
                return;
            }

            var newGroups = new List<MajorWorkGroupData>();
            int id = 0;

            foreach (LayoutGroupEntry entry in preset.groups)
            {
                string liveDefName = "MajorWorkGroup_" + id++;
                var data = new MajorWorkGroupData(liveDefName, entry.groupLabel, string.Empty);
                foreach (string wgName in entry.assignedWorkGiverDefNames)
                {
                    if (DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName) != null)
                    {
                        data.assignedWorkGiverDefNames.Add(wgName);
                    }
                    else if (Prefs.DevMode)
                    {
                        Log.Message("[WorkTabGroups] Skipped missing WorkGiver def: " + wgName);
                    }
                }

                newGroups.Add(data);
            }

            List<WorkLayoutEntry> layoutOrder;
            if (preset.layoutOrder != null && preset.layoutOrder.Count > 0)
            {
                layoutOrder = new List<WorkLayoutEntry>();
                int groupIndex = 0;
                foreach (WorkLayoutEntry entry in preset.layoutOrder)
                {
                    if (entry.kind == WorkLayoutEntryKind.WorkType)
                    {
                        layoutOrder.Add(WorkLayoutEntry.ForWorkType(entry.key));
                    }
                    else if (groupIndex < newGroups.Count)
                    {
                        layoutOrder.Add(WorkLayoutEntry.ForCustomGroup(newGroups[groupIndex].defName));
                        groupIndex++;
                    }
                }
            }
            else
            {
                var pseudoGroups = new List<MajorWorkGroupData>();
                id = 0;
                foreach (LayoutGroupEntry entry in preset.groups)
                {
                    string liveDefName = "MajorWorkGroup_" + id++;
                    var data = new MajorWorkGroupData(
                        liveDefName,
                        entry.groupLabel,
                        entry.insertAfterAnchor ?? string.Empty);
                    data.assignedWorkGiverDefNames.AddRange(entry.assignedWorkGiverDefNames);
                    pseudoGroups.Add(data);
                }

                layoutOrder = LayoutOrderMigration.MigrateFromAnchors(pseudoGroups);
                newGroups.Clear();
                foreach (MajorWorkGroupData pseudo in pseudoGroups)
                {
                    newGroups.Add(new MajorWorkGroupData(pseudo.defName, pseudo.label, string.Empty)
                    {
                        expanded = pseudo.expanded
                    });
                    newGroups.Last().assignedWorkGiverDefNames.AddRange(pseudo.assignedWorkGiverDefNames);
                }
            }

            manager.ReplaceGroupsFromPreset(newGroups, layoutOrder);
        }
    }
}
