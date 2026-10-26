namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
     
    public class LogicHeroWinQuestsChangedCommand : Command
    {

        public override void Encode(ByteStream stream)
        {
      
            stream.WriteVInt(0);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 220;
        }
    }
}
