namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
    using HashCore.Laser.Server;

    public class LogicEditBattlePassCommand : Command
    {
        public int CharacterId;
        public int VanityId;
        public bool unk1;
        public int Index;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterId=ByteStreamHelper.ReadDataReference(stream);
            VanityId=ByteStreamHelper.ReadDataReference(stream);
            unk1 = stream.ReadBoolean();
            Index = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (VanityId != 0 && !Register(homeMode.Home, VanityId)) return 0;

            switch (Index)
            {
                case 0:
                    homeMode.Home.DefaultBattleCard.Thumbnail1 = VanityId;
                    break;
                case 1:
                    homeMode.Home.DefaultBattleCard.Thumbnail2 = VanityId;
                    break;
                case 5:
                    homeMode.Home.DefaultBattleCard.Emote = VanityId;
                    break;
                case 10:
                    homeMode.Home.DefaultBattleCard.Title = VanityId;
                    break;
                case 15:
                    homeMode.Home.DefaultBattleCard.Frames = VanityId;
                    break;
            }
            return 0;
        }

        // Клиент ищет надетую вещь в VanityItems из OHD. Если её там нет, домашний
        // экран падает, поэтому надетое сразу добавляем в список разблокированного.
        private static bool Register(ClientHome home, int globalId)
        {
            List<int> target;
            switch (GlobalId.GetClassId(globalId))
            {
                case 28: target = home.UnlockedThumbnails; break;
                case 52: target = home.UnlockedEmotes; break;
                case 76: target = home.UnlockedTituls; break;
                case 85: target = home.UnlockedFrames; break;
                default: return false;
            }

            if (target == null) return false;
            if (!target.Contains(globalId)) target.Add(globalId);
            return true;
        }

        public override int GetCommandType()
        {
            return 568;
        }
    }
}
