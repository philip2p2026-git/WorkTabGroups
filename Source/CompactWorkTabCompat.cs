using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkTabGroups
{
    /// <summary>
    /// Optional reflection bridge to Compact Work Tab (Mlie.CompactWorkTab) when loaded.
    /// </summary>
    public static class CompactWorkTabCompat
    {
        private static bool initialized;
        private static bool isActive;
        private static Func<PawnTable, int> getMinHeaderHeight;

        public static bool IsActive
        {
            get
            {
                EnsureInitialized();
                return isActive;
            }
        }

        public static int GetMinHeaderHeight(PawnTable table, int fallback)
        {
            EnsureInitialized();
            if (!isActive || getMinHeaderHeight == null)
            {
                return fallback;
            }

            try
            {
                int height = getMinHeaderHeight(table);
                return height > 0 ? height : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            ModMetaData mod = ModLister.GetActiveModWithIdentifier("Mlie.CompactWorkTab")
                ?? ModLister.GetActiveModWithIdentifier("CaptainArbitrary.CompactWorkTab");
            if (mod == null)
            {
                return;
            }

            Type cacheType = AccessTools.TypeByName("CompactWorkTab.Cache");
            if (cacheType == null)
            {
                return;
            }

            MethodInfo recache = AccessTools.Method(cacheType, "Recache");
            PropertyInfo minHeightProp = AccessTools.Property(cacheType, "MinHeaderHeight");
            if (recache == null || minHeightProp == null)
            {
                return;
            }

            isActive = true;
            getMinHeaderHeight = table =>
            {
                recache.Invoke(null, new object[] { table });
                return (int)minHeightProp.GetValue(null);
            };
        }
    }
}
