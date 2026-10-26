namespace HashCore.Laser.Logic.Battle.Structures
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Data;
     
    using HashCore.Laser.Titan.Math;
    using HashCore.Laser.Logic.Home.Structures;

    public class BattlePlayerMap
    {

        public long MapId;
        public string MapName;
        public int GMV;
        public int MapEnvironmentData;
        public byte[] MapData;

        public long AccountId;
        public string AvatarName;

        public BattlePlayerMap(PlayerMap map) 
        {
            MapId = map.MapId;
            MapName = map.MapName;
            MapEnvironmentData = map.MapEnvironmentData;
            AccountId = map.AccountId;
            AvatarName = map.AvatarName;
            MapData = map.MapData;
            AccountId = map.AccountId;  
            GMV = map.GMV;
        }

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.EncodeLogicLong(stream,MapId);
            stream.WriteString(MapName);
            stream.WriteVInt(GMV);
            stream.WriteDataReference(54, MapEnvironmentData);
            stream.WriteDataReference(0);
            if (MapData != null) stream.WriteBytes(MapData, MapData.Length);
            else stream.WriteBytes(null, 1);
            ByteStreamHelper.EncodeLogicLong(stream,AccountId);
            stream.WriteString(AvatarName);
            stream.WriteVInt(1);//state 
            stream.WriteVInt(-1);
        }
    }
}
