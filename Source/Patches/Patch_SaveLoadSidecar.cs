using System.IO;
using HarmonyLib;
using Verse;

namespace WorkTabGroups.Patches
{
    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.SaveGame))]
    public static class Patch_GameDataSaveLoader_SaveGame
    {
        public static void Prefix(string fileName)
        {
            WorkTabGroupsSaveTracker.SetFromPath(fileName);
        }

        public static void Postfix()
        {
            WorkTabGroupsSidecarStorage.Save(WorkTabGroupsManager.Instance);
        }
    }

    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.LoadGame), typeof(string))]
    public static class Patch_GameDataSaveLoader_LoadGame_String
    {
        public static void Prefix(string saveFileName)
        {
            WorkTabGroupsSaveTracker.SetFromPath(saveFileName);
        }

        public static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(WorkTabGroupsSidecarStorage.TryLoadIntoManager);
        }
    }

    [HarmonyPatch(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.LoadGame), typeof(FileInfo))]
    public static class Patch_GameDataSaveLoader_LoadGame_FileInfo
    {
        public static void Prefix(FileInfo saveFile)
        {
            WorkTabGroupsSaveTracker.SetFromPath(saveFile?.FullName);
        }

        public static void Postfix()
        {
            LongEventHandler.ExecuteWhenFinished(WorkTabGroupsSidecarStorage.TryLoadIntoManager);
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.ExposeData))]
    public static class Patch_Game_ExposeData
    {
        private static WorkTabGroupsManager savingManager;

        public static void Prefix(Game __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                return;
            }

            savingManager = __instance.GetComponent<WorkTabGroupsManager>();
            if (savingManager != null)
            {
                __instance.components.Remove(savingManager);
            }
        }

        public static void Postfix(Game __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving || savingManager == null)
            {
                return;
            }

            __instance.components.Add(savingManager);
            savingManager = null;
        }
    }
}
