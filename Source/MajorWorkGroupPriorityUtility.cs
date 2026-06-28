using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using WorkTab;

namespace WorkTabGroups
{
    public static class MajorWorkGroupPriorityUtility
    {
        public static IEnumerable<WorkGiverDef> GetAssignedWorkGivers(MajorWorkGroupData group)
        {
            if (group?.assignedWorkGiverDefNames == null)
            {
                yield break;
            }

            foreach (string wgName in group.assignedWorkGiverDefNames)
            {
                WorkGiverDef wg = DefDatabase<WorkGiverDef>.GetNamedSilentFail(wgName);
                if (wg != null)
                {
                    yield return wg;
                }
            }
        }

        public static bool AllowedToDo(Pawn pawn, MajorWorkGroupData group)
        {
            if (pawn == null || group == null)
            {
                return false;
            }

            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (pawn.WorkGiverAllowedToDo(wg))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetPriority(Pawn pawn, MajorWorkGroupData group, int hour)
        {
            if (hour < 0)
            {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            if (!AllowedToDo(pawn, group))
            {
                return 0;
            }

            int min = int.MaxValue;
            bool any = false;
            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                int prio = pawn.GetPriority(wg, hour);
                if (prio > 0)
                {
                    min = Math.Min(min, prio);
                    any = true;
                }
            }

            return any ? min : 0;
        }

        public static void SetPriority(Pawn pawn, MajorWorkGroupData group, int priority, List<int> hours)
        {
            if (!AllowedToDo(pawn, group))
            {
                return;
            }

            if (hours == null || hours.Count == 0)
            {
                hours = TimeUtilities.WholeDay;
            }

            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (pawn.WorkGiverAllowedToDo(wg))
                {
                    pawn.SetPriority(wg, priority, hours);
                }
            }
        }

        public static void IncrementPriority(Pawn pawn, MajorWorkGroupData group, int hour, List<int> hours, bool playSound = true)
        {
            if (hour < 0)
            {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            int priority = GetPriority(pawn, group, hour);
            SetPriority(pawn, group, priority + 1, hours);
            if (playSound && priority == 0)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            if (playSound && priority > 0)
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        public static void DecrementPriority(Pawn pawn, MajorWorkGroupData group, int hour, List<int> hours, bool playSound = true)
        {
            if (hour < 0)
            {
                hour = GenLocalDate.HourOfDay(pawn);
            }

            int priority = GetPriority(pawn, group, hour);
            SetPriority(pawn, group, priority - 1, hours);
            if (playSound && priority > 1)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            if (playSound && priority == 1)
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }

        public static void IncrementPriority(MajorWorkGroupData group, List<Pawn> pawns, int hour, List<int> hours, bool playSound = true)
        {
            if (pawns.NullOrEmpty())
            {
                return;
            }

            if (hour < 0)
            {
                hour = GenLocalDate.HourOfDay(pawns.FirstOrDefault());
            }

            if (playSound && pawns.Any(p => GetPriority(p, group, hour) > 0))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }

            foreach (Pawn pawn in pawns.Where(p => GetPriority(p, group, hour) > 0))
            {
                IncrementPriority(pawn, group, hour, hours, playSound: false);
            }
        }

        public static void DecrementPriority(MajorWorkGroupData group, List<Pawn> pawns, int hour, List<int> hours, bool playSound = true)
        {
            if (pawns.NullOrEmpty())
            {
                return;
            }

            if (hour < 0)
            {
                hour = GenLocalDate.HourOfDay(pawns.FirstOrDefault());
            }

            if (playSound && pawns.Any(p => GetPriority(p, group, hour) != 1))
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }

            foreach (Pawn pawn in pawns.Where(p => GetPriority(p, group, hour) != 1))
            {
                DecrementPriority(pawn, group, hour, hours, playSound: false);
            }
        }

        public static WorkTypeDef RepresentativeWorkType(Pawn pawn, MajorWorkGroupData group)
        {
            WorkTypeDef best = null;
            float bestSkill = -1f;
            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (wg.workType == null)
                {
                    continue;
                }

                if (pawn == null)
                {
                    return wg.workType;
                }

                float skill = pawn.skills.AverageOfRelevantSkillsFor(wg.workType);
                if (skill > bestSkill)
                {
                    bestSkill = skill;
                    best = wg.workType;
                }
            }

            return best ?? GetAssignedWorkGivers(group).FirstOrDefault()?.workType;
        }

        public static WorkTypeDef RepresentativeWorkType(MajorWorkGroupData group)
        {
            return RepresentativeWorkType(null, group);
        }

        public static bool TimeScheduled(Pawn pawn, MajorWorkGroupData group)
        {
            PriorityTracker tracker = PriorityManager.Get[pawn];
            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (tracker.TimeScheduled(wg))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool PartScheduled(Pawn pawn, MajorWorkGroupData group)
        {
            foreach (int hour in TimeUtilities.WholeDay)
            {
                int groupPrio = GetPriority(pawn, group, hour);
                foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
                {
                    if (pawn.GetPriority(wg, hour) != groupPrio)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsDoingGroupJob(Pawn pawn, MajorWorkGroupData group)
        {
            WorkGiverDef current = pawn.CurJob?.workGiverDef;
            if (current == null || group?.assignedWorkGiverDefNames == null)
            {
                return false;
            }

            return group.assignedWorkGiverDefNames.Contains(current.defName);
        }

        public static bool HasIdeoOpposedWork(Pawn pawn, MajorWorkGroupData group)
        {
            if (pawn?.Ideo == null)
            {
                return false;
            }

            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (wg.workType != null && pawn.Ideo.IsWorkTypeConsideredDangerous(wg.workType))
                {
                    return true;
                }
            }

            return false;
        }

        public static WorkTypeDef FirstIdeoOpposedWorkType(Pawn pawn, MajorWorkGroupData group)
        {
            if (pawn?.Ideo == null)
            {
                return null;
            }

            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                if (wg.workType != null && pawn.Ideo.IsWorkTypeConsideredDangerous(wg.workType))
                {
                    return wg.workType;
                }
            }

            return null;
        }

        public static string TipForPawnGroup(Pawn pawn, MajorWorkGroupData group)
        {
            var tip = new StringBuilder();
            if (group != null && !group.label.NullOrEmpty())
            {
                tip.AppendLine(group.label);
            }

            int hour = MainTabWindow_WorkTab.VisibleHour;
            int priority = GetPriority(pawn, group, hour);
            tip.AppendLine(DrawUtilities.PriorityLabel(priority));

            foreach (WorkGiverDef wg in GetAssignedWorkGivers(group))
            {
                bool incapable = !pawn.CapableOf(wg);
                tip.AppendLine();
                tip.Append(DrawUtilities.TipForPawnWorker(pawn, wg, incapable));
            }

            return tip.ToString().TrimEndNewlines();
        }
    }
}
