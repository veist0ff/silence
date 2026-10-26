namespace HashCore.Laser.Logic.Data
{
    public class EmoteBundleData : LogicData
    {
        public EmoteBundleData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }
        public string Name { get; set; }

        public bool CanCanBeBought { get; set; }
        public string RepresentingHighlightEmote { get; set; }

    }
}
