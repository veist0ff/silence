namespace HashCore.Laser.Logic.Command.Home
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Items;

    /// <summary>
    /// Снять метку «новое» в магазине, тип 515.
    ///
    /// **Из-за этой команды разъезжался весь клиентский ход.** У клиента
    /// `LogicClearShopTickersCommand::decode` (0x1003c6fc8) читает после заголовка
    /// ЧЕТЫРЕ VInt-а, а у сервера `Decode` не был переопределён вовсе — ноль чтений.
    /// Поток оставался на четыре числа позади, следующий тип команды читался
    /// мусором («команда 1 не найдена»), и `EndClientTurn` обрывал разбор всего
    /// пакета. В пакете бывало и по 44 команды — терялись все, кроме первой,
    /// отсюда и рассинхрон состояний.
    ///
    /// Поля (по `execute`, 0x1003c703c):
    /// <list type="bullet">
    ///   <item>секция магазина,</item>
    ///   <item>новое состояние метки (0 новое, 1 просмотрено, 2 снято),</item>
    ///   <item>идентификатор предложения (+72 у LogicOfferBundle),</item>
    ///   <item>его номер в списке — запасной способ поиска.</item>
    /// </list>
    ///
    /// Состояние только РАСТЁТ и ограничено двойкой: и `setOfferState`
    /// (0x1004c3318), и все её соседи делают `state = max(state, new)` при `new &lt;= 2`.
    /// Раньше сервер вместо этого ставил всем предложениям сразу 2.
    /// </summary>
    public class LogicClearShopTickersCommand : Command
    {
        /// <summary>Какую часть магазина гасим. Значения — из switch в execute.</summary>
        public int Section;

        /// <summary>Новое состояние метки: 0 новое, 1 просмотрено, 2 снято.</summary>
        public int State;

        /// <summary>Идентификатор предложения, для секции 1.</summary>
        public int OfferId;

        /// <summary>Номер предложения в списке — если по идентификатору не нашлось.</summary>
        public int OfferIndex;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Section = stream.ReadVInt();
            State = stream.ReadVInt();
            OfferId = stream.ReadVInt();
            OfferIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            ClientHome home = homeMode?.Home;
            if (home == null) return 1;

            List<OfferBundle> bundles = home.OfferBundles;
            if (bundles == null) return 1;

            switch (Section)
            {
                case 1:
                    SetOfferState(bundles);
                    break;

                // Ежедневный подарок: витрины с контекстом 1 и 2.
                case 2:
                    Apply(bundles, IsDailyFreebie);
                    break;

                // Тройка у клиента ничего не делает — ветка switch пустая.
                case 3:
                    break;

                case 4:
                    home.TokenDoublerTickerState = Raise(home.TokenDoublerTickerState, State);
                    break;

                case 5:
                    home.EventTicketsTickerState = Raise(home.EventTicketsTickerState, State);
                    break;

                case 6:
                    home.CoinPacksTickerState = Raise(home.CoinPacksTickerState, State);
                    break;

                // Раздел скинов (LogicOfferBundle::isSkinSectionOffer, 0x1004f7350).
                case 8:
                    Apply(bundles, IsSkinSectionOffer);
                    break;

                // Предложения за валюту 3 и за валюту 5 — две отдельные полки.
                case 9:
                    Apply(bundles, bundle => bundle.Currency == 3);
                    break;

                case 10:
                    Apply(bundles, bundle => bundle.Currency == 5);
                    break;
            }

            return 0;
        }

        /// <summary>
        /// Секция 1 — одно предложение. Клиент ищет его по идентификатору, и лишь
        /// если такого нет — по номеру в списке.
        /// </summary>
        private void SetOfferState(List<OfferBundle> bundles)
        {
            foreach (OfferBundle bundle in bundles)
            {
                if (bundle.OfferId != OfferId) continue;
                bundle.State = Raise(bundle.State, State);
                return;
            }

            if (OfferIndex < 0 || OfferIndex >= bundles.Count) return;

            OfferBundle byIndex = bundles[OfferIndex];
            if (byIndex == null) return;

            byIndex.State = Raise(byIndex.State, State);
        }

        /// <summary>
        /// Единица означает «просмотрено»: клиент помечает только ПЕРВОЕ ещё не
        /// просмотренное предложение полки. Любое другое значение он раскатывает
        /// на всю полку.
        /// </summary>
        private void Apply(List<OfferBundle> bundles, System.Func<OfferBundle, bool> belongs)
        {
            if (State == 1)
            {
                foreach (OfferBundle bundle in bundles)
                {
                    if (!belongs(bundle) || bundle.State != 0) continue;
                    bundle.State = 1;
                    return;
                }

                return;
            }

            foreach (OfferBundle bundle in bundles)
            {
                if (!belongs(bundle)) continue;
                bundle.State = Raise(bundle.State, State);
            }
        }

        private static bool IsDailyFreebie(OfferBundle bundle)
        {
            return bundle.ShopItemContext == 1 || bundle.ShopItemContext == 2;
        }

        private static bool IsSkinSectionOffer(OfferBundle bundle)
        {
            if (bundle.Items == null || bundle.Items.Count != 1) return false;
            if (bundle.Items[0].Type != ShopItem.Skin) return false;
            if (bundle.Currency >= 3 && bundle.Currency <= 5) return false;
            return bundle.ShopItemContext == 4;
        }

        /// <summary>Состояние только растёт и не переваливает за 2.</summary>
        private static int Raise(int current, int wanted)
        {
            if (wanted > 2) return current;
            return wanted > current ? wanted : current;
        }

        public override int GetCommandType()
        {
            return 515;
        }
    }
}
