namespace AutoPriorities.PawnDataSerializer
{
    public interface IPawnDataStringSerializer
    {
        DeserializedData? Deserialize(byte[] xml);

        byte[] Serialize(SaveDataRequest request);
    }
}
