namespace HashCore.Laser.Logic.Data
{
    public class EmoteData : LogicData
    {
        public EmoteData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool DisabledCN { get; set; }
        public string IconSWF { get; set; }
        public string IconExportName { get; set; }
        public string Character { get; set; }
        public string Skin { get; set; }
        public bool GiveOnSkinUnlock { get; set; }
        public bool IsPicto { get; set; }
        public string BattleCategory { get; set; }
        public string Rarity { get; set; }
        public string EmoteType { get; set; }
        public string SoundEffect { get; set; }
        public int SfxIndex { get; set; }
        public bool LockedForChronos { get; set; }
        public string EmoteBundles { get; set; }
        public bool IsDefaultBattleEmote { get; set; }
        public int PriceBling { get; set; }
        public int PriceGems { get; set; }
        public bool DisableCatalogRelease { get; set; }
        public int CatalogNewDaysAdjustment { get; set; }
        public string NotInCatalogTID { get; set; }
        public string ExtraCatalogCampaign { get; set; }
        public bool HideFromCatalogRarityCategory { get; set; }
        public string UnlockedByFame { get; set; }
    }
}
