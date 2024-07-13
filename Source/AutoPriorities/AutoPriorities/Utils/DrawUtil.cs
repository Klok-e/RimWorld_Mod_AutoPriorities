using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AutoPriorities.Utils
{
    public static class DrawUtil
    {
        /// <summary>
        ///     If false - only background is drawn
        /// </summary>
        public static void EmptyCheckbox(float x, float y, ref bool value, float width = 24f, float height = 24f)
        {
            var rect = new Rect(x, y, width, height);
            DrawWorkBoxBackground(rect);
            if (value) GUI.DrawTexture(rect, WidgetsWork.WorkBoxCheckTex);

            if (Event.current.type != EventType.MouseDown || !Mouse.IsOver(rect)) return;

            value = !value;
        }

        public static int PriorityBox(float x, float y, int priority, int MaxPriority, float width = 25f,
            float height = 25f)
        {
            var rect = new Rect(x, y, width, height);
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

            if (Event.current.type != EventType.MouseDown || !Mouse.IsOver(rect)) return priority;

            var priorityOrig = priority;
            switch (Event.current.button)
            {
                case 0:
                {
                    var priority2 = priorityOrig - 1;
                    if (priority2 < 0) priority2 = MaxPriority;
                    priority = priority2;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    break;
                }
                case 1:
                {
                    var priority2 = priorityOrig + 1;
                    if (priority2 > MaxPriority) priority2 = 0;
                    priority = priority2;
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    break;
                }
            }

            Event.current.Use();

            return priority;
        }

        private static Color ColorOfPriority(int priority)
        {
            return WidgetsWork.ColorOfPriority(priority);
        }

        private static void DrawWorkBoxBackground(Rect rect)
        {
            var texture2D1 = WidgetsWork.WorkBoxBGTex_Awful;
            var texture2D2 = WidgetsWork.WorkBoxBGTex_Bad;
            const float a = 3f / 4f;

            var colorOrig = GUI.color;
            GUI.DrawTexture(rect, texture2D1);
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, a);
            GUI.DrawTexture(rect, texture2D2);
            GUI.color = colorOrig;
        }
    }
}
