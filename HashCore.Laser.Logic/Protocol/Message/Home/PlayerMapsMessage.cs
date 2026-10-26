 
using HashCore.Laser.Logic.Home.Structures;

namespace HashCore.Laser.Logic.Message.Home
{
    public class PlayerMapsMessage : GameMessage
    {
        public PlayerMap[] maps;
        public override void Encode()
        {
            Stream.WriteVInt(maps.Length);
            foreach(PlayerMap map in maps)
            {
                map.Encode(Stream);
            }
        }

        public override int GetMessageType()
        {
            return 22102;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
