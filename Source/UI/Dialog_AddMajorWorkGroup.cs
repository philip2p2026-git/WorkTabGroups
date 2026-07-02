using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_AddMajorWorkGroup : Window
    {
        private readonly int layoutIndex;
        private string groupName = string.Empty;

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public Dialog_AddMajorWorkGroup(int layoutIndex)
        {
            this.layoutIndex = layoutIndex;
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.CreateGroupTitle".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "WorkTabGroups.GroupName".Translate());
            y += 26f;
            groupName = Widgets.TextField(new Rect(0f, y, inRect.width, 30f), groupName);
            y += 45f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "WorkTabGroups.Create".Translate()))
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
            string error = WorkTabGroupsManager.Instance?.CreateGroup(groupName, layoutIndex);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Close();
        }
    }
}
