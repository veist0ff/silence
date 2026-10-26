namespace HashCore.Laser.Logic.Data
{
    public class GearData : LogicData
    {
        public GearData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public string Rarity { get; set; }

        public string[] ExtraHerosAvailableTo { get; set; }

        public int LogicType { get; set; }

        public int ModifierValue { get; set; }

        public string ModifierType { get; set; }

        //public int DamageBoost { get; set; }

        //public int VisionTicks { get; set; }

        //public int HealthShield { get; set; }
    }
}
