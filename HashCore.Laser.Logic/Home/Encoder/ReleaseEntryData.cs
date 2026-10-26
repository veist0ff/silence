
public static class ReleaseEntryData
{
    public static void encode(ByteStream stream)
    {
        stream.WriteVInt(1);
        {
            DateTime psina = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time"));
            DateTime characterTargetDateTime = new DateTime(2026, 6, 1, 8, 0, 0);
            TimeSpan characterTimeDifference = characterTargetDateTime - psina;
            int characterSecondsRemaining = (int)characterTimeDifference.TotalSeconds; 
            stream.WriteDataReference(16,100);
            stream.WriteInt(characterSecondsRemaining);
            stream.WriteInt(characterSecondsRemaining);
            stream.WriteInt(characterSecondsRemaining);
            stream.WriteBoolean(false);

        }

    }
}
