namespace HashCore.Laser.Logic.Data
{
    public class TitlesData : LogicData
    {
        public TitlesData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }

        public string TitleTID { get; set; }

        public string Gradient { get; set; }
    }
}
