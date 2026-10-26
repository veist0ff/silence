namespace HashCore.Laser.Logic.Message.Club
{
     
    public class ChangeAllianceSettingsMessage : GameMessage
    {
        public string Description;
        public int BadgeId;
        public int RegionId;
        public int Type;
        public int RequiredTrophies;
        public bool FamilyFriendly;

        public override void Decode()
        {
            Description = Stream.ReadString();

            BadgeId = ByteStreamHelper.ReadDataReference(Stream);
            RegionId = ByteStreamHelper.ReadDataReference(Stream);

            Type = Stream.ReadVInt();
            RequiredTrophies = Stream.ReadVInt();
            FamilyFriendly = Stream.ReadBoolean();
        }

        public override int GetMessageType()
        {
            return 14316;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
