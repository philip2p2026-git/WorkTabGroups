using Verse;

namespace WorkTabGroups
{
    public enum WorkLayoutEntryKind
    {
        WorkType,
        CustomGroup
    }

    public class WorkLayoutEntry : IExposable
    {
        public WorkLayoutEntryKind kind;
        public string key;

        public WorkLayoutEntry()
        {
        }

        public WorkLayoutEntry(WorkLayoutEntryKind kind, string key)
        {
            this.kind = kind;
            this.key = key;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref kind, "kind", WorkLayoutEntryKind.WorkType);
            Scribe_Values.Look(ref key, "key");
        }

        public static WorkLayoutEntry ForWorkType(string workTypeDefName)
        {
            return new WorkLayoutEntry(WorkLayoutEntryKind.WorkType, workTypeDefName);
        }

        public static WorkLayoutEntry ForCustomGroup(string groupDefName)
        {
            return new WorkLayoutEntry(WorkLayoutEntryKind.CustomGroup, groupDefName);
        }
    }
}
