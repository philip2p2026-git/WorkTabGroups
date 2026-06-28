using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_SaveGroupPreset : Window
    {
        private readonly string groupDefName;
        private string presetName;

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public Dialog_SaveGroupPreset(string groupDefName)
        {
            this.groupDefName = groupDefName;
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.SaveGroupPresetTitle".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
            presetName = Widgets.TextField(new Rect(0f, y, inRect.width, 30f), presetName);
            y += 45f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "WorkTabGroups.Save".Translate()))
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
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Messages.Message("WorkTabGroups.Error.EmptyName".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorkTabGroupsSettings settings = WorkTabGroupsMod.Settings;
            MajorWorkGroupData group = WorkTabGroupsManager.Instance?.GetGroup(groupDefName);
            if (settings == null || group == null)
            {
                return;
            }

            string error = settings.SaveGroupPreset(presetName.Trim(), group);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("WorkTabGroups.PresetSaved".Translate(presetName), MessageTypeDefOf.PositiveEvent, false);
            Close();
        }
    }

    public class Dialog_SaveLayoutPreset : Window
    {
        private string presetName;

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public Dialog_SaveLayoutPreset()
        {
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.SaveLayoutPresetTitle".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
            presetName = Widgets.TextField(new Rect(0f, y, inRect.width, 30f), presetName);
            y += 45f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "WorkTabGroups.Save".Translate()))
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
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Messages.Message("WorkTabGroups.Error.EmptyName".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            WorkTabGroupsSettings settings = WorkTabGroupsMod.Settings;
            if (manager == null || settings == null)
            {
                return;
            }

            string error = settings.SaveLayoutPreset(presetName.Trim(), manager);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("WorkTabGroups.PresetSaved".Translate(presetName), MessageTypeDefOf.PositiveEvent, false);
            Close();
        }
    }
}
