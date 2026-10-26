namespace HashCore.Laser.Logic.Data
{
    public class RankedMilestones : LogicData
    {
        public RankedMilestones(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public int Type { get; set; }

        public int Index { get; set; }

        public int ProgressStart { get; set; }

        public int Progress { get; set; }

        public int Season { get; set; }

        public string PrimaryRewardType { get; set; }

        public int PrimaryRewardCount { get; set; }

        public int PrimaryRewardExtraData { get; set; }

        public string PrimaryRewardData { get; set; }

        public string SecondaryRewardType { get; set; }

        public int SecondaryRewardCount { get; set; }

        public int SecondaryRewardExtraData { get; set; }

        public string SecondaryRewardData { get; set; }
    }
}
