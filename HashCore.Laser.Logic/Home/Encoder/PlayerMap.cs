namespace HashCore.Laser.Logic.Home.Structures
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Data;

    using HashCore.Laser.Titan.Math;
    using HashCore.Laser.Logic.Home.Structures;

    [JsonObject(MemberSerialization.OptIn)]
    public class PlayerMap
    {

        [JsonProperty] public long MapId;
        [JsonProperty] public string MapName;
        [JsonProperty] public int GMV;
        [JsonProperty] public int MapEnvironmentData;
        [JsonProperty] public byte[] MapData;

        [JsonProperty] public long AccountId;
        [JsonProperty] public string AvatarName;

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.EncodeLogicLong(stream,MapId);
            stream.WriteString(MapName);
            stream.WriteVInt(GMV);
            stream.WriteDataReference(54, MapEnvironmentData);
            if (MapData != null) stream.WriteBytes(MapData, MapData.Length);
            else stream.WriteBytes(null, 1);
            ByteStreamHelper.EncodeLogicLong(stream,AccountId);
            stream.WriteString(AvatarName);
            stream.WriteVInt(1);//state 
            stream.WriteLong(0);//update time since epoch
            stream.WriteVInt(0);
            stream.WriteVInt(0);//freindly signoff count
            stream.WriteVInt(0);//likes
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }
    }
}
