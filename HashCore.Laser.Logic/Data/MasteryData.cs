namespace HashCore.Laser.Logic.Data
{
    public class MasteryData : LogicData
    {
        public MasteryData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Level { get; set; }

        public int RewardCount_Common { get; set; }

        public string RewardType_Common { get; set; }

        public int RewardCount_Rare { get; set; }

        public string RewardType_Rare { get; set; }

        public int RewardCount_SuperRare { get; set; }

        public string RewardType_SuperRare { get; set; }

        public int RewardCount_Epic { get; set; }

        public string RewardType_Epic { get; set; }

        public int RewardCount_Mythic { get; set; }

        public string RewardType_Mythic { get; set; }

        public int RewardCount_Legendary { get; set; }

        public string RewardType_Legendary { get; set; }

        public int RewardCount_Chromatic { get; set; }

        public string RewardType_Chromatic { get; set; }

    }
}
