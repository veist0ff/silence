namespace HashCore.Laser.Logic.Friends
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Avatar.Structures;
    using HashCore.Laser.Logic.Listener;

    public class Friend
    {
        public long AccountId;
        public int Trophies;
        public PlayerDisplayData DisplayData;

        public int FriendState;
        public int FriendReason;

        [JsonIgnore] public ClientAvatar Avatar => LogicServerListener.Instance.GetAvatar(AccountId);

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(AccountId);

            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);

            if(AccountId>0)  stream.WriteInt(Avatar.Trophies); 
            else stream.WriteInt(0);
            stream.WriteInt(FriendState);
            stream.WriteInt(FriendReason);
            stream.WriteInt(0);//FriendReasonDetails???
            stream.WriteInt(0);

            stream.WriteBoolean(false); // Alliance entry

            stream.WriteString(null);
            if(AccountId>0) stream.WriteInt(LogicServerListener.Instance.IsPlayerOnline(AccountId) ? 0 : (int)(DateTime.UtcNow - Avatar.LastOnline).TotalSeconds); // Last online time
            else stream.WriteInt(0);
            stream.WriteInt(19);//RankedRankData::GetRankedRankDataByRank
            if (stream.WriteBoolean(DisplayData != null))
            {
                DisplayData.Encode(stream);
            }
            stream.WriteInt(0);
            stream.WriteInt(0);
            
        }
    }
}
