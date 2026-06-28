using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public static class PresetApplier
    {
        public static void ApplyGroupPreset(GroupPreset preset, string insertAfterAnchor, WorkTabGroupsManager manager = null)
        {
            manager ??= WorkTabGroupsManager.Instance;
            if (manager == null || preset == null)
            {
                return;
            }

            string error = manager.CreateGroup(preset.groupLabel, insertAfterAnchor);
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

        public static void ApplyLayout(LayoutPreset preset, WorkTabGroupsManager manager = null, bool skipConfirm = false)
        {
            manager ??= WorkTabGroupsManager.Instance;
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
            var presetToLiveId = new Dictionary<string, string>();
            int id = 0;

            foreach (LayoutGroupEntry entry in preset.groups)
            {
                string liveDefName = "MajorWorkGroup_" + id++;
                presetToLiveId[entry.presetGroupId] = liveDefName;

                string anchor = entry.insertAfterAnchor ?? string.Empty;
                if (AnchorKeys.TryParseGroup(anchor, out string presetGroupKey) &&
                    presetToLiveId.TryGetValue(presetGroupKey, out string liveKey))
                {
                    anchor = AnchorKeys.ForGroup(liveKey);
                }

                var data = new MajorWorkGroupData(liveDefName, entry.groupLabel, anchor);
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

            manager.ReplaceGroupsFromPreset(newGroups);
        }
    }
}
