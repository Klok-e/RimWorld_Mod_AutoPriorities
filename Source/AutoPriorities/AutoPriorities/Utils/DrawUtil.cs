using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoPriorities.Utils
{
    internal static class DrawUtil
    {
        public static int PriorityBox(float x, float y, int priority)
        {
            Rect rect = new Rect(x, y, 25f, 25f);
            DrawWorkBoxBackground(rect);

            if (priority > 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                var colorOrig = GUI.color;
                GUI.color = ColorOfPriority(priority);
                Widgets.Label(rect.ContractedBy(-3f), priority.ToStringCached());
                GUI.color = colorOrig;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(rect))
            {
                int priorityOrig = priority;
                bool flag = priority > 0;
                if (Event.current.button == 0)
                {
                    int priority2 = priorityOrig - 1;
                    if (priority2 < 0)
                        priority2 = 4;
                    priority = priority2;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }
                if (Event.current.button == 1)
                {
                    int priority2 = priorityOrig + 1;
                    if (priority2 > 4)
                        priority2 = 0;
                    priority = priority2;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }
                Event.current.Use();
            }
            return priority;
        }

        private static Color ColorOfPriority(int prio)
        {
            return WidgetsWork.ColorOfPriority(prio);
        }

        private static void DrawWorkBoxBackground(Rect rect)
        {
            var texture2D1 = WidgetsWork.WorkBoxBGTex_Awful;
            var texture2D2 = WidgetsWork.WorkBoxBGTex_Bad;
            float a = 3f / 4f;

            var colorOrig = GUI.color;
            GUI.DrawTexture(rect, texture2D1);
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, a);
            GUI.DrawTexture(rect, texture2D2);
            GUI.color = colorOrig;
        }
    }
}
