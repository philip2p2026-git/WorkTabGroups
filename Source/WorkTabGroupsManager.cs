using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WorkTab;

namespace WorkTabGroups
{
    public class WorkTabGroupsManager : GameComponent
    {
        private static WorkTabGroupsManager instance;

        private List<MajorWorkGroupData> groups = new List<MajorWorkGroupData>();
        private int nextGroupId;

        private Dictionary<string, MajorWorkGroupDef> groupDefByName = new Dictionary<string, MajorWorkGroupDef>();
        private Dictionary<string, PawnColumnDef> columnDefByGroupName = new Dictionary<string, PawnColumnDef>();
        private Dictionary<WorkGiverDef, MajorWorkGroupData> workGiverToGroup = new Dictionary<WorkGiverDef, MajorWorkGroupData>();

        public static WorkTabGroupsManager Instance
        {
            get
            {
                if (instance == null && Current.Game != null)
                {
                    instance = Current.Game.GetComponent<WorkTabGroupsManager>();
                }

                return instance;
            }
        }

        public IReadOnlyList<MajorWorkGroupData> Groups => groups;

        public WorkTabGroupsManager(Game game)
        {
            instance = this;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RebuildRuntimeState();
            TryApplyDefaultLayoutOnNewGame();
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            TryApplyDefaultLayoutOnNewGame();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            RebuildRuntimeState();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref groups, "groups", LookMode.Deep);
            Scribe_Values.Look(ref nextGroupId, "nextGroupId", 0);

            if (groups == null)
            {
                groups = new List<MajorWorkGroupData>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SyncNextGroupId();
                RebuildRuntimeState();
            }
        }

        public void RebuildRuntimeState()
        {
            groupDefByName.Clear();
            columnDefByGroupName.Clear();
            workGiverToGroup.Clear();

            foreach (MajorWorkGroupData group in groups)
            {
                EnsureImpliedDefs(group);
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

        public MajorWorkGroupData GetGroup(string defName)
        {
            return groups.FirstOrDefault(g => g.defName == defName);
        }

        public MajorWorkGroupData GetGroupForWorkGiver(WorkGiverDef workGiver)
        {
            if (workGiver == null)
            {
                return null;
            }

            workGiverToGroup.TryGetValue(workGiver, out MajorWorkGroupData group);
            return group;
        }

        public bool IsAssignedToCustomGroup(WorkGiverDef workGiver)
        {
            return workGiver != null && workGiverToGroup.ContainsKey(workGiver);
        }

        public PawnColumnDef GetColumnDefForGroup(MajorWorkGroupData group)
        {
            columnDefByGroupName.TryGetValue(group.defName, out PawnColumnDef col);
            return col;
        }

        public MajorWorkGroupDef GetGroupDef(MajorWorkGroupData group)
        {
            groupDefByName.TryGetValue(group.defName, out MajorWorkGroupDef def);
            return def;
        }

        public PawnColumnDef GetWorkGiverColumn(WorkGiverDef workGiver)
        {
            string colName = "WorkPriority_WorkGiver_" + workGiver.defName;
            return DefDatabase<PawnColumnDef>.GetNamedSilentFail(colName);
        }

        public List<MajorWorkGroupData> GetOrderedGroupsForInjection()
        {
            var remaining = new List<MajorWorkGroupData>(groups);
            var ordered = new List<MajorWorkGroupData>();
            var placed = new HashSet<string>();

            while (remaining.Count > 0)
            {
                bool placedAny = false;
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    MajorWorkGroupData g = remaining[i];
                    if (CanPlaceGroup(g, placed))
                    {
                        ordered.Add(g);
                        placed.Add(g.defName);
                        remaining.RemoveAt(i);
                        placedAny = true;
                    }
                }

                if (!placedAny)
                {
                    Log.Warning("[WorkTabGroups] Circular or invalid anchor chain detected; appending remaining groups.");
                    ordered.AddRange(remaining);
                    break;
                }
            }

            return ordered;
        }

        public string CreateGroup(string label, string insertAfterAnchor)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "WorkTabGroups.Error.EmptyName".Translate();
            }

