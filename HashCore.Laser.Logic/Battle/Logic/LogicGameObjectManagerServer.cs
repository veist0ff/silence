namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Реестр объектов боя, 1:1 с LogicGameObjectManagerServer.
    ///
    /// Раскладка полей оригинала, по которой собран этот класс:
    ///   +0   общий массив объектов, счётчик +12   — отсортирован по GlobalId
    ///   +16  снимок для тика, счётчик +28
    ///   +32  персонажи, счётчик +44
    ///   +48  переносимые персонажи, счётчик +60
    ///   +64  предметы (тип 3), счётчик +76
    ///   +80  снаряды (тип 1), счётчик +92
    ///   +96  зоны (тип 2), счётчик +108
    ///   +112 зоны с типом эффекта 46
    ///   +128 зоны, у данных которых взведён флаг по смещению 240
    ///   +144 зоны с типом эффекта 50 либо 51
    ///   +212 счётчики id, по одному на каждый из четырёх типов
    /// </summary>
    public partial class LogicGameObjectManagerServer
    {
        /// <summary>
        /// Общий массив. Держится отсортированным по GlobalId: addLogicGameObject
        /// (0x10046eee8) дописывает в хвост, только если новый id больше последнего,
        /// иначе ищет место двоичным поиском. На этом же порядке стоит
        /// getGameObjectStartIndex (0x10046f1c0).
        /// </summary>
        public readonly List<LogicGameObjectServer> GameObjects = new List<LogicGameObjectServer>();

        public readonly List<LogicGameObjectServer> Characters = new List<LogicGameObjectServer>();
        public readonly List<LogicGameObjectServer> CarryableCharacters = new List<LogicGameObjectServer>();
        public readonly List<LogicGameObjectServer> Items = new List<LogicGameObjectServer>();
        public readonly List<LogicGameObjectServer> Projectiles = new List<LogicGameObjectServer>();
        public readonly List<LogicGameObjectServer> AreaEffects = new List<LogicGameObjectServer>();

        /// <summary>Счётчики id по типам, поле +212 оригинала.</summary>
        private readonly int[] _globalIdCounters = new int[4];

        /// <summary>Снимок массива на время тика, поле +16 оригинала.</summary>
        private readonly List<LogicGameObjectServer> _tickSnapshot = new List<LogicGameObjectServer>();

        public LogicBattleModeServer BattleMode;

        public LogicGameObjectManagerServer(LogicBattleModeServer battleMode)
        {
            BattleMode = battleMode;
        }

        /// <summary>
        /// 0x10046f164. Тип идёт в класс глобального id со сдвигом на единицу,
        /// чтобы нулевой id остался признаком «id ещё не выдан».
        /// </summary>
        /// <summary>
        /// Счётчик номеров у всех объектов общий, а не свой на каждый тип.
        ///
        /// Клиент различает объекты кадра по этому номеру, и если счётчики
        /// раздельные, то персонаж и предмет приходят к нему с одним и тем же
        /// нулём — он считает их одним объектом.
        /// </summary>
        private int _objectCounter;

        private int GenerateGameObjectGlobalId(LogicGameObjectServer gameObject)
        {
            int type = (int)gameObject.GetGameObjectType();

            // Оригинал здесь зовёт Debugger::fatal с текстом
            // "Index is out of bounds", нам достаточно отсечь объект.
            if (type < 0 || type >= _globalIdCounters.Length) return 0;

            return GlobalId.CreateGlobalId(type + 1, _objectCounter++);
        }

        /// <summary>
        /// 0x10046f1c0. Двоичный поиск первого объекта с GlobalId не меньше заданного.
        /// </summary>
        public int GetGameObjectStartIndex(int globalId)
        {
            int count = GameObjects.Count;
            if (count < 2) return 0;

            int low = 0;
            int high = count - 1;

            while (high > low)
            {
                int mid = (low + high) >> 1;
                if (GameObjects[mid].GlobalId < globalId) low = mid + 1;
                else high = mid;
            }

            return low;
        }

        /// <summary>0x10046eee8.</summary>
        public void AddGameObject(LogicGameObjectServer gameObject)
        {
            if (gameObject == null) return;

            if (gameObject.GlobalId == 0)
                gameObject.GlobalId = GenerateGameObjectGlobalId(gameObject);

            int count = GameObjects.Count;

            if (count == 0 || gameObject.GlobalId > GameObjects[count - 1].GlobalId)
            {
                GameObjects.Add(gameObject);
            }
            else
            {
                int index = GetGameObjectStartIndex(gameObject.GlobalId);

                // Совпадение id значит, что объект добавляют повторно. Оригинал в этом
                // месте только пишет ошибку и идёт дальше, но вставлять дубликат в
                // отсортированный массив нельзя — развалится двоичный поиск.
                if (GameObjects[index].GlobalId == gameObject.GlobalId) return;

                GameObjects.Insert(index, gameObject);
            }

            if (gameObject.Manager == null) gameObject.AttachLogicGameObjectManager(this);

            switch (gameObject.GetGameObjectType())
            {
                case LogicGameObjectType.Character:
                    Characters.Add(gameObject);
                    if (IsCarryable(gameObject)) CarryableCharacters.Add(gameObject);
                    break;

                case LogicGameObjectType.Projectile:
                    Projectiles.Add(gameObject);
                    break;

                case LogicGameObjectType.AreaEffect:
                    AreaEffects.Add(gameObject);
                    break;

                case LogicGameObjectType.Item:
                    Items.Add(gameObject);
                    break;
            }
        }

        /// <summary>Заглушка под LogicCharacterData::isCarryable, пока персонажи не перенесены.</summary>
        private static bool IsCarryable(LogicGameObjectServer gameObject)
        {
            return false;
        }

        public LogicGameObjectServer GetGameObjectById(int globalId)
        {
            int index = GetGameObjectStartIndex(globalId);
            if (index < 0 || index >= GameObjects.Count) return null;

            LogicGameObjectServer found = GameObjects[index];
            return found.GlobalId == globalId ? found : null;
        }

        /// <summary>
        /// 0x10046e58c. Тик идёт по СНИМКУ массива, а не по нему самому: объекты,
        /// созданные внутри тика, в этом же кадре уже не тикают. Порядок важен,
        /// иначе снаряд, рождённый выстрелом, пролетит лишний кадр.
        /// </summary>
        public void Tick()
        {
            _tickSnapshot.Clear();

            int count = GameObjects.Count;
            if (count < 1) return;

            for (int i = 0; i < count; i++) _tickSnapshot.Add(GameObjects[i]);

            for (int i = 0; i < _tickSnapshot.Count; i++) _tickSnapshot[i].Tick();
        }

        /// <summary>
        /// 0x10046f238. Сначала снимаются мёртвые объекты, потом чистятся события
        /// у игроков и у объектов. В кадре это происходит ПОСЛЕ энкода, поэтому
        /// объект, умерший в тике, ещё успевает уехать клиенту.
        /// </summary>
        public void PreTick(List<LogicPlayer> players)
        {
            DoDestructing();

            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    LogicPlayer player = players[i];
                    if (player != null) player.ResetEventsOnTick();
                }
            }

            for (int i = 0; i < GameObjects.Count; i++) GameObjects[i].ResetEventsOnTick();
        }

        /// <summary>
        /// 0x10046e628. Оригинал после каждого снятия начинает проход заново, потому
        /// что снятие сдвигает массив. Здесь тот же результат даёт проход с конца.
        /// </summary>
        public void DoDestructing()
        {
            for (int i = GameObjects.Count - 1; i >= 0; i--)
            {
                if (i >= GameObjects.Count) continue;

                LogicGameObjectServer gameObject = GameObjects[i];
                if (!gameObject.ShouldDestruct()) continue;

                GameObjects.RemoveAt(i);

                switch (gameObject.GetGameObjectType())
                {
                    case LogicGameObjectType.Character:
                        Characters.Remove(gameObject);
                        CarryableCharacters.Remove(gameObject);
                        break;

                    case LogicGameObjectType.Projectile:
                        Projectiles.Remove(gameObject);
                        break;

                    case LogicGameObjectType.AreaEffect:
                        AreaEffects.Remove(gameObject);
                        break;

                    case LogicGameObjectType.Item:
                        Items.Remove(gameObject);
                        break;
                }

                // Оригинал зовёт removeGameObjectReferences у остальных объектов,
                // чтобы никто не держал ссылку на снятый.
                for (int j = 0; j < GameObjects.Count; j++)
                    GameObjects[j].RemoveGameObjectReferences(gameObject);
            }
        }
    }
}
