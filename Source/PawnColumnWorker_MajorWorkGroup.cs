using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using WorkTab;

namespace WorkTabGroups
{
    public class PawnColumnWorker_MajorWorkGroup : AbstractPawnColumnWorker, IExpandableColumn
    {
        private bool expanded;
        private MajorWorkGroupData boundGroup;

        public bool CanExpand { get; set; }

        public bool NeedExpand { get; private set; }

        public bool NeedCollapse { get; private set; }

        public bool Expanded
        {
            get => expanded;
            set
            {
                if (expanded == value)
                {
                    return;
                }

                expanded = value;
                NeedExpand = false;
                NeedCollapse = false;
                if (boundGroup != null)
                {
                    boundGroup.expanded = value;
                }

                InvalidateAssignedWorkGiverCaches();
            }
        }

        public MajorWorkGroupData BoundGroup => boundGroup;

        public override bool VisibleCurrently => true;

        protected override Color DefaultHeaderColor => Color.white;

        protected override GameFont DefaultHeaderFont => GameFont.Small;

        protected override TextAnchor DefaultHeaderAlignment => TextAnchor.MiddleCenter;

        public void BindGroup(MajorWorkGroupData group)
        {
            boundGroup = group;
            expanded = group?.expanded ?? false;
        }

        public bool HeaderExpand()
        {
            if (!InteractionUtilities.Ctrl)
            {
                return false;
            }

            if (Expanded)
            {
                NeedCollapse = true;
            }
            else
            {
                if (!CanExpand)
                {
                    return false;
                }

                NeedExpand = true;
            }

            InvalidateCache();
            return true;
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (!ShouldDrawCell(pawn))
            {
                return;
            }

            Vector2 center = rect.center - new Vector2(25f, 25f) / 2f;
            Rect box = new Rect(center.x, center.y, 25f, 25f);
            HighlightCurrentJob(box, pawn);
            HandleInteractions(rect, pawn);
            DrawGroupBoxFor(box, pawn);
        }

        protected override bool ShouldDrawCell(Pawn pawn)
        {
            return base.ShouldDrawCell(pawn) && MajorWorkGroupPriorityUtility.AllowedToDo(pawn, boundGroup);
        }

        protected override bool IsDoCurrentJob(Pawn pawn)
        {
            return MajorWorkGroupPriorityUtility.IsDoingGroupJob(pawn, boundGroup);
        }

        private void DrawGroupBoxFor(Rect box, Pawn pawn)
        {
            WorkTypeDef workType = MajorWorkGroupPriorityUtility.RepresentativeWorkType(pawn, boundGroup);
            if (workType != null)
            {
                MajorWorkGroupDrawUtility.DrawWorkBoxBackground(box, pawn, workType);
            }

            if (MajorWorkGroupPriorityUtility.TimeScheduled(pawn, boundGroup))
            {
                DrawUtilities.DrawTimeScheduled(box);
            }

            if (MajorWorkGroupPriorityUtility.PartScheduled(pawn, boundGroup))
            {
                DrawUtilities.DrawPartScheduled(box);
            }

            int priority = MajorWorkGroupPriorityUtility.GetPriority(pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour);
            DrawUtilities.DrawPriority(box, priority, small: false);

            if (Mouse.IsOver(box))
            {
                TooltipHandler.TipRegion(box,
                    () => MajorWorkGroupPriorityUtility.TipForPawnGroup(pawn, boundGroup),
                    pawn.thingIDNumber ^ (boundGroup?.defName?.GetHashCode() ?? 0));
            }
        }

