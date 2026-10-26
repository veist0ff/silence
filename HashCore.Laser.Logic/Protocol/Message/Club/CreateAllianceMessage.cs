namespace HashCore.Laser.Logic.Message.Club
{
     
    public class CreateAllianceMessage : GameMessage
    {
        public string Name;
        public string Description;
        public int BadgeId;
        public int RegionId;
        public int Type;
        public int RequiredTrophies;
        public bool FamilyFriendly;
        public bool UseGems;

        public override void Decode()
        {
            Name = Stream.ReadString();
            Description = Stream.ReadString();
            BadgeId = ByteStreamHelper.ReadDataReference(Stream);
            RegionId = ByteStreamHelper.ReadDataReference(Stream);
            Type = Stream.ReadVInt();
            RequiredTrophies = Stream.ReadVInt();
            FamilyFriendly = Stream.ReadBoolean();
            UseGems = Stream.ReadBoolean();
        }

        public override int GetMessageType()
        {
            return 14301;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
