using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.RandomReward;

public class LogicRefreshRandomRewardsCommand : Command
{
    public int ServerCommandId = -1;
    public RandomRewardManager Manager;

    public override void Encode(ByteStream stream)
    {
        stream.WriteVInt(ServerCommandId);
        base.Encode(stream);

        stream.WriteBoolean(Manager != null);
        if (Manager != null) Manager.Encode(stream);
    }

    public override void Decode(ByteStream stream)
    {
        ServerCommandId = stream.ReadVInt();
        base.Decode(stream);

        Manager = null;
        if (stream.ReadBoolean())
        {
            Manager = new RandomRewardManager();
            Manager.Decode(stream);
        }
    }

    public override int Execute(HomeMode homeMode)
    {
        return 0;
    }

    public override int GetCommandType()
    {
        return 228;
    }
}
