using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public static class LayoutDragDropState
    {
        private const int DragAttachmentWindowId = 34003428;
        private static readonly Color DropLineColor = new Color(0.45f, 0.85f, 0.45f, 0.9f);
        private static readonly Color DropTargetColor = new Color(0.45f, 0.85f, 0.45f, 0.35f);
        private static readonly Color UnassignTargetColor = new Color(0.85f, 0.75f, 0.35f, 0.25f);

        public enum DragKind
        {
            None,
            CustomGroup,
            WorkGiver
        }

        public static DragKind Kind = DragKind.None;
        public static int SourceLayoutIndex = -1;
        public static string WorkGiverDefName;
        public static string SourceGroupDefName;
        public static int SourceWorkGiverIndex = -1;
        public static string DragLabel;

        public static void Clear()
        {
            Kind = DragKind.None;
            SourceLayoutIndex = -1;
            WorkGiverDefName = null;
            SourceGroupDefName = null;
            SourceWorkGiverIndex = -1;
            DragLabel = null;
        }

        public static void BeginCustomGroupDrag(int layoutIndex, string label)
        {
            Clear();
            Kind = DragKind.CustomGroup;
            SourceLayoutIndex = layoutIndex;
            DragLabel = label;
        }

        public static void BeginWorkGiverDrag(
            string workGiverDefName,
            string sourceGroupDefName,
            int sourceWorkGiverIndex,
            string label)
        {
            Clear();
            Kind = DragKind.WorkGiver;
            WorkGiverDefName = workGiverDefName;
            SourceGroupDefName = sourceGroupDefName;
            SourceWorkGiverIndex = sourceWorkGiverIndex;
            DragLabel = label;
        }

        public static bool IsDragging => Kind != DragKind.None;

        public static bool IsSourceCustomGroup(int layoutIndex)
        {
            return Kind == DragKind.CustomGroup && SourceLayoutIndex == layoutIndex;
        }

        public static bool IsSourceWorkGiver(string workGiverDefName, string groupDefName, int workGiverIndex)
        {
            return Kind == DragKind.WorkGiver &&
                   WorkGiverDefName == workGiverDefName &&
                   SourceGroupDefName == groupDefName &&
                   SourceWorkGiverIndex == workGiverIndex;
        }

        public static void DrawDropLine(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, DropLineColor);
        }

        public static void DrawDropTargetHighlight(Rect rect, bool unassignZone = false)
        {
            Widgets.DrawBoxSolid(rect, unassignZone ? UnassignTargetColor : DropTargetColor);
        }

        public static void DrawActiveDragVisual()
        {
            if (!IsDragging || DragLabel.NullOrEmpty())
            {
                return;
            }

            Vector2 mouse = Event.current.mousePosition;
            float width = Mathf.Min(300f, DragLabel.GetWidthCached() + 16f);
            Rect attachRect = new Rect(mouse.x + 14f, mouse.y + 6f, width, 26f);
            Find.WindowStack.ImmediateWindow(DragAttachmentWindowId, attachRect, WindowLayer.Super, delegate
            {
                Rect inner = attachRect.AtZero();
                Widgets.DrawBoxSolid(inner, new Color(0.12f, 0.12f, 0.12f, 0.92f));
                Widgets.DrawBox(inner);
                Widgets.Label(inner.ContractedBy(6f, 4f), DragLabel);
            });
        }

        public static Color DimmedContentColor()
        {
            return new Color(1f, 1f, 1f, 0.35f);
        }
    }
}
