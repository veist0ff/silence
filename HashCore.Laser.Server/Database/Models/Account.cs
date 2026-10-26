
    using Newtonsoft.Json;
using HashCore.Laser.Logic.Avatar;
using HashCore.Laser.Logic.Home;

public class Account
    {
        [JsonProperty] public long AccountId;
        [JsonProperty] public string PassToken;

        [JsonProperty] public ClientHome Home;
        [JsonProperty] public ClientAvatar Avatar;

        public Account()
        {
            Home = new ClientHome();
            Avatar = new ClientAvatar();
        }
    }
