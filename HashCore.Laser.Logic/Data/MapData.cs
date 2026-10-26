namespace HashCore.Laser.Logic.Data
{
    public class MapData : LogicData
    {
        public MapData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }
    }
}
