using HashCore.Laser.Logic.Data;
using HashCore.Laser.Logic.Home.Items;

namespace HashCore.Laser.Logic.Home.RandomReward
{
    public partial class RandomRewardManager
    {
        public static bool RewardGigiWin(HomeMode mode, Guid battleToken, int slot, bool competitive,
            bool won, DateTime utcNow)
        {
            if (mode?.Home == null || mode.Avatar == null || slot != 40 || !competitive || !won || battleToken == Guid.Empty)
                return false;
            lock (mode.Home.RewardSync)
            {
                var home = mode.Home;
                if (home.Events?.Any(e => e.Slot == slot && e.BrawlerEarlyAccess != null &&
                    e.EndTime > utcNow.ToLocalTime()) != true) return false;
                var state = home.GigiEarlyAccess;
                if (state.RewardedBattles.Contains(battleToken)) return false;
                state.GrantDailyTicket(utcNow);
                if (state.Tickets <= 0) return false;
                int seed = home.RandomRewardSeed == 0 ? Environment.TickCount | 1 : home.RandomRewardSeed;
                ClaimableRandomReward box = null;
                if (state.Wins % 2 == 1)
                {
                    var container = GetContainer("DaredevilBrawlerBox");
                    if (container == null) return false;
                    box = CreateGigiBox(mode, container, new Random(seed));
                    if (box == null) return false;
                }
                if (box != null)
                {
                    home.RandomRewards.Add(box);
                    home.RandomRewardSeed = unchecked(seed + 1);
                }
                state.TryConsumeTicket();
                state.TryRecordWin(utcNow);
                state.RewardedBattles.Add(battleToken);
                return true;
            }
        }

        /// <summary>
        /// Уже есть у игрока. Открыт наружу: по этому же признаку клиент
        /// пропускает награду и берёт запасную (LogicRewards::getFirstValidReward),
        /// и ранговый пропуск обязан выбирать так же.
        /// </summary>
        public static bool IsOwned(HomeMode mode, LogicGemOffer offer) => offer.Type switch
        {
            ShopItem.Brawler => mode.Avatar.HasHero(offer.ItemDataId),
            ShopItem.Skin => mode.Home.UnlockedSkins?.Contains(GlobalId.CreateGlobalId(29, offer.SkinDataId)) == true,
            ShopItem.Emote => mode.Home.UnlockedEmotes?.Contains(GlobalId.CreateGlobalId(52, offer.SkinDataId)) == true,
            ShopItem.PlayerThumbnail => mode.Home.UnlockedThumbnails?.Contains(GlobalId.CreateGlobalId(28, offer.SkinDataId)) == true,
            ShopItem.Spray => mode.Home.UnlockedSprays?.Contains(GlobalId.CreateGlobalId(68, offer.SkinDataId)) == true,
            _ => false
        };

        public static BrawlerBoxRewardData RollGigiRow(Random random)
        {
            var rows = DataTables.Get(DataType.BrawlerBoxReward).GetDatas().Cast<BrawlerBoxRewardData>()
                .Where(row => row.TicketsInDaredevilBrawlerBox > 0).ToArray();
            int total = rows.Sum(row => row.TicketsInDaredevilBrawlerBox);
            if (total == 0) return null;
            int roll = random.Next(total);
            foreach (var row in rows)
            {
                roll -= row.TicketsInDaredevilBrawlerBox;
                if (roll < 0) return row;
            }
            return null;
        }

        private static ClaimableRandomReward CreateGigiBox(HomeMode mode, RandomRewardContainerData container,
            Random random, IEnumerable<ClaimableRandomReward>? prepared = null)
        {
            if (mode.Avatar == null || container.MultiDropCount <= 0) return null;
            var box = new ClaimableRandomReward
            {
                FormatVersion = ClaimableRandomReward.CURRENT_FORMAT_VERSION,
                ContainerGlobalId = container.GetGlobalId()
            };
            for (int i = 0; i < container.MultiDropCount; i++)
            {
                var row = RollGigiRow(random);
                if (row == null) return null;
                var offer = BuildGigiOffer(mode, row, random);
                if (offer == null || IsOwned(mode, offer) || IsDuplicateUnlock(box, offer) ||
                    (mode.Home.RandomRewards?.Any(p => p != null && IsDuplicateUnlock(p, offer)) ?? false) ||
                    (prepared?.Any(p => IsDuplicateUnlock(p, offer)) ?? false))
                    offer = BuildOffer(mode, row.FallbackTypeName, 0, 0, 0, row.FallbackAmount, random);
                if (offer == null) return null;
                box.Rewards.Add(offer);
                box.RewardDataIds.Add(row.GetGlobalId());
            }
            return box.IsValid() ? box : null;
        }

