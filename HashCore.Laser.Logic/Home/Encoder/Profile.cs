namespace HashCore.Laser.Logic.Home.Structures
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Avatar.Structures;
    using HashCore.Laser.Server;
    using HashCore.Laser.Titan.Math;

    public class Profile
    {
        public PlayerDisplayData DisplayData;
        public long AccountId;
        public Hero[] Heroes;
        public List<LogicVector2> Stats;
        public int Hero;
        public int HeroSkin;
        public int T1;
        public int T2;
        public int E;
        public int Ti;
        public int Frames;

        public Profile()
        {
            DisplayData = new PlayerDisplayData();
            Stats = new List<LogicVector2>();
        }

        public void AddStat(int key, int value)
        {
            Stats.Add(new LogicVector2(key, value));
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVLong(AccountId);
            ByteStreamHelper.WriteDataReference(stream, Hero);
            ByteStreamHelper.WriteDataReference(stream, HeroSkin);

            stream.WriteVInt(Heroes.Length);
            foreach (Hero hero in Heroes)
            {
                hero.Encode(stream);
            }

            stream.WriteVInt(Stats.Count);
            foreach (LogicVector2 stat in Stats)
            {
                stat.Encode(stream);
            }

            DisplayData.Encode(stream);

            stream.WriteBoolean(false);

            stream.WriteString(string.Empty);
            stream.WriteVInt(100);
            stream.WriteVInt(200);
            stream.WriteVInt(0);

            // Окно профиля рисует обе иконки и эмодзи карточки без проверки на null
            // (флагов «по умолчанию», как в BattleCard, тут нет): пустая или битая
            // ссылка — вылет при открытии профиля. Подставляем первую строку таблицы.
            ByteStreamHelper.WriteDataReference(stream, ValidOr(T1, 28000000));
            ByteStreamHelper.WriteDataReference(stream, ValidOr(T2, 28000000));
            ByteStreamHelper.WriteDataReference(stream, ValidOr(E, 52000000));
            ByteStreamHelper.WriteDataReference(stream, Ti);
            ByteStreamHelper.WriteDataReference(stream, Frames);
        }

        /// <summary>Очки ранкеда аккаунта. Пока игра их не начисляет — «Про» (99339), как на главном экране.</summary>
        private static int ValidOr(int globalId, int fallback)
        {
            if (globalId > 0)
            {
                var table = HashCore.Laser.Logic.Data.DataTables.Get(GlobalId.GetClassId(globalId));
                if (table != null && table.GetDataByGlobalId<HashCore.Laser.Logic.Data.Helper.LogicData>(globalId) != null)
                    return globalId;
            }
            return fallback;
        }

        public static int RankedPointsOf(ClientAvatar avatar)
        {
            if (avatar == null || avatar.RankedPoins <= 0) return 99339;
            return avatar.RankedPoins;
        }

        private static int[] s_rankMinElo;

        /// <summary>
        /// Номер ранга ранкеда (колонка Rank из ranked_ranks.csv: 1 — Бронза I … 22 — Про)
        /// по очкам аккаунта и порогам MinELO. По нему клиент рисует иконку ранга
        /// (LogicDataTables::getRankedRankDataByRank).
        /// </summary>
        public static int RankedRankOf(ClientAvatar avatar)
        {
            int points = RankedPointsOf(avatar);
            if (s_rankMinElo == null)
            {
                var list = new System.Collections.Generic.List<int>();
                try
                {
                    string[] lines = System.IO.File.ReadAllLines("Assets/csv_logic/ranked_ranks.csv");
                    for (int i = 2; i < lines.Length; i++)
                    {
                        string[] c = lines[i].Split(',');
                        if (c.Length > 2 && int.TryParse(c[2].Trim('"'), out int min)) list.Add(min);
                    }
                }
                catch { }
                s_rankMinElo = list.ToArray();
            }
            if (s_rankMinElo.Length == 0) return 22;
            int rank = 1;
            for (int i = 0; i < s_rankMinElo.Length; i++) if (points >= s_rankMinElo[i]) rank = i + 1;
            return rank;
        }

        public static Profile Create(ClientHome home, ClientAvatar avatar)
        {
            Profile profile = new Profile();

            profile.AccountId = avatar.AccountId;
            
            PlayerDisplayData playerDisplayData = new PlayerDisplayData();
            playerDisplayData.ThumbnailId = home.ThumbnailId;
            playerDisplayData.NameColorId = home.NameColorId;
            playerDisplayData.Name = avatar.Name;
            profile.DisplayData = playerDisplayData;

            profile.Heroes = avatar.Heroes.ToArray();

            profile.AddStat(1, avatar.TrioWins); 
            profile.AddStat(2, avatar.Experience); 
            profile.AddStat(3, avatar.Trophies);
            profile.AddStat(4, avatar.HighestTrophies);
            profile.AddStat(5, profile.Heroes.Length);
            profile.AddStat(6, 0); 
            profile.AddStat(7, home.ThumbnailId);
            profile.AddStat(8, avatar.SoloWins);
            profile.AddStat(9, 0); 
            profile.AddStat(10, avatar.Rob);
            profile.AddStat(11, avatar.DuoWins);
            profile.AddStat(12, 0); 
            profile.AddStat(13, avatar.Boss); 
            profile.AddStat(14, 0); 
            profile.AddStat(15, avatar.ChallangeWins); 
            profile.AddStat(16, 0); 
            profile.AddStat(17, avatar.LeageTeamWins); 
            profile.AddStat(18, avatar.LeageSoloWins); 
            profile.AddStat(19, avatar.ClubLigWins); 
            profile.AddStat(20, avatar.FameCredits); 
            profile.AddStat(21, 0); 
            profile.AddStat(22, 0); 
            profile.AddStat(23, 0); 
            // Ранг ранкеда: те же очки, что видит игрок на главном экране (ClientHome).
            int rankedPoints = RankedPointsOf(avatar);
            profile.AddStat(24, rankedPoints);
            profile.AddStat(25, System.Math.Max(rankedPoints, avatar.HighestRankedPoints));
            profile.AddStat(26, avatar.NumbLeader); 
            profile.AddStat(27, avatar.Year); 
            profile.AddStat(28, avatar.old35rank); 
            // v64: текущие кубки профиль берёт отсюда (новая система кубков); с HighestNewTrophies = 0 показывал 0.
            profile.AddStat(29, avatar.Trophies); 
            profile.AddStat(30, avatar.Prestige); 
            profile.AddStat(31, avatar.Records); 
            profile.AddStat(32, avatar.RecordsRank); 

            profile.Hero = home.FavouriteCharacter;
            profile.HeroSkin = 0;
            profile.T1 = home.DefaultBattleCard.Thumbnail1;
            profile.T2 = home.DefaultBattleCard.Thumbnail2;
            profile.E = home.DefaultBattleCard.Emote;
            profile.Ti = home.DefaultBattleCard.Title;
            profile.Frames = home.DefaultBattleCard.Frames;

            Logger.Print("[GetPlayerProfileMessage::GetPlayerProfileMessage] init statistick");
            return profile;
        }
    }
}
