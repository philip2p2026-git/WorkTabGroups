using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WorkTabGroups
{
    public class WorkTabGroupsSettings : ModSettings
    {
        public List<GroupPreset> groupPresets = new List<GroupPreset>();
        public List<LayoutPreset> layoutPresets = new List<LayoutPreset>();
        public string defaultLayoutPresetName = string.Empty;

        public LayoutPreset FindLayoutPreset(string name)
        {
            return layoutPresets.FirstOrDefault(p => p.presetName == name);
        }

        public GroupPreset FindGroupPreset(string name)
        {
            return groupPresets.FirstOrDefault(p => p.presetName == name);
        }

        public string SaveGroupPreset(string presetName, MajorWorkGroupData group)
        {
            if (FindGroupPreset(presetName) != null)
            {
                return "WorkTabGroups.Error.DuplicatePresetName".Translate(presetName);
            }

            groupPresets.Add(WorkTabGroupsManager.Instance.CaptureGroupPreset(presetName, group));
            Write();
            return null;
        }

        public string SaveLayoutPreset(string presetName, WorkTabGroupsManager manager)
        {
            if (manager == null)
            {
                return null;
            }

            if (FindLayoutPreset(presetName) != null)
            {
                return "WorkTabGroups.Error.DuplicatePresetName".Translate(presetName);
            }

            layoutPresets.Add(manager.CaptureLayoutPreset(presetName));
            Write();
            return null;
        }

        public string SaveLayoutPreset(string presetName, LayoutEditorDraft draft)
        {
            if (draft == null)
            {
                return null;
            }

            if (FindLayoutPreset(presetName) != null)
            {
                return "WorkTabGroups.Error.DuplicatePresetName".Translate(presetName);
            }

            layoutPresets.Add(draft.CaptureLayoutPreset(presetName));
            Write();
            return null;
        }

        public void DeleteGroupPreset(string presetName)
        {
            groupPresets.RemoveAll(p => p.presetName == presetName);
            Write();
        }

        public void DeleteLayoutPreset(string presetName)
        {
            layoutPresets.RemoveAll(p => p.presetName == presetName);
            if (defaultLayoutPresetName == presetName)
            {
                defaultLayoutPresetName = string.Empty;
            }

            Write();
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref groupPresets, "groupPresets", LookMode.Deep);
            Scribe_Collections.Look(ref layoutPresets, "layoutPresets", LookMode.Deep);
            Scribe_Values.Look(ref defaultLayoutPresetName, "defaultLayoutPresetName", string.Empty);

            if (groupPresets == null)
            {
                groupPresets = new List<GroupPreset>();
            }

            if (layoutPresets == null)
            {
                layoutPresets = new List<LayoutPreset>();
            }
        }
    }
}
