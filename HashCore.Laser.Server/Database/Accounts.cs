using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Al
{
    public static class Accounts
    {
        private static long AvatarIdCounter;
        private static string ConnectionString;

        public static void Init(string user, string password)
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = "127.0.0.1";
            builder.UserID = user;
            builder.Password = password;
            builder.SslMode = MySqlSslMode.Disabled;
            builder.Database = Configuration.Instance.DatabaseName;
            builder.CharacterSet = "utf8mb4";
            builder.AllowPublicKeyRetrieval = true;

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            ConnectionString = builder.ToString();

            AccountCache.Init();

            AvatarIdCounter = GetMaxAvatarId();
        }

        public static long GetMaxAvatarId()
        {
            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand("SELECT coalesce(MAX(Id), 0) FROM accounts", Connection);

            long result = Convert.ToInt64(command.ExecuteScalar());
            Connection.Close();
            return result;
        }

        public static Account Create()
        {
            Account account = new Account();
            account.Avatar.TutorialsCompletedCount = 0; // новый аккаунт начинает с обучения, как в оригинале
            account.AccountId = ++AvatarIdCounter;
            account.PassToken = Helpers.RandomString(40);

            account.Avatar.AccountId = account.AccountId;
            account.Avatar.PassToken = account.PassToken;

            account.Home.HomeId = account.AccountId;

            Hero hero = new Hero(16000000);
            account.Avatar.Heroes.Add(hero);
            //hero.SelectedGadgetId=
            string json;
            lock (account.Home.RewardSync) json = JsonConvert.SerializeObject(account);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"INSERT INTO accounts (`Id`, `Trophies`, `Data`) VALUES ({(long)account.AccountId}, {account.Avatar.Trophies}, @data)", Connection);
            command.Parameters?.AddWithValue("@data", json);
            command.ExecuteNonQuery();
            Connection.Close();

            AccountCache.Cache(account);

            return account;
        }

        public static void Save(Account account)
        {
            if (account == null) return;

            string json;
            lock (account.Home.RewardSync) json = JsonConvert.SerializeObject(account);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"UPDATE accounts SET `Trophies`='{account.Avatar.Trophies}', `Data`=@data WHERE Id = '{account.AccountId}'", Connection);
            command.Parameters?.AddWithValue("@data", json);
            command.ExecuteNonQuery();
            Connection.Close();
        }

        public static Account Load(long id)
        {
            if (AccountCache.IsAccountCached(id))
            {
                return AccountCache.GetAccount(id);
            }

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"SELECT * FROM accounts WHERE Id = '{id}'", Connection);
            MySqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                Account account;

                try
                {
                    account = JsonConvert.DeserializeObject<Account>((string)reader["Data"]);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Account {id} could not be deserialized: {exception.Message}");
                    Connection.Close();
                    return null;
                }

                if (account == null) return null;
                AccountCache.Cache(account);
                Connection.Close();
                return account;
            }
            Connection.Close();
            return null;
        }

        public static List<Account> GetRankingList()
        {
            #region GetGlobal

            List<Account> list = Query("SELECT `Data` FROM accounts ORDER BY `Trophies` DESC LIMIT 200");

            list.Sort(delegate (Account a, Account b)
            {
                return b.Avatar.Trophies - a.Avatar.Trophies;
            });

            return list;

            #endregion
        }

        public static List<Account> GetBrawlerRankingList(int HeroDataId)
        {
            #region GetGlobal

            List<Account> list = Query("SELECT `Data` FROM accounts ORDER BY `Trophies` DESC LIMIT 500");

            list.RemoveAll(account => !account.Avatar.HasHero(HeroDataId));

            list.Sort(delegate (Account a, Account b)
            {
                return b.Avatar.GetHero(HeroDataId).Trophies - a.Avatar.GetHero(HeroDataId).Trophies;
            });

            if (list.Count > 200) list.RemoveRange(200, list.Count - 200);

            return list;

            #endregion
        }

        private static List<Account> Query(string sql)
        {
            var list = new List<Account>();

            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            try
                            {
                                Account account = JsonConvert.DeserializeObject<Account>((string)reader["Data"]);
                                if (account == null || account.Avatar == null) continue;
                                list.Add(account);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine("Skipped a ranking row: " + exception.Message);
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Ranking query failed: " + exception.Message);
            }

            return list;
        }
    }
}
