namespace HashCore.Laser.Logic.Notification
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Calendar;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Home.RandomReward;
    using HashCore.Laser.Logic.Avatar.Structures;

    // Все письма клиента v64. Порядок полей в каждом Encode — порядок чтения в
    // decode соответствующего класса клиента; в комментарии — смещение поля в
    // объекте клиента, по нему поле упоминается в разборе.
    //
    // Типы 49, 77, 78 в NotificationFactory::create отсутствуют. 59, 60, 62 —
    // письма клубной лиги (AllianceLeagueEventDay{End,Start},
    // AllianceLeagueSeasonStart): у сервера нет клубной лиги, они не заведены.

    // ---------------------------------------------------------------- 26
    /// <summary>BuddyGachaResultNotification: награды, затем +52, +64, +56, +60.</summary>
    public class BuddyGachaResultNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public int Value52, Value64, Value56, Value60;
        public override int GetNotificationType() => 26;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Rewards.Count > 0) NotificationRewards.Deliver(homeMode, Rewards);
        }
    }

    // ---------------------------------------------------------------- 27
    /// <summary>BrawlerEarlyAccessBonusRewardNotification: булево +68, необязательные награды +56, VInt +64.</summary>
    public class BrawlerEarlyAccessBonusRewardNotification : BaseNotification
    {
        [JsonProperty] public bool Flag68;
        [JsonProperty] public List<LogicGemOffer> Rewards;
        [JsonProperty] public int Value64;
        public override int GetNotificationType() => 27;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteBoolean(Flag68);
            NotificationStream.WriteOptionalRewards(stream, Rewards);
            stream.WriteVInt(Value64);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Rewards != null) NotificationRewards.Deliver(homeMode, Rewards);
        }
    }

    // ---------------------------------------------------------------- 28
    public class Notification28 : BaseNotification
    {
        [JsonProperty] public int Value52, Value56;
        public override int GetNotificationType() => 28;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
        }
    }

    // ---------------------------------------------------------------- 29
    /// <summary>Автосбор сезона соревновательного пропуска: номер сезона.</summary>
    public class CompetitivePassAutoCollectNotification : BaseNotification
    {
        [JsonProperty] public int Season;
        public override int GetNotificationType() => 29;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season);
        }
    }

    // ---------------------------------------------------------------- 30
    public class CompetitivePassRewardNotification : BaseNotification
    {
        [JsonProperty] public int Season, BillingPackage;
        public override int GetNotificationType() => 30;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season);
            stream.WriteVInt(BillingPackage);
        }
    }

    // ---------------------------------------------------------------- 31
    public class CompetitivePassPointsRewardNotification : BaseNotification
    {
        [JsonProperty] public int Season, Points;
        [JsonProperty] public bool Flag60;
        public override int GetNotificationType() => 31;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season);
            stream.WriteVInt(Points);
            stream.WriteBoolean(Flag60);
        }
    }

    // ---------------------------------------------------------------- 32
    public class CompetitivePassIAPDeliveryNotification : BaseNotification
    {
        [JsonProperty] public int BillingPackage;
        public override int GetNotificationType() => 32;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(BillingPackage);
        }
    }

    // ---------------------------------------------------------------- 33
    /// <summary>VLong +56 (два VInt), VInt +64, VInt +52.</summary>
    public class Notification33 : BaseNotification
    {
        [JsonProperty] public long Value56;
        [JsonProperty] public int Value64, Value52;
        public override int GetNotificationType() => 33;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteLogicLong(stream, Value56);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value52);
        }
    }

    // ---------------------------------------------------------------- 34
    /// <summary>TemporaryHeroTeardownNotification: несобранные награды мастерства и боец.</summary>
    public class TemporaryHeroTeardownNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public int HeroGlobalId;
        public override int GetNotificationType() => 34;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
            ByteStreamHelper.WriteDataReference(stream, HeroGlobalId);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Rewards.Count > 0) NotificationRewards.Deliver(homeMode, Rewards);
        }
    }

    // ---------------------------------------------------------------- 35
    public class ContestEventEndNotification : BaseNotification
    {
        [JsonProperty] public int Value52, ContestId;
        [JsonProperty] public long? Value64;
        [JsonProperty] public int Value72, Value76;
        [JsonProperty] public LogicGemOffer Reward;
        [JsonProperty] public List<LogicGemOffer> Offers = new List<LogicGemOffer>();
        [JsonProperty] public int Value88;
        public override int GetNotificationType() => 35;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(ContestId);
            NotificationStream.WriteOptionalLogicLong(stream, Value64);
            stream.WriteVInt(Value72);
            stream.WriteVInt(Value76);
            NotificationStream.WriteOptionalOffer(stream, Reward);
            NotificationStream.WriteOfferList(stream, Offers);
            stream.WriteVInt(Value88);
        }
    }

    // ---------------------------------------------------------------- 36
    /// <summary>TrophySeasonEndNotification.</summary>
    public class TrophySeasonEndNotification : BaseNotification
    {
        [JsonProperty] public int Value80, Value88, Data72, Value120;
        [JsonProperty] public bool Flag124;
        [JsonProperty] public int Value112;
        [JsonProperty] public List<LogicGemOffer> DroppedRewards = new List<LogicGemOffer>();
        [JsonProperty] public int Value60, Value84;
        [JsonProperty] public bool Flag117;
        [JsonProperty] public int Value52, Value64, Value56;
        [JsonProperty] public List<TrophySeasonHeroEntry> Heroes = new List<TrophySeasonHeroEntry>();
        [JsonProperty] public bool Flag116;
        public override int GetNotificationType() => 36;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value80);
            stream.WriteVInt(Value88);
            ByteStreamHelper.WriteDataReference(stream, Data72);
            stream.WriteVInt(Value120);
            stream.WriteBoolean(Flag124);
            stream.WriteVInt(Value112);
            NotificationStream.WriteRewards(stream, DroppedRewards);
            stream.WriteVInt(Value60);
            stream.WriteVInt(Value84);
            stream.WriteBoolean(Flag117);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Heroes.Count);
            foreach (TrophySeasonHeroEntry hero in Heroes)
                if (stream.WriteBoolean(hero != null)) hero.Encode(stream);
            stream.WriteBoolean(Flag116);
        }
    }

    // ---------------------------------------------------------------- 37
    /// <summary>EventResourceRewardNotification: VInt +52 и одно предложение +56 (без флага).</summary>
    public class EventResourceRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public LogicGemOffer Reward = new LogicGemOffer();
        public override int GetNotificationType() => 37;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            (Reward ?? new LogicGemOffer()).Encode(stream);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Reward);
    }

    // ---------------------------------------------------------------- 38
    public class EventRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, Value56, Value60, Value64;
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        public override int GetNotificationType() => 38;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
            stream.WriteVInt(Value64);
            NotificationStream.WriteOfferList(stream, Rewards);
        }
    }

    // ---------------------------------------------------------------- 39
    public class AllianceCollabEventRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, Value56;
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public long AllianceId;
        public override int GetNotificationType() => 39;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
            NotificationStream.WriteOfferList(stream, Rewards);
            NotificationStream.WriteLogicLong(stream, AllianceId);
        }
    }

    // ---------------------------------------------------------------- 40
    public class Notification40 : BaseNotification
    {
        [JsonProperty] public int Value52;
        public override int GetNotificationType() => 40;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
        }
    }

    // ---------------------------------------------------------------- 41
    /// <summary>RevokeCosmeticsNotification: список отозванной косметики.</summary>
    public class RevokeCosmeticsNotification : BaseNotification
    {
        [JsonProperty] public List<int> Items = new List<int>();
        public override int GetNotificationType() => 41;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteDataReferenceList(stream, Items);
        }
    }

    // ---------------------------------------------------------------- 42
    public class Notification42 : BaseNotification
    {
        [JsonProperty] public int Count, Value56;
        public override int GetNotificationType() => 42;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Count);
            stream.WriteVInt(Value56);
        }
    }

    // ---------------------------------------------------------------- 43
    /// <summary>StarrDropContainerRewardNotification: контейнер (random_reward_containers) и количество.</summary>
    public class StarrDropContainerRewardNotification : BaseNotification
    {
        [JsonProperty] public int ContainerGlobalId;
        [JsonProperty] public int Count = 1;
        public override int GetNotificationType() => 43;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            ByteStreamHelper.WriteDataReference(stream, ContainerGlobalId);
            stream.WriteVInt(Count);
        }

        public override void OnView(HomeMode homeMode)
        {
            // Клиент: при ссылке на контейнер и количестве ≥ 1 — слушатель +856.
            if (ContainerGlobalId <= 0 || Count < 1) return;
            RandomRewardContainerData container = DataTables.Get(DataType.RandomRewardContainer)
                ?.GetDataByGlobalId<RandomRewardContainerData>(ContainerGlobalId);
            if (container == null) return;
            for (int i = 0; i < Count; i++) RandomRewardManager.Give(homeMode, container);
        }
    }

    // ---------------------------------------------------------------- 44
    public class AlliancePiggyBankEventEndNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public long AllianceId;
        [JsonProperty] public int Value64;
        [JsonProperty] public List<LogicGemOffer> Offers72 = new List<LogicGemOffer>();
        [JsonProperty] public int Value88;
        [JsonProperty] public List<LogicGemOffer> Offers96 = new List<LogicGemOffer>();
        [JsonProperty] public List<LogicGemOffer> Offers112 = new List<LogicGemOffer>();
        [JsonProperty] public int Data144, Value152, Value156;
        [JsonProperty] public List<LogicGemOffer> Offers128 = new List<LogicGemOffer>();
        public override int GetNotificationType() => 44;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            NotificationStream.WriteLogicLong(stream, AllianceId);
            stream.WriteVInt(Value64);
            NotificationStream.WriteOfferList(stream, Offers72);
            stream.WriteVInt(Value88);
            NotificationStream.WriteOfferList(stream, Offers96);
            NotificationStream.WriteOfferList(stream, Offers112);
            ByteStreamHelper.WriteDataReference(stream, Data144);
            stream.WriteVInt(Value152);
            stream.WriteVInt(Value156);
            NotificationStream.WriteOfferList(stream, Offers128);
        }
    }

    // ---------------------------------------------------------------- 45
    public class RankedRankUpRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public bool Flag56;
        [JsonProperty] public int Value60, Value64;
        [JsonProperty] public LogicGemOffer Reward;
        public override int GetNotificationType() => 45;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteBoolean(Flag56);
            stream.WriteVInt(Value60);
            stream.WriteVInt(Value64);
            NotificationStream.WriteOptionalOffer(stream, Reward);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Reward != null && Reward.Count > 0) NotificationRewards.Deliver(homeMode, Reward);
        }
    }

    // ---------------------------------------------------------------- 46
    public class RankedSeasonStartNotification : BaseNotification
    {
        [JsonProperty] public int Value52, Value56, Value60, Value64, Value68, Value72;
        [JsonProperty] public LogicGemOffer Reward;
        public override int GetNotificationType() => 46;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value68);
            stream.WriteVInt(Value72);
            NotificationStream.WriteOptionalOffer(stream, Reward);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Reward != null && Reward.Count > 0) NotificationRewards.Deliver(homeMode, Reward);
        }
    }

    // ---------------------------------------------------------------- 90
    /// <summary>
    /// ResourceRewardNotification: +52 причина, +56 ресурс (resources.csv),
    /// +60 количество, +64 вид подписи (1 — текст письма и «поддержка»,
    /// 3 — ещё один вариант), +68 (1 — «кредиты хрома переведены»).
    /// Последние два клиент читает, только если поток не кончился, — а он в
    /// обоих местах не кончается, так что они пишутся всегда. +52 == 59 —
    /// «возмещение блингами».
    /// </summary>
    public class ResourceRewardNotification : BaseNotification
    {
        [JsonProperty] public int Reason;
        [JsonProperty] public int ResourceGlobalId;
        [JsonProperty] public int Count;
        [JsonProperty] public int CaptionMode;
        [JsonProperty] public int Value68;
        public override int GetNotificationType() => 90;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Reason);
            stream.WriteVInt(ResourceGlobalId);
            stream.WriteVInt(Count);
            stream.WriteVInt(CaptionMode);
            stream.WriteVInt(Value68);
        }

        public override void OnView(HomeMode homeMode)
            => NotificationRewards.GiveResource(homeMode, ResourceGlobalId, Count);
    }

    // ---------------------------------------------------------------- 47
    /// <summary>StarPointsToBlingNotification: всё письмо 90 и ещё два VInt (+72, +76).</summary>
    public class StarPointsToBlingNotification : ResourceRewardNotification
    {
        [JsonProperty] public int Value72, Value76;
        public override int GetNotificationType() => 47;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value72);
            stream.WriteVInt(Value76);
        }
    }

    // ---------------------------------------------------------------- 48
    /// <summary>RewardBundleNotification: VInt +52 и набор наград.</summary>
    public class RewardBundleNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        public override int GetNotificationType() => 48;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            NotificationStream.WriteRewards(stream, Rewards);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Rewards);
    }

    // ---------------------------------------------------------------- 50
    public class OldMetaAutoCollectNotification : BaseNotification
    {
        [JsonProperty] public int Season64, Value52, Value56, Value60;
        public override int GetNotificationType() => 50;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season64);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
        }
    }

    // ---------------------------------------------------------------- 51
    /// <summary>Монеты, которые клиент начисляет сам (HeroLvlUpMaterial, причина 45).</summary>
    public class CoinCompensationNotification : BaseNotification
    {
        [JsonProperty] public int Coins, Value56, Value60;
        public override int GetNotificationType() => 51;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Coins);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Coins > 0) homeMode?.Avatar?.AddGold(Coins);
        }
    }

    // ---------------------------------------------------------------- 52
    /// <summary>GearRewardNotification: +56 боец, +60 снаряжение.</summary>
    public class GearRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, HeroGlobalId, GearGlobalId;
        public override int GetNotificationType() => 52;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(HeroGlobalId);
            stream.WriteVInt(GearGlobalId);
        }
    }

    // ---------------------------------------------------------------- 53 / 54
    public class AllianceQuestsSeasonEndNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public long AllianceId;
        [JsonProperty] public int Value64, Value68, Value72;
        public override int GetNotificationType() => 53;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            NotificationStream.WriteLogicLong(stream, AllianceId);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value68);
            stream.WriteVInt(Value72);
        }
    }

    public class AllianceQuestsSeasonStartNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public long AllianceId;
        [JsonProperty] public int Value64, Value68;
        public override int GetNotificationType() => 54;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            NotificationStream.WriteLogicLong(stream, AllianceId);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value68);
        }
    }

    // ---------------------------------------------------------------- 55
    /// <summary>QuestRewardNotification: награды +56, VInt +72, необязательные награды +64.</summary>
    public class QuestRewardNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public int Value72;
        [JsonProperty] public List<LogicGemOffer> ExtraRewards;
        public override int GetNotificationType() => 55;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
            stream.WriteVInt(Value72);
            NotificationStream.WriteOptionalRewards(stream, ExtraRewards);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Rewards);
    }

    // ---------------------------------------------------------------- 56
    /// <summary>Age gate: TID_AGE_GATE_NOTIFICATION_%d по +52.</summary>
    public class AgeGateNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        public override int GetNotificationType() => 56;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
        }
    }

    // ---------------------------------------------------------------- 57
    /// <summary>ImportantMessageNotification: строка-ссылка +56 и булево +72. Высокая плашка.</summary>
    public class ImportantMessageNotification : BaseNotification
    {
        [JsonProperty] public string Message;
        [JsonProperty] public bool Flag72;
        public override int GetNotificationType() => 57;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteStringReference(Message ?? string.Empty);
            stream.WriteBoolean(Flag72);
        }
    }

    // ---------------------------------------------------------------- 58
    public class AllianceMigrationNotification : BaseNotification
    {
        [JsonProperty] public long AllianceId;
        [JsonProperty] public int Data64;
        [JsonProperty] public string Name;
        public override int GetNotificationType() => 58;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteLogicLong(stream, AllianceId);
            ByteStreamHelper.WriteDataReference(stream, Data64);
            stream.WriteStringReference(Name ?? string.Empty);
        }
    }

    // ---------------------------------------------------------------- 61
    /// <summary>AllianceLeagueSeasonEndNotification. Клубные монеты — +100 (шестой из одиннадцати VInt).</summary>
    public class AllianceLeagueSeasonEndNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public long AllianceId, Value64;
        [JsonProperty] public int Data72;
        [JsonProperty] public int[] Values = new int[11];
        public override int GetNotificationType() => 61;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            NotificationStream.WriteLogicLong(stream, AllianceId);
            NotificationStream.WriteLogicLong(stream, Value64);
            ByteStreamHelper.WriteDataReference(stream, Data72);
            for (int i = 0; i < 11; i++) stream.WriteVInt(Values != null && i < Values.Length ? Values[i] : 0);
        }
    }

    // ---------------------------------------------------------------- 63 / 70
    /// <summary>
    /// ChallengeRewardNotification (0x... decode): награды +56, VInt +52
    /// (подпись в InboxItem::setCaptionWithRewards), VInt +64, строка +72,
    /// необязательные награды +80. При открытии клиент отдаёт слушателю +56 и +52
    /// — награду приносит сервер.
    /// </summary>
    public class ChallengeRewardNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public int CaptionValue;
        [JsonProperty] public int Value64;
        [JsonProperty] public string ChallengeName;
        [JsonProperty] public List<LogicGemOffer> ExtraRewards;
        [JsonProperty] public int Type = 63;
        public override int GetNotificationType() => Type == 70 ? 70 : 63;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
            stream.WriteVInt(CaptionValue);
            stream.WriteVInt(Value64);
            stream.WriteString(ChallengeName);
            NotificationStream.WriteOptionalRewards(stream, ExtraRewards);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Rewards);
    }

    // ---------------------------------------------------------------- 64
    /// <summary>
    /// Произвольное предложение: +56 тип LogicGemOffer, +60 количество. Подпись
    /// TID_SHOP_OFFER_TITLE_&lt;тип&gt;. Клиент кладёт {тип, количество} в
    /// giveReward.
    /// </summary>
    public class GemOfferRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public int OfferType;
        [JsonProperty] public int Count;
        public override int GetNotificationType() => 64;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(OfferType);
            stream.WriteVInt(Count);
        }

        public override void OnView(HomeMode homeMode)
            => NotificationRewards.Deliver(homeMode, new LogicGemOffer((ShopItem)OfferType, Count));
    }

    // ---------------------------------------------------------------- 65
    public class SkinPurchaseOptionRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, SkinGlobalId, Value64;
        public override int GetNotificationType() => 65;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            ByteStreamHelper.WriteDataReference(stream, SkinGlobalId);
            stream.WriteVInt(Value64);
        }
    }

    // ---------------------------------------------------------------- 66
    /// <summary>Всплывающий текст: только базовые поля. Во входящие не кладётся.</summary>
    public class FloaterTextNotification : BaseNotification
    {
        public FloaterTextNotification() { }
        public FloaterTextNotification(string text) { Text = text; }

        public override int GetNotificationType() => 66;
        public override bool IsPersistent => false;
    }

    // ---------------------------------------------------------------- 67
    public class RankedMidSeasonRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public NotificationRewardConfig Config;
        public override int GetNotificationType() => 67;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            if (stream.WriteBoolean(Config != null)) Config.Encode(stream);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Config?.Offer != null) NotificationRewards.Deliver(homeMode, Config.Offer);
        }
    }

    // ---------------------------------------------------------------- 68
    public class RankedSeasonEndNotification : BaseNotification
    {
        [JsonProperty] public int Value52, Value56, Value60, Value64, Value68, Value72;
        public override int GetNotificationType() => 68;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Value56);
            stream.WriteVInt(Value60);
            stream.WriteVInt(Value64);
            stream.WriteVInt(Value68);
            stream.WriteVInt(Value72);
        }
    }

    // ---------------------------------------------------------------- 69
    /// <summary>RewardCompensationNotification: необязательные награды +56, затем VInt +52.</summary>
    public class RewardCompensationNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards;
        [JsonProperty] public int Value52;
        public override int GetNotificationType() => 69;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteOptionalRewards(stream, Rewards);
            stream.WriteVInt(Value52);
        }
    }

    // ---------------------------------------------------------------- 71
    /// <summary>Жетоны пропуска: клиент сам прибавляет +56 к сезону +52.</summary>
    public class BrawlPassPointRewardNotification : BaseNotification
    {
        [JsonProperty] public int Season, Points;
        public override int GetNotificationType() => 71;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season);
            stream.WriteVInt(Points);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (homeMode?.Home != null && Points > 0) homeMode.Home.BrawlPassTokens += Points;
        }
    }

    // ---------------------------------------------------------------- 72
    /// <summary>Косметика: +52 — глобальный номер спрея, значка, иконки, титула или рамки.</summary>
    public class VanityItemRewardNotification : BaseNotification
    {
        [JsonProperty] public int ItemGlobalId;
        public override int GetNotificationType() => 72;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(ItemGlobalId);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (ItemGlobalId > 0)
                NotificationRewards.DeliverDrop(homeMode, new GatchaDrop(11) { Count = 1, DataGlobalId = ItemGlobalId });
        }
    }

    // ---------------------------------------------------------------- 73
    public class BrawlPassRewardNotification : BaseNotification
    {
        [JsonProperty] public int Season, PassType;
        public override int GetNotificationType() => 73;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Season);
            stream.WriteVInt(PassType);
        }
    }

    // ---------------------------------------------------------------- 74
    /// <summary>Компенсация билетов: клиент сам начисляет +56 гемов.</summary>
    public class TicketCompensationNotification : BaseNotification
    {
        [JsonProperty] public int Value52, Diamonds;
        public override int GetNotificationType() => 74;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Diamonds);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (Diamonds > 0) homeMode?.Avatar?.AddDiamonds(Diamonds);
        }
    }

    // ---------------------------------------------------------------- 75
    /// <summary>Скин за испытание: +52 глобальный номер скина, +56.</summary>
    public class ChallengeSkinRewardNotification : BaseNotification
    {
        [JsonProperty] public int SkinGlobalId, Value56;
        public override int GetNotificationType() => 75;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(SkinGlobalId);
            stream.WriteVInt(Value56);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.DeliverSkin(homeMode, SkinGlobalId);
    }

    // ---------------------------------------------------------------- 76
    public class QualifyNotification : BaseNotification
    {
        public override int GetNotificationType() => 76;
    }

    // ---------------------------------------------------------------- 79
    /// <summary>ScoreDecayNotification: записи и VInt +72. Блинги = сумма четвёртых полей.</summary>
    public class ScoreDecayNotification : BaseNotification
    {
        [JsonProperty] public List<TimedIntValueEntry> Entries = new List<TimedIntValueEntry>();
        [JsonProperty] public int Value72;
        public override int GetNotificationType() => 79;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Entries.Count);
            foreach (TimedIntValueEntry entry in Entries) (entry ?? new TimedIntValueEntry()).Encode(stream);
            stream.WriteVInt(Value72);
        }

        public override void OnView(HomeMode homeMode)
        {
            int total = 0;
            foreach (TimedIntValueEntry entry in Entries) total += entry?.Amount ?? 0;
            if (total > 0) homeMode?.Avatar?.AddBlings(total);
        }
    }

    // ---------------------------------------------------------------- 80
    /// <summary>Перенос звёздных очков: клиент сам начисляет +52 (TID_LEG_TRO_MIG_INFO_SHORT).</summary>
    public class StarPointMigrationNotification : BaseNotification
    {
        [JsonProperty] public int StarPoints;
        public override int GetNotificationType() => 80;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(StarPoints);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (homeMode?.Avatar != null && StarPoints > 0) homeMode.Avatar.StarPoints += StarPoints;
        }
    }

    // ---------------------------------------------------------------- 81
    /// <summary>
    /// Свободный текст, высокая плашка. +52 == 1 — отправитель «система»,
    /// иначе «поддержка».
    /// </summary>
    public class FreeTextNotification : BaseNotification
    {
        public const int SENDER_SUPPORT = 0;
        public const int SENDER_SYSTEM = 1;

        [JsonProperty] public int Sender;

        public FreeTextNotification() { }
        public FreeTextNotification(string text, int sender = SENDER_SYSTEM) { Text = text; Sender = sender; }

        public override int GetNotificationType() => 81;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Sender);
        }
    }

    // ---------------------------------------------------------------- 82
    /// <summary>BandNotification: письмо от игрока. Отправитель, два необязательных LogicLong, строка, VInt.</summary>
    public class BandNotification : BaseNotification
    {
        [JsonProperty] public string SenderName;
        [JsonProperty] public int SenderThumbnail = 28000000;
        [JsonProperty] public int SenderNameColor = 43000000;
        [JsonProperty] public long? Value64, Value72;
        [JsonProperty] public string Message80;
        [JsonProperty] public int Value88;
        public override int GetNotificationType() => 82;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            new PlayerDisplayData(SenderThumbnail, SenderNameColor, SenderName ?? string.Empty).Encode(stream);
            NotificationStream.WriteOptionalLogicLong(stream, Value64);
            NotificationStream.WriteOptionalLogicLong(stream, Value72);
            stream.WriteString(Message80);
            stream.WriteVInt(Value88);
        }
    }

    // ---------------------------------------------------------------- 83
    /// <summary>PromoPopupNotification: три текста, файл, строка-ссылка +96, VInt +88, три булевых.</summary>
    public class PromoPopupNotification : BaseNotification
    {
        [JsonProperty] public ChronosTextEntry Title = new ChronosTextEntry();
        [JsonProperty] public ChronosTextEntry Body = new ChronosTextEntry();
        [JsonProperty] public ChronosTextEntry Button = new ChronosTextEntry();
        [JsonProperty] public PromoFileEntry File = new PromoFileEntry();
        [JsonProperty] public string Link;
        [JsonProperty] public int Value88;
        [JsonProperty] public bool Flag112, Flag113, Flag114;
        public override int GetNotificationType() => 83;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            (Title ?? new ChronosTextEntry()).Encode(stream);
            (Body ?? new ChronosTextEntry()).Encode(stream);
            (Button ?? new ChronosTextEntry()).Encode(stream);
            (File ?? new PromoFileEntry()).Encode(stream);
            stream.WriteStringReference(Link ?? string.Empty);
            stream.WriteVInt(Value88);
            stream.WriteBoolean(Flag112);
            stream.WriteBoolean(Flag113);
            stream.WriteBoolean(Flag114);
        }
    }

    // ---------------------------------------------------------------- 84
    /// <summary>Карточка бойца (+56 — cards.csv): клиент кладёт {тип 5, 1, карточка} в giveReward.</summary>
    public class StarPowerRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, CardGlobalId;
        public override int GetNotificationType() => 84;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(CardGlobalId);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (CardGlobalId > 0)
                NotificationRewards.DeliverDrop(homeMode, new GatchaDrop(4) { Count = 1, CardGlobalId = CardGlobalId });
        }
    }

    // ---------------------------------------------------------------- 85
    /// <summary>RevokeNotification: +52 вид (1 — высокая плашка), +56, 8 байт +64, +60, строка +72.</summary>
    public class RevokeNotification : BaseNotification
    {
        [JsonProperty] public int Kind, Value56;
        [JsonProperty] public long Value64;
        [JsonProperty] public int Value60;
        [JsonProperty] public string Reason;
        public override int GetNotificationType() => 85;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Kind);
            stream.WriteVInt(Value56);
            stream.WriteLong(Value64);
            stream.WriteVInt(Value60);
            stream.WriteString(Reason);
        }
    }

    // ---------------------------------------------------------------- 86
    public class BrawlPassIAPDeliveryNotification : BaseNotification
    {
        [JsonProperty] public int BillingPackage;
        public override int GetNotificationType() => 86;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(BillingPackage);
        }
    }

    // ---------------------------------------------------------------- 87
    /// <summary>NewsPopupNotification: строка-ссылка +56.</summary>
    public class NewsPopupNotification : BaseNotification
    {
        [JsonProperty] public string Link;
        public override int GetNotificationType() => 87;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteStringReference(Link ?? string.Empty);
        }
    }

    // ---------------------------------------------------------------- 88 / 89 / 91
    /// <summary>Общая раскладка «+52, количество +56» писем 88, 89, 91.</summary>
    public abstract class CountRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52;
        [JsonProperty] public int Count;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(Count);
        }
    }

    /// <summary>Удвоитель жетонов (giveReward, тип 9).</summary>
    public class CoinDoublerRewardNotification : CountRewardNotification
    {
        public override int GetNotificationType() => 88;
        public override void OnView(HomeMode homeMode)
            => NotificationRewards.Deliver(homeMode, new LogicGemOffer(ShopItem.CoinDoubler, Count));
    }

    /// <summary>Гемы (giveReward, тип 16).</summary>
    public class GemRewardNotification : CountRewardNotification
    {
        public GemRewardNotification() { }
        public GemRewardNotification(int count, string text = null) { Count = count; Text = text; }

        public override int GetNotificationType() => 89;
        public override void OnView(HomeMode homeMode)
            => NotificationRewards.Deliver(homeMode, new LogicGemOffer(ShopItem.Gems, Count));
    }

    /// <summary>Билеты (giveReward, тип 7). В v64 билетов нет, доставлять нечего.</summary>
    public class TicketRewardNotification : CountRewardNotification
    {
        public override int GetNotificationType() => 91;
    }

    // ---------------------------------------------------------------- 92
    /// <summary>Очки силы конкретному бойцу: +56 боец, +60 количество (giveReward, тип 8).</summary>
    public class HeroPowerRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, HeroGlobalId, Count;
        public override int GetNotificationType() => 92;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(HeroGlobalId);
            stream.WriteVInt(Count);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (HeroGlobalId > 0 && Count > 0)
                NotificationRewards.DeliverDrop(homeMode, new GatchaDrop(6) { Count = Count, DataGlobalId = HeroGlobalId });
        }
    }

    // ---------------------------------------------------------------- 93
    /// <summary>Боец: +56 боец, +60 уровень (больше 1 — подпись «с уровнем»).</summary>
    public class HeroRewardNotification : BaseNotification
    {
        [JsonProperty] public int Value52, HeroGlobalId;
        [JsonProperty] public int Level = 1;
        public override int GetNotificationType() => 93;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Value52);
            stream.WriteVInt(HeroGlobalId);
            stream.WriteVInt(Level);
        }

        public override void OnView(HomeMode homeMode)
        {
            if (HeroGlobalId <= 0 || homeMode?.Avatar == null) return;
            if (homeMode.Avatar.HasHero(HeroGlobalId)) return;
            NotificationRewards.Deliver(homeMode, new LogicGemOffer(ShopItem.Brawler, 1, HeroGlobalId));
        }
    }

    // ---------------------------------------------------------------- 94
    /// <summary>Скин: +52 причина (1 → 16, 2 → 6 у слушателя), +56 скин, булево +60.</summary>
    public class SkinRewardNotification : BaseNotification
    {
        [JsonProperty] public int Reason, SkinGlobalId;
        [JsonProperty] public bool Flag60;
        public override int GetNotificationType() => 94;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Reason);
            stream.WriteVInt(SkinGlobalId);
            stream.WriteBoolean(Flag60);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.DeliverSkin(homeMode, SkinGlobalId);
    }

    // ---------------------------------------------------------------- 95
    public class TrophyWorldCompensationNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        public override int GetNotificationType() => 95;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Rewards);
    }

    // ---------------------------------------------------------------- 96
    /// <summary>Календарь возвращения: запись календаря, LogicDailyStreakEntry (два VInt), VInt +72.</summary>
    public class WelcomeBackLoginCalendarNotification : BaseNotification
    {
        [JsonProperty] public WelcomeBackLoginCalendarEntry Entry = new WelcomeBackLoginCalendarEntry();
        [JsonProperty] public int StreakType, StreakValue, Value72;
        public override int GetNotificationType() => 96;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            (Entry ?? new WelcomeBackLoginCalendarEntry()).Encode(stream);
            stream.WriteVInt(StreakType);
            stream.WriteVInt(StreakValue);
            stream.WriteVInt(Value72);
        }
    }

    // ---------------------------------------------------------------- 97
    public class BrawlPassAutoCollectSeasonNotification : BaseNotification
    {
        [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();
        [JsonProperty] public int Season;
        public override int GetNotificationType() => 97;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            NotificationStream.WriteRewards(stream, Rewards);
            stream.WriteVInt(Season);
        }

        public override void OnView(HomeMode homeMode) => NotificationRewards.Deliver(homeMode, Rewards);
    }

    // ---------------------------------------------------------------- 98
    /// <summary>
    /// Итог этапа рогалика: булево «победа», затем необязательные
    /// LogicPlayerRogueliteEventData и LogicRogueliteEvent. Сервер их не
    /// собирает и пишет отсутствующими.
    /// </summary>
    public class RogueliteStageResultNotification : BaseNotification
    {
        [JsonProperty] public bool Victory;

        /// <summary>+56 — снимок забега ПОСЛЕ этапа: пройденные этапы, места, выбранный боец.</summary>
        [JsonProperty] public PlayerRogueliteEventData Run;

        /// <summary>+64 — описание события (этапы и награды), по нему рисуется экран итога.</summary>
        [JsonProperty] public RogueliteEvent Event;

        public override int GetNotificationType() => 98;

        // Окно итога (RogueliteStagesPopup::showForEndResult) без обоих объектов
        // не открывается: «Invalid notification for RogueliteStagesPopup».
        // Во входящих его не видно — клиент сам удаляет письмо командой 514,
        // когда окно отработало (RogueliteUtil::shouldDeleteStageResultNotificationAfterUse).
        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteBoolean(Victory);
            if (stream.WriteBoolean(Run != null)) Run.Encode(stream);
            if (stream.WriteBoolean(Event != null)) Event.Encode(stream);
        }
    }
}
