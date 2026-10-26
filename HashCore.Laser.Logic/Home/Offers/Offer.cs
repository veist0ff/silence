namespace HashCore.Laser.Logic.Home.Items
{
    using Newtonsoft.Json; 

    [JsonObject(MemberSerialization.OptIn)]
    public class LogicGemOffer
    {
        [JsonProperty] public ShopItem Type;
        [JsonProperty] public int Count;
        [JsonProperty] public int ItemDataId;
        [JsonProperty] public int SkinDataId;

        public LogicGemOffer()
        {
        }

        public LogicGemOffer(ShopItem type, int count)
        {
            Type = type;
            Count = count;
        }

        public LogicGemOffer(ShopItem type, int count, int itemGlobalId) : this(type, count)
        {
            ItemDataId = itemGlobalId;
        }

        public LogicGemOffer(ShopItem type, int count, int itemGlobalId, int skinGlobalId) : this(type, count, itemGlobalId)
        {
            SkinDataId = skinGlobalId;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt((int)Type);
            stream.WriteVInt(Count);
            ByteStreamHelper.WriteDataReference(stream, ItemDataId);
            stream.WriteVInt(SkinDataId);
        }

        /// <summary>
        /// Обратная сторона Encode: ровно то же, что делает клиент в
        /// `LogicGemOffer::decode` (0x1004f2ef0). Нужна там, где предложение
        /// приходит от клиента, — например внутри QuestData в команде 979.
        /// </summary>
        public void Decode(ByteStream stream)
        {
            Type = (ShopItem)stream.ReadVInt();
            Count = stream.ReadVInt();
            ItemDataId = ByteStreamHelper.ReadDataReference(stream);
            SkinDataId = stream.ReadVInt();
        }
    }

 public enum ShopItem
{
    FreeBox = 0,
    Coin = 1,
    GuaranteedBox = 2,  //вырезано
    Brawler = 3,  
    Skin = 4,
    Item = 5,
    BrawlBox = 6,
    Ticket = 7,
    HeroPower = 8,
    CoinDoubler = 9,
    MegaBox = 10,
    Keys = 11,
    WildcardPower = 12,
    EventSlot = 13,
    BigBox = 14,
    AdBox = 15,
    Gems = 16,
    StarPoints = 17,
    QuestsFeature = 18,
    Emote = 19,
    EmoteBundle = 20,
    RandomEmotes = 21,
    RandomEmotesForBrawler = 22,
    RandomEmoteOfRarity = 23,
    SkinAndHero = 24,
    PlayerThumbnail = 25,
    PurchaseOptionSkin = 26,
    RandomEmotesPackOfRarity = 27,
    BrawlPassTokens = 28,
    ClubFeature = 29,
    GuaranteedHeroWithLevel = 30,
    GuaranteedBoxWithLevel = 31,
    GearToken = 32, GearScrap = 33,  //вырезано 
    LevelUpgrade = 34,
    Spray = 35,
    SprayBundle = 36,
    SpraySlot = 37,
    RecruitToken = 38,
    ChromaticToken = 39,
    PowerPoints = 41,
    BrawlPassTailReward = 42,
    PlayerTitle = 43,
    DailyQuests = 44,
    Bling = 45,
    BrawlPassBundle = 46,
    OverchargeStarrDrop = 47,
    BrawlerUpgradeFromLevelToLevel = 48,
    StarrDropContainer = 49,
    RandomStarrDrop = 50,
    BrawlPassUnlockBrawler = 51,
    BrawlPassPlusBundle = 52,
    RandomCollabDrop = 53,  // не ворк
    SpecialEventTokens = 54,    // гемы но скорее всего когда врублен евент там определенные евентовые валюты
    ArchetypeItem = 56,  // v57 пакетики эти 
    CollabPoints = 57, // не ворк мб заворкает как и 54 айтем
    AngelicStar = 58,
    DemonicStar = 59,
    RandomPresentDrop = 60,  // падарки v59
    CompetitivePassTailReward = 61, // крч то что давалось в конце бп типо коины поинты и кредиты только без циферек (юзалось до v53)
    ProgressionSkinLevelUp = 62,  //вылет хд
    CompetitivePassPoints = 64,  //тож вылет
    DailyStreak = 65,   //блинги с именем ежедневные победы
    RandomSushiDrop = 66,
    TrophySeasonBox = 67,  //странно
    //дальше лень хд
    ChooseSuperRareBrawler = 69,
    RecordsFeature = 70,
    BrawlerBox = 71,
    MegaBox2 = 55,
    CollabScore = 72,
    GraffitiBox = 73,
    TrickBox = 74,
    CollabFeature = 75,
    CollabEventCurrency = 76,
    MechBox = 77
}
}
