using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WorkTabGroups
{
    internal static class MajorWorkGroupDrawUtility
    {
        public static void DrawWorkBoxBackground(Rect rect, Pawn pawn, WorkTypeDef workDef)
        {
            if (workDef == null)
            {
                return;
            }

            float skill = pawn.skills.AverageOfRelevantSkillsFor(workDef);
            Texture2D back;
            Texture2D front;
            float frontAlpha;
            if (skill < 4f)
            {
                back = WidgetsWork.WorkBoxBGTex_Awful;
                front = WidgetsWork.WorkBoxBGTex_Bad;
                frontAlpha = skill / 4f;
            }
            else if (skill <= 14f)
            {
                back = WidgetsWork.WorkBoxBGTex_Bad;
                front = WidgetsWork.WorkBoxBGTex_Mid;
                frontAlpha = (skill - 4f) / 10f;
            }
            else
            {
                back = WidgetsWork.WorkBoxBGTex_Mid;
                front = WidgetsWork.WorkBoxBGTex_Excellent;
                frontAlpha = (skill - 14f) / 6f;
            }

            GUI.DrawTexture(rect, back);
            Color saved = GUI.color;
            GUI.color = new Color(saved.r, saved.g, saved.b, frontAlpha);
            GUI.DrawTexture(rect, front);
            GUI.color = saved;

            if (pawn.Ideo != null && pawn.Ideo.IsWorkTypeConsideredDangerous(workDef))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect, WidgetsWork.WorkBoxOverlay_PreceptWarning);
            }

            if (workDef.relevantSkills.Any() && skill <= 2f && pawn.workSettings.WorkIsActive(workDef))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(rect.ContractedBy(-2f), WidgetsWork.WorkBoxOverlay_Warning);
            }

            Passion passion = pawn.skills.MaxPassionOfRelevantSkillsFor(workDef);
            if ((int)passion > 0)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Texture2D passionTex = passion == Passion.Major
                    ? WidgetsWork.PassionWorkboxMajorIcon
                    : WidgetsWork.PassionWorkboxMinorIcon;
                GUI.DrawTexture(new Rect(rect.xMax - 12f, rect.yMax - 12f, 12f, 12f), passionTex);
                GUI.color = Color.white;
            }
        }
    }
}
