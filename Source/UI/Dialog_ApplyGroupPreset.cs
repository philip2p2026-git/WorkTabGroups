using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_ApplyGroupPreset : Window
    {
        private readonly GroupPreset preset;

        public override Vector2 InitialSize => new Vector2(440f, 180f);

        public Dialog_ApplyGroupPreset(GroupPreset preset)
        {
            this.preset = preset;
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.ApplyGroupPresetTitle".Translate(preset.presetName));
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(0f, 45f, inRect.width, 48f), "WorkTabGroups.LayoutEditor.ApplyGroupPresetTip".Translate());

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, 110f, 160f, 35f), "WorkTabGroups.Apply".Translate()))
            {
                TryConfirm();
            }
        }

        public override void OnAcceptKeyPressed()
        {
            TryConfirm();
            Event.current.Use();
        }

        private void TryConfirm()
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            int layoutIndex = manager?.WorkLayoutOrder.Count ?? 0;
            PresetApplier.ApplyGroupPreset(preset, layoutIndex);
            Close();
        }
    }
}
