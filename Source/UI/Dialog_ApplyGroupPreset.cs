using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_ApplyGroupPreset : Window
    {
        private readonly GroupPreset preset;
        private int anchorIndex;
        private List<AnchorOption> anchorOptions;

        public override Vector2 InitialSize => new Vector2(440f, 220f);

        public Dialog_ApplyGroupPreset(GroupPreset preset)
        {
            this.preset = preset;
            anchorOptions = AnchorPickerUtility.BuildAnchorOptions();
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.ApplyGroupPresetTitle".Translate(preset.presetName));
            Text.Font = GameFont.Small;

            float y = 45f;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "WorkTabGroups.InsertAfter".Translate());
            y += 26f;

            anchorIndex = Mathf.Clamp(anchorIndex, 0, anchorOptions.Count - 1);
            if (Widgets.ButtonText(new Rect(0f, y, inRect.width, 30f), anchorOptions[anchorIndex].label))
            {
                List<FloatMenuOption> menu = new List<FloatMenuOption>();
                for (int i = 0; i < anchorOptions.Count; i++)
                {
                    int captured = i;
                    menu.Add(new FloatMenuOption(anchorOptions[i].label, () => anchorIndex = captured));
                }

                Find.WindowStack.Add(new FloatMenu(menu));
            }

            y += 45f;
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "WorkTabGroups.Apply".Translate()))
            {
                string anchor = anchorOptions[anchorIndex].anchor;
                PresetApplier.ApplyGroupPreset(preset, anchor);
                Close();
            }
        }
    }
}
