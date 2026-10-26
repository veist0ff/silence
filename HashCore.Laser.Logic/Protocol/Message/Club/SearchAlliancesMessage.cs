namespace HashCore.Laser.Logic.Message.Club
{
    public class SearchAlliancesMessage : GameMessage
    {
        public string SearchTerm;
        public int Filter152;
        public int Filter156;
        public int MaxResults;
        public int Filter164;
        public bool FamilyFriendlyOnly;
        public int Filter176;
        public int Filter168;

        public override void Decode()
        {
            SearchTerm = Stream.ReadString();
            Filter152 = Stream.ReadInt();
            Filter156 = Stream.ReadInt();
            MaxResults = Stream.ReadInt();
            Filter164 = Stream.ReadInt();
            FamilyFriendlyOnly = Stream.ReadBoolean();
            Filter176 = Stream.ReadInt();
            Filter168 = Stream.ReadInt();
        }

        public override int GetMessageType()
        {
            return 14324;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
