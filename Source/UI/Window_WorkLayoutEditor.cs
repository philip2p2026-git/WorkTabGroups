using System.Collections.Generic;
using System.Linq;
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
        private const float ToolbarHeight = 68f;

        private Vector2 scrollPosition;
        private string newGroupName = string.Empty;
        private string selectedGroupDefName;
        private readonly HashSet<string> expandedWorkTypes = new HashSet<string>();
        private int dropLayoutIndex = -1;
        private string dropGroupDefName;
        private int dropWorkGiverIndex = -1;

        public override Vector2 InitialSize => new Vector2(520f, 640f);

        public Window_WorkLayoutEditor()
        {
            doCloseButton = true;
            doCloseX = true;
            forcePause = false;
            absorbInputAroundWindow = false;
            draggable = true;
            resizeable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.EnsureRegistered();
            if (manager == null)
            {
                Widgets.Label(inRect, "WorkTabGroups.LayoutEditor.NoGame".Translate());
                return;
            }

            manager.EnsureWorkLayoutOrder();
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), "WorkTabGroups.LayoutEditor.Title".Translate());
            Text.Font = GameFont.Small;

            Rect toolbarRect = new Rect(0f, 34f, inRect.width, ToolbarHeight);
            DrawToolbar(toolbarRect, manager);

            Rect listRect = new Rect(0f, 34f + ToolbarHeight + 6f, inRect.width, inRect.height - 40f - ToolbarHeight);
            DrawLayoutList(listRect, manager);

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                TryCompleteDrag(manager);
            }
        }

        private void DrawToolbar(Rect rect, WorkTabGroupsManager manager)
        {
            float x = rect.x;
            float y = rect.y;
            float buttonWidth = 90f;

            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.LayoutEditor.AddGroup".Translate()))
            {
                TryAddGroup(manager);
            }

            x += buttonWidth + 4f;
            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.SaveLayoutPreset".Translate()))
            {
                Find.WindowStack.Add(new Dialog_SaveLayoutPreset());
            }

            x += buttonWidth + 4f;
            if (Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.LayoutEditor.LoadPreset".Translate()))
            {
                OpenLoadPresetMenu();
            }

            x += buttonWidth + 4f;
            if (!selectedGroupDefName.NullOrEmpty() &&
                Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.RenameGroup".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameMajorWorkGroup(selectedGroupDefName));
            }

            x += buttonWidth + 4f;
            if (!selectedGroupDefName.NullOrEmpty() &&
                Widgets.ButtonText(new Rect(x, y, buttonWidth, 28f), "WorkTabGroups.DeleteGroup".Translate()))
            {
                MajorWorkGroupData group = manager.GetGroup(selectedGroupDefName);
                if (group != null)
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "WorkTabGroups.ConfirmDeleteGroup".Translate(group.label),
                        () =>
                        {
                            manager.DeleteGroup(selectedGroupDefName);
                            selectedGroupDefName = null;
                        }));
                }
            }

            x += buttonWidth + 4f;
            if (!selectedGroupDefName.NullOrEmpty() &&
                Widgets.ButtonText(new Rect(x, y, buttonWidth + 20f, 28f), "WorkTabGroups.SaveAsGroupPreset".Translate()))
            {
                Find.WindowStack.Add(new Dialog_SaveGroupPreset(selectedGroupDefName));
            }

            float nameFieldY = y + 32f;
            Widgets.Label(new Rect(rect.x, nameFieldY, 90f, 24f), "WorkTabGroups.GroupName".Translate());
            newGroupName = Widgets.TextField(new Rect(rect.x + 92f, nameFieldY, rect.width - 92f, 24f), newGroupName);
        }

        private void TryAddGroup(WorkTabGroupsManager manager)
        {
            if (newGroupName.NullOrEmpty())
            {
                Messages.Message("WorkTabGroups.Error.EmptyName".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            int insertIndex = manager.WorkLayoutOrder.Count;
            if (!selectedGroupDefName.NullOrEmpty())
            {
                for (int i = 0; i < manager.WorkLayoutOrder.Count; i++)
                {
                    if (manager.WorkLayoutOrder[i].kind == WorkLayoutEntryKind.CustomGroup &&
                        manager.WorkLayoutOrder[i].key == selectedGroupDefName)
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
            }

            string error = manager.CreateGroup(newGroupName, insertIndex);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            newGroupName = string.Empty;
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
                options.Add(new FloatMenuOption(captured.presetName, () => PresetApplier.ApplyLayout(captured)));
            }

            if (options.Count == 0)
            {
                Messages.Message("WorkTabGroups.LayoutEditor.NoPresets".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawLayoutList(Rect rect, WorkTabGroupsManager manager)
        {
            float contentHeight = 0f;
            IReadOnlyList<WorkLayoutEntry> layoutOrder = manager.WorkLayoutOrder;
            for (int i = 0; i < layoutOrder.Count; i++)
            {
                contentHeight += RowHeight;
                WorkLayoutEntry entry = layoutOrder[i];
                if (entry.kind == WorkLayoutEntryKind.WorkType && expandedWorkTypes.Contains(entry.key))
                {
                    WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key);
                    contentHeight += LayoutOrderUtility.GetUnassignedWorkGivers(workType, manager).Count * RowHeight;
                }
                else if (entry.kind == WorkLayoutEntryKind.CustomGroup)
                {
                    MajorWorkGroupData group = manager.GetGroup(entry.key);
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
            dropGroupDefName = null;
            dropWorkGiverIndex = -1;

            for (int i = 0; i < layoutOrder.Count; i++)
            {
                WorkLayoutEntry entry = layoutOrder[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);

                if (entry.kind == WorkLayoutEntryKind.WorkType)
                {
                    DrawWorkTypeRow(rowRect, entry.key, manager);
                    y += RowHeight;
                    if (expandedWorkTypes.Contains(entry.key))
                    {
                        WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(entry.key);
                        foreach (WorkGiverDef wg in LayoutOrderUtility.GetUnassignedWorkGivers(workType, manager))
                        {
                            Rect wgRect = new Rect(Indent, y, viewRect.width - Indent, RowHeight);
                            DrawWorkGiverRow(wgRect, wg, null, -1, manager);
                            y += RowHeight;
                        }
                    }
                }
                else
                {
                    MajorWorkGroupData group = manager.GetGroup(entry.key);
                    if (group == null)
                    {
                        continue;
                    }

                    DrawCustomGroupRow(rowRect, group, i, manager);
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
                            DrawWorkGiverRow(wgRect, wg, group.defName, wgIndex, manager);
                            y += RowHeight;
                        }
                    }
                }

                if (LayoutDragDropState.IsDragging && Mouse.IsOver(rowRect))
                {
                    dropLayoutIndex = i + 1;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawWorkTypeRow(Rect rect, string workTypeDefName, WorkTabGroupsManager manager)
        {
            WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(workTypeDefName);
            if (workType == null)
            {
                return;
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

        private void DrawCustomGroupRow(Rect rect, MajorWorkGroupData group, int layoutIndex, WorkTabGroupsManager manager)
        {
            Rect dragRect = new Rect(rect.x, rect.y, DragHandleWidth, rect.height);
            Rect bodyRect = new Rect(rect.x + DragHandleWidth, rect.y, rect.width - DragHandleWidth, rect.height);

            Widgets.Label(dragRect, "≡");
            if (Widgets.ButtonInvisible(dragRect) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                LayoutDragDropState.BeginCustomGroupDrag(layoutIndex);
                selectedGroupDefName = group.defName;
                Event.current.Use();
            }

            if (Widgets.ButtonInvisible(bodyRect))
            {
                group.expanded = !group.expanded;
                selectedGroupDefName = group.defName;
            }

            if (selectedGroupDefName == group.defName)
            {
                Widgets.DrawHighlight(bodyRect);
            }
            else
            {
                Widgets.DrawHighlightIfMouseover(bodyRect);
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.CustomGroup &&
                dropLayoutIndex == layoutIndex + 1)
            {
                Widgets.DrawBox(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f));
            }

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver && Mouse.IsOver(bodyRect))
            {
                dropGroupDefName = group.defName;
                dropWorkGiverIndex = group.assignedWorkGiverDefNames.Count;
                Widgets.DrawHighlight(bodyRect);
            }

            string arrow = group.expanded ? "▼" : "▶";
            Widgets.Label(bodyRect, "  " + arrow + " " + group.label + " (" + "WorkTabGroups.LayoutEditor.CustomGroup".Translate() + ")");
            TooltipHandler.TipRegion(bodyRect, "WorkTabGroups.LayoutEditor.CustomGroupTip".Translate());
        }

        private void DrawWorkGiverRow(
            Rect rect,
            WorkGiverDef workGiver,
            string groupDefName,
            int workGiverIndex,
            WorkTabGroupsManager manager)
        {
            Rect dragRect = new Rect(rect.x, rect.y, DragHandleWidth, rect.height);
            Rect bodyRect = new Rect(rect.x + DragHandleWidth, rect.y, rect.width - DragHandleWidth, rect.height);

            Widgets.Label(dragRect, "≡");
            if (Widgets.ButtonInvisible(dragRect) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                LayoutDragDropState.BeginWorkGiverDrag(workGiver.defName, groupDefName, workGiverIndex);
                Event.current.Use();
            }

            Widgets.DrawHighlightIfMouseover(bodyRect);
            Widgets.Label(bodyRect, "    " + workGiver.LabelCap);

            if (LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver &&
                !groupDefName.NullOrEmpty() &&
                Mouse.IsOver(rect))
            {
                dropGroupDefName = groupDefName;
                dropWorkGiverIndex = workGiverIndex;
                Widgets.DrawBox(new Rect(rect.x, rect.y, rect.width, 2f));
            }

            if (LayoutDragDropState.IsDragging && LayoutDragDropState.Kind == LayoutDragDropState.DragKind.WorkGiver)
            {
                LayoutDragDropState.DrawGhost(rect);
            }
        }

        private void TryCompleteDrag(WorkTabGroupsManager manager)
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
                    manager.MoveLayoutEntry(LayoutDragDropState.SourceLayoutIndex, dropLayoutIndex);
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
                            MajorWorkGroupData group = manager.GetGroup(dropGroupDefName);
                            if (group != null)
                            {
                                manager.MoveWorkGiverWithinGroup(
                                    group,
                                    LayoutDragDropState.SourceWorkGiverIndex,
                                    dropWorkGiverIndex);
                            }
                        }
                        else
                        {
                            manager.AssignWorkGiverAt(workGiver, dropGroupDefName, dropWorkGiverIndex);
                        }
                    }
                    else if (!LayoutDragDropState.SourceGroupDefName.NullOrEmpty())
                    {
                        manager.UnassignWorkGiver(workGiver);
                    }
                }
            }

            LayoutDragDropState.Clear();
            Event.current.Use();
        }
    }
}
