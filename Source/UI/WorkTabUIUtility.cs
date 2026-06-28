using System.Collections.Generic;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public static class WorkTabUIUtility
    {
        public static void OpenPresetsFloatMenu()
        {
            WorkTabGroupsSettings settings = WorkTabGroupsMod.Settings;
            if (settings == null)
            {
                return;
            }

            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("WorkTabGroups.SaveLayoutPreset".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_SaveLayoutPreset());
                })
            };

            foreach (LayoutPreset preset in settings.layoutPresets)
            {
                LayoutPreset captured = preset;
                options.Add(new FloatMenuOption("WorkTabGroups.LoadLayoutPreset".Translate(captured.presetName), () =>
                {
                    PresetApplier.ApplyLayout(captured);
                }));
            }

            options.Add(new FloatMenuOption("WorkTabGroups.OpenModSettings".Translate(), () =>
            {
                Find.WindowStack.Add(new Dialog_ModSettings(WorkTabGroupsMod.Instance));
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
