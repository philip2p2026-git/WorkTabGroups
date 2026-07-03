using System.Collections.Generic;
using System.IO;
using Verse;

namespace WorkTabGroups
{
    public static class WorkTabGroupsSidecarStorage
    {
        private static string StorageDirectory => Path.Combine(GenFilePaths.SaveDataFolderPath, "WorkTabGroups");

        public static void Save(WorkTabGroupsManager manager)
        {
            string saveName = WorkTabGroupsSaveTracker.CurrentSaveName;
            if (saveName.NullOrEmpty())
            {
                return;
            }

            if (manager == null || manager.Groups.Count == 0)
            {
                DeleteForSave(saveName);
                return;
            }

            Directory.CreateDirectory(StorageDirectory);

            var data = new WorkTabGroupsSidecarData();
            manager.WriteToSidecarData(data);

            string path = GetPath(saveName);
            Scribe.saver.InitSaving(path, "WorkTabGroupsSidecar");
            try
            {
                Scribe_Deep.Look(ref data, "data");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
        }

        public static void TryLoadIntoManager()
        {
            string saveName = WorkTabGroupsSaveTracker.CurrentSaveName;
            if (saveName.NullOrEmpty())
            {
                return;
            }

            WorkTabGroupsManager manager = WorkTabGroupsManager.EnsureRegistered();
            if (manager == null)
            {
                return;
            }

            if (manager.IsSidecarLoadedForSave(saveName))
            {
                return;
            }

            string path = GetPath(saveName);
            if (!File.Exists(path))
            {
                if (manager.Groups.Count > 0)
                {
                    manager.ClearAllGroups();
                }

                manager.MarkSidecarLoaded(saveName);
                LongEventHandler.ExecuteWhenFinished(WorkTabGroupsManager.RequestColumnRebuild);
                return;
            }

            WorkTabGroupsSidecarData data = null;
            Scribe.loader.InitLoading(path);
            try
            {
                Scribe_Deep.Look(ref data, "data");
            }
            finally
            {
                Scribe.loader.FinalizeLoading();
            }

            if (data?.groups == null || data.groups.Count == 0)
            {
                if (manager.Groups.Count > 0)
                {
                    manager.ClearAllGroups();
                }

                manager.MarkSidecarLoaded(saveName);
                LongEventHandler.ExecuteWhenFinished(WorkTabGroupsManager.RequestColumnRebuild);
                return;
            }

            if (data.workLayoutOrder == null)
            {
                data.workLayoutOrder = new List<WorkLayoutEntry>();
            }

            LayoutPruneReport pruneReport = LayoutSanitizer.PruneLayoutDataWithReport(data.groups, data.workLayoutOrder);

            if (data.groups.Count == 0)
            {
                DeleteForSave(saveName);
                if (manager.Groups.Count > 0)
                {
                    manager.ClearAllGroups();
                }

                manager.MarkSidecarLoaded(saveName);
                LongEventHandler.ExecuteWhenFinished(WorkTabGroupsManager.RequestColumnRebuild);
                return;
            }

            manager.ApplyPersistedState(data.groups, data.workLayoutOrder, data.nextGroupId);
            manager.MarkSidecarLoaded(saveName);
            if (pruneReport.HasChanges)
            {
                manager.SetPendingLayoutChangeNotice(pruneReport);
            }

            LayoutSanitizer.PruneInvalidReferences(manager);
            Save(manager);
        }

        public static void DeleteForCurrentSave()
        {
            if (!WorkTabGroupsSaveTracker.CurrentSaveName.NullOrEmpty())
            {
                DeleteForSave(WorkTabGroupsSaveTracker.CurrentSaveName);
            }
        }

        public static void DeleteForSave(string saveName)
        {
            if (saveName.NullOrEmpty())
            {
                return;
            }

            string path = GetPath(saveName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetPath(string saveName)
        {
            string safeName = saveName;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return Path.Combine(StorageDirectory, safeName + ".xml");
        }
    }
}
