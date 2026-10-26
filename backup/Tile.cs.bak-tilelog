namespace HashCore.Laser.Logic.Battle.Level
{
    using Newtonsoft.Json.Bson;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    public class Tile
    {
        public readonly char Code;
        public readonly int X, Y;

        public int TileX => X / 300;
        public int TileY => Y / 300;
        public readonly TileData Data;

        private bool Destructed;

        public int LifeTime;

        public int PlacerIndex;

        public static int TileCodeToInstanceId(char code)
        {
            switch (code)
            {

                default:
                    return -1;
            }
        }
        public bool BlocksVision()
        {
            return Data.BlocksProjectiles;
        }
        public Tile(char code, int x, int y)
        {
            Code = code;
            X = x;
            Y = y;

            foreach (LogicData data in DataTables.Get(DataType.Tile).Datas)
            {
                TileData tileData = data as TileData;
                if (tileData != null)
                {
                    
                    if (tileData.TileCode[0] == code)
                    {
                        Data = tileData;
                       // if (tileData.TileCode == "B" && tileData != null && this != null) Destruct();
                        break;
                    }
                }
            }

            if (Data == null)
            {
                Data = DataTables.Get(DataType.Tile).GetData<TileData>(0);
            }
        }

        public Tile(int code, int x, int y)
        {
            X = x;
            Y = y;

            foreach (LogicData data in DataTables.Get(DataType.Tile).Datas)
            {
                TileData tileData = data as TileData;
                if (tileData != null)
                {
                    if (tileData.DynamicCode == code)
                    {
                        Data = tileData;
                        break;
                    }
                }
            }

            if (Data == null)
            {
                Data = DataTables.Get(DataType.Tile).GetData<TileData>(0);
            }
            LifeTime = Data.Lifetime;
        }

        public void Destruct(bool PiercesEnvironment=true)
        {
            if(Data.IsDestructible&&(PiercesEnvironment||Data.IsDestructibleNormalWeapon)) Destructed = true;
        }

        public void Construct()
        {
            Destructed = false;
        }

        public bool IsDestructed()
        {
            return Destructed;
        }

        public int GetCheckSum()
        {
            return 0;
        }
    }
}
