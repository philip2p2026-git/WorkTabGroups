using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class Dialog_CreateMajorWorkGroup : Window
    {
        private string groupName = string.Empty;
        private int anchorIndex;
        private List<AnchorOption> anchorOptions;
        private readonly WorkGiverDef assignAfterCreate;

        public override Vector2 InitialSize => new Vector2(440f, 280f);

        public Dialog_CreateMajorWorkGroup(WorkGiverDef assignAfterCreate = null)
        {
            this.assignAfterCreate = assignAfterCreate;
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            anchorOptions = AnchorPickerUtility.BuildAnchorOptions();
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
            y += 40f;

            Widgets.Label(new Rect(0f, y, inRect.width, 24f), "WorkTabGroups.InsertAfter".Translate());
            y += 26f;

            if (anchorOptions.Count > 0)
            {
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
            }

            y += 45f;
            if (Widgets.ButtonText(new Rect(inRect.width / 2f - 80f, y, 160f, 35f), "WorkTabGroups.Create".Translate()))
            {
                Confirm();
            }
        }

        private void Confirm()
        {
            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager == null)
            {
                return;
            }

            string anchor = anchorOptions.Count > 0 ? anchorOptions[anchorIndex].anchor : string.Empty;
            string error = manager.CreateGroup(groupName, anchor);
            if (error != null)
            {
                Messages.Message(error, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (assignAfterCreate != null)
            {
                MajorWorkGroupData created = manager.Groups[manager.Groups.Count - 1];
                manager.AssignWorkGiver(assignAfterCreate, created.defName);
            }

            Close();
        }
    }
}
