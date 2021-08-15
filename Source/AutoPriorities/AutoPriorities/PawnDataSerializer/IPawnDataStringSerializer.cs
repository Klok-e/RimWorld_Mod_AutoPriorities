namespace AutoPriorities.PawnDataSerializer
{
    public interface IPawnDataStringSerializer
    {
        SaveData? Deserialize(byte[] xml);

        byte[] Serialize(SaveDataRequest request);
    }
}
