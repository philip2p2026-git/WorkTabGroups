using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    public class WorkTabGroupsMod : Mod
    {
        public static WorkTabGroupsMod Instance { get; private set; }
        public static WorkTabGroupsSettings Settings { get; private set; }

        public WorkTabGroupsMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<WorkTabGroupsSettings>();

            var harmony = new Harmony("philip2p2026.worktabgroups");
            harmony.PatchAll();

            Log.Message("[WorkTabGroups] Loaded — Harmony patches applied.");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("WorkTabGroups.Settings.GroupsHeader".Translate());
            listing.Gap(6f);

            WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
            if (manager != null && manager.Groups.Count > 0)
            {
                foreach (MajorWorkGroupData group in manager.Groups.ToList())
                {
                    listing.Label(group.label + " (" + group.assignedWorkGiverDefNames.Count + " WorkTabGroups.WorkGivers".Translate() + ")");
                    string defName = group.defName;
                    string label = group.label;
                    if (listing.ButtonText("WorkTabGroups.Delete".Translate() + ": " + label))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "WorkTabGroups.ConfirmDeleteGroup".Translate(label),
                            () => WorkTabGroupsManager.Instance?.DeleteGroup(defName)));
                    }

                    listing.Gap(2f);
                }
            }
            else
            {
                listing.Label("WorkTabGroups.Settings.NoGroups".Translate());
            }

            listing.Gap(12f);
            listing.Label("WorkTabGroups.Settings.PresetsHeader".Translate());
            listing.Gap(6f);

            if (listing.ButtonText("WorkTabGroups.SaveLayoutPreset".Translate()))
            {
                Find.WindowStack.Add(new Dialog_SaveLayoutPreset());
            }

            listing.Gap(4f);
            listing.Label("WorkTabGroups.Settings.LoadLayout".Translate());
            for (int i = 0; i < Settings.layoutPresets.Count; i++)
            {
                LayoutPreset preset = Settings.layoutPresets[i];
                if (listing.ButtonText("WorkTabGroups.Apply".Translate() + ": " + preset.presetName))
                {
                    PresetApplier.ApplyLayout(preset);
                }

                if (listing.ButtonText("WorkTabGroups.Delete".Translate() + ": " + preset.presetName))
                {
                    Settings.DeleteLayoutPreset(preset.presetName);
                }

                listing.Gap(2f);
            }

            listing.Gap(8f);
            listing.Label("WorkTabGroups.Settings.GroupPresetsHeader".Translate());
            for (int i = 0; i < Settings.groupPresets.Count; i++)
            {
                GroupPreset preset = Settings.groupPresets[i];
                listing.Label(preset.presetName + " → " + preset.groupLabel);
                if (listing.ButtonText("WorkTabGroups.Apply".Translate() + ": " + preset.presetName))
                {
                    Find.WindowStack.Add(new Dialog_ApplyGroupPreset(preset));
                }

                if (listing.ButtonText("WorkTabGroups.Delete".Translate() + ": " + preset.presetName))
                {
                    Settings.DeleteGroupPreset(preset.presetName);
                }

                listing.Gap(2f);
            }

            listing.Gap(12f);
            listing.Label("WorkTabGroups.Settings.DefaultLayout".Translate());
            listing.Gap(4f);

            var layoutNames = new List<string> { "WorkTabGroups.Settings.None".Translate() };
            layoutNames.AddRange(Settings.layoutPresets.Select(p => p.presetName));

            int currentIndex = 0;
            if (!Settings.defaultLayoutPresetName.NullOrEmpty())
            {
                int idx = layoutNames.IndexOf(Settings.defaultLayoutPresetName);
                if (idx >= 0)
                {
                    currentIndex = idx;
                }
            }

            if (layoutNames.Count > 0 && listing.ButtonText(layoutNames[currentIndex]))
            {
                List<FloatMenuOption> menu = new List<FloatMenuOption>();
                for (int i = 0; i < layoutNames.Count; i++)
                {
                    int captured = i;
                    menu.Add(new FloatMenuOption(layoutNames[i], () =>
                    {
                        Settings.defaultLayoutPresetName = captured == 0 ? string.Empty : layoutNames[captured];
                        Settings.Write();
                    }));
                }

                Find.WindowStack.Add(new FloatMenu(menu));
            }

            listing.Label("WorkTabGroups.Settings.DefaultLayoutTip".Translate());
            listing.Gap(12f);

            listing.Label("WorkTabGroups.Settings.ModRemovalHeader".Translate());
            listing.Gap(4f);
            listing.Label("WorkTabGroups.Settings.ModRemovalTip".Translate());
            if (listing.ButtonText("WorkTabGroups.Settings.PrepareForModRemoval".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WorkTabGroups.ConfirmPrepareForModRemoval".Translate(),
                    () =>
                    {
                        WorkTabGroupsManager manager = WorkTabGroupsManager.Instance;
                        if (manager != null)
                        {
                            manager.PrepareForModRemoval();
                        }
                        else
                        {
                            Messages.Message("WorkTabGroups.PreparedForModRemoval".Translate(), MessageTypeDefOf.PositiveEvent, false);
                        }
                    }));
            }

            listing.End();
        }

        public override string SettingsCategory()
        {
            return "Work Tab Groups";
        }
    }
}