        protected override void HandleInteractionsDetailed(Rect rect, Pawn pawn)
        {
            if ((Event.current.type != EventType.MouseDown && Event.current.type != EventType.ScrollWheel) ||
                !Mouse.IsOver(rect))
            {
                return;
            }

            int priority = MajorWorkGroupPriorityUtility.GetPriority(pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour);
            if (InteractionUtilities.ScrolledUp(rect, stopPropagation: true) || InteractionUtilities.RightClicked(rect))
            {
                MajorWorkGroupPriorityUtility.IncrementPriority(
                    pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour, MainTabWindow_WorkTab.SelectedHours);
            }

            if (InteractionUtilities.ScrolledDown(rect, stopPropagation: true) || InteractionUtilities.LeftClicked(rect))
            {
                MajorWorkGroupPriorityUtility.DecrementPriority(
                    pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour, MainTabWindow_WorkTab.SelectedHours);
            }

            int priorityAfter = MajorWorkGroupPriorityUtility.GetPriority(pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour);
            if (priority == 0 && priorityAfter > 0 && HasLowRelevantSkill(pawn))
            {
                SoundDefOf.Crunch.PlayOneShotOnCamera();
            }

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, (KnowledgeAmount)5);
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.ManualWorkPriorities, (KnowledgeAmount)4);
        }

        protected override void HandleInteractionsToggle(Rect rect, Pawn pawn)
        {
            if ((Event.current.type != EventType.MouseDown &&
                 (Event.current.type != EventType.ScrollWheel || Settings.disableScrollwheel)) ||
                !Mouse.IsOver(rect))
            {
                return;
            }

            if (MajorWorkGroupPriorityUtility.GetPriority(pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour) > 0)
            {
                MajorWorkGroupPriorityUtility.SetPriority(pawn, boundGroup, 0, MainTabWindow_WorkTab.SelectedHours);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
            else
            {
                MajorWorkGroupPriorityUtility.SetPriority(
                    pawn, boundGroup, Settings.defaultPriority, MainTabWindow_WorkTab.SelectedHours);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                if (HasLowRelevantSkill(pawn))
                {
                    SoundDefOf.Crunch.PlayOneShotOnCamera();
                }

                TryWarnIdeoOpposed(pawn);
            }

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.WorkTab, (KnowledgeAmount)5);
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 32;
        }

        public override int GetOptimalHeaderWidth(PawnTable table)
        {
            return Mathf.Max(60, (boundGroup?.label ?? "Group").Length * 8);
        }

        public override int GetMinHeaderHeight(PawnTable table)
        {
            int fallback = base.GetMinHeaderHeight(table);
            return CompactWorkTabCompat.GetMinHeaderHeight(table, fallback);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            string label = boundGroup?.label;
            if (!label.NullOrEmpty())
            {
                Text.Font = DefaultHeaderFont;
                GUI.color = DefaultHeaderColor;
                Text.Anchor = DefaultHeaderAlignment;
                Rect labelRect = rect;
                labelRect.xMin += GetHeaderOffsetX(rect);
                Widgets.Label(labelRect, label.Truncate(rect.width));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            Rect interactableHeaderRect = GetInteractableHeaderRect(rect, table);
            if (Mouse.IsOver(interactableHeaderRect))
            {
                Widgets.DrawHighlight(interactableHeaderRect);
                string headerTip = GetHeaderTip(table);
                if (!headerTip.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(interactableHeaderRect, headerTip);
                }
            }

            if (Widgets.ButtonInvisible(interactableHeaderRect))
            {
                HeaderClicked(interactableHeaderRect, table);
            }

            HeaderInteractions(interactableHeaderRect, table);
        }

        public override void InvalidateCache()
        {
            base.InvalidateCache();
        }

        protected override string GetHeaderTip(PawnTable table)
        {
            string tip = boundGroup?.label ?? string.Empty;
            if (Expanded)
            {
                tip += "\n" + "WorkTabGroups.CollapseGroupTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }

            if (CanExpand && !Expanded)
            {
                tip += "\n" + "WorkTabGroups.ExpandGroupTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            }

            tip += "\n" + "WorkTabGroups.GroupHeaderRightClickTip".Translate().Colorize(ColoredText.SubtleGrayColor);
            return CreateHeaderTip(tip);
        }

        protected override void HeaderClicked(Rect headerRect, PawnTable table)
        {
            if (HeaderExpand())
            {
                MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
            }
        }

        protected override void HeaderInteractions(Rect headerRect, PawnTable table, bool clicked = false)
        {
            bool rightClick = clicked ? InteractionUtilities.RightClicked() : InteractionUtilities.RightClicked(headerRect);
            if (rightClick && !InteractionUtilities.Shift)
            {
                TryOpenGroupContextMenu(headerRect);
                return;
            }

            if (!Mouse.IsOver(headerRect) || !InteractionUtilities.Shift)
            {
                return;
            }

            List<Pawn> pawns = table.PawnsListForReading.Where(ShouldDrawCell).ToList();
            if (Find.PlaySettings.useWorkPriorities)
            {
                if (InteractionUtilities.ScrolledUp(headerRect, stopPropagation: true))
                {
                    MajorWorkGroupPriorityUtility.IncrementPriority(
                        boundGroup, pawns, MainTabWindow_WorkTab.VisibleHour, MainTabWindow_WorkTab.SelectedHours);
                }

                if (InteractionUtilities.ScrolledDown(headerRect, stopPropagation: true))
                {
                    MajorWorkGroupPriorityUtility.DecrementPriority(
                        boundGroup, pawns, MainTabWindow_WorkTab.VisibleHour, MainTabWindow_WorkTab.SelectedHours);
                }

                return;
            }

            if (InteractionUtilities.ScrolledUp(headerRect, stopPropagation: true) &&
                pawns.Any(p => MajorWorkGroupPriorityUtility.GetPriority(p, boundGroup, MainTabWindow_WorkTab.VisibleHour) != 0))
            {
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
                foreach (Pawn pawn in pawns)
                {
                    MajorWorkGroupPriorityUtility.SetPriority(pawn, boundGroup, 0, MainTabWindow_WorkTab.SelectedHours);
                }
            }

            if (InteractionUtilities.ScrolledDown(headerRect, stopPropagation: true) &&
                pawns.Any(p => MajorWorkGroupPriorityUtility.GetPriority(p, boundGroup, MainTabWindow_WorkTab.VisibleHour) == 0))
            {
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                foreach (Pawn pawn in pawns)
                {
                    MajorWorkGroupPriorityUtility.SetPriority(
                        pawn, boundGroup, Settings.defaultPriority, MainTabWindow_WorkTab.SelectedHours);
                }
            }
        }

        private void TryOpenGroupContextMenu(Rect headerRect)
        {
            if (boundGroup == null)
            {
                return;
            }

            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("WorkTabGroups.RenameGroup".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_RenameMajorWorkGroup(boundGroup.defName));
                }),
                new FloatMenuOption("WorkTabGroups.ChangePosition".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_PickAnchor(boundGroup.defName, isNewGroup: false));
                }),
                new FloatMenuOption("WorkTabGroups.SaveAsGroupPreset".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_SaveGroupPreset(boundGroup.defName));
                }),
                new FloatMenuOption("WorkTabGroups.DeleteGroup".Translate(), () =>
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "WorkTabGroups.ConfirmDeleteGroup".Translate(boundGroup.label),
                        () => WorkTabGroupsManager.Instance?.DeleteGroup(boundGroup.defName)));
                })
            };

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private bool HasLowRelevantSkill(Pawn pawn)
        {
            foreach (WorkGiverDef wg in MajorWorkGroupPriorityUtility.GetAssignedWorkGivers(boundGroup))
            {
                if (wg.workType.relevantSkills.Any() &&
                    pawn.skills.AverageOfRelevantSkillsFor(wg.workType) <= 2f)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryWarnIdeoOpposed(Pawn pawn)
        {
            WorkTypeDef opposed = MajorWorkGroupPriorityUtility.FirstIdeoOpposedWorkType(pawn, boundGroup);
            if (opposed == null)
            {
                return;
            }

            if (MajorWorkGroupPriorityUtility.GetPriority(pawn, boundGroup, MainTabWindow_WorkTab.VisibleHour) <= 0)
            {
                return;
            }

            Messages.Message(
                "MessageIdeoOpposedWorkTypeSelected".Translate(pawn.Named("PAWN"), opposed.gerundLabel),
                pawn,
                MessageTypeDefOf.CautionInput);
            SoundDefOf.DislikedWorkTypeActivated.PlayOneShotOnCamera();
        }

        private void InvalidateAssignedWorkGiverCaches()
        {
            if (boundGroup == null)
            {
                return;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return;
            }

            foreach (string wgName in boundGroup.assignedWorkGiverDefNames)
            {
                WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                PawnColumnDef col = wg != null ? manager.GetWorkGiverColumn(wg) : null;
                if (col?.Worker is PawnColumnWorker_WorkGiver wgWorker)
                {
                    wgWorker.InvalidateCache();
                }
            }
        }
    }
}
