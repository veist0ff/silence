namespace HashCore.Laser.Logic.Notification
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Home.RandomReward;

    /// <summary>
    /// Выдача наград при открытии письма.
    ///
    /// LogicViewInboxNotificationCommand::execute (0x1003d5428) делит типы на
    /// два вида. Одни клиент начисляет сам, прямо в аватар: звёздные очки (80),
    /// блинги (79), монеты (51), гемы (74), «прочие» ресурсы письма 90. Здесь
    /// сервер обязан сделать ровно то же молча, иначе счётчики разъедутся.
    ///
    /// Остальные клиент отдаёт слушателю дома — через giveReward
    /// (LogicOfferBundle из одного LogicGemOffer) или напрямую. У клиента этот
    /// слушатель ничего не меняет: награду приносит сервер доставкой
    /// (LogicGiveDeliveryItemsCommand), клиент применяет её и показывает анимацию.
    /// </summary>
    public static class NotificationRewards
    {
        // Номера строк resources.csv, которые клиент при открытии письма 90
        // превращает в предложение и отдаёт слушателю (а не начисляет сам).
        public const int RESOURCE_DIAMONDS = 5000000;
        public const int RESOURCE_COINS = 5000008;          // HeroLvlUpMaterial
        public const int RESOURCE_STAR_POINTS = 5000010;    // LegendaryTrophies
        public const int RESOURCE_RECRUIT_TOKENS = 5000019;
        public const int RESOURCE_CHROMATIC_TOKENS = 5000020;
        public const int RESOURCE_FAME = 5000021;
        public const int RESOURCE_POWER_POINTS = 5000022;
        public const int RESOURCE_BLING = 5000023;
        public const int RESOURCE_COLLAB_POINTS = 5000024;

        public static void Deliver(HomeMode homeMode, IEnumerable<LogicGemOffer> offers)
        {
            if (homeMode == null || offers == null) return;
            RandomRewardManager.Deliver(homeMode, offers);
        }

        public static void Deliver(HomeMode homeMode, LogicGemOffer offer)
        {
            if (offer == null) return;
            Deliver(homeMode, new[] { offer });
        }

        public static void DeliverDrop(HomeMode homeMode, GatchaDrop drop)
        {
            if (drop == null) return;
            RandomRewardManager.DeliverDrops(homeMode, new[] { drop });
        }

        public static void DeliverSkin(HomeMode homeMode, int skinGlobalId)
        {
            if (skinGlobalId <= 0) return;
            int instance = GlobalId.GetClassId(skinGlobalId) == 29
                ? GlobalId.GetInstanceId(skinGlobalId)
                : skinGlobalId;
            if (homeMode?.Home?.UnlockedSkins?.Contains(GlobalId.CreateGlobalId(29, instance)) == true) return;

            Deliver(homeMode, new LogicGemOffer(ShopItem.Skin, 1, 0, instance));
        }

        /// <summary>
        /// Письмо 90/47 — ресурс по ссылке на resources.csv. Монеты, хромо-,
        /// рекрут-кредиты, очки силы и блинги клиент отдаёт слушателю, их
        /// приносит доставка. Всё прочее он начисляет сам — сервер повторяет.
        /// </summary>
        public static void GiveResource(HomeMode homeMode, int resourceGlobalId, int count)
        {
            if (homeMode?.Avatar == null || count <= 0) return;

            switch (resourceGlobalId)
            {
                case RESOURCE_COINS:
                    DeliverDrop(homeMode, new GatchaDrop(7) { Count = count });
                    return;
                case RESOURCE_CHROMATIC_TOKENS:
                    DeliverDrop(homeMode, new GatchaDrop(23) { Count = count });
                    return;
                case RESOURCE_RECRUIT_TOKENS:
                    DeliverDrop(homeMode, new GatchaDrop(22) { Count = count });
                    return;
                case RESOURCE_POWER_POINTS:
                    DeliverDrop(homeMode, new GatchaDrop(24) { Count = count });
                    return;
                case RESOURCE_BLING:
                    DeliverDrop(homeMode, new GatchaDrop(25) { Count = count });
                    return;
                case RESOURCE_COLLAB_POINTS:
                    // Очков коллаборации сервер не хранит.
                    return;

                // Дальше — то, что клиент начисляет сам.
                case RESOURCE_DIAMONDS:
                    homeMode.Avatar.AddDiamonds(count);
                    return;
                case RESOURCE_STAR_POINTS:
                    homeMode.Avatar.StarPoints += count;
                    return;
                case RESOURCE_FAME:
                    homeMode.Avatar.FameCredits += count;
                    return;
            }
        }
    }
}
