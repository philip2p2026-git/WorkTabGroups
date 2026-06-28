using System.IO;
using Verse;

namespace WorkTabGroups
{
    public static class WorkTabGroupsSaveTracker
    {
        public static string CurrentSaveName { get; private set; }

        public static void SetFromPath(string filepath)
        {
            if (filepath.NullOrEmpty())
            {
                return;
            }

            CurrentSaveName = Path.GetFileNameWithoutExtension(filepath);
        }
    }
}
