namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Message.Home;
    
    public class LogicClaimTailRewardCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            stream.ReadVInt();
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 535;
        }
    }
}
