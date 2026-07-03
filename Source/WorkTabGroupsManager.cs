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
        private List<WorkLayoutEntry> workLayoutOrder = new List<WorkLayoutEntry>();
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

        public IReadOnlyList<WorkLayoutEntry> WorkLayoutOrder => workLayoutOrder;

        internal List<WorkLayoutEntry> WorkLayoutOrderMutable => workLayoutOrder;

        internal List<MajorWorkGroupData> GroupsMutable => groups;

        internal int NextGroupId => nextGroupId;

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
            Scribe_Collections.Look(ref workLayoutOrder, "workLayoutOrder", LookMode.Deep);
            Scribe_Values.Look(ref nextGroupId, "nextGroupId", 0);

            if (groups == null)
            {
                groups = new List<MajorWorkGroupData>();
            }

            if (workLayoutOrder == null)
            {
                workLayoutOrder = new List<WorkLayoutEntry>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                SyncNextGroupId();
                EnsureWorkLayoutOrder();
                RebuildRuntimeState();
                LongEventHandler.ExecuteWhenFinished(RequestColumnRebuild);
            }
        }

        public void PersistLayoutState()
        {
            EnsureRegistered();
            LayoutSanitizer.PruneInvalidReferences(this);
            EnsureWorkLayoutOrder();
        }

        public void RebuildRuntimeState()
        {
            LayoutSanitizer.PruneInvalidReferences(this);
            groupDefByName.Clear();
            columnDefByGroupName.Clear();
            workGiverToGroup.Clear();
            EnsureWorkLayoutOrder();

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
            if (workGiver == null)
            {
                return null;
            }

            string colName = "WorkPriority_WorkGiver_" + workGiver.defName;
            return DefDatabase<PawnColumnDef>.GetNamedSilentFail(colName);
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
                LayoutOrderUtility.SyncWorkTypesInLayoutOrder(workLayoutOrder, nativeOrder, this);
            }
        }

        internal void WriteToSidecarData(WorkTabGroupsSidecarData data)
        {
            EnsureWorkLayoutOrder();
            data.groups = new List<MajorWorkGroupData>();
            foreach (MajorWorkGroupData group in groups)
            {
                var copy = new MajorWorkGroupData(group.defName, group.label, string.Empty)
                {
                    expanded = group.expanded
                };
                copy.assignedWorkGiverDefNames.AddRange(group.assignedWorkGiverDefNames);
                data.groups.Add(copy);
            }

            data.workLayoutOrder = new List<WorkLayoutEntry>();
            foreach (WorkLayoutEntry entry in workLayoutOrder)
            {
                data.workLayoutOrder.Add(new WorkLayoutEntry(entry.kind, entry.key));
            }

            data.nextGroupId = nextGroupId;
        }

        internal void ApplyPersistedState(
            List<MajorWorkGroupData> loadedGroups,
            List<WorkLayoutEntry> loadedLayoutOrder,
            int loadedNextGroupId)
        {
            ReplaceGroupsFromPreset(loadedGroups, loadedLayoutOrder);
            nextGroupId = loadedNextGroupId;
            SyncNextGroupId();
        }
        public void PrepareForModRemoval()
        {
            LayoutSanitizer.PruneInvalidReferences(this);

            foreach (WorkGiverDef workGiver in workGiverToGroup.Keys.ToList())
            {
                UnassignWorkGiver(workGiver);
            }

            ClearAllGroups();
            WorkTabGroupsSidecarStorage.DeleteForCurrentSave();

            if (Current.Game?.components != null)
            {
                Current.Game.components.RemoveAll(c => c is WorkTabGroupsManager);
                ClearInstance();
                Messages.Message("WorkTabGroups.PreparedForModRemoval".Translate(), MessageTypeDefOf.PositiveEvent, false);
            }
        }

        public string CreateGroup(string label, int layoutIndex = -1)
        {
            EnsureRegistered();
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
            EnsureImpliedDefs(data);

            if (layoutIndex < 0 || layoutIndex > workLayoutOrder.Count)
            {
                layoutIndex = workLayoutOrder.Count;
            }

            workLayoutOrder.Insert(layoutIndex, WorkLayoutEntry.ForCustomGroup(defName));
            PersistLayoutState();
            RequestColumnRebuild();
            return null;
        }

        public string CreateGroup(string label, string insertAfterAnchor)
        {
            int layoutIndex = ResolveLayoutIndexFromAnchor(insertAfterAnchor);
            return CreateGroup(label, layoutIndex);
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
            PersistLayoutState();
            RequestColumnRelayout();
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
            PersistLayoutState();
            RequestColumnRelayout();
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
            PersistLayoutState();
            RequestColumnRelayout();
        }

        public int ResolveLayoutIndexFromAnchor(string insertAfterAnchor)
        {
            EnsureWorkLayoutOrder();
            string anchor = insertAfterAnchor ?? string.Empty;
            if (AnchorKeys.IsStart(anchor))
            {
                return 0;
            }

            if (AnchorKeys.TryParseWorkType(anchor, out string workTypeName))
            {
                for (int i = 0; i < workLayoutOrder.Count; i++)
                {
                    if (workLayoutOrder[i].kind == WorkLayoutEntryKind.WorkType &&
                        workLayoutOrder[i].key == workTypeName)
                    {
                        return i + 1;
                    }
                }
            }

            return workLayoutOrder.Count;
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
            PersistLayoutState();
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
            workLayoutOrder.RemoveAll(e =>
                e.kind == WorkLayoutEntryKind.CustomGroup && e.key == defName);
            RemoveImpliedDefs(group);
            RebuildRuntimeState();
            PersistLayoutState();
            RequestColumnRebuild();
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

                PersistLayoutState();
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
            workLayoutOrder = LayoutOrderUtility.BuildDefaultLayoutOrder();
            RebuildRuntimeState();
            RequestColumnRebuild();
        }

        public void ReplaceGroupsFromPreset(IEnumerable<MajorWorkGroupData> newGroups, List<WorkLayoutEntry> newLayoutOrder = null)
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

            if (newLayoutOrder != null && newLayoutOrder.Count > 0)
            {
                workLayoutOrder = new List<WorkLayoutEntry>(newLayoutOrder);
            }
            else if (groups.Count > 0)
            {
                workLayoutOrder = LayoutOrderMigration.MigrateFromAnchors(groups);
            }
            else
            {
                workLayoutOrder = LayoutOrderUtility.BuildDefaultLayoutOrder();
            }

            SyncNextGroupId();
            EnsureWorkLayoutOrder();
            RebuildRuntimeState();
            PersistLayoutState();
            RequestColumnRebuild();
        }

        public LayoutPreset CaptureLayoutPreset(string presetName)
        {
            EnsureWorkLayoutOrder();
            var preset = new LayoutPreset { presetName = presetName };

            foreach (WorkLayoutEntry entry in workLayoutOrder)
            {
                preset.layoutOrder.Add(new WorkLayoutEntry(entry.kind, entry.key));
            }

            foreach (MajorWorkGroupData g in groups)
            {
                preset.groups.Add(new LayoutGroupEntry
                {
                    groupLabel = g.label,
                    presetGroupId = g.defName,
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

        public void CommitLayoutDraft(LayoutEditorDraft draft)
        {
            if (draft == null)
            {
                return;
            }

            List<MajorWorkGroupData> clonedGroups = draft.CloneGroupsForCommit();
            List<WorkLayoutEntry> clonedOrder = draft.CloneWorkLayoutOrderForCommit();
            ReplaceGroupsFromPreset(clonedGroups, clonedOrder);
            nextGroupId = Math.Max(nextGroupId, draft.NextGroupId);
            SyncNextGroupId();
            PersistLayoutState();

            if (Prefs.DevMode)
            {
                Log.Message($"[WorkTabGroups] Applied layout draft ({groups.Count} groups).");
            }
        }

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
