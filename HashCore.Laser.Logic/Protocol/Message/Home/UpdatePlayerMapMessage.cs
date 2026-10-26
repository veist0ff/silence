namespace HashCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
     
    using HashCore.Laser.Logic.Home.Items;
    
    using HashCore.Laser.Server;
    using HashCore.Laser.Titan.Util;

    public class UpdatePlayerMapMessage : GameMessage
    {
        public int MapId;
        public byte[] MapData;
        public override void Decode()
        {
            MapId=(int) ByteStreamHelper.DecodeLogicLong(Stream);
            byte[] bytes = Stream.ReadBytes(Stream.ReadBytesLength(), 900000);
            MapData = bytes;
            ZLibHelper.DecompressInMySQLFormat(bytes, out byte[] output);
          Logger.Print(Encoding.UTF8.GetString(output, 0, output.Length));
        }

        public override int GetMessageType()
        {
            return 12103;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
