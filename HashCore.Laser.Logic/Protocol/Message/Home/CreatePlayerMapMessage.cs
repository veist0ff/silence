 
namespace HashCore.Laser.Logic.Message.Home
{
    public class CreatePlayerMapMessage : GameMessage
    {
        public string name;
        public int GMV;
        public int PMED;
        public override void Decode()
        {
            name= Stream.ReadString();
            GMV=Stream.ReadVInt();
            PMED= GlobalId.GetInstanceId( ByteStreamHelper.ReadDataReference(Stream));
        }

        public override int GetMessageType()
        {
            return 12100;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
