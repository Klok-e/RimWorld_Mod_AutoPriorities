using System.Text.RegularExpressions;
using AutoPriorities.PawnDataSerializer.Exporter;
using Verse;

namespace AutoPriorities.Ui
{
    public class NameExportDialog : Dialog_Rename<SavedPawnDataReference>
    {
        private static readonly Regex ValidNameRegex = new(@"^[\w]+$");

        public NameExportDialog(SavedPawnDataReference pawnDataReference)
            : base(pawnDataReference)
        {
        }

        protected override AcceptanceReport NameIsValid(string newName)
        {
            return ValidNameRegex.IsMatch(newName);
        }
    }
}