            if (!ValidateAnchor(insertAfterAnchor, null))
            {
                return "WorkTabGroups.Error.InvalidAnchor".Translate();
            }

            string defName = "MajorWorkGroup_" + nextGroupId++;
            var data = new MajorWorkGroupData(defName, label.Trim(), insertAfterAnchor ?? string.Empty);
            groups.Add(data);
            EnsureImpliedDefs(data);
            RequestColumnRebuild();
            return null;
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
            RequestColumnRebuild();
            return null;
        }

        public void DeleteGroup(string defName)
        {
            MajorWorkGroupData group = GetGroup(defName);
            if (group == null)
            {
                return;
            }

            groups.Remove(group);
            RemoveImpliedDefs(group);
            RebuildRuntimeState();
            RequestColumnRebuild();
        }

        public string SetAnchor(string defName, string insertAfterAnchor)
        {
            MajorWorkGroupData group = GetGroup(defName);
            if (group == null)
            {
                return "WorkTabGroups.Error.GroupNotFound".Translate();
            }

            if (!ValidateAnchor(insertAfterAnchor, group.defName))
            {
                return "WorkTabGroups.Error.InvalidAnchor".Translate();
            }

            group.insertAfterAnchor = insertAfterAnchor ?? string.Empty;
            RequestColumnRebuild();
            return null;
        }

        public void AssignWorkGiver(WorkGiverDef workGiver, string groupDefName)
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

            if (!group.assignedWorkGiverDefNames.Contains(workGiver.defName))
            {
                group.assignedWorkGiverDefNames.Add(workGiver.defName);
            }

            workGiverToGroup[workGiver] = group;
            RequestColumnRebuild();
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
                RequestColumnRebuild();
            }
        }

        public void ClearAllGroups()
        {
            foreach (MajorWorkGroupData group in groups.ToList())
            {
                RemoveImpliedDefs(group);
            }

            groups.Clear();
            RebuildRuntimeState();
            RequestColumnRebuild();
        }

        public void ReplaceGroupsFromPreset(IEnumerable<MajorWorkGroupData> newGroups)
        {
            foreach (MajorWorkGroupData group in groups.ToList())
            {
                RemoveImpliedDefs(group);
            }

            groups.Clear();
            foreach (MajorWorkGroupData g in newGroups)
            {
                groups.Add(g);
                EnsureImpliedDefs(g);
            }

            SyncNextGroupId();
            RebuildRuntimeState();
            RequestColumnRebuild();
        }

        public LayoutPreset CaptureLayoutPreset(string presetName)
        {
            var preset = new LayoutPreset { presetName = presetName };
            var idMap = new Dictionary<string, string>();

            foreach (MajorWorkGroupData g in groups)
            {
                string presetId = "g" + preset.groups.Count;
                idMap[g.defName] = presetId;

                string anchor = g.insertAfterAnchor;
                if (AnchorKeys.TryParseGroup(anchor, out string groupKey) && idMap.TryGetValue(groupKey, out string mapped))
                {
                    anchor = AnchorKeys.ForPresetGroup(mapped);
                }

                preset.groups.Add(new LayoutGroupEntry
                {
                    groupLabel = g.label,
                    presetGroupId = presetId,
                    insertAfterAnchor = anchor ?? string.Empty,
                    assignedWorkGiverDefNames = new List<string>(g.assignedWorkGiverDefNames)
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

        private bool CanPlaceGroup(MajorWorkGroupData group, HashSet<string> placed)
        {
            if (AnchorKeys.IsStart(group.insertAfterAnchor))
            {
                return true;
            }

            if (AnchorKeys.TryParseWorkType(group.insertAfterAnchor, out _))
            {
                return true;
            }

            if (AnchorKeys.TryParseGroup(group.insertAfterAnchor, out string groupKey))
            {
                return placed.Contains(groupKey);
            }

            return true;
        }

        private bool ValidateAnchor(string anchor, string selfDefName)
        {
            if (AnchorKeys.IsStart(anchor))
            {
                return true;
            }

            if (AnchorKeys.TryParseWorkType(anchor, out string wtName))
            {
                return DefDatabase<WorkTypeDef>.GetNamedSilentFail(wtName) != null;
            }

            if (AnchorKeys.TryParseGroup(anchor, out string groupKey))
            {
                if (!string.IsNullOrEmpty(selfDefName) && groupKey == selfDefName)
                {
                    return false;
                }

                if (GetGroup(groupKey) == null)
                {
                    return false;
                }

                return !WouldCreateCycle(selfDefName, anchor);
            }

            return false;
        }

        private bool WouldCreateCycle(string selfDefName, string anchor)
        {
            if (string.IsNullOrEmpty(selfDefName))
            {
                return false;
            }

            var visited = new HashSet<string> { selfDefName };
            string current = anchor;

            while (!AnchorKeys.IsStart(current) && AnchorKeys.TryParseGroup(current, out string groupKey))
            {
                if (!visited.Add(groupKey))
                {
                    return true;
                }

                MajorWorkGroupData g = GetGroup(groupKey);
                if (g == null)
                {
                    break;
                }

                current = g.insertAfterAnchor;
            }

            return false;
        }

        private void EnsureImpliedDefs(MajorWorkGroupData data)
        {
            string groupDefName = "WorkTabGroups_MajorWorkGroupDef_" + data.defName;
            string colDefName = "WorkTabGroups_MajorWorkGroupCol_" + data.defName;

            MajorWorkGroupDef groupDef = DefDatabase<MajorWorkGroupDef>.GetNamedSilentFail(groupDefName);
            if (groupDef == null)
            {
                groupDef = new MajorWorkGroupDef
                {
                    defName = groupDefName,
                    data = data
                };
                DefGenerator.AddImpliedDef(groupDef, false);
            }
            else
            {
                groupDef.data = data;
            }

            PawnColumnDef colDef = DefDatabase<PawnColumnDef>.GetNamedSilentFail(colDefName);
            if (colDef == null)
            {
                colDef = new PawnColumnDef_MajorWorkGroup
                {
                    defName = colDefName,
                    majorWorkGroup = groupDef,
                    workerClass = typeof(PawnColumnWorker_MajorWorkGroup),
                    sortable = false,
                    modContentPack = WorkTabGroupsMod.Instance?.Content
                };
                DefGenerator.AddImpliedDef(colDef, false);
            }
            else if (colDef is PawnColumnDef_MajorWorkGroup majorCol)
            {
                majorCol.majorWorkGroup = groupDef;
            }

            groupDefByName[data.defName] = groupDef;
            columnDefByGroupName[data.defName] = colDef;
        }

        private void RemoveImpliedDefs(MajorWorkGroupData data)
        {
            groupDefByName.Remove(data.defName);
            columnDefByGroupName.Remove(data.defName);
        }

        private void SyncNextGroupId()
        {
            int max = nextGroupId;
            foreach (MajorWorkGroupData group in groups)
            {
                if (group.defName != null && group.defName.StartsWith("MajorWorkGroup_") &&
                    int.TryParse(group.defName.Substring("MajorWorkGroup_".Length), out int id))
                {
                    max = Math.Max(max, id + 1);
                }
            }

            nextGroupId = max;
        }

        private void TryApplyDefaultLayoutOnNewGame()
        {
            if (groups.Count > 0)
            {
                return;
            }

            WorkTabGroupsSettings settings = WorkTabGroupsMod.Settings;
            if (settings == null || settings.defaultLayoutPresetName.NullOrEmpty())
            {
                return;
            }

            LayoutPreset preset = settings.FindLayoutPreset(settings.defaultLayoutPresetName);
            if (preset != null)
            {
                PresetApplier.ApplyLayout(preset, this, skipConfirm: true);
            }
        }

        public static void RequestColumnRebuild()
        {
            DefGenerator_GenerateImpliedDefs_PreResolve.ReBuildWorkTabColumns();
        }
    }
}
