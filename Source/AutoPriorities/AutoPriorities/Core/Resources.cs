using System.Reflection;
using UnityEngine;

namespace AutoPriorities.Core
{
    [Verse.StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D _autoPrioritiesButtonIcon;
        public static readonly Texture2D _plusIcon;
        public static readonly Texture2D _minusIcon;

        static Resources()
        {
            _autoPrioritiesButtonIcon = Verse.ContentFinder<Texture2D>.Get("work_button_icon");
            _plusIcon = Verse.ContentFinder<Texture2D>.Get("Plus");
            _minusIcon = Verse.ContentFinder<Texture2D>.Get("Minus");
        }
    }
}