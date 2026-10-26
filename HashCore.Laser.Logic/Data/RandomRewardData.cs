namespace HashCore.Laser.Logic.Data
{
    using HashCore.Laser.Logic.Battle.Logic;

    public class RandomRewardData : LogicData
    {
        public RandomRewardData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public string TypeName { get; set; }
        public int TypeValue { get; set; }
        public int TypePriceMin { get; set; }
        public int TypePriceMax { get; set; }
        public int AmountMin { get; set; }
        public int AmountMax { get; set; }
        public string FallbackTypeName { get; set; }
        public int FallbackAmount { get; set; }

        /// <summary>
        /// Сколько билетов у этой награды в розыгрыше данного контейнера.
        ///
        /// **Из-за этого менеджер случайных наград не работал вообще нигде.**
        /// Свойства назывались `TicketsInRareDraw` и далее, а колонки в
        /// random_rewards.csv зовутся `TicketsInRareStar` и далее. `Row`
        /// привязывает колонки строго по имени свойства, поэтому ВСЕ билеты
        /// молча оставались нулями: сумма весов выходила нулевой, розыгрыш не
        /// находил ни одной награды, ящик собирался пустым и выбрасывался. С
        /// точки зрения игрока — «получено» и ничего.
        ///
        /// Имя колонки — это `TicketsIn` плюс имя контейнера, и так для 39 из 43
        /// контейнеров таблицы (остальные четыре — копилка клуба и ящики бойцов —
        /// разыгрываются по своим таблицам). Поэтому колонку выбираем по
        /// контейнеру, а не по одной лишь редкости: иначе не работали бы ни
        /// коллабные ящики, ни подарочные, ни сезонные, ни ранговый.
        /// </summary>
        public int GetTicketsForContainer(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName)) return 0;
            return LogicDataAccess.GetInt(this, "TicketsIn" + containerName.Trim());
        }

        /// <summary>
        /// Запасной путь, когда контейнер неизвестен: обычные Starr Drop по
        /// редкости. Имена колонок — настоящие, из таблицы.
        /// </summary>
        public int GetTickets(int rarity)
        {
            switch (rarity)
            {
                case 1: return StarTickets("Rare");
                case 2: return StarTickets("SuperRare");
                case 3: return StarTickets("Epic");
                case 4: return StarTickets("Mythic");
                case 5: return StarTickets("Legendary");
                case 6: return StarTickets("Overcharge");
            }

            return 0;
        }

        /// <summary>
        /// Билеты обычного Starr Drop нужной редкости.
        ///
        /// Таблицу уже один раз подменяли: в прежней колонки звались
        /// `TicketsInRareDraw`, в нынешней — `TicketsInRareStar`. Читаем сначала
        /// нынешнее имя, потом прежнее, чтобы сервер работал с любой версией
        /// таблицы и молчаливых нулей больше не случилось.
        /// </summary>
        private int StarTickets(string rarityName)
        {
            int tickets = LogicDataAccess.GetInt(this, "TicketsIn" + rarityName + "Star");
            if (tickets != 0) return tickets;

            return LogicDataAccess.GetInt(this, "TicketsIn" + rarityName + "Draw");
        }
    }
}
