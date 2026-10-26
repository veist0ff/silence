namespace HashCore.Laser.Logic.Data
{
    public class CardData : LogicData
    {
        public CardData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public string LinkedCard { get; set; }
        public string IconSWF { get; set; }
        public string IconExportName { get; set; }
        public string Target { get; set; }
        public string TargetClassArchetype { get; set; }
        public bool Disabled { get; set; }
        public bool LockedForChronos { get; set; }
        public int RequiredChromatics { get; set; }
        public int MetaType { get; set; }
        public string RequiresCard { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Skill { get; set; }
        public int Value { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }
        public int Value5 { get; set; }
        public int Value6 { get; set; }
        public string StatusEffect { get; set; }
        public string StatusEffectOvercharged { get; set; }
        public string AreaEffect { get; set; }
        public string Projectiles { get; set; }
        public string Traits { get; set; }
        public string Components { get; set; }
        public string Rarity { get; set; }
        public string TID { get; set; }
        public string OverrideDescription { get; set; }
        public string PowerNumberTID { get; set; }
        public string PowerNumber2TID { get; set; }
        public string PowerNumber3TID { get; set; }
        public string PowerNumber4TID { get; set; }
        public string PowerIcon1ExportName { get; set; }
        public string PowerIcon2ExportName { get; set; }
        public int SortOrder { get; set; }
        public bool DontUpgradeStat { get; set; }
        public bool HideDamageStat { get; set; }
        public int DirectPurchasePrice { get; set; }
        public string RogueliteDeck { get; set; }
    }
}
