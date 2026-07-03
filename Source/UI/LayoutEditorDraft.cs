using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public class LayoutEditorDraft
    {
        private readonly List<MajorWorkGroupData> groups = new List<MajorWorkGroupData>();
        private List<WorkLayoutEntry> workLayoutOrder = new List<WorkLayoutEntry>();
        private int nextGroupId;
        private readonly Dictionary<WorkGiverDef, MajorWorkGroupData> workGiverToGroup =
            new Dictionary<WorkGiverDef, MajorWorkGroupData>();

        public IReadOnlyList<MajorWorkGroupData> Groups => groups;

        public IReadOnlyList<WorkLayoutEntry> WorkLayoutOrder => workLayoutOrder;

        public int NextGroupId => nextGroupId;

        private LayoutEditorDraft()
        {
        }

        public static LayoutEditorDraft FromDisplayedColumns()
        {
            CapturedLayoutData captured = LayoutOrderUtility.CaptureFromDisplayedColumns();
            if (captured == null)
            {
                return null;
            }

            var draft = new LayoutEditorDraft();
            draft.groups.AddRange(LayoutOrderUtility.CloneGroups(captured.groups));
            draft.workLayoutOrder = LayoutOrderUtility.CloneWorkLayoutOrder(captured.workLayoutOrder);
            draft.nextGroupId = captured.nextGroupId;
            draft.RebuildAssignmentMap();
            draft.EnsureWorkLayoutOrder();
            return draft;
        }

        public static LayoutEditorDraft FromManager(WorkTabGroupsManager manager)
        {
            if (manager == null)
            {
                return null;
            }

            manager.EnsureWorkLayoutOrder();
            var draft = new LayoutEditorDraft();
            draft.groups.AddRange(LayoutOrderUtility.CloneGroups(manager.Groups));
            draft.workLayoutOrder = LayoutOrderUtility.CloneWorkLayoutOrder(manager.WorkLayoutOrder);
            draft.nextGroupId = manager.NextGroupId;
            draft.RebuildAssignmentMap();
            return draft;
        }

        public static LayoutEditorDraft ForEditorOpen(WorkTabGroupsManager manager)
        {
            LayoutEditorDraft draft = FromManager(manager);
            if (draft == null)
            {
                return FromDisplayedColumns();
            }

            CapturedLayoutData captured = LayoutOrderUtility.CaptureFromDisplayedColumns();
            if (captured != null)
            {
                draft.ReconcileFromCapture(captured);
            }

            return draft;
        }

        private void ReconcileFromCapture(CapturedLayoutData captured)
        {
            if (captured == null)
            {
                return;
            }

            foreach (MajorWorkGroupData capturedGroup in captured.groups)
            {
                if (capturedGroup == null || capturedGroup.defName.NullOrEmpty())
                {
                    continue;
                }

                if (GetGroup(capturedGroup.defName) != null)
                {
                    continue;
                }

                groups.Add(LayoutOrderUtility.CloneGroup(capturedGroup));
            }

            if (groups.Count > 0)
            {
                nextGroupId = Math.Max(nextGroupId, LayoutOrderUtility.ComputeNextGroupId(groups));
            }

            for (int i = 0; i < captured.workLayoutOrder.Count; i++)
            {
                WorkLayoutEntry entry = captured.workLayoutOrder[i];
                if (entry.kind != WorkLayoutEntryKind.CustomGroup || entry.key.NullOrEmpty())
                {
                    continue;
                }

                if (GetGroup(entry.key) == null)
                {
                    continue;
                }

                if (workLayoutOrder.Any(e =>
                        e.kind == WorkLayoutEntryKind.CustomGroup && e.key == entry.key))
                {
                    continue;
                }

                int insertIndex = UnityEngine.Mathf.Clamp(i, 0, workLayoutOrder.Count);
                workLayoutOrder.Insert(insertIndex, WorkLayoutEntry.ForCustomGroup(entry.key));
            }

            EnsureWorkLayoutOrder();
            RebuildAssignmentMap();
        }

        public void EnsureWorkLayoutOrder()
        {
            List<string> nativeOrder = LayoutOrderUtility.GetNativeWorkTypeOrder();
            if (workLayoutOrder.Count == 0)
            {
                workLayoutOrder = groups.Count > 0
                    ? LayoutOrderMigration.MigrateFromAnchors(groups, nativeOrder)
                    : LayoutOrderUtility.BuildDefaultLayoutOrder(nativeOrder);
            }
            else
            {
                LayoutOrderUtility.SyncWorkTypesInLayoutOrder(workLayoutOrder, nativeOrder, groups);
            }
        }

        public MajorWorkGroupData GetGroup(string defName)
        {
            return groups.FirstOrDefault(g => g.defName == defName);
        }

        public bool IsAssignedToCustomGroup(WorkGiverDef workGiver)
        {
            return workGiver != null && workGiverToGroup.ContainsKey(workGiver);
        }

        public string CreateGroup(string label, int layoutIndex = -1)
        {
            EnsureWorkLayoutOrder();

            if (string.IsNullOrWhiteSpace(label))
            {
                return "WorkTabGroups.Error.EmptyName".Translate();
            }

            string defName = "MajorWorkGroup_" + nextGroupId++;
            var data = new MajorWorkGroupData(defName, label.Trim(), string.Empty)
            {
                expanded = true
            };
            groups.Add(data);

            if (layoutIndex < 0 || layoutIndex > workLayoutOrder.Count)
            {
                layoutIndex = workLayoutOrder.Count;
            }

            workLayoutOrder.Insert(layoutIndex, WorkLayoutEntry.ForCustomGroup(defName));
            RebuildAssignmentMap();
            return null;
        }

        public void MoveLayoutEntry(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= workLayoutOrder.Count ||
                toIndex < 0 || toIndex > workLayoutOrder.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            WorkLayoutEntry entry = workLayoutOrder[fromIndex];
            if (entry.kind != WorkLayoutEntryKind.CustomGroup)
            {
                return;
            }

            workLayoutOrder.RemoveAt(fromIndex);
            if (toIndex > fromIndex)
            {
                toIndex--;
            }

            toIndex = UnityEngine.Mathf.Clamp(toIndex, 0, workLayoutOrder.Count);
            workLayoutOrder.Insert(toIndex, entry);
        }

        public void MoveWorkGiverWithinGroup(MajorWorkGroupData group, int fromIndex, int toIndex)
        {
            if (group == null)
            {
                return;
            }

            List<string> order = group.assignedWorkGiverDefNames;
            if (fromIndex < 0 || fromIndex >= order.Count ||
                toIndex < 0 || toIndex >= order.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            string moving = order[fromIndex];
            order.RemoveAt(fromIndex);
            order.Insert(toIndex, moving);
        }

        public void AssignWorkGiverAt(WorkGiverDef workGiver, string groupDefName, int indexInGroup = -1)
        {
            if (workGiver == null)
            {
                return;
            }

            UnassignWorkGiver(workGiver);

            MajorWorkGroupData group = GetGroup(groupDefName);
            if (group == null)
            {
                return;
            }

            if (indexInGroup < 0 || indexInGroup > group.assignedWorkGiverDefNames.Count)
            {
                indexInGroup = group.assignedWorkGiverDefNames.Count;
            }

            group.assignedWorkGiverDefNames.Insert(indexInGroup, workGiver.defName);
            group.expanded = true;
            workGiverToGroup[workGiver] = group;
        }

        public void AssignWorkGiver(WorkGiverDef workGiver, string groupDefName)
        {
            AssignWorkGiverAt(workGiver, groupDefName);
        }

        public void UnassignWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return;
            }

            if (workGiverToGroup.TryGetValue(workGiver, out MajorWorkGroupData group))
            {
                group.assignedWorkGiverDefNames.Remove(workGiver.defName);
                workGiverToGroup.Remove(workGiver);

                if (group.assignedWorkGiverDefNames.Count == 0)
                {
                    group.expanded = false;
                }
            }
        }

        public string RenameGroup(string defName, string newLabel)
        {
            MajorWorkGroupData group = GetGroup(defName);
            if (group == null)
            {
                return "WorkTabGroups.Error.GroupNotFound".Translate();
            }

            if (string.IsNullOrWhiteSpace(newLabel))
            {
                return "WorkTabGroups.Error.EmptyName".Translate();
            }

            group.label = newLabel.Trim();
            return null;
        }

        public void DeleteGroup(string defName)
        {
            MajorWorkGroupData group = GetGroup(defName);
            if (group == null)
            {
                return;
            }

            foreach (string wgName in group.assignedWorkGiverDefNames.ToList())
            {
                WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                if (wg != null)
                {
                    workGiverToGroup.Remove(wg);
                }
            }

            groups.Remove(group);
            workLayoutOrder.RemoveAll(e =>
                e.kind == WorkLayoutEntryKind.CustomGroup && e.key == defName);
            RebuildAssignmentMap();
        }

        public void ReplaceFromPreset(LayoutPreset preset)
        {
            if (preset == null)
            {
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

            groups.Clear();
            groups.AddRange(newGroups);
            workLayoutOrder = layoutOrder;
            nextGroupId = LayoutOrderUtility.ComputeNextGroupId(groups);
            EnsureWorkLayoutOrder();
            RebuildAssignmentMap();
        }

        public LayoutPreset CaptureLayoutPreset(string presetName)
        {
            EnsureWorkLayoutOrder();
            var preset = new LayoutPreset { presetName = presetName };

            foreach (WorkLayoutEntry entry in workLayoutOrder)
            {
                preset.layoutOrder.Add(new WorkLayoutEntry(entry.kind, entry.key));
            }

            foreach (MajorWorkGroupData group in groups)
            {
                preset.groups.Add(new LayoutGroupEntry
                {
                    groupLabel = group.label,
                    presetGroupId = group.defName,
                    assignedWorkGiverDefNames = new List<string>(group.assignedWorkGiverDefNames)
                });
            }

            return preset;
        }

        public GroupPreset CaptureGroupPreset(string presetName, MajorWorkGroupData group)
        {
            return new GroupPreset
            {
                presetName = presetName,
                groupLabel = group.label,
                assignedWorkGiverDefNames = new List<string>(group.assignedWorkGiverDefNames)
            };
        }

        public bool HasChangesComparedTo(WorkTabGroupsManager manager)
        {
            if (manager == null)
            {
                return true;
            }

            manager.EnsureWorkLayoutOrder();
            if (groups.Count != manager.Groups.Count)
            {
                return true;
            }

            if (workLayoutOrder.Count != manager.WorkLayoutOrder.Count)
            {
                return true;
            }

            for (int i = 0; i < workLayoutOrder.Count; i++)
            {
                WorkLayoutEntry draftEntry = workLayoutOrder[i];
                WorkLayoutEntry liveEntry = manager.WorkLayoutOrder[i];
                if (draftEntry.kind != liveEntry.kind || draftEntry.key != liveEntry.key)
                {
                    return true;
                }
            }

            foreach (MajorWorkGroupData draftGroup in groups)
            {
                MajorWorkGroupData liveGroup = manager.GetGroup(draftGroup.defName);
                if (liveGroup == null ||
                    liveGroup.label != draftGroup.label ||
                    liveGroup.expanded != draftGroup.expanded ||
                    !draftGroup.assignedWorkGiverDefNames.SequenceEqual(liveGroup.assignedWorkGiverDefNames))
                {
                    return true;
                }
            }

            return false;
        }

        internal List<MajorWorkGroupData> CloneGroupsForCommit()
        {
            return LayoutOrderUtility.CloneGroups(groups);
        }

        internal List<WorkLayoutEntry> CloneWorkLayoutOrderForCommit()
        {
            return LayoutOrderUtility.CloneWorkLayoutOrder(workLayoutOrder);
        }

        private void RebuildAssignmentMap()
        {
            workGiverToGroup.Clear();
            foreach (MajorWorkGroupData group in groups)
            {
                foreach (string wgName in group.assignedWorkGiverDefNames)
                {
                    WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                    if (wg != null)
                    {
                        workGiverToGroup[wg] = group;
                    }
                }
            }
        }
    }
}
