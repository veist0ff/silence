namespace HashCore.Laser.Logic.Message.Ranking
{
    public class LeaderboardMessage : GameMessage
    {
        public int LeaderboardType { get; set; }

        public int Season { get; set; }

        public int HeroDataId { get; set; }

        public string? Region { get; set; }

        public List<RankingEntry> Entries { get; }

        public int OwnScore { get; set; } = -1;

        public int OwnRank { get; set; } = -1;

        public int Unknown { get; set; }

        public int RegionId { get; set; }

        public string? CountryCode { get; set; }

        public LeaderboardMessage() : base()
        {
            Entries = new List<RankingEntry>();
            LeaderboardType = 1;
        }

        public override void Encode()
        {
            Stream.WriteVInt(LeaderboardType);
            Stream.WriteVInt(Season);
            ByteStreamHelper.WriteDataReference(Stream, HeroDataId);
            Stream.WriteString(Region);

            Stream.WriteVInt(Entries.Count);
            foreach (RankingEntry entry in Entries)
                entry.Encode(Stream);

            Stream.WriteVInt(OwnScore);
            Stream.WriteVInt(OwnRank);
            Stream.WriteVInt(Unknown);
            Stream.WriteVInt(RegionId);
            Stream.WriteString(CountryCode);
        }

        public override void Decode()
        {
            base.Decode();

            Entries.Clear();
            LeaderboardType = Stream.ReadVInt();
            Season = Stream.ReadVInt();
            HeroDataId = ByteStreamHelper.ReadDataReference(Stream);
            Region = Stream.ReadString(900000);

            var count = Stream.ReadVInt();
            if (count < 0 || count > 900000)
                throw new InvalidDataException($"Invalid leaderboard entry count: {count}");
            for (var i = 0; i < count; i++)
            {
                var entry = new RankingEntry();
                entry.Decode(Stream);
                Entries.Add(entry);
            }

            OwnScore = Stream.ReadVInt();
            OwnRank = Stream.ReadVInt();
            Unknown = Stream.ReadVInt();
            RegionId = Stream.ReadVInt();
            CountryCode = Stream.ReadString(900000);
        }

        public override int GetMessageType()
        {
            return 24403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
