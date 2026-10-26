namespace HashCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
     
    using HashCore.Laser.Logic.Home.Items;

    public class DeletePlayerMapResponseMessage : GameMessage
    {
        public int MapId;
        public int ErrorCode;
        public override void Encode()
        {
            Stream.WriteVInt(ErrorCode);
            ByteStreamHelper.EncodeLogicLong(Stream, MapId);
        }

        public override int GetMessageType()
        {
            return 22101;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
