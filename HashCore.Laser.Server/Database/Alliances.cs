
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using HashCore.Laser.Logic.Club;
using HashCore.Laser.Server.Database.Cache;

    public static class Alliances
    {
        private static long AllianceIdCounter;
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

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            ConnectionString = builder.ToString();

            AllianceCache.Init();

            AllianceIdCounter = GetMaxAllianceId();
        }

        public static long GetMaxAllianceId()
        {
            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand("SELECT coalesce(MAX(Id), 0) FROM alliances", Connection);

            long result = Convert.ToInt64(command.ExecuteScalar());
            Connection.Close();
            return result;
        }

        public static void Create(Alliance alliance)
        {
            if (alliance == null) return;
            alliance.Id = ++AllianceIdCounter;
            string json = JsonConvert.SerializeObject(alliance);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"INSERT INTO alliances (`Id`, `Name`, `Trophies`, `Data`) VALUES ({(long)alliance.Id}, @name, {alliance.Trophies}, @data)", Connection);
            command.Parameters?.AddWithValue("@data", json);
            command.Parameters?.AddWithValue("@name", alliance.Name);
            command.ExecuteNonQuery();
            Connection.Close();

            AllianceCache.Cache(alliance);
        }

        public static void Save(Alliance alliance)
        {
            if (alliance == null) return;

            string json = JsonConvert.SerializeObject(alliance);

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"UPDATE alliances SET `Trophies`='{alliance.Trophies}', `Data`=@data WHERE Id = '{(long)alliance.Id}'", Connection);
            command.Parameters?.AddWithValue("@data", json);
            command.ExecuteNonQuery();
            Connection.Close();
        }

        public static Alliance Load(long id)
        {
            if (AllianceCache.IsAllianceCached(id))
            {
                return AllianceCache.GetAlliance(id);
            }

            var Connection = new MySqlConnection(ConnectionString);
            Connection.Open();
            MySqlCommand command = new MySqlCommand($"SELECT * FROM alliances WHERE Id = '{id}'", Connection);
            MySqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                Alliance alliance = JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]);
                AllianceCache.Cache(alliance);
                Connection.Close();
                return alliance;
            }
            Connection.Close();
            return null;
        }

        public static List<Alliance> GetRankingList()
        {
            #region GetGlobal

            var list = new List<Alliance>();
            //return list;
            try
            {
                using (var connection = new MySqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand($"SELECT * FROM alliances ORDER BY `Trophies` DESC LIMIT 200",
                        connection))
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                            list.Add(JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]));
                    }

                    connection.Close();
                }

                return list;
            }
            catch (Exception exception)
            {
                return list;
            }

            #endregion
        }

        public static List<Alliance> GetRandomAlliances(int maxCount)
        {
            List<Alliance> result = new List<Alliance>();
            int limit = Math.Clamp(maxCount, 1, 50);

            try
            {
                using MySqlConnection connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using MySqlCommand command = new MySqlCommand(
                    $"SELECT `Data` FROM alliances ORDER BY RAND() LIMIT {limit}", connection);
                using MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Alliance alliance = JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]);
                    if (alliance != null) result.Add(alliance);
                }
            }
            catch (Exception exception)
            {
                HashCore.Laser.Server.Logger.Error($"[Alliances::GetRandomAlliances] {exception.Message}");
            }

            return result;
        }

        public static List<Alliance> Search(string searchTerm, int maxCount)
        {
            List<Alliance> result = new List<Alliance>();
            string term = searchTerm?.Trim();
            if (string.IsNullOrEmpty(term)) return result;

            int limit = Math.Clamp(maxCount, 1, 50);

            try
            {
                using MySqlConnection connection = new MySqlConnection(ConnectionString);
                connection.Open();

                using MySqlCommand command = new MySqlCommand(
                    $"SELECT `Data` FROM alliances WHERE `Name` LIKE @term ORDER BY `Trophies` DESC LIMIT {limit}",
                    connection);
                command.Parameters.AddWithValue("@term", "%" + term + "%");

                using MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Alliance alliance = JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]);
                    if (alliance != null) result.Add(alliance);
                }
            }
            catch (Exception)
            {
                // An empty result is a valid search response; do not tear down the session.
            }

            return result;
        }
    }
