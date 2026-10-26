namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Режим боя на сервере, 1:1 с LogicBattleModeServer.
    ///
    /// Смещения оригинала, по которым собран класс:
    ///   +16   счётчик тиков            (уходит в encode как номер кадра)
    ///   +104  тайлмап
    ///   +112  массив игроков
    ///   +155  бой закончен
    ///   +212  id режима
    ///   +216  данные вариации режима
    ///   +308  тик, на котором бой закончился
    ///   +328  флаг «модификатор уже добавлен»
    ///   +332  счётчик, по которому добавляется модификатор 6
    ///   +1108 счётчик догоняющих подтиков
    ///   +1121 флаг, который дописывается в поток после энкода
    ///   +1123 включено ли масштабирование сложности ботов
    ///   +1344 объект режима игры
    ///   +1448 бой идёт офлайн, с ботами
    /// </summary>
    public partial class LogicBattleModeServer
    {
        /// <summary>Режимы, для которых tick() добавляет модификатор 6.</summary>
        private const int GameModeVariationA = 61;
        private const int GameModeVariationB = 10;

        public LogicGameObjectManagerServer GameObjectManager;

        /// <summary>Поле +104.</summary>
        public LogicTileMap TileMap;

        /// <summary>
        /// Поиск пути. 0x1004e7c30 отдаёт его персонажам, когда те строят маршрут.
        /// Один на весь бой: он тяжёлый и переиспользуется между вызовами.
        /// </summary>
        public LogicPathFinder PathFinder;

        /// <summary>
        /// Поле +24. Генератор боя. Зерно ставится через setRandomSeed
        /// (0x1004e2358), которая заодно пишет его и в поле +20.
        /// </summary>
        public readonly LogicRandom Random = new LogicRandom();

        /// <summary>Поле +20. Зерно, с которого бой стартовал.</summary>
        public int RandomSeed;

        public readonly List<LogicPlayer> Players = new List<LogicPlayer>();

        /// <summary>Номер текущего тика, поле +16.</summary>
        public int Tick;

        /// <summary>Поле +155. Пока не взведено, кадр каждый раз пересчитывает условие конца.</summary>
        public bool IsGameOver;

        /// <summary>Поле +308. Тик, на котором бой признан законченным, плюс один.</summary>
        public int GameOverTick;

        /// <summary>Поле +212.</summary>
        public int GameMode;

        /// <summary>Поле +332.</summary>
        public int EventModifierSpawnCounter;

        /// <summary>Поле +328.</summary>
        private bool _eventModifierAdded;

        /// <summary>Поле +1123.</summary>
        public bool ScaleBotDifficultyEnabled;

        /// <summary>Поле +1448.</summary>
        public bool IsOffline;

        /// <summary>
        /// Поле +1108. Сколько подтиков ещё надо догнать. executeOneTick в конце
        /// уменьшает его и, пока он не ушёл в минус, зовёт себя заново.
        /// </summary>
        public int CatchUpTicks;

        public LogicBattleModeServer()
        {
            GameObjectManager = new LogicGameObjectManagerServer(this);
        }

        /// <summary>
        /// Кадр боя целиком, как его гоняет BattleMode::update (0x100376d3c).
        ///
        /// Порядок обязателен: сначала снимаются мёртвые объекты и чистятся
        /// события прошлого тика, затем применяется накопленный ввод, и только
        /// потом идёт шаг мира. Если поменять местами первое и третье, объект,
        /// умерший в прошлом кадре, отработает лишний тик.
        /// </summary>
        public void Update()
        {
            GameObjectManager.PreTick(Players);
            ExecuteOfflineInputMessages();
            TickUpdate(1);
        }

        /// <summary>0x1004e6b34. Применяет накопленный ввод игроков.</summary>
        protected virtual void ExecuteOfflineInputMessages()
        {
        }

        /// <summary>
        /// 0x1004e6a60. Оригинал крутит executeOneTick ровно count раз:
        /// счётчик там заводится как count + 1 и цикл идёт, пока он больше единицы.
        /// </summary>
        public void TickUpdate(int count)
        {
            if (count < 1) return;

            for (int i = 0; i < count; i++) ExecuteOneTick();
        }

        /// <summary>
        /// 0x1004e4114. Один шаг мира. Порядок взят из либы буквально:
        ///
        ///   1. пока бой не закончен — пересчитать условие конца;
        ///   2. ввод ботов;
        ///   3. шаг мира;
        ///   4. энкод состояния игрокам;
        ///   5. увеличить номер тика;
        ///   6. если остались догоняющие подтики — preTick и повторить кадр.
        ///
        /// ВАЖНО: preTick здесь вызывается ТОЛЬКО между догоняющими подтиками.
        /// В обычном кадре его зовёт внешний цикл ДО этого метода — см. Update.
        /// Поэтому объект, умерший в тике, ещё попадает в энкод этого кадра, а
        /// снимается в начале следующего.
        /// </summary>
        public void ExecuteOneTick()
        {
            if (!IsGameOver)
            {
                IsGameOver = CalculateIsGameOver();

                if (IsGameOver)
                {
                    OnGameOver();
                    GameOverTick = Tick + 1;
                }
            }

            if (IsOffline) ExecuteAIBotClientInput();

            TickWorld();

            EncodeState();

            Tick++;

            // Оригинал уменьшает счётчик ТОЛЬКО когда собирается повторить кадр:
            // проверка там стоит на исходном значении, а запись идёт уже внутри
            // ветки. Если уменьшать безусловно, счётчик уползает в минус между
            // кадрами и догон перестаёт работать.
            if (CatchUpTicks >= 1)
            {
                CatchUpTicks--;
                GameObjectManager.PreTick(Players);
                ExecuteOneTick();
            }
        }

        /// <summary>
        /// 0x1004e2bf4. Шаг мира. Объекты, тайлмап, отложенный спавн событий,
        /// объект режима и позиции игроков — ровно в этом порядке.
        /// </summary>
        public void TickWorld()
        {
            if (ScaleBotDifficultyEnabled) ScaleBotDifficulty();

            if ((GameMode == GameModeVariationA || GameMode == GameModeVariationB)
                && EventModifierSpawnCounter >= 3
                && !_eventModifierAdded)
            {
                AddEventModifier(6, 0);
                _eventModifierAdded = true;
            }

            GameObjectManager.Tick();
            TickTileMap();
            TickSpawnEventStuffDelayed();
            TickGameModeObject();
            TickPlayerPositions();
        }

        /// <summary>0x1004e48dc. Условие конца боя, своё на каждый режим.</summary>
        protected virtual bool CalculateIsGameOver()
        {
            return false;
        }

        /// <summary>Слот +56 объекта режима, который либа дёргает на game over.</summary>
        protected virtual void OnGameOver()
        {
        }

        /// <summary>0x1004e4cd4.</summary>
        protected virtual void ExecuteAIBotClientInput()
        {
        }

        /// <summary>LogicTileMap::tick, вызывается из 0x1004e2c64.</summary>
        protected virtual void TickTileMap()
        {
            if (TileMap != null) TileMap.Tick(GameObjectManager);
        }

        /// <summary>0x1004e52ac.</summary>
        protected virtual void TickSpawnEventStuffDelayed()
        {
        }

        /// <summary>
        /// Объект режима по смещению +1344. Пока бой идёт, зовётся слот +24,
        /// после конца боя — слот +40.
        /// </summary>
        protected virtual void TickGameModeObject()
        {
        }

        /// <summary>0x1004e2f98.</summary>
        protected virtual void TickPlayerPositions()
        {
        }

        /// <summary>0x1004e2cf8.</summary>
        protected virtual void AddEventModifier(int modifier, int extra)
        {
        }

        /// <summary>0x1004e2640.</summary>
        protected virtual void ScaleBotDifficulty()
        {
        }

        /// <summary>
        /// Энкод кадра. В оригинале это LogicGameObjectManagerServer::encode в поток
        /// каждого игрока плюс довесок, свой на режим: 7 пишет два числа из менеджера,
        /// 37 — закрашенные тайлы, 41 — отфильтрованных персонажей, 64 — данные забега.
        /// </summary>
        protected virtual void EncodeState()
        {
        }

        /// <summary>0x1004e4c58.</summary>
        public int AlivePlayerCount()
        {
            int alive = 0;

            for (int i = 0; i < Players.Count; i++)
            {
                LogicPlayer player = Players[i];
                if (player == null) continue;

                LogicGameObjectServer character =
                    GameObjectManager.GetGameObjectById(player.ControlledCharacterGlobalId);

                if (character != null && character.IsAlive()) alive++;
            }

            return alive;
        }

        /// <summary>0x1004e93d0.</summary>
        public void ForceGameOver()
        {
            IsGameOver = true;
            GameOverTick = Tick + 1;
        }

        /// <summary>0x1004e7c30.</summary>
        public LogicPathFinder GetPathFinder()
        {
            if (PathFinder == null && TileMap != null) PathFinder = new LogicPathFinder(TileMap);
            return PathFinder;
        }

        /// <summary>Поле +160. Точки, с которых каждая команда смотрит на поле.</summary>
        public readonly List<List<LogicTeamMemberElement>> TeamMemberPositions =
            new List<List<LogicTeamMemberElement>>();

        /// <summary>
        /// 0x1004e4d48. Глаза команды. У несуществующей команды их нет, и это не
        /// ошибка: ничейный объект видит поле только сам за себя.
        /// </summary>
        public List<LogicTeamMemberElement> GetTeamMemberPositions(int teamIndex)
        {
            if (teamIndex < 0 || teamIndex >= TeamMemberPositions.Count) return null;

            return TeamMemberPositions[teamIndex];
        }

        /// <summary>
        /// Свойства режима, от которых зависит бой. Все они смотрят на строку
        /// режима из таблицы; здесь это точки расширения, и сам режим их задаёт.
        /// </summary>
        public virtual bool IsKnockout()
        {
            return false;
        }

        public virtual bool IsPayloadSingle()
        {
            return false;
        }

        public virtual bool IsHotZone()
        {
            return false;
        }

        /// <summary>Возрождаются ли бойцы после смерти.</summary>
        public virtual bool HasRespawns()
        {
            return true;
        }

        /// <summary>0x1004e2358.</summary>
        public void SetRandomSeed(int seed)
        {
            RandomSeed = seed;
            Random.SetSeed(seed);
        }

        /// <summary>0x1004e328c.</summary>
        public int GetRandomInt(int max)
        {
            return Random.Rand(max);
        }

        /// <summary>
        /// 0x1004e3260. Оригинал считает это как rand(max - min) + min, поэтому
        /// верхняя граница не включается.
        /// </summary>
        public int GetRandomInt(int min, int max)
        {
            return Random.Rand(max - min) + min;
        }

        /// <summary>
        /// Сколько игроков считает режим. Используется при кодировании владельца
        /// объекта, а не только для счёта.
        /// </summary>
        /// <summary>Сколько тиков длится вступление перед началом боя.</summary>
        public virtual int GetIntroTicks()
        {
            return 0;
        }

        /// <summary>Включён ли в бою модификатор события.</summary>
        public virtual bool HasEventModifier(int modifier)
        {
            return false;
        }

        public virtual int GetPlayerCount()
        {
            return Players.Count;
        }

        public LogicPlayer GetPlayer(int index)
        {
            if (index < 0 || index >= Players.Count) return null;
            return Players[index];
        }
    }
}
