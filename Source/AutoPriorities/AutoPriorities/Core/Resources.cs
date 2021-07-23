using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D AutoPrioritiesButtonIcon;
        public static readonly Texture2D PlusIcon;
        public static readonly Texture2D MinusIcon;

        static Resources()
        {
            AutoPrioritiesButtonIcon = ContentFinder<Texture2D>.Get("work_button_icon");
            PlusIcon = ContentFinder<Texture2D>.Get("Plus");
            MinusIcon = ContentFinder<Texture2D>.Get("Minus");
        }
    }
}
