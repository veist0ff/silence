namespace HashCore.Laser.Logic.Command.Avatar
{
    using HashCore.Laser.Logic.Home;
     
    public class LogicDiamondsAddedCommand : Command
    {
        public int Amount;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Avatar.AddDiamonds(Amount);
            return 0;
        }

        public override int GetCommandType()
        {
            return 202;
        }
    }
}
