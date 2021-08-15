using RimWorld;
using Verse;

namespace AutoPriorities.Ui
{
    public class NameExportDialog:Dialog_Rename
    {
        protected override void SetName(string name)
        {
            throw new System.NotImplementedException();
        }
        
        protected override AcceptanceReport NameIsValid( string newName )
        {
            throw new System.NotImplementedException();

            // if all checks are passed, return true.
            return true;
        }
    }
}
