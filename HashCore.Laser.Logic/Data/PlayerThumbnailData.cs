using System;

namespace HashCore.Laser.Logic.Data
{
    public class PlayerThumbnailData : LogicData
    {
        public PlayerThumbnailData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool DisabledCN { get; set; }
        public int LegacyExpLevelLimit { get; set; }
        public int RequiredTotalTrophies { get; set; }
        public string RequiredHero { get; set; }
        public string IconSWF { get; set; }
        public string IconExportName { get; set; }
        public int SortOrder { get; set; }
        public bool IsReward { get; set; }
        public bool IsAvailableForOffers { get; set; }
        public bool LockedForChronos { get; set; }
        public int PriceBling { get; set; }
        public int PriceGems { get; set; }
        public bool DisableCatalogRelease { get; set; }
        public bool HideInCatalogWhenNotOwned { get; set; }
        public int CatalogNewDaysAdjustment { get; set; }
        public string CatalogPreRequirementSkin { get; set; }
        public bool GiveOnSkinUnlock { get; set; }
        public string NotInCatalogTID { get; set; }
        public string ExtraCatalogCampaign { get; set; }
        public string ExtraCatalogHero { get; set; }
        public bool HideFromCatalogMainCategory { get; set; }
        public string UnlockedByFame { get; set; }
    }
}
