namespace HashCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
     
    using HashCore.Laser.Logic.Home.Items;
     
    public class DeletePlayerMapMessage : GameMessage
    {
        public int MapId;
        public override void Decode()
        {
            MapId=(int) ByteStreamHelper.DecodeLogicLong(Stream);
        }

        public override int GetMessageType()
        {
            return 12101;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
