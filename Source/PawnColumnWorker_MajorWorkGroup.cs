using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
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
                MainTabWindow_WorkTab.SetCurrentWorkTabDirty();
            }
        }

        public MajorWorkGroupData BoundGroup => boundGroup;

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
            if (!InteractionUtilities.Ctrl || !CanExpand)
            {
                return false;
            }

            if (Expanded)
            {
                NeedCollapse = true;
            }
            else
            {
                NeedExpand = true;
            }

            InvalidateCache();
            return true;
        }

        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
        }

        protected override void HandleInteractionsDetailed(Rect rect, Pawn pawn)
        {
        }

        protected override void HandleInteractionsToggle(Rect rect, Pawn pawn)
        {
        }

        public override int GetMinWidth(PawnTable table)
        {
            return 32;
        }

        public override int GetOptimalHeaderWidth(PawnTable table)
        {
            return Mathf.Max(60, (boundGroup?.label ?? "Group").Length * 8);
        }

        public override void DoHeader(Rect rect, PawnTable table)
        {
            base.DoHeader(rect, table);
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
            base.HeaderClicked(headerRect, table);
            HeaderExpand();
        }

        protected override void HeaderInteractions(Rect headerRect, PawnTable table, bool clicked = false)
        {
            if (!Mouse.IsOver(headerRect))
            {
                return;
            }

            bool rightClick = clicked ? InteractionUtilities.RightClicked() : InteractionUtilities.RightClicked(headerRect);
            if (rightClick && !InteractionUtilities.Shift)
            {
                TryOpenGroupContextMenu(headerRect);
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
