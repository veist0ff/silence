namespace HashCore.Laser.Logic.Data
{
    // LogicDataFactory::createAutoData, 0x100401be0, case 116.
    public sealed class BrawlerBoxRewardData : LogicData
    {
        public BrawlerBoxRewardData(Row row, DataTable table) : base(row, table)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public int TicketsInDancerBrawlerBox { get; set; }
        public int TicketsInFuryBrawlerBox { get; set; }
        public int TicketsInDaredevilBrawlerBox { get; set; }
        public string TypeName { get; set; }
        public string SpecificBrawler { get; set; }
        public int TypeValue { get; set; }
        public bool InPremiumDraw { get; set; }
        public int TypePriceMin { get; set; }
        public int TypePriceMax { get; set; }
        public int AmountMin { get; set; }
        public int AmountMax { get; set; }
        public string FallbackTypeName { get; set; }
        public int FallbackAmount { get; set; }
        public string NoEventFallbackTypeName { get; set; }
        public int NoEventFallbackAmount { get; set; }
        public int TierForVisualization { get; set; }
        public string[] SkinsInReward { get; set; }
        public int[] TicketsInSkinDraw { get; set; }
        public string[] SpraysInReward { get; set; }
        public int[] TicketsInSprayDraw { get; set; }
        public string[] ProfilePicsInReward { get; set; }
        public int[] TicketsInProfilePicDraw { get; set; }
        public string[] PinsInReward { get; set; }
        public int[] TicketsInPinDraw { get; set; }
    }
}
