namespace HashCore.Laser.Logic.Home.Challenges
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Home.RandomReward;
    using HashCore.Laser.Logic.Notification;
    using HashCore.Laser.Server;

    /// <summary>Личный прогресс игрока в испытании. Хранится в доме.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ChallengeProgress
    {
        /// <summary>Какому выпуску принадлежит прогресс (ChallengeDefinition.Key).</summary>
        [JsonProperty] public string Key;
        [JsonProperty] public int Wins;
        [JsonProperty] public int LivesUsed;
        [JsonProperty] public int LivesPurchased;
        [JsonProperty] public bool Completed;
        [JsonProperty] public List<int> RewardedWins = new List<int>();
    }

    /// <summary>
    /// Логика испытания. Всё, что клиент считает у себя, повторено здесь теми же
    /// правилами, иначе после боя или докупки жизней дом разъедется.
    /// </summary>
    public static class ChallengeManager
    {
        /// <summary>Прогресс под текущий выпуск; при смене выпуска — чистый.</summary>
        public static ChallengeProgress GetProgress(ClientHome home)
        {
            ChallengeDefinition def = ChallengeDefinition.Current;
            if (home == null || def == null) return null;

            if (home.Challenge == null || home.Challenge.Key != def.Key)
                home.Challenge = new ChallengeProgress { Key = def.Key };
            home.Challenge.RewardedWins ??= new List<int>();
            return home.Challenge;
        }

        /// <summary>
        /// Личные поля на копию события перед отправкой дома. Число побед этапа
        /// (+64) одинаково для всех, остальное — только у слота 20.
        /// </summary>
        public static void ApplyToEvent(ClientHome home, EventData e)
        {
            ChallengeDefinition def = e?.Challenge;
            if (def == null) return;

            ChallengeStage stage = def.GetStage(e.Slot);
            e.ChallengeStageWins = stage?.Wins ?? 0;

            ChallengeProgress progress = GetProgress(home);
            e.ChallengeLivesUsed = progress?.LivesUsed ?? 0;
            e.ChallengeTotalWins = progress?.Wins ?? 0;
            e.ChallengeLivesPurchased = progress?.LivesPurchased ?? 0;
        }

        /// <summary>Ключи конфигурации испытания: 28 — жизней, 10039 — жизней за докупку.</summary>
        public static IEnumerable<KeyValuePair<int, int>> GetConfValues()
        {
            ChallengeDefinition def = ChallengeDefinition.Current;
            if (def == null) yield break;
            yield return new KeyValuePair<int, int>(ChallengeDefinition.CONF_MAX_LIVES, def.MaxLives);
            yield return new KeyValuePair<int, int>(ChallengeDefinition.CONF_LIVES_PER_PURCHASE, def.LivesPerPurchase);
        }

        public static bool HasRemainingLives(ChallengeDefinition def, ChallengeProgress progress)
            => def.InfiniteLives || def.MaxLives - progress.LivesUsed > 0;

        /// <summary>
        /// Итог боя в слоте испытания. Победа — как у клиента
        /// (BattleEndPopup::isChallengeWin): в выживании соло место не ниже 4-го,
        /// в дуо — не ниже 2-го, в остальных режимах — победа команды.
        /// Иначе тратится жизнь.
        /// </summary>
        public static void RegisterBattle(HomeMode homeMode, int eventSlot, bool won)
        {
            ChallengeDefinition def = ChallengeDefinition.Current;
            if (def == null || homeMode?.Home == null) return;
            if (!ChallengeDefinition.IsChallengeSlot(eventSlot)) return;

            ChallengeProgress progress = GetProgress(homeMode.Home);
            if (progress.Completed) return;

            if (!won)
            {
                if (!def.InfiniteLives) progress.LivesUsed++;
                Logger.Print($"[challenge] {def.Key}: поражение, жизней потрачено {progress.LivesUsed}/{def.MaxLives}");
                return;
            }

            progress.Wins++;
            Logger.Print($"[challenge] {def.Key}: победа {progress.Wins}/{def.TotalWins}");

            // Награда за эту победу — та, что клиент показал из SeasonRewardsMessage.
            List<LogicGemOffer> rewards = new List<LogicGemOffer>();
            foreach (ChallengeWinReward reward in def.WinRewards)
            {
                if (reward.Win != progress.Wins || reward.Offer == null) continue;
                if (progress.RewardedWins.Contains(reward.Win)) continue;
                rewards.Add(reward.Offer);
            }
            // Письма испытания только кладём в дом: бой кончился, клиент заберёт их с
            // домом при возврате. Отдельная команда 206 давала вторую копию с тем же
            // номером — просмотр отмечал одну, окно показывало другую до перезахода.
            if (rewards.Count > 0)
            {
                progress.RewardedWins.Add(progress.Wins);
                NotificationFactory.Store(homeMode.Home, new ChallengeRewardNotification
                {
                    Rewards = rewards,
                    CaptionValue = progress.Wins,
                    ChallengeName = def.Key
                });
            }

            if (progress.Wins >= def.TotalWins && def.TotalWins > 0)
            {
                progress.Completed = true;
                GiveCompletionReward(homeMode, def);
            }
        }

        /// <summary>
        /// Награда за прохождение. Скин приходит письмом 75
        /// (ChallengeSkinRewardNotification — клиент открывает QualifiedPopup),
        /// остальное — письмом 70 с набором наград.
        /// </summary>
        private static void GiveCompletionReward(HomeMode homeMode, ChallengeDefinition def)
        {
            LogicGemOffer reward = def.CompletionReward;
            Logger.Print($"[challenge] {def.Key}: пройдено");
            if (reward == null) return;

            if (reward.Type == ShopItem.Skin && reward.SkinDataId > 0)
            {
                NotificationFactory.Store(homeMode.Home, new ChallengeSkinRewardNotification
                {
                    SkinGlobalId = GlobalId.CreateGlobalId(29, reward.SkinDataId)
                });
                return;
            }

            NotificationFactory.Store(homeMode.Home, new ChallengeRewardNotification
            {
                Type = 70,
                Rewards = new List<LogicGemOffer> { reward },
                CaptionValue = def.TotalWins,
                ChallengeName = def.Key
            });
        }

        /// <summary>
        /// Докупка жизней (540). Зеркало LogicPurchaseChallengeLivesCommand:
        /// проверки из encode (0x1003ca8c4), действие из execute (0x1003ca99c) —
        /// списать цену по индексу «сколько раз докупали», вернуть 10039 жизней,
        /// увеличить счётчик докупок.
        /// </summary>
        public static int PurchaseLives(HomeMode homeMode)
        {
            ChallengeDefinition def = ChallengeDefinition.Current;
            if (def == null || homeMode?.Home == null || homeMode.Avatar == null) return 4;
            if (def.ChallengeId == 0) return 4;

            ChallengeProgress progress = GetProgress(homeMode.Home);
            int index = progress.LivesPurchased;
            int price = index >= 0 && index < def.LivesPrices.Count ? def.LivesPrices[index] : 0;
            if (price < 1 || def.LivesPerPurchase < 1) return 5;
            if (homeMode.Avatar.Diamonds < price) return 2;
            if (progress.LivesUsed < def.LivesPerPurchase) return 3;

            homeMode.Avatar.UseDiamonds(price);
            progress.LivesUsed -= def.LivesPerPurchase;
            progress.LivesPurchased++;
            Logger.Print($"[challenge] {def.Key}: куплено {def.LivesPerPurchase} жизней за {price}");
            return 0;
        }

        /// <summary>Записи наград для SeasonRewardsMessage.</summary>
        public static List<ChallengeWinReward> GetWinRewards(int key)
        {
            ChallengeDefinition def = ChallengeDefinition.Current;
            if (def == null || key != def.RewardsKey) return new List<ChallengeWinReward>();
            return def.WinRewards;
        }
    }
}
