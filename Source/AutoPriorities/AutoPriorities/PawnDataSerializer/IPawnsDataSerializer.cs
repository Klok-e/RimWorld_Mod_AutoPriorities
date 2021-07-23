namespace AutoPriorities.PawnDataSerializer
{
    public interface IPawnsDataSerializer
    {
        SaveData LoadSavedData();

        void SaveData(SaveDataRequest request);
    }
}
