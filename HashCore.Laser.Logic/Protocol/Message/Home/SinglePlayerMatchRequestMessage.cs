 
namespace HashCore.Laser.Logic.Message.Home
{
    public class SinglePlayerMatchRequestMessage  : GameMessage
    {
        public SinglePlayerMatchRequestMessage() : base()
        {
            ;
        }

        public int CharacterId;
        public int SkinId;
        public int EventSlot;

        public override void Decode()
        {
            CharacterId = ByteStreamHelper.ReadDataReference(Stream);
            SkinId = ByteStreamHelper.ReadDataReference(Stream);
        }

        public override int GetMessageType()
        {
            return 14118;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
