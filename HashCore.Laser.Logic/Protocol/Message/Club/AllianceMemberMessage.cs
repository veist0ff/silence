namespace HashCore.Laser.Logic.Message.Club
{
    using HashCore.Laser.Logic.Club;

    public class AllianceMemberMessage : GameMessage
    {
        public long AllianceId;

        public AllianceMember Entry;

        public override void Encode()
        {
            Stream.WriteLong(AllianceId);
            Entry.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 24308;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
