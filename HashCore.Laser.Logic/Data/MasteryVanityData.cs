namespace HashCore.Laser.Logic.Data
{
    public class MasteryVanityData : LogicData
    {
        public MasteryVanityData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string RewardEmotes { get; set; }

        public string RewardPlayerIcons { get; set; }

        public string RewardTitles { get; set; }
    }
}
