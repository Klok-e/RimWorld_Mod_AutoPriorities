using System.Linq;
using System.Text.RegularExpressions;
using AutoPriorities.PawnDataSerializer.Exporter;
using Verse;

namespace AutoPriorities.Ui
{
    public class NameExportDialog : Dialog_Rename<SavedPawnDataRenameableReference>
    {
        private static readonly Regex ValidNameRegex = new(@"^[\w]+$");
        private readonly string[] _invalidNames;

        public NameExportDialog(SavedPawnDataRenameableReference pawnDataRenameableReference, string[] invalidNames)
            : base(pawnDataRenameableReference)
        {
            _invalidNames = invalidNames;
        }

        protected override AcceptanceReport NameIsValid(string newName)
        {
            return !_invalidNames.Contains(newName) && ValidNameRegex.IsMatch(newName);
        }
    }
}
