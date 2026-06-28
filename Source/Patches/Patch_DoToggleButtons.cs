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
        private const float ButtonSize = 30f;

        public static void Postfix(MainTabWindow_WorkTab __instance, Rect canvas)
        {
            const float margin = 18f;
            Rect buttonRect = new Rect(canvas.xMax - ButtonSize - margin, canvas.yMin, ButtonSize, ButtonSize);

            // Shift left past Work Tab's three toggle buttons
            buttonRect.x -= (ButtonSize + margin) * 3f;

            TooltipHandler.TipRegion(buttonRect, "WorkTabGroups.AddGroupTip".Translate());
            if (Widgets.ButtonText(buttonRect, "+", true, true, true))
            {
                Find.WindowStack.Add(new Dialog_CreateMajorWorkGroup());
            }

            buttonRect.x -= ButtonSize + margin;
            TooltipHandler.TipRegion(buttonRect, "WorkTabGroups.PresetsTip".Translate());
            if (Widgets.ButtonText(buttonRect, "P", true, true, true))
            {
                WorkTabUIUtility.OpenPresetsFloatMenu();
            }
        }
    }
}
