namespace HashCore.Laser.Logic.Message.Team
{
    public class TeamSpectateMessage : GameMessage
    {
        public long TeamId;

        public override void Decode()
        {
            TeamId = Stream.ReadVLong();
        }

        public override int GetMessageType()
        {
            return 14358;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
