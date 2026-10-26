namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Recruit;

    public class LogicBrawlerRecruitRoadChangedCommand : Command
    {
        public HomeMode homeMode;

        public BrawlerRecruitRoad Road;

        public int ServerCommandId = -1;

        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(ServerCommandId);
            base.Encode(stream);

            BrawlerRecruitRoad road = Road;
            if (road == null && homeMode != null && homeMode.Home != null)
            {
                road = BrawlerRecruitRoad.Build(homeMode.Home);
            }

            stream.WriteBoolean(road != null);
            if (road != null)
            {
                road.Encode(stream);
            }
        }

        public override void Decode(ByteStream stream)
        {
            ServerCommandId = stream.ReadVInt();
            base.Decode(stream);

            Road = null;
            if (stream.ReadBoolean())
            {
                Road = new BrawlerRecruitRoad();
                Road.Decode(stream);
            }
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 227;
        }
    }
}
