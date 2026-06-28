using RimWorld;
using Verse;

namespace WorkTabGroups
{
    public static class AnchorKeys
    {
        public const string WorkTypePrefix = "WorkType:";
        public const string GroupPrefix = "Group:";

        public static string ForWorkType(WorkTypeDef workType)
        {
            return WorkTypePrefix + workType.defName;
        }

        public static string ForGroup(string groupDefName)
        {
            return GroupPrefix + groupDefName;
        }

        public static string ForPresetGroup(string presetGroupId)
        {
            return GroupPrefix + presetGroupId;
        }

        public static bool IsStart(string anchor)
        {
            return string.IsNullOrEmpty(anchor);
        }

        public static bool TryParseWorkType(string anchor, out string workTypeDefName)
        {
            if (!string.IsNullOrEmpty(anchor) && anchor.StartsWith(WorkTypePrefix))
            {
                workTypeDefName = anchor.Substring(WorkTypePrefix.Length);
                return true;
            }

            workTypeDefName = null;
            return false;
        }

        public static bool TryParseGroup(string anchor, out string groupKey)
        {
            if (!string.IsNullOrEmpty(anchor) && anchor.StartsWith(GroupPrefix))
            {
                groupKey = anchor.Substring(GroupPrefix.Length);
                return true;
            }

            groupKey = null;
            return false;
        }
    }
}
