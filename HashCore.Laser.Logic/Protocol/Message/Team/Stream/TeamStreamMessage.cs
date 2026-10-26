namespace HashCore.Laser.Logic.Message.Team.Stream
{
    using HashCore.Laser.Logic.Team.Stream;
    using HashCore.Laser.Titan.Math;

    public class TeamStreamMessage : GameMessage
    {
        public LogicLong TeamId { get; set; }
        public TeamStreamEntry[] Entries { get; set; }

        public override void Encode()
        {
            Stream.WriteVLong(TeamId);
            Stream.WriteVInt(Entries.Length);
            foreach (TeamStreamEntry entry in Entries)
            {
                entry.Encode(Stream);
            }
        }

        public override int GetMessageType()
        {
            return 24131;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
