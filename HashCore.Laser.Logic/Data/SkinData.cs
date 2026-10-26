namespace HashCore.Laser.Logic.Data
{
    public class SkinData : LogicData
    {
        public SkinData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public string Conf { get; set; }
        public bool Disabled { get; set; }
        public string Campaigns { get; set; }
        public int UpdateNumber { get; set; }
        public string IconOverrideSWF { get; set; }
        public string IconOverrideExportName { get; set; }
        public int SkinGroupId { get; set; }
        public int ObtainType { get; set; }
        public int ObtainTypeCN { get; set; }
        public bool LastChance { get; set; }
        public string MasterSkin { get; set; }
        public string ProgressionSkinBase { get; set; }
        public int ProgressionSkinLevel { get; set; }
        public bool ProgressionSkinFinal { get; set; }
        public string PetSkin { get; set; }
        public string PetSkin2 { get; set; }
        public string OverchargePetSkin { get; set; }
        public string OverchargePetSkin2 { get; set; }
        public int PriceClubCoins { get; set; }
        public int PriceStarPoints { get; set; }
        public int PriceBling { get; set; }
        public bool CanDropWithoutBlingPrice { get; set; }
        public int PriceGems { get; set; }
        public int DiscountPriceGems { get; set; }
        public int PriceCoins { get; set; }
        public string Rarity { get; set; }
        public string TID { get; set; }
        public string ShopTID { get; set; }
        public string Features { get; set; }
        public string CommunityCredit { get; set; }
        public string MaterialsFile { get; set; }
        public string DiffuseTexture { get; set; }
        public string SpecularTexture { get; set; }
        public string OutlineShader { get; set; }
        public string PackOfferAnimOverride { get; set; }
        public int BattleIntroXOffset { get; set; }
        public int BattleIntroZOffset { get; set; }
        public bool BattleIntroVFX { get; set; }
        public bool DisableCatalogRelease { get; set; }
        public string CatalogPreRequirementSkin { get; set; }
        public int CatalogNewDaysAdjustment { get; set; }
        public string NotInCatalogTID { get; set; }
        public bool PreciseCollision { get; set; }
    }
}
