namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Message.Home;
    
    public class LogicGatchaCommand : Command
    {
        public int BoxIndex;

        public LogicGatchaCommand() : base()
        {
            ;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            BoxIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 500;
        }
    }
}
