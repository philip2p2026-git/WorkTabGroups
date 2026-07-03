using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Window_WorkLayoutEditor : Window
    {
        private const float RowHeight = 28f;
        private const float Indent = 20f;
        private const float DragHandleWidth = 18f;
        private const float ToolbarHeight = 36f;
        private const float DropLineHeight = 4f;

        private LayoutEditorDraft draft;
        private Vector2 scrollPosition;
        private string selectedGroupDefName;
        private readonly HashSet<string> expandedWorkTypes = new HashSet<string>();
        private int dropLayoutIndex = -1;
        private float dropInsertionY = -1f;
        private string dropGroupDefName;
        private int dropWorkGiverIndex = -1;

        public override Vector2 InitialSize => new Vector2(520f, 640f);

        public Window_WorkLayoutEditor()
        {
            doCloseButton = false;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = false;
            draggable = true;
            resizeable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            draft = LayoutEditorDraft.FromDisplayedColumns()
                    ?? LayoutEditorDraft.FromManager(WorkTabGroupsManager.EnsureRegistered());
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (draft == null)
            {
                Widgets.Label(inRect, "WorkTabGroups.LayoutEditor.NoGame".Translate());
                return;
            }

            draft.EnsureWorkLayoutOrder();
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "WorkTabGroups.LayoutEditor.Title".Translate());
            Text.Font = GameFont.Small;

            Rect toolbarRect = new Rect(0f, 34f, inRect.width, ToolbarHeight);
            DrawToolbar(toolbarRect);

            Rect listRect = new Rect(0f, 34f + ToolbarHeight + 6f, inRect.width, inRect.height - 40f - ToolbarHeight);
            DrawLayoutList(listRect);

            if (LayoutDragDropState.IsDragging)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    Event.current.Use();
                }

                if (Event.current.rawType == EventType.MouseUp && Event.current.button == 0)
                {
                    TryCompleteDrag();
                }
            }
        }

        private void DrawToolbar(Rect rect)
        {
            float x = rect.x;
            float y = rect.y;
            float buttonWidth = 90f;

            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.LayoutEditor.AddGroup".Translate()))
            {
                Find.WindowStack.Add(new Dialog_AddMajorWorkGroup(draft, GetInsertIndexForNewGroup()));
            }

            x += buttonWidth + 4f;
            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.Save".Translate()))
            {
                Find.WindowStack.Add(new Dialog_SaveLayoutPreset(draft));
            }

            x += buttonWidth + 4f;
            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.LayoutEditor.LoadPreset".Translate()))
            {
                OpenLoadPresetMenu();
            }

            Rect applyRect = new Rect(rect.xMax - buttonWidth, y, buttonWidth, 28f);
            if (Widgets.ButtonText(applyRect, "WorkTabGroups.Apply".Translate()))
            {
                ApplyDraft();
            }

            TooltipHandler.TipRegion(applyRect, "WorkTabGroups.LayoutEditor.ApplyTip".Translate());
        }

        private void ApplyDraft()
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.EnsureRegistered();
            if (manager == null || draft == null)
            {
                return;
            }

            manager.CommitLayoutDraft(draft);
            draft = LayoutEditorDraft.FromManager(manager) ?? draft;
        }

        private static int GetInsertIndexForNewGroup()
        {
            return 0;
        }

        private void OpenLoadPresetMenu()
        {
            WorkTabGroupsSettings settings = WorkTabGroupsMod.Settings;
            if (settings == null)
            {
                return;
            }

            var options = new List<FloatMenuOption>();
            foreach (LayoutPreset preset in settings.layoutPresets)
            {
                LayoutPreset captured = preset;
                options.Add(new FloatMenuOption(captured.presetName, () => draft.ReplaceFromPreset(captured)));
            }

            if (options.Count == 0)
            {
                Messages.Message("WorkTabGroups.LayoutEditor.NoPresets".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawLayoutList(Rect rect)
        {
            float contentHeight = 0f;
            IReadOnlyList<WorkLayoutEntry> layoutOrder = draft.WorkLayoutOrder;
            for (int i = 0; i < layoutOrder.Count; i++)
            {
                contentHeight += RowHeight;
                WorkLayoutEntry entry = layoutOrder[i];
                if (entry.kind == WorkLayoutEntryKind.WorkType && expandedWorkTypes.Contains(entry.key))
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key);
                    contentHeight += LayoutOrderUtility.GetUnassignedWorkGivers(
                        workType, draft.IsAssignedToCustomGroup).Count * RowHeight;
                }
                else if (entry.kind == WorkLayoutEntryKind.CustomGroup)
                {
                    MajorWorkGroupData group = draft.GetGroup(entry.key);
                    if (group?.expanded == true)
                    {
                        contentHeight += group.assignedWorkGiverDefNames.Count * RowHeight;
                    }
                }
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, contentHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float y = 0f;
            dropLayoutIndex = -1;
            dropInsertionY = -1f;
            dropGroupDefName = null;
            dropWorkGiverIndex = -1;

            for (int i = 0; i < layoutOrder.Count; i++)
            {
                WorkLayoutEntry entry = layoutOrder[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);

                if (entry.kind == WorkLayoutEntryKind.WorkType)
                {
                    DrawWorkTypeRow(rowRect, entry.key);
                    y += RowHeight;
                    if (expandedWorkTypes.Contains(entry.key))
                    {
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key);
                        foreach (WorkGiverDef wg in LayoutOrderUtility.GetUnassignedWorkGivers(
                                     workType, draft.IsAssignedToCustomGroup))
                        {
                            Rect wgRect = new Rect(Indent, y, viewRect.width - Indent, RowHeight);
                            DrawWorkGiverRow(wgRect, wg, null, -1);
                            y += RowHeight;
                        }
                    }
                }
                else
                {
                    MajorWorkGroupData group = draft.GetGroup(entry.key);
                    if (group == null)
                    {
                        continue;
                    }

                    DrawCustomGroupRow(rowRect, group, i);
                    y += RowHeight;

                    if (group.expanded)
                    {
                        for (int wgIndex = 0; wgIndex < group.assignedWorkGiverDefNames.Count; wgIndex++)
                        {
                            WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(group.assignedWorkGiverDefNames[wgIndex]);
                            if (wg == null)
                            {
                                continue;
                            }

                            Rect wgRect = new Rect(Indent, y, viewRect.width - Indent, RowHeight);
                            DrawWorkGiverRow(wgRect, wg, group.defName, wgIndex);
                            y += RowHeight;
                        }
                    }
                }

                if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.CustomGroup && Mouse.IsOver(rowRect))
                {
                    dropLayoutIndex = i + 1;
                    dropInsertionY = rowRect.yMax;
                }
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.CustomGroup &&
                dropInsertionY >= 0f &&
                dropLayoutIndex >= 0 &&
                dropLayoutIndex != LayoutDragDropState.SourceLayoutIndex &&
                dropLayoutIndex != LayoutDragDropState.SourceLayoutIndex + 1)
            {
                LayoutDragDropState.DrawDropLine(
                    new Rect(0f, dropInsertionY - DropLineHeight * 0.5f, viewRect.width, DropLineHeight));
            }

            Widgets.EndScrollView();

            if (LayoutDragDropState.IsDragging)
            {
                LayoutDragDropState.DrawActiveDragVisual();
            }
        }

        private void DrawWorkTypeRow(Rect rect, string workTypeDefName)
        {
            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
            if (workType == null)
            {
                return;
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver &&
                !LayoutDragDropState.SourceGroupDefName.NullOrEmpty() &&
                Mouse.IsOver(rect))
            {
                LayoutDragDropState.DrawDropTargetHighlight(rect, unassignZone: true);
            }

            bool expanded = expandedWorkTypes.Contains(workTypeDefName);
            if (Widgets.ButtonInvisible(rect))
            {
                if (expanded)
                {
                    expandedWorkTypes.Remove(workTypeDefName);
                }
                else
                {
                    expandedWorkTypes.Add(workTypeDefName);
                }
            }

            Widgets.DrawHighlightIfMouseover(rect);
            string arrow = expanded ? "▼" : "▶";
            Widgets.Label(rect, "  " + arrow + " " + workType.labelShort);
            TooltipHandler.TipRegion(rect, "WorkTabGroups.LayoutEditor.WorkTypeTip".Translate());
        }

        private void DrawCustomGroupRow(Rect rect, MajorWorkGroupData group, int layoutIndex)
        {
            Rect dragRect = new Rect(rect.x, rect.y, DragHandleWidth, rect.height);
            Rect bodyRect = new Rect(rect.x + DragHandleWidth, rect.y, rect.width - DragHandleWidth, rect.height);
            bool isSource = LayoutDragDropState.IsSourceCustomGroup(layoutIndex);

            Widgets.Label(dragRect, "≡");
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(dragRect))
            {
                LayoutDragDropState.BeginCustomGroupDrag(layoutIndex, group.label);
                selectedGroupDefName = group.defName;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(bodyRect))
            {
                OpenCustomGroupContextMenu(group);
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                group.expanded = !group.expanded;
                selectedGroupDefName = group.defName;
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver && Mouse.IsOver(bodyRect))
            {
                dropGroupDefName = group.defName;
                dropWorkGiverIndex = group.assignedWorkGiverDefNames.Count;
                LayoutDragDropState.DrawDropTargetHighlight(bodyRect);
            }

            if (selectedGroupDefName == group.defName)
            {
                Widgets.DrawHighlight(bodyRect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(bodyRect);
            }

            Color prevColor = GUI.color;
            if (isSource && LayoutDragDropState.IsDragging)
            {
                GUI.color = LayoutDragDropState.DimmedContentColor();
            }

            string arrow = group.expanded ? "▼" : "▶";
            Widgets.Label(bodyRect, "  " + arrow + " " + group.label + " (" + "WorkTabGroups.LayoutEditor.CustomGroup".Translate() + ")");
            GUI.color = prevColor;
            TooltipHandler.TipRegion(bodyRect, "WorkTabGroups.LayoutEditor.CustomGroupTip".Translate());
        }

        private void DrawWorkGiverRow(
            Rect rect,
            WorkGiverDef workGiver,
            string groupDefName,
            int workGiverIndex)
        {
            Rect dragRect = new Rect(rect.x, rect.y, DragHandleWidth, rect.height);
            Rect bodyRect = new Rect(rect.x + DragHandleWidth, rect.y, rect.width - DragHandleWidth, rect.height);
            bool isSource = LayoutDragDropState.IsSourceWorkGiver(workGiver.defName, groupDefName, workGiverIndex);

            Widgets.Label(dragRect, "≡");
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(dragRect))
            {
                LayoutDragDropState.BeginWorkGiverDrag(
                    workGiver.defName,
                    groupDefName,
                    workGiverIndex,
                    workGiver.LabelCap);
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(bodyRect))
            {
                OpenWorkGiverContextMenu(workGiver, groupDefName);
                Event.current.Use();
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver &&
                !groupDefName.NullOrEmpty() &&
                Mouse.IsOver(rect) &&
                !isSource)
            {
                dropGroupDefName = groupDefName;
                dropWorkGiverIndex = workGiverIndex;
                LayoutDragDropState.DrawDropLine(new Rect(rect.x, rect.y, rect.width, DropLineHeight));
            }

            Widgets.DrawHighlightIfMouseover(bodyRect);

            Color prevColor = GUI.color;
            if (isSource && LayoutDragDropState.IsDragging)
            {
                GUI.color = LayoutDragDropState.DimmedContentColor();
            }

            Widgets.Label(bodyRect, "    " + workGiver.LabelCap);
            GUI.color = prevColor;
            TooltipHandler.TipRegion(bodyRect, "WorkTabGroups.LayoutEditor.WorkGiverTip".Translate());
        }

        private void OpenCustomGroupContextMenu(MajorWorkGroupData group)
        {
            string defName = group.defName;
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("WorkTabGroups.RenameGroup".Translate(), () =>
                    Find.WindowStack.Add(new Dialog_RenameMajorWorkGroup(draft, defName))),
                new FloatMenuOption("WorkTabGroups.DeleteGroup".Translate(), () =>
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "WorkTabGroups.ConfirmDeleteGroup".Translate(group.label),
                        () =>
                        {
                            draft.DeleteGroup(defName);
                            if (selectedGroupDefName == defName)
                            {
                                selectedGroupDefName = null;
                            }
                        })))
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenWorkGiverContextMenu(WorkGiverDef workGiver, string currentGroupDefName)
        {
            var options = new List<FloatMenuOption>();
            List<MajorWorkGroupData> groups = GetCustomGroupsInLayout();

            if (groups.Count == 0)
            {
                options.Add(new FloatMenuOption("WorkTabGroups.LayoutEditor.NoCustomGroups".Translate(), null));
            }
            else
            {
                foreach (MajorWorkGroupData group in groups)
                {
                    if (group.defName == currentGroupDefName)
                    {
                        continue;
                    }

                    MajorWorkGroupData captured = group;
                    options.Add(new FloatMenuOption(
                        "WorkTabGroups.LayoutEditor.AssignToGroup".Translate(captured.label),
                        () => draft.AssignWorkGiver(workGiver, captured.defName)));
                }
            }

            if (!currentGroupDefName.NullOrEmpty())
            {
                options.Add(new FloatMenuOption(
                    "WorkTabGroups.LayoutEditor.RemoveFromGroup".Translate(),
                    () => draft.UnassignWorkGiver(workGiver)));
            }

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private List<MajorWorkGroupData> GetCustomGroupsInLayout()
        {
            var groups = new List<MajorWorkGroupData>();
            foreach (WorkLayoutEntry entry in draft.WorkLayoutOrder)
            {
                if (entry.kind != WorkLayoutEntryKind.CustomGroup)
                {
                    continue;
                }

                MajorWorkGroupData group = draft.GetGroup(entry.key);
                if (group != null)
                {
                    groups.Add(group);
                }
            }

            return groups;
        }

        private void TryCompleteDrag()
        {
            if (!LayoutDragDropState.IsDragging)
            {
                return;
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.CustomGroup)
            {
                if (dropLayoutIndex >= 0 && dropLayoutIndex != LayoutDragDropState.SourceLayoutIndex &&
                    dropLayoutIndex != LayoutDragDropState.SourceLayoutIndex + 1)
                {
                    draft.MoveLayoutEntry(LayoutDragDropState.SourceLayoutIndex, dropLayoutIndex);
                }
            }
            else if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver)
            {
                WorkGiverDef workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(LayoutDragDropState.WorkGiverDefName);
                if (workGiver != null)
                {
                    if (!dropGroupDefName.NullOrEmpty())
                    {
                        if (LayoutDragDropState.SourceGroupDefName == dropGroupDefName &&
                            LayoutDragDropState.SourceWorkGiverIndex >= 0 &&
                            dropWorkGiverIndex >= 0)
                        {
                            MajorWorkGroupData group = draft.GetGroup(dropGroupDefName);
                            if (group != null)
                            {
                                draft.MoveWorkGiverWithinGroup(
                                    group,
                                    LayoutDragDropState.SourceWorkGiverIndex,
                                    dropWorkGiverIndex);
                            }
                        }
                        else
                        {
                            draft.AssignWorkGiverAt(workGiver, dropGroupDefName, dropWorkGiverIndex);
                        }
                    }
                    else if (!LayoutDragDropState.SourceGroupDefName.NullOrEmpty())
                    {
                        draft.UnassignWorkGiver(workGiver);
                    }
                }
            }

            LayoutDragDropState.Clear();
            Event.current.Use();
        }
    }
}
