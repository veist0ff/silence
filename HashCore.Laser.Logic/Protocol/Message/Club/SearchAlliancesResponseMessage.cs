namespace HashCore.Laser.Logic.Message.Club
{
    using HashCore.Laser.Logic.Club;

    public class SearchAlliancesResponseMessage : GameMessage
    {
        public string SearchTerm;

        public readonly List<AllianceHeader> Alliances = new List<AllianceHeader>();

        public override void Encode()
        {
            Stream.WriteString(SearchTerm);

            Stream.WriteVInt(Alliances.Count);
            foreach (AllianceHeader alliance in Alliances) alliance.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 24310;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
