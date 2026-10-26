namespace HashCore.Laser.Logic.Command
{
    using HashCore.Laser.Logic.Home;

    public class LogicOnewayTriggerCommand : Command
    {
        public int TriggerId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            TriggerId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode.ActiveBoxType != 0)
            {
                homeMode.GenerateBoxReward(homeMode.ActiveBoxType);
                homeMode.ActiveBoxType = 0;
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 1;
        }
    }
}
