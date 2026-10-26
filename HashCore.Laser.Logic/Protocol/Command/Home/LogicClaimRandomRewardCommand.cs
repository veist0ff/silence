namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.RandomReward;

    public class LogicClaimRandomRewardCommand : Command
    {
        public int Context;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Context = stream.ReadVInt();
        }

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Context);
        }

        public override int Execute(HomeMode homeMode)
        {
            return RandomRewardManager.Claim(homeMode, Context);
        }

        public override int GetCommandType()
        {
            return 571;
        }
    }
}
