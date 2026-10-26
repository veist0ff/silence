namespace HashCore.Laser.Logic.Data
{
    public class RandomRewardContainerData : LogicData
    {
        public RandomRewardContainerData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public string TID { get; set; }
        public int TicketsInWinGamesDraw { get; set; }
        public int TicketsInCollabDraw { get; set; }
        public int TicketsInPresentDraw { get; set; }
        public int Rarity { get; set; }
        public bool IsRarityOfferContainer { get; set; }
        public string GroupType { get; set; }
        public int CollabId { get; set; }
        public bool IsOverchargeDraw { get; set; }
        public bool IsRankedDraw { get; set; }
        public int MultiDropCount { get; set; }
        public int PremiumDrawCountForMultiDrops { get; set; }
        public string VisualType { get; set; }
        public string SoloOfferDefaultBgFrameName { get; set; }
        public string RarityFrameLabel { get; set; }
    }
}