        private static LogicGemOffer BuildGigiOffer(HomeMode mode, BrawlerBoxRewardData row, Random random)
        {
            string typeName = row.TypeName?.Trim() ?? string.Empty;
            if (typeName.Equals("TokenDoubler", StringComparison.OrdinalIgnoreCase))
                return Currency(ShopItem.CoinDoubler, RollAmount(random, row.AmountMin, row.AmountMax));
            if (typeName.Equals("SpecificBrawler", StringComparison.OrdinalIgnoreCase))
            {
                var hero = DataTables.Get(DataType.Character).GetData<CharacterData>(row.SpecificBrawler);
                if (hero == null || hero.Disabled || !hero.IsHero() || DataTables.GetUnlockCardFor(hero) == null) return null;
                return new LogicGemOffer { Type = ShopItem.Brawler, Count = 1, ItemDataId = hero.GetGlobalId() };
            }

            var (names, weights, type, tableType) = typeName switch
            {
                "ListedSkin" => (row.SkinsInReward, row.TicketsInSkinDraw, ShopItem.Skin, DataType.Skin),
                "ListedPin" => (row.PinsInReward, row.TicketsInPinDraw, ShopItem.Emote, DataType.Emote),
                "ListedProfilePic" => (row.ProfilePicsInReward, row.TicketsInProfilePicDraw, ShopItem.PlayerThumbnail, DataType.PlayerThumbnail),
                "ListedSpray" => (row.SpraysInReward, row.TicketsInSprayDraw, ShopItem.Spray, DataType.Spray),
                _ => (Array.Empty<string>(), Array.Empty<int>(), ShopItem.Coin, DataType.Character)
            };
            if (!typeName.StartsWith("Listed", StringComparison.OrdinalIgnoreCase))
                return BuildOffer(mode, typeName, row.TypeValue, row.TypePriceMin, row.TypePriceMax,
                    RollAmount(random, row.AmountMin, row.AmountMax), random);

            // Select using CSV weights, then pay fallback for an ineligible/duplicate
            // item. Never reroll the category to improve unlock odds.
            if (names == null || weights == null) return null;
            int total = weights.Where(w => w > 0).Sum();
            if (total == 0 || names.Length != weights.Length) return null;
            int roll = random.Next(total);
            for (int j = 0; j < names.Length; j++)
            {
                if (weights[j] <= 0) continue;
                roll -= weights[j];
                if (roll >= 0) continue;
                var data = DataTables.Get(tableType).GetData<LogicData>(names[j]);
                if (data == null) return null;
                int heroId = 0;
                switch (data)
                {
                    case SkinData skin:
                        var conf = DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skin.Conf);
                        if (skin.Disabled || conf == null || !HasRequiredHero(mode, conf.Character)) return null;
                        heroId = DataTables.Get(DataType.Character).GetData<CharacterData>(conf.Character).GetGlobalId();
                        break;
                    case EmoteData pin when pin.Disabled || !HasRequiredHero(mode, pin.Character): return null;
                    case PlayerThumbnailData icon when icon.Disabled || !HasRequiredHero(mode, icon.RequiredHero): return null;
                    case SprayData spray when spray.Disabled || !HasRequiredHero(mode, spray.Character): return null;
                }
                return new LogicGemOffer { Type = type, Count = 1, SkinDataId = data.GetInstanceId(), ItemDataId = heroId };
            }
            return null;
        }

        public static bool PurchaseGigiBoxes(HomeMode mode, OfferBundle bundle)
        {
            if (mode?.Home == null || mode.Avatar == null || bundle == null) return false;
            lock (mode.Home.RewardSync)
            {
                var home = mode.Home;
                var container = GetContainer("DaredevilBrawlerBox");
                if (container == null || bundle.Purchased || bundle.EndTime <= DateTime.UtcNow || bundle.Cost < 0 ||
                    string.IsNullOrEmpty(bundle.Claim) || (bundle.Claim != "debug" && home.OffersClaimed.Contains(bundle.Claim)) ||
                    bundle.Items.Count == 0 || bundle.Items.Any(o => o == null || o.Type != ShopItem.BrawlerBox ||
                        o.SkinDataId != container.GetGlobalId() || o.Count <= 0)) return false;
                int balance = bundle.Currency switch { 0 => mode.Avatar.Diamonds, 1 => mode.Avatar.Gold, 6 => mode.Avatar.Blings, _ => -1 };
                if (balance < bundle.Cost) return false;
                long count = bundle.Items.Sum(o => (long)o.Count);
                if (count > 1000) return false; // Bound work for malformed server shop configuration.
                int seed = home.RandomRewardSeed == 0 ? Environment.TickCount | 1 : home.RandomRewardSeed;
                var pending = new List<ClaimableRandomReward>();
                for (int i = 0; i < count; i++)
                {
                    var box = CreateGigiBox(mode, container, new Random(seed++), pending);
                    if (box == null) return false;
                    pending.Add(box);
                }
                bool paid = bundle.Currency switch
                {
                    0 => mode.Avatar.UseDiamonds(bundle.Cost),
                    1 => mode.Avatar.UseGold(bundle.Cost),
                    6 => mode.Avatar.UseBlings(bundle.Cost),
                    _ => false
                };
                if (!paid) return false;
                home.RandomRewards.AddRange(pending);
                home.RandomRewardSeed = seed;
                bundle.Purchased = true;
                if (bundle.Claim != "debug") home.OffersClaimed.Add(bundle.Claim);
                Send(mode);
                return true;
            }
        }
    }
}
