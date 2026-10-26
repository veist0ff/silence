 
namespace HashCore.Laser.Logic.Message.Ranking
{
    public class GetLeaderboardMessage : GameMessage
    {
        public bool IsRegional { get; set; }
        public int LeaderboardType { get; set; }
        public int HeroDataId { get; set; }
        public int Season { get; set; }

        public override void Encode()
        {
            base.Encode();

            Stream.WriteBoolean(IsRegional);
            Stream.WriteVInt(LeaderboardType);
            ByteStreamHelper.WriteDataReference(Stream, HeroDataId);
            Stream.WriteVInt(Season);
        }

        public override void Decode()
        {
            base.Decode();

            IsRegional = Stream.ReadBoolean();
            LeaderboardType = Stream.ReadVInt();
            HeroDataId = ByteStreamHelper.ReadDataReference(Stream);
            Season = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
