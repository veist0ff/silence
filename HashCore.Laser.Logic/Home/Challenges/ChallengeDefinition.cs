namespace HashCore.Laser.Logic.Home.Challenges
{
    using HashCore.Laser.Logic.Home.Items;

    /// <summary>
    /// Испытание, как его видит клиент v64.
    ///
    /// Это не отдельная структура, а события в слотах 20..29: каждый слот —
    /// этап со своей картой, число побед этапа лежит в поле события +64
    /// (ChampionshipChallengePopup::getMaxWins складывает их по всем слотам
    /// 20..29). Всё личное — только в событии слота 20:
    ///
    ///   +60   сколько жизней потрачено (осталось = conf 28 − +60)
    ///   +88   всего побед (по нему getCurrentMap выбирает текущий этап)
    ///   +68   сколько раз докупали жизни (индекс в списке цен +80)
    ///   +80   цены докупки жизней (LogicConfData::getPriceForChallengeLives)
    ///   +100  ChallengeId из challenges.csv; при 0 докупка запрещена
    ///   +104  бесконечные жизни (hasRemainingLives)
    ///   +136  файл chronos с логотипом и шапкой (event_logo, event_header…)
    ///   +160  награда за прохождение (getCompletionRewardFromEvent / скин)
    ///
    /// Ключи конфигурации: 28 — жизней всего, 10039 — жизней за одну докупку.
    /// Награды за победы клиент просит отдельно: GetSeasonRewardsMessage (12938)
    /// с ключом 4 (ChallengeId 0), 5 (ChallengeId 1) или 6 (прочие) — ответ
    /// SeasonRewardsMessage (22033).
    /// </summary>
    public class ChallengeDefinition
    {
        public const int FIRST_SLOT = 20;
        public const int LAST_SLOT = 29;

        public const int CONF_MAX_LIVES = 28;
        public const int CONF_LIVES_PER_PURCHASE = 10039;

        /// <summary>Действующее испытание. Ставит сервер из gameplay.json; null — испытания нет.</summary>
        public static ChallengeDefinition Current;

        /// <summary>Имя выпуска. Сменили — прогресс всех игроков начинается заново.</summary>
        public string Key = "challenge";
        public int ChallengeId = 7;
        public ChronosFileEntry ChronosFile;
        public int MaxLives = 3;
        public int LivesPerPurchase = 1;
        public List<int> LivesPrices = new List<int>();
        public bool InfiniteLives;
        public DateTime? EndTime;
        public List<ChallengeStage> Stages = new List<ChallengeStage>();
        public List<ChallengeWinReward> WinRewards = new List<ChallengeWinReward>();
        public LogicGemOffer CompletionReward;

        public static bool IsChallengeSlot(int slot) => slot >= FIRST_SLOT && slot <= LAST_SLOT;

        public int TotalWins
        {
            get
            {
                int total = 0;
                foreach (ChallengeStage stage in Stages) total += stage.Wins;
                return total;
            }
        }

        public ChallengeStage GetStage(int slot) => Stages.Find(s => s.Slot == slot);

        /// <summary>Ключ, под которым клиент просит награды (EventItem-хелпер 0x1001338b8).</summary>
        public int RewardsKey => ChallengeId == 0 ? 4 : ChallengeId == 1 ? 5 : 6;
    }

    public class ChallengeStage
    {
        public int Slot;
        public string[] GameModes = new string[0];
        public int Location;
        public int Wins = 1;
    }

    /// <summary>Награда за N-ю победу — RewardEntry в SeasonRewardsMessage.</summary>
    public class ChallengeWinReward
    {
        public int Win;
        public LogicGemOffer Offer;
    }
}
