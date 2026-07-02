using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public static class LayoutDragDropState
    {
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

        public static void Clear()
        {
            Kind = DragKind.None;
            SourceLayoutIndex = -1;
            WorkGiverDefName = null;
            SourceGroupDefName = null;
            SourceWorkGiverIndex = -1;
        }

        public static void BeginCustomGroupDrag(int layoutIndex)
        {
            Clear();
            Kind = DragKind.CustomGroup;
            SourceLayoutIndex = layoutIndex;
        }

        public static void BeginWorkGiverDrag(string workGiverDefName, string sourceGroupDefName, int sourceWorkGiverIndex)
        {
            Clear();
            Kind = DragKind.WorkGiver;
            WorkGiverDefName = workGiverDefName;
            SourceGroupDefName = sourceGroupDefName;
            SourceWorkGiverIndex = sourceWorkGiverIndex;
        }

        public static bool IsDragging => Kind != DragKind.None;

        public static void DrawGhost(Rect rect)
        {
            if (!IsDragging)
            {
                return;
            }

            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            Widgets.DrawHighlight(rect);
            GUI.color = Color.white;
        }
    }
}
