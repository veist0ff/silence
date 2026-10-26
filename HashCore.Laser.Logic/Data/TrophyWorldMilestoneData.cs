using HashCore.Laser.Logic.Data.Helper;
using HashCore.Laser.Logic.Data.Reader;

namespace HashCore.Laser.Logic.Data
{
    public class TrophyWorldMilestoneData : LogicData
    {
        public TrophyWorldMilestoneData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public int Index { get; set; }
        public int TrophyCount { get; set; }
        public string RewardType { get; set; }
        public int RewardCount { get; set; }
        public int RewardExtraData { get; set; }
        public string RewardData { get; set; }
        public string FallbackRewardType { get; set; }
        public int FallbackRewardCount { get; set; }
        public int FallbackRewardExtraData { get; set; }
        public string FallbackRewardData { get; set; }
    }
}
