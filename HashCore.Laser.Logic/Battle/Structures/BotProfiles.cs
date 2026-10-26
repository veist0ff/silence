namespace HashCore.Laser.Logic.Battle.Structures
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Avatar.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Structures;
    using HashCore.Laser.Titan.Math;

    /// <summary>
    /// Ботам — живой вид: свой номер аккаунта, ник, иконка, скин бойца и сгенерированный
    /// профиль, который сервер отдаёт на запрос профиля (из боя или итогов). Профиль
    /// хранится в памяти, пока сервер работает.
    /// </summary>
    public static class BotProfiles
    {
        /// <summary>Номера аккаунтов ботов — с этого числа, выше обычных аккаунтов.</summary>
        public const long FirstBotAccountId = 900000000;
        private static long s_nextId = FirstBotAccountId;
        private static readonly ConcurrentDictionary<long, Profile> s_profiles = new ConcurrentDictionary<long, Profile>();
        private static readonly Random s_rand = new Random();

        private static readonly string[] Names =
        {
            "Shadow", "Beast", "Elite", "Nexus", "Vortex", "Kira", "Lev", "Maks", "Tima", "Artem",
            "Danya", "Egor", "Nikita", "Sasha", "Vlad", "Zhenya", "Ilya", "Kostya", "Misha", "Roma",
            "ProGamer", "Brawler69", "XxSniperxX", "BullMain", "ShellyGod", "MortisPro", "SpikeLord",
            "Toxic", "Ghost", "Raven", "Blaze", "Frost", "Storm", "Ninja", "Joker", "Wolf", "Tiger",
            "Призрак", "Тень", "Кот", "Волк", "Лис", "Шторм", "Буря", "Мастер", "Легенда", "Чемпион",
        };

        public static long NextAccountId() => System.Threading.Interlocked.Increment(ref s_nextId);

        public static bool IsBot(long accountId) => accountId > FirstBotAccountId && accountId < FirstBotAccountId + 100000000;

        public static string RandomName()
        {
            lock (s_rand)
            {
                string n = Names[s_rand.Next(Names.Length)];
                return s_rand.Next(3) == 0 ? n + s_rand.Next(10, 999) : n;
            }
        }

        public static int RandomThumbnail()
        {
            lock (s_rand) return GlobalId.CreateGlobalId(28, s_rand.Next(0, 40));
        }

        /// <summary>Случайный скин бойца (не отключённый) или 0 — скин по умолчанию.</summary>
        public static int RandomSkin(int characterId)
        {
            CharacterData character = DataTables.Get(DataType.Character)?.GetDataByGlobalId<CharacterData>(characterId);
            if (character == null) return 0;
            var skins = new List<int>();
            foreach (LogicData d in DataTables.Get(DataType.Skin).GetDatas())
            {
                if (d is not SkinData skin || skin.Disabled || string.IsNullOrEmpty(skin.Conf)) continue;
                SkinConfData conf = DataTables.Get(DataType.SkinConf)?.GetData<SkinConfData>(skin.Conf);
                if (conf == null || conf.Character != character.Name) continue;
                skins.Add(skin.GetGlobalId());
            }
            if (skins.Count == 0) return 0;
            lock (s_rand)
            {
                // Треть ботов — в стандартном облике, как у живых игроков.
                if (s_rand.Next(3) == 0) return 0;
                return skins[s_rand.Next(skins.Count)];
            }
        }

        /// <summary>Профиль бота: его боец сверху, ещё несколько случайных бойцов и статистика.</summary>
        public static void Register(BattlePlayer bot)
        {
            Profile p = new Profile();
            p.AccountId = bot.AccountId;
            p.DisplayData = new PlayerDisplayData(bot.DisplayData.ThumbnailId, 43000000, bot.DisplayData.Name);

            var heroes = new List<Hero>();
            var chars = new List<int>();
            foreach (LogicData d in DataTables.Get(DataType.Character).GetDatas())
                if (d is CharacterData c && c.IsHero() && !c.Disabled) chars.Add(c.GetGlobalId());

            int total = 0;
            lock (s_rand)
            {
                int count = s_rand.Next(8, Math.Min(40, Math.Max(9, chars.Count)));
                var picked = new HashSet<int> { bot.CharacterId };
                while (picked.Count < count && picked.Count < chars.Count) picked.Add(chars[s_rand.Next(chars.Count)]);
                foreach (int cid in picked)
                {
                    Hero h;
                    try { h = new Hero(cid); } catch { continue; }
                    h.Trophies = s_rand.Next(150, 1000);
                    h.HighestTrophies = h.Trophies + s_rand.Next(0, 150);
                    h.PowerLevel = s_rand.Next(7, 12);
                    total += h.Trophies;
                    if (cid == bot.CharacterId) heroes.Insert(0, h); else heroes.Add(h);
                }

                p.Heroes = heroes.ToArray();
                p.AddStat(1, s_rand.Next(200, 3000));          // победы 3 на 3
                p.AddStat(2, s_rand.Next(5000, 60000));        // опыт
                p.AddStat(3, total);                           // кубки
                p.AddStat(4, total + s_rand.Next(0, 800));     // рекорд кубков
                p.AddStat(5, p.Heroes.Length);
                p.AddStat(7, bot.DisplayData.ThumbnailId);
                p.AddStat(8, s_rand.Next(50, 800));            // победы в одиночном шд
                p.AddStat(11, s_rand.Next(30, 500));           // победы в парном шд
                p.AddStat(24, s_rand.Next(1000, 9000));        // очки ранкеда
                p.AddStat(25, s_rand.Next(1000, 9000));
                p.AddStat(29, total);
            }
            p.Hero = bot.CharacterId;
            p.HeroSkin = bot.SkinId;
            s_profiles[bot.AccountId] = p;
        }

        public static Profile Get(long accountId) => s_profiles.TryGetValue(accountId, out Profile p) ? p : null;
    }
}
