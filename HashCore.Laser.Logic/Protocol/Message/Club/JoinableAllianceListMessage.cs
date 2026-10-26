namespace HashCore.Laser.Logic.Message.Club
{
    using HashCore.Laser.Logic.Club;

    public class JoinableAllianceListMessage : GameMessage
    {
        public List<AllianceHeader> JoinableAlliances;

        public JoinableAllianceListMessage() : base()
        {
            JoinableAlliances = new List<AllianceHeader>();
        }

        public override void Encode()
        {
            Stream.WriteVInt(JoinableAlliances.Count);
            foreach (AllianceHeader entry in JoinableAlliances)
            {
                entry.Encode(Stream);
            }
        }

        public override int GetMessageType()
        {
            return 24304;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
