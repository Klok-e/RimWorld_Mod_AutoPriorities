using System.Text.RegularExpressions;
using AutoPriorities.PawnDataSerializer.Exporter;
using RimWorld;
using Verse;

namespace AutoPriorities.Ui
{
    public class NameExportDialog:Dialog_Rename
    {
        private readonly IPawnDataExporter _exporter;
        private static readonly Regex ValidNameRegex = new Regex(@"^[\w]+$");

        public NameExportDialog(IPawnDataExporter exporter)
        {
            _exporter = exporter;
        }

        protected override void SetName(string name)
        {
            _exporter.ExportCurrentPawnData(name);
        }
        
        protected override AcceptanceReport NameIsValid( string newName )
        {
            return ValidNameRegex.IsMatch(newName);
        }
    }
}
