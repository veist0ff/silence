 
namespace HashCore.Laser.Logic.Message.Home
{
    public class MatchmakeRequestMessage : GameMessage
    {
        public MatchmakeRequestMessage() : base()
        {
            CharacterInstanceId=new List<int>();
        }
        public int Unk1;
        public List<int> CharacterInstanceId;
        public int EventSlot;
        public int Unk2;
        public int Unk3;
        public int Unk4;
        public int Unk5;

        public override void Decode()
        {
            Unk1 = Stream.ReadVInt();
            Unk2 = ByteStreamHelper.ReadDataReference(Stream);//CharacterId
            EventSlot = Stream.ReadVInt();
            for (int i = Stream.ReadVInt(); i > 0; i--)
            {
                CharacterInstanceId.Add(ByteStreamHelper.ReadDataReference(Stream));
            }

            Unk3 = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 18977;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
