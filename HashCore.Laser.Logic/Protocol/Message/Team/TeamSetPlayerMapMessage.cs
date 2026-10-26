namespace HashCore.Laser.Logic.Message.Team
{
     
    public class TeamSetPlayerMapMessage : GameMessage
    {
        public long mapid;
        public override int GetMessageType()
        {
            return 12110;
        }
        public override void Decode()
        {
            mapid = Stream.ReadVLong();
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
