namespace HashCore.Laser.Logic.Club
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Data.Helper;

    public class AllianceHeader
    {
        private long Id;

        private string Name;
        private int Badge;
        private int PlayersCount;
        private int Trophies;
        private int RequiredTrophies;

        private int Type;
        private int RegionId;
        private string Region;
        private bool FamilyFriendly;

        public AllianceHeader(long id, string name, int badge, int playersCount, int trophies,
            int requiredTrophies, int type, int regionId = 0, string region = "RU", bool familyFriendly = false)
        {
            Id = id;
            Name = name;
            Badge = badge;
            PlayersCount = playersCount;
            Trophies = trophies;
            RequiredTrophies = requiredTrophies;

            Type = type;
            RegionId = regionId;
            Region = region;
            FamilyFriendly = familyFriendly;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(Id);
            stream.WriteString(Name);
            ByteStreamHelper.WriteDataReference(stream, Badge);
            stream.WriteVInt(Type);
            stream.WriteVInt(PlayersCount);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(RequiredTrophies);
            ByteStreamHelper.WriteDataReference(stream, RegionId);
            stream.WriteString(Region ?? "RU");
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteBoolean(FamilyFriendly);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }
    }
}
