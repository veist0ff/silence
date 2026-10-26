using HashCore.Laser.Logic.Home;
 
namespace HashCore.Laser.Logic.Command.Home
{

    public class LogicSetSupportedCreatorCommand : Command
    {
        public string Name;
        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(1);
            stream.WriteString(Name);
            stream.WriteVInt(1);
            base.Encode(stream);

        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 215;
        }
    }
}
