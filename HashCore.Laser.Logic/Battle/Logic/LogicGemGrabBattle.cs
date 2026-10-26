namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Захват кристаллов.
    ///
    /// Посреди карты стоит источник и раз в несколько секунд выбрасывает кристалл.
    /// Кристаллы подбирают на бегу, за каждый команде идёт очко, а убитый боец всё
    /// несомое рассыпает.
    ///
    /// Побеждает не тот, кто первым набрал нужное число, а тот, кто удержал его
    /// до конца обратного отсчёта: пока идёт отсчёт, соперник может отобрать хотя
    /// бы один кристалл, и отсчёт сорвётся.
    ///
    /// Число кристаллов и длина отсчёта в таблице режимов этой сборки не заданы,
    /// поэтому взяты те же, по которым уже играет старый бой: десять и семнадцать
    /// секунд.
    /// </summary>
    public class LogicGemGrabBattle : LogicServerBattle
    {
        /// <summary>Сколько кристаллов нужно удержать.</summary>
        public const int DefaultScoreToWin = 10;

        /// <summary>Сколько тиков идёт обратный отсчёт.</summary>
        public const int CountdownTicks = 20 * 17;

        /// <summary>Имя предмета-источника в таблице.</summary>
        public const string OrbSpawnerName = "OrbSpawner";

        /// <summary>Имя предмета-кристалла в таблице.</summary>
        public const string PointName = "Point";

        /// <summary>Сколько кристаллов нужно удержать в этом бою.</summary>
        public int ScoreToWin = DefaultScoreToWin;

        /// <summary>На каком тике отсчёт кончится. Ноль означает, что он не идёт.</summary>
        public int CountdownEndTick;

        /// <summary>Команда, которая ведёт отсчёт; минус один — никто.</summary>
        public int LeadingTeam = -1;

        /// <summary>Данные кристалла, они же нужны при рассыпании.</summary>
        public ItemData PointData;

        /// <summary>Источник кристаллов на карте.</summary>
        public LogicOrbSpawnerItem Spawner;

        public LogicGemGrabBattle(LogicBattleSession session) : base(session)
        {
        }

        /// <summary>
        /// Ставит источник кристаллов. Место — середина карты, как и у старого боя.
        ///
        /// Без данных предмета в таблице ставить нечего, и бой тогда просто идёт
        /// без кристаллов: это лучше, чем падать посреди кадра.
        /// </summary>
        public bool CreateSpawner()
        {
            PointData = FindItem(PointName);
            ItemData spawnerData = FindItem(OrbSpawnerName);

            if (spawnerData == null || PointData == null || TileMap == null) return false;

            Spawner = new LogicOrbSpawnerItem(spawnerData);
            Spawner.PointData = PointData;

            GameObjectManager.AddGameObject(Spawner);
            Spawner.SetPosition(TileMap.LogicWidth / 2, TileMap.LogicHeight / 2);

            return true;
        }

        /// <summary>Сколько кристаллов у команды. Это сумма очков её игроков.</summary>
        public int GetTeamScore(int teamIndex)
        {
            int score = 0;

            for (int i = 0; i < Players.Count; i++)
            {
                LogicPlayer player = Players[i];
                if (player != null && player.TeamIndex == teamIndex) score += player.Score;
            }

            return score;
        }

        /// <summary>
        /// Конец боя. Пока отсчёт не пошёл, бой не кончается ничем другим: даже
        /// выбитая насухо команда возрождается, и решает только счёт.
        /// </summary>
        protected override bool CalculateIsGameOver()
        {
            int first = GetTeamScore(0);
            int second = GetTeamScore(1);

            int leader = -1;

            if (first >= ScoreToWin && first > second) leader = 0;
            else if (second >= ScoreToWin && second > first) leader = 1;

            if (leader == -1)
            {
                // Счёт сорвался: отсчёт начнётся заново, а не продолжится с места.
                CountdownEndTick = 0;
                LeadingTeam = -1;
                return false;
            }

            if (LeadingTeam != leader)
            {
                LeadingTeam = leader;
                CountdownEndTick = Tick + CountdownTicks;
                return false;
            }

            return Tick >= CountdownEndTick;
        }

        /// <summary>Чья команда победила. Спрашивается уже после конца боя.</summary>
        public override int GetWinnerTeam()
        {
            return LeadingTeam;
        }

        /// <summary>
        /// Сколько ещё идёт отсчёт. Клиент рисует по этому числу обратный отсчёт
        /// над счётом, и пока отсчёта нет, там ноль.
        /// </summary>
        protected override int GetCountdown()
        {
            if (CountdownEndTick <= 0) return 0;

            int left = CountdownEndTick - Tick;
            return left > 0 ? left : 0;
        }

        private static ItemData FindItem(string name)
        {
            DataTable table = DataTables.Get(DataType.Item);
            if (table == null) return null;

            foreach (LogicData data in table.GetDatas())
            {
                ItemData item = data as ItemData;
                if (item != null && item.Name == name) return item;
            }

            return null;
        }
    }
}
