namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Server;

    /// <summary>
    /// Покупка гаджета, пассивки или гипера, тип 557.
    ///
    /// Формат снят с клиента (`LogicPurchaseGadgetOrStarPowerCommand::decode`
    /// 0x1003cb578): VInt с номером строки в cards.csv и булев признак «плачу
    /// самоцветами». В `execute` (0x1003cb73c) клиент берёт карточку из таблицы 23
    /// по этому номеру, списывает `DirectPurchasePrice` и зовёт `setItem` —
    /// то есть кладёт карточку в инвентарь.
    ///
    /// На сервере команда не была зарегистрирована вообще: в логе она падала как
    /// «command 557 not found», покупка нигде не сохранялась и после перезахода
    /// пропадала.
    /// </summary>
    public class LogicPurchaseGadgetOrStarPowerCommand : Command
    {
        /// <summary>Номер строки карточки в cards.csv.</summary>
        public int CardInstanceId;

        /// <summary>Игрок доплачивает самоцветами вместо монет.</summary>
        public bool PayWithGems;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            CardInstanceId = stream.ReadVInt();
            PayWithGems = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Avatar == null) return -1;

            DataTable table = DataTables.Get(DataType.Card);
            if (table == null) return -1;

            CardData card = table.GetDataWithId<CardData>(CardInstanceId);
            if (card == null)
            {
                Logger.Warning($"[purchase::card] карточка {CardInstanceId} не найдена");
                return -1;
            }

            // Гаджет, пассивка и гипер — только эти три вида карточек покупаются
            // поштучно. Остальное приходит из ящиков и наград.
            if (card.MetaType != 4 && card.MetaType != 5 && card.MetaType != 6)
            {
                Logger.Warning($"[purchase::card] карточка {card.Name} не покупается поштучно (MetaType {card.MetaType})");
                return -1;
            }

            Hero hero = FindHeroFor(homeMode, card);
            if (hero == null)
            {
                Logger.Warning($"[purchase::card] у игрока нет бойца под карточку {card.Name}");
                return -1;
            }

            if (hero.UnlockedCards == null) hero.UnlockedCards = new List<int>();
            if (hero.UnlockedCards.Contains(CardInstanceId)) return 0;

            int price = card.DirectPurchasePrice;
            if (price > 0 && !PayWithGems && !homeMode.Avatar.UseGold(price)) return -2;

            hero.UnlockedCards.Add(CardInstanceId);

            // Клиент сразу ставит купленное в слот, делаем так же.
            switch (card.MetaType)
            {
                case 4: hero.SelectedStarPowerId = CardInstanceId; break;
                case 5: hero.SelectedGadgetId = CardInstanceId; break;
                case 6: hero.SelectedOverChargeId = CardInstanceId; break;
            }

            Logger.Print($"[purchase::card] {card.Name} куплена для {hero.CharacterData?.Name}");
            return 0;
        }

        /// <summary>Боец, которому принадлежит карточка: колонка Target в cards.csv.</summary>
        private static Hero FindHeroFor(HomeMode homeMode, CardData card)
        {
            foreach (Hero hero in homeMode.Avatar.Heroes)
            {
                if (hero?.CharacterData == null) continue;
                if (hero.CharacterData.Name == card.Target) return hero;
            }

            return null;
        }

        public override int GetCommandType()
        {
            return 557;
        }
    }
}
