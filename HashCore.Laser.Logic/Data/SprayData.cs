using System;

namespace HashCore.Laser.Logic.Data
{
    public class SprayData : LogicData
    {
        public SprayData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool DisabledCN { get; set; }
        public string FileName { get; set; }
        public string ExportName { get; set; }
        public string Character { get; set; }
        public string Skin { get; set; }
        public bool GiveOnSkinUnlock { get; set; }
        public string Rarity { get; set; }
        public int EffectColorR { get; set; }
        public int EffectColorG { get; set; }
        public int EffectColorB { get; set; }
        public bool FlipSprayForEnemies { get; set; }
        public bool LockedForChronos { get; set; }
        public string SprayBundles { get; set; }
        public bool IsDefaultBattleSpray { get; set; }
        public string Texture { get; set; }
        public int PriceBling { get; set; }
        public int PriceGems { get; set; }
        public bool DisableCatalogRelease { get; set; }
        public int CatalogNewDaysAdjustment { get; set; }
        public string NotInCatalogTID { get; set; }
        public bool UseShineVFX { get; set; }
        public string ExtraCatalogCampaign { get; set; }
        public bool HideFromCatalogMainCategory { get; set; }
        public bool DisableFromCatalogIfNotOwned { get; set; }
    }
}
