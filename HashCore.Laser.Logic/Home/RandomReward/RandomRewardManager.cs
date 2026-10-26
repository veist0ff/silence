namespace HashCore.Laser.Logic.Home.RandomReward
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Home.Recruit;
    using HashCore.Laser.Logic.Home.Skins;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Server;

    public class RandomRewardSlot
    {
        public int DataGlobalId;
        public int Count;
        public int Extra;

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            stream.WriteVInt(Count);
            stream.WriteVInt(Extra);
        }

        public void Decode(ByteStream stream)
        {
            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Count = stream.ReadVInt();
            Extra = stream.ReadVInt();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ClaimableRandomReward
    {
        public const int CURRENT_FORMAT_VERSION = 1;

        [JsonProperty] public int FormatVersion;
        [JsonProperty] public int ContainerGlobalId;
        [JsonProperty] public int SourceType;
        [JsonProperty] public int UpgradeTaps;
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public List<int> RewardDataIds = new List<int>();

        public RandomRewardContainerData Container =>
            DataTables.Get(DataType.RandomRewardContainer)
                ?.GetDataByGlobalId<RandomRewardContainerData>(ContainerGlobalId);

        /// <summary>
        /// Годна ли награда к отправке.
        ///
        /// Раньше здесь стоял БЕЛЫЙ СПИСОК типов с `default: return false`, плюс
        /// требование совпадения версии формата и длины списка идентификаторов.
        /// Любое несовпадение делало битой награду ЦЕЛИКОМ, а `Build` такие
        /// награды молча выбрасывал ещё до отправки — игрок не видел ничего, и
        /// выглядело это как «менеджер не работает нигде». Хуже всего версия
        /// формата: у всех наград, сохранённых до появления этого поля, она ноль,
        /// то есть разом пропадало всё накопленное на живых аккаунтах.
        ///
        /// У клиента (`LogicClaimableRandomReward::decode`, 0x10048f748) никакой
        /// проверки нет вовсе: он просто читает. Поэтому и здесь проверяем только
        /// то, от чего он может упасть, — и не больше.
        /// </summary>
        public bool IsValid()
        {
            if (Rewards == null || Rewards.Count == 0) return false;
            if (Container == null) return false;

            foreach (LogicGemOffer offer in Rewards)
            {
                if (IsOfferUsable(offer)) return true;
            }

            return false;
        }

        /// <summary>
        /// Убирает из награды только те предложения, которые отдать клиенту
        /// нельзя, вместе с парными идентификаторами, и выравнивает списки.
        /// Возвращает false, если после чистки не осталось ничего.
        ///
        /// Именно чистка, а не отбраковка целиком: одно негодное предложение не
        /// повод отнимать у игрока весь ящик.
        /// </summary>
        public ClaimableRandomReward SanitizedCopy(out int removed)
        {
            removed = 0;
            if (Rewards == null || Container == null) return null;

            ClaimableRandomReward copy = new ClaimableRandomReward
            {
                FormatVersion = FormatVersion,
                ContainerGlobalId = ContainerGlobalId,
                SourceType = SourceType,
                UpgradeTaps = UpgradeTaps
            };

            for (int i = 0; i < Rewards.Count; i++)
            {
                if (!IsOfferUsable(Rewards[i]))
                {
                    removed++;
                    continue;
                }

                copy.Rewards.Add(Rewards[i]);
                copy.RewardDataIds.Add(i < RewardDataIds.Count ? RewardDataIds[i] : 0);
            }

            return copy.Rewards.Count > 0 ? copy : null;
        }

        /// <summary>
        /// Можно ли отдать клиенту это предложение.
        ///
        /// Неизвестный тип — НЕ повод считать предложение битым: на проводе тип
        /// уходит обычным числом, и клиент его просто прочитает. Проверяем лишь
        /// то, что умеем: не ссылается ли предложение на несуществующую строку
        /// таблицы, от чего клиент и правда падает.
        /// </summary>
        private static bool IsOfferUsable(LogicGemOffer offer)
        {
            if (offer == null || offer.Count <= 0) return false;

            switch (offer.Type)
            {
                case ShopItem.Brawler: return RowExists(DataType.Character, offer.ItemDataId);
                case ShopItem.HeroPower: return RowExists(DataType.Card, offer.ItemDataId);
                case ShopItem.Skin: return RowExists(DataType.Skin, offer.SkinDataId);
                case ShopItem.Emote: return RowExists(DataType.Emote, offer.SkinDataId);
                case ShopItem.PlayerThumbnail: return RowExists(DataType.PlayerThumbnail, offer.SkinDataId);
                case ShopItem.Spray: return RowExists(DataType.Spray, offer.SkinDataId);

                // Валюты и всё остальное отдаём как есть.
                default: return true;
            }
        }

        /// <summary>
        /// Есть ли такая строка в таблице. Без исключений: `GetDataWithId` лезет
        /// в список по индексу БЕЗ проверки границ, и любой мусорный
        /// идентификатор ронял не награду, а всю отправку дома целиком.
        /// </summary>
        private static bool RowExists(DataType type, int id)
        {
            DataTable table = DataTables.Get(type);
            if (table == null) return false;

            int instance = GlobalId.GetInstanceId(id);
            return instance >= 0 && instance < table.Count;
        }

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, ContainerGlobalId);

            stream.WriteVInt(Rewards.Count);
            foreach (LogicGemOffer offer in Rewards) offer.Encode(stream);

            stream.WriteVInt(SourceType);
            stream.WriteVInt(UpgradeTaps);

            // Клиент ждёт столько же идентификаторов, сколько наград. Если списки
            // почему-то разошлись, добиваем ТОЛЬКО отправку, а не сохранённое
            // состояние: раньше нули дописывались в сам список, после чего IsValid
            // считал награду битой и её молча выбрасывали — отсюда и «через раз
            // забирает награды».
            stream.WriteVInt(Rewards.Count);
            for (int i = 0; i < Rewards.Count; i++)
            {
                stream.WriteVInt(i < RewardDataIds.Count ? RewardDataIds[i] : 0);
            }

            if (RewardDataIds.Count != Rewards.Count)
            {
                Logger.Warning($"[randomReward] списки разошлись: наград {Rewards.Count}, "
                    + $"идентификаторов {RewardDataIds.Count}, контейнер {ContainerGlobalId}");
            }
        }

        public void Decode(ByteStream stream)
        {
            ContainerGlobalId = ByteStreamHelper.ReadDataReference(stream);

            Rewards.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                LogicGemOffer offer = new LogicGemOffer();
                offer.Type = (ShopItem)stream.ReadVInt();
                offer.Count = stream.ReadVInt();
                offer.ItemDataId = ByteStreamHelper.ReadDataReference(stream);
                offer.SkinDataId = stream.ReadVInt();
                Rewards.Add(offer);
            }

            SourceType = stream.ReadVInt();
            UpgradeTaps = stream.ReadVInt();

            RewardDataIds.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--) RewardDataIds.Add(stream.ReadVInt());

            FormatVersion = CURRENT_FORMAT_VERSION;
        }
    }

    public partial class RandomRewardManager
    {
        public readonly List<RandomRewardSlot> Slots = new List<RandomRewardSlot>();
        public readonly List<RandomRewardSlot> TrailingSlots = new List<RandomRewardSlot>();
        public readonly List<ClaimableRandomReward> Claimable = new List<ClaimableRandomReward>();

        public int Seed;
        /// <summary>
        /// Поле +24 менеджера. Это отметка «дорожку ежедневных побед посмотрели»:
        /// её ставит LogicDailyWinTrackSeenCommand (0x1003c7b30), записывая единицу
        /// ровно по этому смещению, а HomePageDailyWinTrack::refreshButtonGraphics
        /// (0x10034049c) по ней решает, играть ли привлекающую анимацию.
        /// </summary>
        public bool DailyWinTrackSeen;
        public int Unk28;

        public int WinProgress;
        public int Unk48;

        public readonly List<int> ProgressList = new List<int>();

        public int TrackLength;

        public readonly List<int> ProgressList2 = new List<int>();
        public readonly List<int> RewardList = new List<int>();
        public readonly List<int> RewardStateList = new List<int>();

        public int Unk160;
        public bool Unk164;

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Slots.Count);
            foreach (RandomRewardSlot slot in Slots) slot.Encode(stream);

            stream.WriteVInt(Claimable.Count);
            foreach (ClaimableRandomReward reward in Claimable)
            {
                stream.WriteBoolean(reward != null);
                if (reward != null) reward.Encode(stream);
            }

            // The manager's LogicRandom seed goes out as a raw int, not a VInt.
            stream.WriteInt(Seed);

            stream.WriteBoolean(DailyWinTrackSeen);
            stream.WriteVInt(Unk28);
            stream.WriteVInt(WinProgress);
            stream.WriteVInt(Unk48);

            WriteInts(stream, ProgressList);
            stream.WriteVInt(TrackLength);
            WriteInts(stream, ProgressList2);
            WriteInts(stream, RewardList);
            WriteInts(stream, RewardStateList);

            stream.WriteVInt(Unk160);
            stream.WriteBoolean(Unk164);

            stream.WriteVInt(TrailingSlots.Count);
            foreach (RandomRewardSlot slot in TrailingSlots) slot.Encode(stream);
        }

        private static void WriteInts(ByteStream stream, List<int> values)
        {
            stream.WriteVInt(values.Count);
            foreach (int value in values) stream.WriteVInt(value);
        }

        private static void ReadInts(ByteStream stream, List<int> values)
        {
            values.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--) values.Add(stream.ReadVInt());
        }

        public void Decode(ByteStream stream)
        {
            Slots.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                RandomRewardSlot slot = new RandomRewardSlot();
                slot.Decode(stream);
                Slots.Add(slot);
            }

            Claimable.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                if (!stream.ReadBoolean())
                {
                    Claimable.Add(null);
                    continue;
                }

                ClaimableRandomReward reward = new ClaimableRandomReward();
                reward.Decode(stream);
                Claimable.Add(reward);
            }

            Seed = stream.ReadInt();
            DailyWinTrackSeen = stream.ReadBoolean();
            Unk28 = stream.ReadVInt();
            WinProgress = stream.ReadVInt();
            Unk48 = stream.ReadVInt();

            ReadInts(stream, ProgressList);
            TrackLength = stream.ReadVInt();
            ReadInts(stream, ProgressList2);
            while (ProgressList2.Count < ProgressList.Count) ProgressList2.Add(0);

            ReadInts(stream, RewardList);
            while (RewardList.Count < TrackLength) RewardList.Add(0);

            ReadInts(stream, RewardStateList);
            while (RewardStateList.Count < TrackLength) RewardStateList.Add(1);

            Unk160 = stream.ReadVInt();
            Unk164 = stream.ReadBoolean();

            TrailingSlots.Clear();
            for (int i = stream.ReadVInt(); i > 0; i--)
            {
                RandomRewardSlot slot = new RandomRewardSlot();
                slot.Decode(stream);
                TrailingSlots.Add(slot);
            }
        }

        public static RandomRewardManager Build(ClientHome home)
        {
            RandomRewardManager manager = new RandomRewardManager();
            if (home == null) return manager;

            lock (home.RewardSync)
            {
            if (home.RandomRewardSeed == 0) home.RandomRewardSeed = Environment.TickCount | 1;
            manager.Seed = home.RandomRewardSeed;
            manager.DailyWinTrackSeen = home.DailyWinTrackSeen;

            if (home.RandomRewards != null)
            {
                foreach (ClaimableRandomReward reward in home.RandomRewards)
                {
                    if (reward == null) continue;

                    // Чиним награду вместо того, чтобы выбрасывать её целиком, и
                    // ни при каких условиях не молчим: раньше награда просто
                    // исчезала по дороге к клиенту, и найти причину было нечем.
                    ClaimableRandomReward sendable = reward.SanitizedCopy(out int removed);
                    if (sendable == null)
                    {
                        Logger.Warning($"[randomReward] награда не отправлена: контейнер "
                            + $"{reward.ContainerGlobalId}"
                            + (reward.Container == null ? " (нет в таблице контейнеров)" : "")
                            + $", предложений {reward.Rewards?.Count ?? 0}");
                        continue;
                    }

                    if (removed > 0)
                    {
                        Logger.Warning($"[randomReward] из награды {reward.ContainerGlobalId} "
                            + $"при отправке пропущено негодных предложений: {removed}");
                    }

                    manager.Claimable.Add(sendable);
                }
            }

            return manager;
            }
        }

        /// <summary>Короткая расшифровка содержимого награды для журнала.</summary>
        internal static string Describe(ClaimableRandomReward claimable)
        {
            if (claimable?.Rewards == null || claimable.Rewards.Count == 0) return "пусто";

            List<string> parts = new List<string>();
            for (int i = 0; i < claimable.Rewards.Count; i++)
            {
                LogicGemOffer offer = claimable.Rewards[i];
                if (offer == null) { parts.Add("null"); continue; }

                int id = i < claimable.RewardDataIds.Count ? claimable.RewardDataIds[i] : 0;
                parts.Add($"{offer.Type} x{offer.Count} (предмет {offer.ItemDataId}, "
                    + $"скин {offer.SkinDataId}, строка {id})");
            }

            return string.Join("; ", parts);
        }

        public static RandomRewardContainerData GetContainer(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return DataTables.Get(DataType.RandomRewardContainer)
                ?.GetData<RandomRewardContainerData>(name.Trim());
        }

        public static RandomRewardData GetReward(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return DataTables.Get(DataType.RandomReward)
                ?.GetData<RandomRewardData>(name.Trim());
        }

        public static RandomRewardContainerData RollWinContainer(Random random)
        {
            DataTable table = DataTables.Get(DataType.RandomRewardContainer);
            if (table == null) return null;

            int total = 0;
            foreach (RandomRewardContainerData container in table.GetDatas())
            {
                if (container != null && container.TicketsInWinGamesDraw > 0) total += container.TicketsInWinGamesDraw;
            }
            if (total <= 0) return null;

            int roll = random.Next(total);
            foreach (RandomRewardContainerData container in table.GetDatas())
            {
                if (container == null || container.TicketsInWinGamesDraw <= 0) continue;

                roll -= container.TicketsInWinGamesDraw;
                if (roll < 0) return container;
            }
            return null;
        }

        private static readonly int[][] TROPHY_BOX_RARITY_WEIGHTS =
        {
            new[] { 50, 28, 15, 5, 2 },
            new[] { 46, 29, 17, 6, 2 },
            new[] { 42, 29, 19, 7, 3 },
            new[] { 38, 29, 21, 9, 3 },
            new[] { 34, 28, 23, 11, 4 }
        };

        private static int RollBoxRarity(RandomRewardContainerData container, Random random)
        {
            int trophyBoxLevel = GetTrophyBoxLevel(container);
            if (trophyBoxLevel > 0)
            {
                int roll = random.Next(100);
                int[] weights = TROPHY_BOX_RARITY_WEIGHTS[trophyBoxLevel - 1];
                for (int rarity = 1; rarity <= weights.Length; rarity++)
                {
                    roll -= weights[rarity - 1];
                    if (roll < 0) return rarity;
                }
            }

            RandomRewardContainerData rarityContainer = RollWinContainer(random);
            return rarityContainer == null ? 1 : GetFixedContainerRarity(rarityContainer);
        }

        private static int GetTrophyBoxLevel(RandomRewardContainerData container)
        {
            if (!string.Equals(container?.GroupType, "SEASON_BOX", StringComparison.OrdinalIgnoreCase))
                return 0;

            switch ((container.RarityFrameLabel ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "level_1": return 1;
                case "level_2": return 2;
                case "level_3": return 3;
                case "level_4": return 4;
                case "level_5": return 5;
                default: return 0;
            }
        }

        private static int GetDropCount(RandomRewardContainerData container, Random random)
        {
            if (GetTrophyBoxLevel(container) == 5) return random.Next(19, 51);
            return container.MultiDropCount > 0 ? container.MultiDropCount : 1;
        }

        public const int UPGRADE_TAPS = 4;

        /// <summary>
        /// Розыгрыш награды для конкретного контейнера.
        ///
        /// Веса берём из колонки самого контейнера (`TicketsIn` + его имя) — так
        /// устроена таблица. Если у контейнера своей колонки нет или она пустая,
        /// откатываемся на обычные Starr Drop по редкости, чтобы ящик всё равно
        /// не вышел пустым.
        /// </summary>
        private static RandomRewardData RollReward(RandomRewardContainerData container, int rarity,
            Random random, bool allowBrawlers = true)
        {
            RandomRewardData rolled = RollByColumn(container?.Name, rarity, random, allowBrawlers, true);
            if (rolled != null) return rolled;

            // Запасной путь: по редкости.
            return RollByColumn(null, rarity, random, allowBrawlers, false);
        }

        private static RandomRewardData RollReward(int rarity, Random random, bool allowBrawlers = true)
        {
            return RollReward(null, rarity, random, allowBrawlers);
        }

        private static RandomRewardData RollByColumn(string containerName, int rarity, Random random,
            bool allowBrawlers, bool byContainer)
        {
            DataTable table = DataTables.Get(DataType.RandomReward);
            if (table == null) return null;
            if (byContainer && string.IsNullOrWhiteSpace(containerName)) return null;

            int total = 0;
            foreach (RandomRewardData data in table.GetDatas())
            {
                total += Tickets(data, containerName, rarity, allowBrawlers, byContainer);
            }
            if (total <= 0) return null;

            int roll = random.Next(total);
            foreach (RandomRewardData data in table.GetDatas())
            {
                int tickets = Tickets(data, containerName, rarity, allowBrawlers, byContainer);
                if (tickets <= 0) continue;

                roll -= tickets;
                if (roll < 0) return data;
            }

            return null;
        }

        private static int Tickets(RandomRewardData data, string containerName, int rarity,
            bool allowBrawlers, bool byContainer)
        {
            if (data == null) return 0;
            if (!allowBrawlers && string.Equals(data.TypeName, "Brawler",
                    StringComparison.OrdinalIgnoreCase)) return 0;

            int tickets = byContainer
                ? data.GetTicketsForContainer(containerName)
                : data.GetTickets(rarity);

            return tickets > 0 ? tickets : 0;
        }

        private static int RollAmount(Random random, int min, int max)
        {
            if (max < min) max = min;
            if (max <= min) return min;
            return random.Next(min, max + 1);
        }

        private static LogicGemOffer Currency(ShopItem type, int amount)
        {
            if (amount <= 0) return null;
            return new LogicGemOffer { Type = type, Count = amount };
        }

        private static LogicGemOffer BuildOffer(HomeMode homeMode, string typeName, int typeValue,
            int priceMin, int priceMax, int amount, Random random,
            RandomRewardContainerData container = null)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            string type = typeName.Trim().ToUpperInvariant();

            // Ранговые разновидности своего набора в таблицах не имеют, поэтому
            // сводим их к общей косметике: отбор всё равно делает строка награды.
            if (type == "RANKEDSTARSKIN") type = "SKIN";
            else if (type == "RANKEDSTARSPRAY") type = "SPRAY";
            else if (type == "RANKEDSTARPROFILEPIC") type = "PROFILEPIC";

            // А у коллабных набор свой: он прописан в collabs.csv у того коллаба,
            // к которому привязан контейнер. Двойка в TypeValue выбирает запасной
            // набор весов (AltDraw) — так сделаны двусторонние коллабы.
            bool alternate = typeValue == 2;

            switch (type)
            {
                case "COINS": return Currency(ShopItem.Coin, amount);
                case "POWERPOINTS": return Currency(ShopItem.PowerPoints, amount);
                case "CREDITS": return Currency(ShopItem.RecruitToken, amount);
                case "BLING": return Currency(ShopItem.Bling, amount);
                case "GEMS": return Currency(ShopItem.Gems, amount);

                case "BRAWLER":
                {
                    int brawler = PickBrawler(homeMode, typeValue, random);
                    if (brawler == 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Brawler, Count = 1, ItemDataId = brawler };
                }

                case "SKIN":
                {
                    int skin = PickSkin(homeMode, priceMin, priceMax, random);
                    if (skin < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Skin, Count = 1, SkinDataId = skin };
                }

                case "PIN":
                {
                    int emote = PickEmote(homeMode, priceMin, priceMax, random);
                    if (emote < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Emote, Count = 1, SkinDataId = emote };
                }

                case "PROFILEPIC":
                {
                    int thumbnail = PickThumbnail(homeMode, priceMin, priceMax, random);
                    if (thumbnail < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.PlayerThumbnail, Count = 1, SkinDataId = thumbnail };
                }

                // Удвоитель монет. В таблице у него 75 билетов по Starr Drop, но
                // сервер этот тип не знал: предложение выходило пустым, и ящик с
                // единственным дропом собирался пустым и выбрасывался.
                case "TOKENDOUBLER": return Currency(ShopItem.CoinDoubler, amount);

                case "SPRAY":
                {
                    int spray = PickSpray(homeMode, priceMin, priceMax, random);
                    if (spray < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Spray, Count = 1, SkinDataId = spray };
                }

                // Карточки бойца. У клиента это один и тот же вид выдачи (4):
                // LogicGatchaDrop кладёт LogicCardData через setItem. Различаются
                // они только колонкой MetaType в cards.csv:
                // 4 — пассивка, 5 — гаджет, 6 — гиперзаряд, 7 — мутация.
                case "STARPOWER": return CardOffer(homeMode, 4, random);
                case "GADGET": return CardOffer(homeMode, 5, random);
                case "OVERCHARGE": return CardOffer(homeMode, 6, random);
                case "COLLABMUTATION": return CardOffer(homeMode, 7, random);

                // Коллабные награды. Сервер этих типов не знал вовсе, а на них
                // висят ВСЕ билеты MythicCollabDrop и LegendaryCollabDrop — те
                // самые ящики, где скин гарантирован, — и большая часть билетов
                // мегаящика, граффити-ящика и мех-ящика.
                case "COLLABSKIN":
                {
                    int skin = PickFromCollab(homeMode, container, DataType.Skin,
                        priceMin, priceMax, alternate, random);
                    if (skin < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Skin, Count = 1, SkinDataId = skin };
                }

                case "COLLABSPRAY":
                {
                    int spray = PickFromCollab(homeMode, container, DataType.Spray,
                        priceMin, priceMax, alternate, random);
                    if (spray < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Spray, Count = 1, SkinDataId = spray };
                }

                case "COLLABPIN":
                {
                    int pin = PickFromCollab(homeMode, container, DataType.Emote,
                        priceMin, priceMax, alternate, random);
                    if (pin < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.Emote, Count = 1, SkinDataId = pin };
                }

                case "COLLABPROFILEPIC":
                {
                    int pic = PickFromCollab(homeMode, container, DataType.PlayerThumbnail,
                        priceMin, priceMax, alternate, random);
                    if (pic < 0) return null;
                    return new LogicGemOffer { Type = ShopItem.PlayerThumbnail, Count = 1, SkinDataId = pic };
                }
            }

            return null;
        }

        /// <summary>
        /// Карточка нужного вида для бойца, который у игрока есть и у которого
        /// этой карточки ещё нет. Вид задаётся колонкой MetaType в cards.csv:
        /// 4 пассивка, 5 гаджет, 6 гиперзаряд, 7 мутация.
        /// </summary>
        private static LogicGemOffer CardOffer(HomeMode homeMode, int metaType, Random random)
        {
            DataTable table = DataTables.Get(DataType.Card);
            if (table == null || homeMode?.Avatar == null) return null;

            List<int> candidates = new List<int>();

            foreach (CardData card in table.GetDatas())
            {
                if (card == null || card.Disabled) continue;
                if (card.MetaType != metaType) continue;
                if (string.IsNullOrEmpty(card.Target)) continue;

                Hero hero = FindHeroForCard(homeMode, card);
                if (hero == null) continue;

                int instanceId = card.GetInstanceId();
                if (hero.UnlockedCards != null && hero.UnlockedCards.Contains(instanceId)) continue;

                candidates.Add(card.GetGlobalId());
            }

            if (candidates.Count == 0) return null;

            return new LogicGemOffer
            {
                Type = ShopItem.HeroPower,
                Count = 1,
                ItemDataId = candidates[random.Next(candidates.Count)]
            };
        }

        /// <summary>Боец, которому принадлежит карточка. Null, если его нет у игрока.</summary>
        internal static Hero FindHeroForCard(HomeMode homeMode, CardData card)
        {
            if (homeMode?.Avatar?.Heroes == null || card == null) return null;

            foreach (Hero hero in homeMode.Avatar.Heroes)
            {
                if (hero?.CharacterData == null) continue;
                if (hero.CharacterData.Name == card.Target) return hero;
            }

            return null;
        }

        /// <summary>
        /// Награда коллаба — из набора ТОГО коллаба, которому принадлежит сам
        /// контейнер (колонка CollabId в random_reward_containers.csv). Набор и
        /// веса лежат в collabs.csv, см. <see cref="CollabRewards"/>.
        ///
        /// Ничего не подбираем наугад: если подходящего в наборе не осталось,
        /// возвращаем «нет», и сработает запасная награда из той же строки
        /// random_rewards.csv. Решает таблица, а не сервер.
        /// </summary>
        private static int PickFromCollab(HomeMode homeMode, RandomRewardContainerData container,
            DataType type, int priceMin, int priceMax, bool alternate, Random random)
        {
            ClientHome home = homeMode?.Home;
            DataTable table = DataTables.Get(type);
            if (home == null || table == null) return -1;

            int collabId = container != null && container.CollabId > 0
                ? container.CollabId
                : CollabRewards.ActiveCollabId;

            CollabRewards.Pool pool = type switch
            {
                DataType.Skin => CollabRewards.GetSkins(collabId),
                DataType.Spray => CollabRewards.GetSprays(collabId),
                DataType.Emote => CollabRewards.GetPins(collabId),
                DataType.PlayerThumbnail => CollabRewards.GetProfilePics(collabId),
                _ => null
            };

            List<int> candidates = new List<int>();
            List<int> weights = new List<int>();
            int total = 0;

            total += Collect(home, table, type, pool, priceMin, priceMax, alternate,
                candidates, weights);

            // Особые скины коллаба лежат отдельным набором с теми же правилами.
            if (type == DataType.Skin)
            {
                total += Collect(home, table, type, CollabRewards.GetSpecialSkins(collabId),
                    priceMin, priceMax, alternate, candidates, weights);
            }

            if (total <= 0) return -1;

            int roll = random.Next(total);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0) return candidates[i];
            }

            return -1;
        }

        private static int Collect(ClientHome home, DataTable table, DataType type,
            CollabRewards.Pool pool, int priceMin, int priceMax, bool alternate,
            List<int> candidates, List<int> weights)
        {
            if (pool == null) return 0;

            int total = 0;

            for (int i = 0; i < pool.Count; i++)
            {
                int tickets = pool.GetTickets(i, alternate);
                if (tickets <= 0) continue;

                int instanceId = pool.InstanceIds[i];
                if (!IsCollabItemAvailable(home, table, type, instanceId, priceMin, priceMax)) continue;

                candidates.Add(instanceId);
                weights.Add(tickets);
                total += tickets;
            }

            return total;
        }

        /// <summary>
        /// Годится ли предмет коллаба: существует, не отключён, ещё не получен и
        /// попадает в диапазон цены из строки награды.
        /// </summary>
        private static bool IsCollabItemAvailable(ClientHome home, DataTable table, DataType type,
            int instanceId, int priceMin, int priceMax)
        {
            switch (type)
            {
                case DataType.Skin:
                {
                    SkinData skin = table.GetDataWithId<SkinData>(instanceId);
                    if (skin == null || skin.Disabled) return false;
                    if (home.UnlockedSkins != null
                        && home.UnlockedSkins.Contains(skin.GetGlobalId())) return false;

                    // Диапазон применяем только когда он задан: у многих строк
                    // стоит 0-0, то есть без ограничения по цене.
                    if (priceMin > 0 || priceMax > 0) return InPriceRange(skin.PriceGems, priceMin, priceMax);
                    return true;
                }

                case DataType.Spray:
                {
                    SprayData spray = table.GetDataWithId<SprayData>(instanceId);
                    if (spray == null || spray.Disabled) return false;
                    return home.UnlockedSprays == null
                        || !home.UnlockedSprays.Contains(spray.GetGlobalId());
                }

                case DataType.Emote:
                {
                    EmoteData emote = table.GetDataWithId<EmoteData>(instanceId);
                    if (emote == null || emote.Disabled) return false;
                    return home.UnlockedEmotes == null
                        || !home.UnlockedEmotes.Contains(emote.GetGlobalId());
                }

                case DataType.PlayerThumbnail:
                {
                    PlayerThumbnailData thumbnail = table.GetDataWithId<PlayerThumbnailData>(instanceId);
                    if (thumbnail == null || thumbnail.Disabled) return false;
                    return home.UnlockedThumbnails == null
                        || !home.UnlockedThumbnails.Contains(thumbnail.GetGlobalId());
                }
            }

            return false;
        }

        /// <summary>
        /// Спрей, которого у игрока ещё нет. Цена у спреев указана в блингах, а
        /// не в кристаллах, поэтому диапазон сверяем с PriceBling.
        /// </summary>
        private static int PickSpray(HomeMode homeMode, int priceMin, int priceMax, Random random)
        {
            DataTable table = DataTables.Get(DataType.Spray);
            ClientHome home = homeMode?.Home;
            if (table == null || home == null) return -1;

            List<int> candidates = new List<int>();
            foreach (SprayData spray in table.GetDatas())
            {
                if (spray == null || spray.Disabled) continue;
                if (!InPriceRange(spray.PriceBling, priceMin, priceMax)) continue;
                if (home.UnlockedSprays != null
                    && home.UnlockedSprays.Contains(spray.GetGlobalId())) continue;

                candidates.Add(spray.GetInstanceId());
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }

        private static LogicGemOffer BuildOffer(HomeMode homeMode, RandomRewardData data, Random random,
            RandomRewardContainerData container = null)
        {
            if (data == null) return null;

            LogicGemOffer offer = BuildOffer(homeMode, data.TypeName, data.TypeValue,
                data.TypePriceMin, data.TypePriceMax,
                RollAmount(random, data.AmountMin, data.AmountMax), random, container);

            if (offer != null) return offer;

            return BuildOffer(homeMode, data.FallbackTypeName, 0, 0, 0,
                data.FallbackAmount, random, container);
        }

        private static bool IsDuplicateUnlock(ClaimableRandomReward claimable, LogicGemOffer candidate)
        {
            if (candidate == null) return false;

            foreach (LogicGemOffer existing in claimable.Rewards)
            {
                if (existing == null || existing.Type != candidate.Type) continue;

                switch (candidate.Type)
                {
                    case ShopItem.Brawler:
                        if (existing.ItemDataId == candidate.ItemDataId) return true;
                        break;

                    case ShopItem.Skin:
                    case ShopItem.Emote:
                    case ShopItem.PlayerThumbnail:
                    case ShopItem.Spray:
                        if (existing.SkinDataId == candidate.SkinDataId) return true;
                        break;
                }
            }

            return false;
        }

        private static int GetRarityValue(string rarity)
        {
            switch (rarity)
            {
                case "rare": return 1;
                case "super_rare": return 2;
                case "epic": return 3;
                case "mega_epic": return 4;
                case "legendary": return 5;
                case "ultra_legendary": return 5;
            }
            return 0;
        }

        private static int PickBrawler(HomeMode homeMode, int rarityValue, Random random)
        {
            DataTable table = DataTables.Get(DataType.Character);
            if (table == null || homeMode.Avatar == null) return 0;

            List<int> candidates = new List<int>();
            foreach (CharacterData character in table.GetDatas())
            {
                if (character == null || character.Disabled || !character.IsHero()) continue;

                int globalId = character.GetGlobalId();
                if (homeMode.Avatar.HasHero(globalId)) continue;
                if (GetRarityValue(BrawlerRecruit.GetRarity(globalId)) != rarityValue) continue;

                candidates.Add(globalId);
            }

            return candidates.Count == 0 ? 0 : candidates[random.Next(candidates.Count)];
        }

        private static RandomRewardData FindRewardData(string typeName, int typeValue)
        {
            DataTable table = DataTables.Get(DataType.RandomReward);
            if (table == null) return null;

            foreach (RandomRewardData data in table.GetDatas())
            {
                if (data == null || data.TypeValue != typeValue) continue;
                if (string.Equals(data.TypeName, typeName, StringComparison.OrdinalIgnoreCase)) return data;
            }

            return null;
        }

        private static void AddFeaturedBrawler(HomeMode homeMode, RandomRewardContainerData container,
            ClaimableRandomReward claimable)
        {
            if (!string.Equals(container.GroupType, "BRAWLER_BOX", StringComparison.OrdinalIgnoreCase)) return;

            string featuredName = container.SoloOfferDefaultBgFrameName;
            if (string.IsNullOrWhiteSpace(featuredName) && container.Name.EndsWith("BrawlerBox"))
                featuredName = container.Name.Substring(0, container.Name.Length - "BrawlerBox".Length);

            DataTable characterTable = DataTables.Get(DataType.Character);
            if (characterTable == null || string.IsNullOrWhiteSpace(featuredName)) return;

            CharacterData featured = null;
            foreach (CharacterData character in characterTable.GetDatas())
            {
                if (character != null && character.IsHero()
                    && string.Equals(character.Name, featuredName, StringComparison.OrdinalIgnoreCase))
                {
                    featured = character;
                    break;
                }
            }

            if (featured == null || homeMode.Avatar.HasHero(featured.GetGlobalId())) return;

            int rarity = GetRarityValue(BrawlerRecruit.GetRarity(featured.GetGlobalId()));
            RandomRewardData rewardData = FindRewardData("Brawler", rarity);
            if (rewardData == null) return;

            claimable.Rewards.Add(new LogicGemOffer
            {
                Type = ShopItem.Brawler,
                Count = 1,
                ItemDataId = featured.GetGlobalId()
            });
            claimable.RewardDataIds.Add(rewardData.GetGlobalId());
        }

        private static bool InPriceRange(int price, int min, int max)
        {
            if (price <= 0) return false;
            if (min > 0 && price < min) return false;
            if (max > 0 && price > max) return false;
            return true;
        }

        private static bool HasRequiredHero(HomeMode homeMode, string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return true;

            CharacterData character = DataTables.Get(DataType.Character)?.GetData<CharacterData>(characterName);
            return character != null && homeMode?.Avatar?.HasHero(character.GetGlobalId()) == true;
        }

        private static int PickSkin(HomeMode homeMode, int priceMin, int priceMax, Random random)
        {
            DataTable table = DataTables.Get(DataType.Skin);
            ClientHome home = homeMode?.Home;
            if (table == null || home == null) return -1;

            List<int> candidates = new List<int>();
            foreach (SkinData skin in table.GetDatas())
            {
                if (skin == null || skin.Disabled) continue;
                if (!InPriceRange(skin.PriceGems, priceMin, priceMax)) continue;
                if (home.UnlockedSkins.Contains(skin.GetGlobalId())) continue;

                SkinConfData skinConf = DataTables.Get(DataType.SkinConf)?.GetData<SkinConfData>(skin.Conf);
                if (skinConf == null || !HasRequiredHero(homeMode, skinConf.Character)) continue;

                candidates.Add(skin.GetInstanceId());
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }

        private static int PickEmote(HomeMode homeMode, int priceMin, int priceMax, Random random)
        {
            DataTable table = DataTables.Get(DataType.Emote);
            ClientHome home = homeMode?.Home;
            if (table == null || home == null) return -1;

            List<int> candidates = new List<int>();
            foreach (EmoteData emote in table.GetDatas())
            {
                if (emote == null || emote.Disabled) continue;
                if (!InPriceRange(emote.PriceGems, priceMin, priceMax)) continue;
                if (home.UnlockedEmotes.Contains(emote.GetGlobalId())) continue;
                if (!HasRequiredHero(homeMode, emote.Character)) continue;

                candidates.Add(emote.GetInstanceId());
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }

        private static int PickThumbnail(HomeMode homeMode, int priceMin, int priceMax, Random random)
        {
            DataTable table = DataTables.Get(DataType.PlayerThumbnail);
            ClientHome home = homeMode?.Home;
            if (table == null || home == null) return -1;

            List<int> candidates = new List<int>();
            foreach (PlayerThumbnailData thumbnail in table.GetDatas())
            {
                if (thumbnail == null || thumbnail.Disabled) continue;
                if (!InPriceRange(thumbnail.PriceGems, priceMin, priceMax)) continue;
                if (home.UnlockedThumbnails.Contains(thumbnail.GetGlobalId())) continue;
                if (!HasRequiredHero(homeMode, thumbnail.RequiredHero)) continue;

                candidates.Add(thumbnail.GetInstanceId());
            }

            return candidates.Count == 0 ? -1 : candidates[random.Next(candidates.Count)];
        }

        public static ClaimableRandomReward CreateReward(HomeMode homeMode,
            RandomRewardContainerData container, Random random)
        {
            if (homeMode?.Home == null || container == null) return null;
            if (container.Name == "DaredevilBrawlerBox") return CreateGigiBox(homeMode, container, random);

            int fixedRarity = GetFixedContainerRarity(container);
            int count = GetDropCount(container, random);

            ClaimableRandomReward claimable = new ClaimableRandomReward();
            claimable.FormatVersion = ClaimableRandomReward.CURRENT_FORMAT_VERSION;
            claimable.ContainerGlobalId = container.GetGlobalId();
            claimable.SourceType = container.IsRankedDraw ? 2 : 0;
            claimable.UpgradeTaps = IsStarrDrop(container) ? UPGRADE_TAPS : 0;

            AddFeaturedBrawler(homeMode, container, claimable);

            int premiumDraws = Math.Min(container.PremiumDrawCountForMultiDrops,
                count - claimable.Rewards.Count);
            int premiumRarity = GetPremiumRarity(container);

            for (int i = claimable.Rewards.Count; i < count; i++)
            {
                int rarity;
                if (fixedRarity > 0)
                {
                    rarity = fixedRarity;
                }
                else if (premiumDraws > 0 && premiumRarity > 0)
                {
                    rarity = premiumRarity;
                    premiumDraws--;
                }
                else
                {
                    rarity = RollBoxRarity(container, random);
                }

                bool allowBrawlers = !string.Equals(container.GroupType, "BRAWLER_BOX",
                    StringComparison.OrdinalIgnoreCase);
                RandomRewardData data = RollReward(container, rarity, random, allowBrawlers);
                if (data == null) continue;

                LogicGemOffer offer = BuildOffer(homeMode, data, random, container);
                if (IsDuplicateUnlock(claimable, offer))
                {
                    offer = BuildOffer(homeMode, data.FallbackTypeName, 0, 0, 0,
                        data.FallbackAmount, random, container);
                }
                if (offer == null) continue;

                claimable.Rewards.Add(offer);
                claimable.RewardDataIds.Add(data.GetGlobalId());
            }

            return claimable.Rewards.Count == 0 ? null : claimable;
        }

        private static int GetFixedContainerRarity(RandomRewardContainerData container)
        {
            if (container.Rarity > 0) return container.Rarity;

            switch ((container.RarityFrameLabel ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "rare": return 1;
                case "super_rare": return 2;
                case "epic": return 3;
                case "mythic": return 4;
                case "legendary": return 5;
                case "overcharge": return 6;
            }

            if (container.IsRankedDraw) return 4;

            switch (container.Name)
            {
                case "RareStar": return 1;
                case "SuperRareStar": return 2;
                case "EpicStar": return 3;
                case "MythicStar": return 4;
                case "LegendaryStar": return 5;
                case "OverchargeStar": return 6;
                default: return 0;
            }
        }

        private static int GetPremiumRarity(RandomRewardContainerData container)
        {
            switch ((container.SoloOfferDefaultBgFrameName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "offer_rarity_rare": return 1;
                case "offer_rarity_super_rare": return 2;
                case "offer_rarity_epic": return 3;
                case "offer_rarity_mythic": return 4;
                case "offer_rarity_legendary": return 5;
            }

            return GetTrophyBoxLevel(container);
        }

        private static bool IsStarrDrop(RandomRewardContainerData container)
        {
            return container.CollabId <= 0
                && string.IsNullOrEmpty(container.VisualType)
                && (string.IsNullOrEmpty(container.GroupType) || container.GroupType == "NONE");
        }

        public static ClaimableRandomReward Give(HomeMode homeMode, string containerName, bool send = true)
        {
            return Give(homeMode, GetContainer(containerName), send);
        }

        public static ClaimableRandomReward Give(HomeMode homeMode, RandomRewardContainerData container, bool send = true)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || container == null) return null;

            lock (home.RewardSync)
            {
            if (home.RandomRewardSeed == 0) home.RandomRewardSeed = Environment.TickCount | 1;

            ClaimableRandomReward claimable = CreateReward(homeMode, container, new Random(home.RandomRewardSeed++));
            if (claimable == null)
            {
                Logger.Warning($"[randomReward] ящик «{container.Name}» собрать не вышло: "
                    + $"вид «{container.VisualType}», группа «{container.GroupType}», "
                    + $"дропов {container.MultiDropCount}, редкость {container.Rarity}");
                return null;
            }

            // Пишем, что именно выдали: без этого причину «анимации нет, награды
            // нет» приходится искать вслепую — на проводе всё корректно, а
            // расходится обычно содержимое.
            Logger.Print($"[randomReward] выдан ящик «{container.Name}» (вид «{container.VisualType}», "
                + $"группа «{container.GroupType}»), предложений {claimable.Rewards.Count}: "
                + Describe(claimable));

            if (home.RandomRewards == null) home.RandomRewards = new List<ClaimableRandomReward>();
            home.RandomRewards.Add(claimable);

            if (send) Send(homeMode);
            return claimable;
            }
        }

        /// <summary>
        /// Колоды события, которых у игрока ещё нет. Берём из cards.csv по
        /// MetaType 12 — так же, как их собирает витрина набора.
        /// </summary>
        private static List<int> MissingCollabDecks(ClientHome home, int count)
        {
            List<int> result = new List<int>();

            DataTable table = DataTables.Get(DataType.Card);
            if (table == null || count < 1) return result;

            foreach (CardData card in table.GetDatas())
            {
                if (result.Count >= count) break;
                if (card == null || card.Disabled) continue;
                if (card.MetaType != 12) continue;
                if (string.IsNullOrEmpty(card.Target)) continue;

                int deckId = card.GetInstanceId() + 23000000;
                if (home.UnlockedRogueliteDecks != null
                    && home.UnlockedRogueliteDecks.Contains(deckId)) continue;

                result.Add(deckId);
            }

            return result;
        }

        /// <summary>Строка CollabSkin 1-199 Gem в random_rewards.csv, класс таблицы 79.</summary>
        private const int COLLAB_SKIN_ROW  = 79000090;

        /// <summary>Строка Bling M Collab — запасная награда, когда альбом уже собран.</summary>
        private const int COLLAB_BLING_ROW = 79000063;

        /// <summary>
        /// Карточка события. Обычный путь CreateReward здесь не работает: у коллабных
        /// контейнеров веса лежат в TicketsInCollabDraw, а розыгрыш читает
        /// TicketsInRareStar и её соседей, поэтому наград не находится вовсе, Give
        /// возвращает null и клиенту нечего вскрывать — анимации нет, а валюта списана.
        ///
        /// Поэтому награду собираем сами: выдаём первый несобранный скин альбома,
        /// а когда альбом закрыт — блинги. Идентификатор строки награды обязан
        /// разрешаться в random_rewards.csv, иначе Build отсеет выдачу при отправке.
        /// </summary>
        public static ClaimableRandomReward GiveCollabCardPack(HomeMode homeMode,
            RandomRewardContainerData container, IReadOnlyList<int> albumSkins)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || container == null) return null;

            ClaimableRandomReward claimable;

            lock (home.RewardSync)
            {
                claimable = new ClaimableRandomReward
                {
                    FormatVersion = ClaimableRandomReward.CURRENT_FORMAT_VERSION,
                    ContainerGlobalId = container.GetGlobalId(),
                    SourceType = 0,
                    UpgradeTaps = 0,
                };

                int skin = 0;
                if (albumSkins != null)
                {
                    foreach (int candidate in albumSkins)
                    {
                        if (home.UnlockedSkins != null
                            && home.UnlockedSkins.Contains(GlobalId.CreateGlobalId(29, candidate))) continue;
                        skin = candidate;
                        break;
                    }
                }

                if (skin != 0)
                {
                    claimable.Rewards.Add(new LogicGemOffer(ShopItem.Skin, 1) { SkinDataId = skin });
                    claimable.RewardDataIds.Add(COLLAB_SKIN_ROW);
                }

                // Колоды события. Раньше контейнер отдавал только скин или блинги,
                // карточек в нём не было вовсе — поэтому после перезахода у игрока
                // ничего и не оставалось. Колоды берём те, которых ещё нет, и
                // отдаём как карту (вид выдачи 4): LogicGatchaDrop запишет её в
                // список открытых колод дома.
                int wanted = container.MultiDropCount > 0 ? container.MultiDropCount : 1;
                foreach (int deck in MissingCollabDecks(home, wanted))
                {
                    claimable.Rewards.Add(new LogicGemOffer(ShopItem.HeroPower, 1) { ItemDataId = deck });
                    claimable.RewardDataIds.Add(COLLAB_BLING_ROW);
                }

                if (claimable.Rewards.Count == 0)
                {
                    claimable.Rewards.Add(new LogicGemOffer(ShopItem.Bling, 100));
                    claimable.RewardDataIds.Add(COLLAB_BLING_ROW);
                }

                if (!claimable.IsValid()) return null;

                if (home.RandomRewards == null) home.RandomRewards = new List<ClaimableRandomReward>();
                home.RandomRewards.Add(claimable);
            }

            Send(homeMode);
            return claimable;
        }

        public static ClaimableRandomReward GiveStarrDrop(HomeMode homeMode, bool send = true)
        {
            ClientHome home = homeMode?.Home;
            if (home == null) return null;

            if (home.RandomRewardSeed == 0) home.RandomRewardSeed = Environment.TickCount | 1;
            return Give(homeMode, RollWinContainer(new Random(home.RandomRewardSeed++)), send);
        }

        public static int GiveStarrDrops(HomeMode homeMode, int count, int rarity = 0, bool send = true)
        {
            if (homeMode?.Home == null || count <= 0) return 0;

            ClientHome home = homeMode.Home;
            if (home.RandomRewards == null) home.RandomRewards = new List<ClaimableRandomReward>();
            int initialCount = home.RandomRewards.Count;

            string containerName = null;
            switch (rarity)
            {
                case 1: containerName = "RareStar"; break;
                case 2: containerName = "SuperRareStar"; break;
                case 3: containerName = "EpicStar"; break;
                case 4: containerName = "MythicStar"; break;
                case 5: containerName = "LegendaryStar"; break;
                case 6: containerName = "OverchargeStar"; break;
            }

            int queued = 0;
            for (int i = 0; i < count; i++)
            {
                ClaimableRandomReward reward = containerName == null
                    ? GiveStarrDrop(homeMode, false)
                    : Give(homeMode, containerName, false);

                if (reward == null)
                {
                    // A milestone claim must never leave half of a multi-drop reward queued.
                    if (home.RandomRewards.Count > initialCount)
                        home.RandomRewards.RemoveRange(initialCount, home.RandomRewards.Count - initialCount);
                    return 0;
                }

                queued++;
            }

            if (send && queued > 0) Send(homeMode);
            return queued;
        }

        /// <summary>
        /// Предложение в выдачу. Открыт наружу, потому что этой же раскладкой
        /// выдаются награды рангового пропуска.
        /// </summary>
        public static GatchaDrop CreateGatchaDrop(LogicGemOffer offer)
        {
            GatchaDrop drop;
            switch (offer.Type)
            {
                case ShopItem.Coin: drop = new GatchaDrop(7); break;
                case ShopItem.Gems: drop = new GatchaDrop(8); break;
                case ShopItem.StarPoints: drop = new GatchaDrop(12); break;
                case ShopItem.RecruitToken: drop = new GatchaDrop(22); break;
                case ShopItem.ChromaticToken: drop = new GatchaDrop(23); break;
                case ShopItem.PowerPoints: drop = new GatchaDrop(24); break;
                case ShopItem.Bling: drop = new GatchaDrop(25); break;
                case ShopItem.CoinDoubler: drop = new GatchaDrop(2); break;
                case ShopItem.Spray:
                    return new GatchaDrop(11) { Count = 1, DataGlobalId = GlobalId.CreateGlobalId(68, offer.SkinDataId) };

                // Карточка бойца. Вид выдачи 4 — тот же, что у клиента:
                // LogicGatchaDrop берёт карту из ссылки на данные и ставит её
                // через LogicClientAvatar::setItem.
                case ShopItem.HeroPower:
                    drop = new GatchaDrop(4);
                    drop.Count = 1;
                    drop.CardGlobalId = offer.ItemDataId;
                    return drop;

                case ShopItem.Brawler:
                    drop = new GatchaDrop(1);
                    drop.Count = 1;
                    drop.DataGlobalId = offer.ItemDataId;
                    return drop;

                case ShopItem.Skin:
                    drop = new GatchaDrop(9);
                    drop.Count = 1;
                    drop.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    return drop;

                case ShopItem.Emote:
                    drop = new GatchaDrop(11);
                    drop.Count = 1;
                    drop.DataGlobalId = GlobalId.CreateGlobalId(52, offer.SkinDataId);
                    return drop;

                case ShopItem.PlayerThumbnail:
                    drop = new GatchaDrop(11);
                    drop.Count = 1;
                    drop.DataGlobalId = GlobalId.CreateGlobalId(28, offer.SkinDataId);
                    return drop;

                default: return null;
            }

            drop.Count = offer.Count;
            return drop;
        }

        private static void AddSkinCosmetics(HomeMode homeMode, LogicGemOffer offer,
            DeliveryUnit unit, HashSet<int> addedCosmetics)
        {
            if (offer?.Type != ShopItem.Skin || homeMode?.Home == null) return;

            SkinData skin = DataTables.Get(DataType.Skin)?.GetDataWithId<SkinData>(offer.SkinDataId);
            if (skin == null) return;

            DataTable emoteTable = DataTables.Get(DataType.Emote);
            if (emoteTable != null)
            {
                foreach (EmoteData emote in emoteTable.GetDatas())
                {
                    if (emote == null) continue;
                    if (!string.Equals(emote.Skin, skin.Name, StringComparison.Ordinal)) continue;

                    int globalId = emote.GetGlobalId();
                    if (homeMode.Home.UnlockedEmotes?.Contains(globalId) == true) continue;
                    if (!addedCosmetics.Add(globalId)) continue;

                    unit.AddDrop(new GatchaDrop(11)
                    {
                        Count = 1,
                        DataGlobalId = globalId
                    });
                }
            }

            DataTable thumbnailTable = DataTables.Get(DataType.PlayerThumbnail);
            if (thumbnailTable == null) return;

            foreach (PlayerThumbnailData thumbnail in thumbnailTable.GetDatas())
            {
                if (thumbnail == null) continue;
                if (!string.Equals(thumbnail.CatalogPreRequirementSkin, skin.Name,
                        StringComparison.Ordinal)) continue;

                int globalId = thumbnail.GetGlobalId();
                if (homeMode.Home.UnlockedThumbnails?.Contains(globalId) == true) continue;
                if (!addedCosmetics.Add(globalId)) continue;

                unit.AddDrop(new GatchaDrop(11)
                {
                    Count = 1,
                    DataGlobalId = globalId
                });
            }
        }

        /// <summary>
        /// Выдать набор предложений ровно тем же путём, каким менеджер выдаёт
        /// содержимое ящика: собрать единицу доставки, ДОБАВИТЬ К НЕЙ КОСМЕТИКУ
        /// СКИНОВ, применить к состоянию и тем же объектом отправить клиенту.
        ///
        /// Из-за отсутствия последнего шага ранговый пропуск и выдавал не то, что
        /// показывал клиент: иконки, спреи и прочая мелочь, которая цепляется к
        /// скину, до игрока не доезжала. Теперь у всех выдач один путь.
        /// </summary>
        public static bool Deliver(HomeMode homeMode, IEnumerable<LogicGemOffer> offers,
            int unitType = 100, int unk76 = 0)
        {
            if (homeMode?.Home == null || offers == null) return false;

            DeliveryUnit unit = new DeliveryUnit(unitType);
            HashSet<int> addedCosmetics = new HashSet<int>();

            foreach (LogicGemOffer offer in offers)
            {
                if (offer == null) continue;

                GatchaDrop drop = CreateGatchaDrop(offer);
                if (drop == null) continue;

                unit.AddDrop(drop);
                AddSkinCosmetics(homeMode, offer, unit, addedCosmetics);
            }

            if (unit.GetDrops().Length == 0) return false;

            LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
            if (unk76 != 0) delivery.Unk76 = unk76;
            delivery.DeliveryUnits.Add(unit);

            delivery.Execute(homeMode);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage
            {
                Command = delivery
            };
            homeMode.GameListener?.SendMessage(message);

            return true;
        }

        /// <summary>
        /// Выдать готовые виды выдачи (GatchaDrop) одной доставкой: применить к
        /// состоянию и тем же объектом отправить клиенту. Нужна там, где награда
        /// не выражается предложением магазина, — очки силы конкретному бойцу,
        /// карточка, косметика из писем.
        /// </summary>
        public static bool DeliverDrops(HomeMode homeMode, IEnumerable<GatchaDrop> drops, int unitType = 100)
        {
            if (homeMode?.Home == null || drops == null) return false;

            DeliveryUnit unit = new DeliveryUnit(unitType);
            foreach (GatchaDrop drop in drops)
                if (drop != null) unit.AddDrop(drop);
            if (unit.GetDrops().Length == 0) return false;

            LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
            delivery.DeliveryUnits.Add(unit);
            delivery.Execute(homeMode);

            homeMode.GameListener?.SendMessage(new AvailableServerCommandMessage { Command = delivery });
            return true;
        }

        public static int Claim(HomeMode homeMode, int context)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || homeMode.Avatar == null) return 2;

            lock (home.RewardSync)
            {
            if (home.RandomRewards == null || home.RandomRewards.Count == 0)
            {
                // Make sure a client that repeated 571 cannot keep a stale local manager.
                Send(homeMode);
                return 0;
            }

            // Награду в голове очереди сначала ЧИНИМ, и выбрасываем, только если
            // после чистки в ней не осталось ничего. Раньше здесь стояла та же
            // строгая проверка, что и в Build, и целые ящики пропадали из-за
            // одного неузнанного предложения.
            while (home.RandomRewards.Count > 0)
            {
                ClaimableRandomReward head = home.RandomRewards[0];
                if (head != null && head.SanitizedCopy(out int dropped) != null)
                {
                    if (dropped > 0)
                    {
                        Logger.Warning($"[randomReward] в выдаваемой награде "
                            + $"{head.ContainerGlobalId} негодных предложений: {dropped}");
                    }
                    break;
                }

                Logger.Warning($"[randomReward] выброшена пустая награда: контейнер "
                    + $"{head?.ContainerGlobalId}, наград {head?.Rewards?.Count}, "
                    + $"идентификаторов {head?.RewardDataIds?.Count}");
                home.RandomRewards.RemoveAt(0);
            }
            if (home.RandomRewards.Count == 0)
            {
                Send(homeMode);
                return 0;
            }

            ClaimableRandomReward claimable = home.RandomRewards[0];
            home.RandomRewards.RemoveAt(0);

            DeliveryUnit unit = new DeliveryUnit(100);
            HashSet<int> addedCosmetics = new HashSet<int>();
            for (int i = 0; i < claimable.Rewards.Count; i++)
            {
                LogicGemOffer offer = claimable.Rewards[i];

                // Списки могут разойтись — по индексу лезем только убедившись,
                // что он есть. Раньше тут вылетало исключение прямо посреди выдачи.
                int rewardDataId = i < claimable.RewardDataIds.Count ? claimable.RewardDataIds[i] : 0;

                if (GlobalId.GetClassId(rewardDataId) == (int)DataType.BrawlerBoxReward && IsOwned(homeMode, offer))
                {
                    BrawlerBoxRewardData source = DataTables.Get(DataType.BrawlerBoxReward)
                        ?.GetDataByGlobalId<BrawlerBoxRewardData>(rewardDataId);

                    if (source != null)
                    {
                        offer = BuildOffer(homeMode, source.FallbackTypeName, 0, 0, 0,
                            source.FallbackAmount, Random.Shared);
                    }
                }
                GatchaDrop drop = CreateGatchaDrop(offer);
                if (drop == null) continue;

                unit.AddDrop(drop);
                AddSkinCosmetics(homeMode, offer, unit, addedCosmetics);
            }

            if (unit.GetDrops().Length > 0)
            {
                LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand
                {
                    Unk76 = 5
                };
                delivery.DeliveryUnits.Add(unit);

                // Apply the exact same drops to authoritative state and to the client.
                delivery.Execute(homeMode);

                AvailableServerCommandMessage deliveryMessage = new AvailableServerCommandMessage
                {
                    Command = delivery
                };
                homeMode.GameListener?.SendMessage(deliveryMessage);
            }

            Send(homeMode);

            return 0;
            }
        }

        public static void Send(HomeMode homeMode)
        {
            if (homeMode?.GameListener == null || homeMode.Home == null) return;

            LogicRefreshRandomRewardsCommand command = new LogicRefreshRandomRewardsCommand();
            command.Manager = Build(homeMode.Home);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

    }
}
