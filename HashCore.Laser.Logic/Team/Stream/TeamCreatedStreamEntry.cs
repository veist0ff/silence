namespace HashCore.Laser.Logic.Team.Stream
{
     
    public class TeamCreatedStreamEntry : TeamStreamEntry
    {
        public long TeamId;

        public override void Encode(ByteStream encoder)
        {
            base.Encode(encoder);
            encoder.WriteLong(TeamId);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
            encoder.WriteVInt(0);
        }

        public override int GetStreamEntryType()
        {
            return 77;
        }
    }
}
