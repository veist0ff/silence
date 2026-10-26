namespace HashCore.Laser.Logic.Command.Home
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Home.RandomReward;
    using HashCore.Laser.Logic.Home.Skins;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Server;

    /// <summary>
    /// Забрать ступень рангового пропуска, тип 591.
    ///
    /// Формат снят с клиента (`LogicClaimCompetitivePassRewardCommand::decode`
    /// 0x1003c3bd8): три VInt — трек, сезон и номер ступени. Ступень ищется в
    /// competitive_pass_tiers.csv по этой тройке (`getTierByIndex`), награда берётся
    /// первой подходящей из пары основная/запасная (`LogicRewards::getFirstValidReward`).
    ///
    /// Треки в таблице: **1 — премиум**, **2 — бесплатный**, 3 — хвост (его забирает
    /// отдельная команда 594).
    ///
    /// Сервер эту команду не знал вовсе, поэтому ранговый пропуск не выдавал ничего.
    /// </summary>
    public class LogicClaimCompetitivePassRewardCommand : Command
    {
        public int TrackType;
        public int Season;
        public int Index;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            TrackType = stream.ReadVInt();
            Season = stream.ReadVInt();
            Index = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null || homeMode.Avatar == null) return 1;

            RankedMilestones tier = RankedPassRewards.FindTier(TrackType, Season, Index);
            if (tier == null)
            {
                Logger.Warning($"[rankedPass] ступени нет: трек {TrackType}, сезон {Season}, номер {Index}");
                return 1;
            }

            string key = RankedPassRewards.ClaimKey(TrackType, Season, Index);

            if (homeMode.Home.RankedPassClaimed == null)
                homeMode.Home.RankedPassClaimed = new List<string>();

            if (homeMode.Home.RankedPassClaimed.Contains(key)) return 3;

            if (!RankedPassRewards.Grant(homeMode, tier)) return 1;

            homeMode.Home.RankedPassClaimed.Add(key);
            Logger.Print($"[rankedPass] выдана ступень {tier.Name}");
            return 0;
        }

        public override int GetCommandType()
        {
            return 591;
        }
    }

    /// <summary>
    /// Общая часть для обеих команд рангового пропуска: поиск ступени в таблице и
    /// выдача её награды.
    /// </summary>
    internal static class RankedPassRewards
    {
        /// <summary>Строка из competitive_pass_tiers.csv по треку, сезону и номеру.</summary>
        public static RankedMilestones FindTier(int trackType, int season, int index)
        {
            DataTable table = DataTables.Get(DataType.RankedMilestones);
            if (table == null) return null;

            foreach (LogicData data in table.GetDatas())
            {
                if (data is not RankedMilestones tier) continue;
                if (tier.Type != trackType) continue;
                if (tier.Season != season) continue;
                if (tier.Index != index) continue;
                return tier;
            }

            return null;
        }

        public static string ClaimKey(int trackType, int season, int index)
        {
            return trackType + ":" + season + ":" + index;
        }

        /// <summary>
        /// Выдаёт награду ступени.
        ///
        /// Клиент берёт ПЕРВУЮ ГОДНУЮ награду (`LogicRewards::getFirstValidReward`):
        /// косметику, которая у игрока уже есть, он пропускает и рисует запасную.
        /// Сервер обязан выбирать по тому же правилу — иначе он выдаст не то, что
        /// показал клиент, и состояния разойдутся. Раньше сервер всегда пробовал
        /// основную, и на уже имеющихся иконках и спреях получался рассинхрон.
        /// </summary>
        public static bool Grant(HomeMode homeMode, RankedMilestones tier)
        {
            if (IsRewardValid(homeMode, tier.PrimaryRewardType, tier.PrimaryRewardData)
                && GrantOne(homeMode, tier.PrimaryRewardType, tier.PrimaryRewardCount, tier.PrimaryRewardData, tier.PrimaryRewardExtraData))
            {
                return true;
            }

            return GrantOne(homeMode, tier.SecondaryRewardType, tier.SecondaryRewardCount, tier.SecondaryRewardData, tier.SecondaryRewardExtraData);
        }

        /// <summary>Косметику, которая уже есть, клиент не показывает — и мы не выдаём.</summary>
        private static bool IsRewardValid(HomeMode homeMode, string rewardType, string rewardData)
        {
            if (string.IsNullOrWhiteSpace(rewardType)) return false;
            if (!int.TryParse(rewardType.Trim(), out int type)) return true;   // прокачка скина

            ShopItem item = (ShopItem)type;

            int instanceId = ResolveInstanceId(item, rewardData);
            if (instanceId <= 0) return true;

            LogicGemOffer offer = new LogicGemOffer(item, 1) { SkinDataId = instanceId };
            return !RandomRewardManager.IsOwned(homeMode, offer);
        }

        private static bool GrantOne(HomeMode homeMode, string rewardType, int count, string rewardData, int extraData = 0)
        {
            if (string.IsNullOrWhiteSpace(rewardType)) return false;
            if (count < 1) count = 1;

            // В таблице тип награды — уже числовой идентификатор предмета, кроме
            // одной строковой записи на прокачку скина.
            if (!int.TryParse(rewardType.Trim(), out int type))
            {
                if (rewardType.Trim() != "PROGRESSION_SKIN_LEVEL_UP")
                {
                    Logger.Warning($"[rankedPass] неизвестный тип награды «{rewardType}»");
                    return false;
                }

                return GrantProgressionSkin(homeMode, rewardData);
            }

            ShopItem item = (ShopItem)type;

            // Тот же подарок, но заданный числом.
            if (item == ShopItem.ProgressionSkinLevelUp) return GrantProgressionSkin(homeMode, rewardData);

            // Звёздные награды выдаются менеджером, а не единицей доставки.
            if (item == ShopItem.RandomStarrDrop || item == ShopItem.StarrDropContainer
                || item == ShopItem.OverchargeStarrDrop)
            {
                // Какой именно ящик — написано в данных награды (RankedStar,
                // OverchargeStar). Раньше имя игнорировалось и всегда катился
                // случайный ящик обычной редкости, то есть не тот, что у клиента.
                RandomRewardContainerData container = RandomRewardManager.GetContainer(rewardData);

                if (container != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (RandomRewardManager.Give(homeMode, container, false) == null) return false;
                    }
                }
                else
                {
                    // Редкость ящика — в ExtraData (хвост Pro Pass: 49,1,5 = легендарный).
                    // Без неё катился случайный ящик, и клиент показывал не тот дроп.
                    int rarity = item == ShopItem.OverchargeStarrDrop ? 6 : extraData;
                    if (RandomRewardManager.GiveStarrDrops(homeMode, count, rarity, false) != count) return false;
                }

                // Один общий снимок менеджера в конце, как это делает трофейная
                // дорога: досылать его на каждый ящик незачем.
                RandomRewardManager.Send(homeMode);
                return true;
            }

            LogicGemOffer offer = new LogicGemOffer(item, count);

            int instanceId = ResolveInstanceId(item, rewardData);
            if (instanceId > 0) offer.SkinDataId = instanceId;

            // Отдаём менеджеру случайных наград: он сам соберёт выдачу тем же
            // путём, что и содержимое ящика, и главное — добавит косметику,
            // которая цепляется к скину. Самодельная выдача этого не делала,
            // поэтому иконки и спреи до игрока не доезжали и клиент расходился
            // с сервером.
            if (!RandomRewardManager.Deliver(homeMode, new[] { offer }))
            {
                Logger.Warning($"[rankedPass] награду {item} собрать нечем");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Повышает прогрессионный скин на уровень и отдаёт клиенту открытый скин
        /// обычной выдачей, чтобы он показал получение.
        /// </summary>
        private static bool GrantProgressionSkin(HomeMode homeMode, string baseName)
        {
            SkinData unlocked = ProgressionSkins.LevelUp(homeMode, baseName);
            if (unlocked == null) return false;

            LogicGemOffer offer = new LogicGemOffer(ShopItem.Skin, 1)
            {
                SkinDataId = unlocked.GetInstanceId()
            };

            // Если скин уже открыт, показывать нечего — это не ошибка.
            RandomRewardManager.Deliver(homeMode, new[] { offer });
            return true;
        }

        /// <summary>
        /// Косметика в таблице задана именем, а в предложении нужен номер строки.
        /// </summary>
        private static int ResolveInstanceId(ShopItem item, string rewardData)
        {
            if (string.IsNullOrWhiteSpace(rewardData)) return 0;

            DataType type;

            switch (item)
            {
                case ShopItem.Emote: type = DataType.Emote; break;
                case ShopItem.PlayerThumbnail: type = DataType.PlayerThumbnail; break;
                case ShopItem.Spray: type = DataType.Spray; break;
                case ShopItem.Skin: type = DataType.Skin; break;
                case ShopItem.PlayerTitle: type = DataType.Titul; break;
                default: return 0;
            }

            LogicData data = DataTables.Get(type)?.GetData<LogicData>(rewardData.Trim());
            if (data == null)
            {
                Logger.Warning($"[rankedPass] «{rewardData}» нет в таблице {type}");
                return 0;
            }

            return data.GetInstanceId();
        }
    }
}
