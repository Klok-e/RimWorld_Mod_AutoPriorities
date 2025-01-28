using System;
using System.IO;
using System.Reflection;
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
        public static readonly string DefaultPreset;

        static Resources()
        {
            AutoPrioritiesButtonIcon = ContentFinder<Texture2D>.Get("work_button_icon");
            PlusIcon = ContentFinder<Texture2D>.Get("Plus");
            MinusIcon = ContentFinder<Texture2D>.Get("Minus");
            DefaultPreset = LoadEmbeddedFile("DefaultPreset.xml");
        }

        private static string LoadEmbeddedFile(string fileName)
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"AutoPriorities.EmbeddedAssets.{fileName}");
                if (stream == null)
                    throw new FileNotFoundException($"Embedded resource {fileName} not found");

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch (Exception e)
            {
                Log.Error($"Could not load {fileName}. Exception: {e}");
            }

            return string.Empty;
        }
    }
}
