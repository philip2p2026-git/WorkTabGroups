using System;
using System.Collections.Generic;
using System.IO;
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

        public static void ClearInstance()
        {
            instance = null;
        }

        public static WorkTabGroupsManager EnsureRegistered()
        {
            if (Current.Game == null)
            {
                return null;
            }

            WorkTabGroupsManager manager = Current.Game.GetComponent<WorkTabGroupsManager>();
            if (manager == null)
            {
                manager = new WorkTabGroupsManager(Current.Game);
                Current.Game.components.Add(manager);
            }

            instance = manager;
            return manager;
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
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
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
                LongEventHandler.ExecuteWhenFinished(RequestColumnRebuild);
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

        public IEnumerable<KeyValuePair<WorkGiverDef, MajorWorkGroupData>> GetAssignedWorkGiverAssignments()
        {
            foreach (KeyValuePair<WorkGiverDef, MajorWorkGroupData> assignment in workGiverToGroup)
            {
                yield return assignment;
            }
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
            // Groups share WorkType-only anchors; list order breaks ties for the same anchor.
            return new List<MajorWorkGroupData>(groups);
        }

        public void PrepareForModRemoval()
        {
            // #region agent log
            AgentLog("H1", "WorkTabGroupsManager.PrepareForModRemoval:entry", "PrepareForModRemoval called", new Dictionary<string, object>
            {
                { "groupCount", groups.Count },
                { "hasGame", Current.Game != null },
                { "componentCount", Current.Game?.components?.Count ?? -1 }
            });
            // #endregion

            ClearAllGroups();

            if (Current.Game?.components != null)
            {
                int removed = Current.Game.components.RemoveAll(c => c is WorkTabGroupsManager);
                ClearInstance();

                // #region agent log
                AgentLog("H1", "WorkTabGroupsManager.PrepareForModRemoval:exit", "Component removed from save", new Dictionary<string, object>
                {
                    { "removedCount", removed },
                    { "remainingComponents", Current.Game.components.Count }
                });
                // #endregion

                Messages.Message("WorkTabGroups.PreparedForModRemoval".Translate(), MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public string CreateGroup(string label, string insertAfterAnchor)
        {
            EnsureRegistered();

            if (string.IsNullOrWhiteSpace(label))
            {
                return "WorkTabGroups.Error.EmptyName".Translate();
            }

            if (!ValidateAnchor(insertAfterAnchor, null))
            {
                return "WorkTabGroups.Error.InvalidAnchor".Translate();
            }

            string defName = "MajorWorkGroup_" + nextGroupId++;
            var data = new MajorWorkGroupData(defName, label.Trim(), insertAfterAnchor ?? string.Empty)
            {
                expanded = true
            };
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
            RequestColumnRelayout();
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
            RequestColumnRelayout();
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

            group.expanded = true;
            workGiverToGroup[workGiver] = group;
            RequestColumnRelayout();
        }

        public bool ReorderWorkGiverInGroup(MajorWorkGroupData group, WorkGiverDef workGiver, int direction)
        {
            if (group == null || workGiver == null || direction == 0)
            {
                return false;
            }

            List<string> order = group.assignedWorkGiverDefNames;
            int index = order.IndexOf(workGiver.defName);
            if (index < 0)
            {
                return false;
            }

            int targetIndex = index + direction;
            if (targetIndex < 0 || targetIndex >= order.Count)
            {
                return false;
            }

            string moving = order[index];
            order.RemoveAt(index);
            order.Insert(targetIndex, moving);
            RequestColumnRelayout();
            return true;
        }

        public bool ReorderNativeWorkGiver(WorkTypeDef workType, WorkGiverDef workGiver, int direction)
        {
            if (workType == null || workGiver == null || direction == 0 || IsAssignedToCustomGroup(workGiver))
            {
                return false;
            }

            List<WorkGiverDef> siblings = WorkTabGroupsColumnOrderUtility.GetNativeUnassignedWorkGivers(workType, this);
            int index = siblings.IndexOf(workGiver);
            if (index < 0)
            {
                return false;
            }

            int targetIndex = index + direction;
            if (targetIndex < 0 || targetIndex >= siblings.Count)
            {
                return false;
            }

            WorkGiverDef neighbor = siblings[targetIndex];
            WorkTabGroupsColumnOrderUtility.SwapPriorityInType(workGiver, neighbor);
            RequestColumnRelayout();
            return true;
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

                PawnColumnDef col = GetWorkGiverColumn(workGiver);
                if (col?.Worker is PawnColumnWorker_WorkGiver wgWorker)
                {
                    wgWorker.ColumnWorkerWorkType = null;
                    wgWorker.InvalidateCache();
                }

                if (group.assignedWorkGiverDefNames.Count == 0)
                {
                    group.expanded = false;
                }

                RequestColumnRelayout();
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

            foreach (MajorWorkGroupData g in groups)
            {
                preset.groups.Add(new LayoutGroupEntry
                {
                    groupLabel = g.label,
                    presetGroupId = "g" + preset.groups.Count,
                    insertAfterAnchor = g.insertAfterAnchor ?? string.Empty,
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

            // Legacy Group: anchors from older saves are rejected; pick a WorkType anchor instead.
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

        public void TryApplyDefaultLayoutOnNewGame()
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

        // #region agent log
        private static void AgentLog(string hypothesisId, string location, string message, Dictionary<string, object> data)
        {
            try
            {
                string rootDir = WorkTabGroupsMod.Instance?.Content?.RootDir;
                if (rootDir.NullOrEmpty())
                {
                    return;
                }

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string dataJson = data == null || data.Count == 0
                    ? "{}"
                    : "{" + string.Join(",", data.Select(kv => $"\"{kv.Key}\":\"{kv.Value}\"")) + "}";
                string line =
                    $"{{\"sessionId\":\"9055a0\",\"hypothesisId\":\"{hypothesisId}\",\"location\":\"{location}\",\"message\":\"{message}\",\"data\":{dataJson},\"timestamp\":{timestamp}}}\n";
                File.AppendAllText(Path.Combine(rootDir, "debug-9055a0.log"), line);
            }
            catch
            {
                // ignore logging failures
            }
        }
        // #endregion

        public static void RequestColumnRebuild()
        {
            DefGenerator_GenerateImpliedDefs_PreResolve.ReBuildWorkTabColumns();
        }

        public static void RequestColumnRelayout()
        {
            WorkTabGroupsColumnBuilder.RelayoutColumns();
        }
    }
}
