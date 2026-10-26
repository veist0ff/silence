namespace HashCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
     
    using HashCore.Laser.Logic.Home.Items;

    public class UpdatePlayerMapResponseMessage : GameMessage
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
            return 22103;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
