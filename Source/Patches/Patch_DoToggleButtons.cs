using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WorkTab;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_WorkTab), "DoToggleButtons")]
    public static class Patch_DoToggleButtons
    {
        private const float ToggleButtonSize = 30f;
        private const float ButtonHeight = 30f;
        private const float ButtonPadding = 16f;

        public static void Postfix(MainTabWindow_WorkTab __instance, Rect canvas)
        {
            const float margin = 18f;
            string label = "WorkTabGroups.LayoutEditor.OpenButton".Translate();
            float buttonWidth = Text.CalcSize(label).x + ButtonPadding;
            float buttonXMax = canvas.xMax - margin - (ToggleButtonSize + margin) * 3f;
            Rect buttonRect = new Rect(buttonXMax - buttonWidth, canvas.yMin, buttonWidth, ButtonHeight);

            TooltipHandler.TipRegion(buttonRect, "WorkTabGroups.LayoutEditor.OpenTip".Translate());
            if (Widgets.ButtonText(buttonRect, label, true, true, true))
            {
                Find.WindowStack.Add(new Window_WorkLayoutEditor());
            }
        }
    }
}
