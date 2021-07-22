using UnityEngine;
using Verse;

namespace AutoPriorities.Core
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D _autoPrioritiesButtonIcon;
        public static readonly Texture2D _plusIcon;
        public static readonly Texture2D _minusIcon;

        static Resources()
        {
            _autoPrioritiesButtonIcon = ContentFinder<Texture2D>.Get("work_button_icon");
            _plusIcon = ContentFinder<Texture2D>.Get("Plus");
            _minusIcon = ContentFinder<Texture2D>.Get("Minus");
        }
    }
}
