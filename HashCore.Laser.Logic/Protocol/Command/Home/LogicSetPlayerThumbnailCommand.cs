namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    
    public class LogicSetPlayerThumbnailCommand : Command
    {
        public int ThumbnailInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            ThumbnailInstanceId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (ThumbnailInstanceId < 0) return 1;

            int globalId = GlobalId.CreateGlobalId(28, ThumbnailInstanceId);
            if (DataTables.Get(DataType.PlayerThumbnail)?.GetDataByGlobalId<PlayerThumbnailData>(globalId) == null) return 2;

            homeMode.Home.ThumbnailId = globalId;
            if (homeMode.Home.UnlockedThumbnails == null) homeMode.Home.UnlockedThumbnails = new List<int>();
            if (!homeMode.Home.UnlockedThumbnails.Contains(globalId)) homeMode.Home.UnlockedThumbnails.Add(globalId);

            if (homeMode.Avatar.Friends.Count > 0)
            {
                foreach (var friend in homeMode.Avatar.Friends)
                {
                    var entry = friend.Avatar?.Friends?.Find(x => x.AccountId == homeMode.Avatar.AccountId);
                    if (entry == null) continue;
                    entry.DisplayData = new Logic.Avatar.Structures.PlayerDisplayData(homeMode.Home.ThumbnailId, homeMode.Home.NameColorId, homeMode.Avatar.Name);
                }
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 505;
        }
    }
}
