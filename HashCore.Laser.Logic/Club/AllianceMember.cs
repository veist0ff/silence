namespace HashCore.Laser.Logic.Club
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Avatar.Structures;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Listener;
   
    public class AllianceMember
    {
        [JsonProperty("displayData")] public PlayerDisplayData DisplayData { get; set; }
        [JsonProperty("accountId")] public long AccountId { get; set; }
        [JsonProperty("trophies")] public int Trophies { get; set; }
        [JsonProperty("role")] public AllianceRole Role { get; set; }

        [JsonIgnore] public ClientAvatar Avatar
        {
            get
            {
                return LogicServerListener.Instance.GetAvatar(AccountId);
            }
        }

        [JsonIgnore] public int CurrentTrophies
        {
            get
            {
                return Avatar?.Trophies ?? Trophies;
            }
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get
            {
                return LogicServerListener.Instance.IsPlayerOnline(AccountId);
            }
        }

        public AllianceMember()
        {
            // For json...
        }

        public AllianceMember(ClientAvatar avatar)
        {
            DisplayData = new PlayerDisplayData(avatar.HomeMode.Home.ThumbnailId, avatar.HomeMode.Home.NameColorId, avatar.Name);
            AccountId = avatar.AccountId;
            Trophies = avatar.Trophies;
            Role = avatar.AllianceRole;
        }

        public void Encode(ByteStream stream)
        {
            ClientAvatar avatar = Avatar;
            bool online = IsOnline;

            stream.WriteLong(AccountId);
            stream.WriteVInt((int)Role);
            stream.WriteVInt(avatar?.Trophies ?? Trophies);

            stream.WriteVInt(online && avatar != null ? avatar.PlayerStatus : 0); // PlayerStatus
            stream.WriteVInt(online ? -1 : SecondsSinceLastOnline(avatar));

            stream.WriteVInt(0); // ??

            stream.WriteBoolean(false);

            DisplayData ??= new PlayerDisplayData(0, 0, avatar?.Name ?? string.Empty);
            if (avatar != null) DisplayData.Name = avatar.Name;
            DisplayData.Encode(stream);

            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteVInt(0);//v53
             stream.WriteVInt(0);//v53
              stream.WriteVInt(0);//v53
               stream.WriteVInt(0);//v53

        }

        private static int SecondsSinceLastOnline(ClientAvatar avatar)
        {
            if (avatar == null) return 0;
            return (int)(DateTime.UtcNow - avatar.LastOnline).TotalSeconds;
        }
    }
}
