namespace HashCore.Laser.Logic.Message.Club
{
    public class AskForJoinableAllianceListMessage : GameMessage
    {
        public int Filter;

        public override void Decode()
        {
            Filter = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14303;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
