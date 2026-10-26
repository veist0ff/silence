namespace HashCore.Laser.Logic.Data
{
    public class LocationData : LogicData
    {
        public LocationData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string TID { get; set; }
        public string LocationTheme { get; set; }
        public bool SupportingCampaignGround { get; set; }
        public string BannerOverrideSWF { get; set; }
        public string BannerOverrideExportName { get; set; }
        public string GameModeVariation { get; set; }
        public string Map { get; set; }
        public string CommunityCredit { get; set; }
        public bool TrainingGroundsEnabled { get; set; }
        public string RecommendedBrawler0 { get; set; }
        public string RecommendedBrawler1 { get; set; }
        public string RecommendedBrawler2 { get; set; }
        public string RecommendedBrawler3 { get; set; }
        public string RecommendedBrawler4 { get; set; }
        public string RecommendedBrawler5 { get; set; }
        public string RecommendedBrawler6 { get; set; }
        public string RecommendedBrawler7 { get; set; }
        public string RecommendedBrawler8 { get; set; }
        public string RecommendedBrawler9 { get; set; }
    }
}
