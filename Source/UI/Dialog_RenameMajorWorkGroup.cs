using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_RenameMajorWorkGroup : Window
    {
        private readonly LayoutEditorDraft draft;
        private readonly string groupDefName;
        private string newName;

        public override Vector2 InitialSize => new Vector2(400f, 180f);

        public Dialog_RenameMajorWorkGroup(LayoutEditorDraft draft, string groupDefName)
        {
            this.draft = draft;
            this.groupDefName = groupDefName;
            MajorWorkGroupData group = draft?.GetGroup(groupDefName);
            newName = group?.label ?? string.Empty;
            doCloseButton = false;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.RenameGroupTitle".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
            newName = Widgets.TextField(new Rect(0f, y, inRect.width, 30f), newName);
            y += 45f;

            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "OK".Translate()))
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
            if (draft == null)
            {
                return;
            }

            string error = draft.RenameGroup(groupDefName, newName);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Close();
        }
    }
}
