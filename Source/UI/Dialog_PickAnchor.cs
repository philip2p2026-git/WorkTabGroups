using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_PickAnchor : Window
    {
        private readonly string groupDefName;
        private readonly bool isNewGroup;
        private int anchorIndex;
        private List<AnchorOption> anchorOptions;

        public override Vector2 InitialSize => new Vector2(440f, 220f);

        public Dialog_PickAnchor(string groupDefName, bool isNewGroup)
        {
            this.groupDefName = groupDefName;
            this.isNewGroup = isNewGroup;
            anchorOptions = AnchorPickerUtility.BuildAnchorOptions(groupDefName);
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "WorkTabGroups.ChangePositionTitle".Translate());
            Text.Font = GameFont.Small;

            float y = 45f;
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
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "OK".Translate()))
            {
                string anchor = anchorOptions[anchorIndex].anchor;
                string error = WorkTabGroupsManager.Instance?.SetAnchor(groupDefName, anchor);
                if (error != null)
                {
                    Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                Close();
            }
        }
    }
}
