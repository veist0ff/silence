namespace HashCore.Laser.Logic.Battle.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using HashCore.Laser.Logic.Battle.Input;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Message.Battle;
    using HashCore.Laser.Server;
    using HashCore.Laser.Titan.Math;

    /// <summary>
    /// Бой на новом движке, подключённый к серверу.
    ///
    /// Снаружи это такой же бой, как и прежний: те же игроки, тот же обмен
    /// сообщениями, тот же список боёв. Внутри вместо старого мира крутится
    /// восстановленный по бинарю — он и считает кадр, и кодирует его клиенту.
    ///
    /// Старый бой при этом остаётся нетронутым: сюда попадают только те бои,
    /// которые заведены с новым движком намеренно.
    /// </summary>
    public class LogicBattleSession : BattleMode
    {
        /// <summary>Сколько кадров в секунде идёт бой.</summary>
        public const int TicksPerSecond = 20;

        /// <summary>Мир на новом движке.</summary>
        public readonly LogicServerBattle World;

        /// <summary>Игрок сервера по его бойцу в новом мире.</summary>
        private readonly Dictionary<int, BattlePlayer> _playersByCharacter = new Dictionary<int, BattlePlayer>();

        /// <summary>Боец нового мира по игроку сервера.</summary>
        private readonly Dictionary<BattlePlayer, LogicHeroCharacter> _charactersByPlayer =
            new Dictionary<BattlePlayer, LogicHeroCharacter>();

        /// <summary>Кому первый кадр уже записан в лог.</summary>
        private readonly System.Collections.Concurrent.ConcurrentDictionary<long, bool> _framesLogged =
            new System.Collections.Concurrent.ConcurrentDictionary<long, bool>();

        /// <summary>Карту удалось загрузить и мир собран.</summary>
        public bool WorldReady { get; private set; }

        public LogicBattleSession(int locationId) : base(locationId)
        {
            World = CreateWorld();
        }

        /// <summary>
        /// Какой мир собрать под этот режим. Режимы, у которых на новом движке
        /// есть своя реализация, получают её; у остальных мир общий, и они идут
        /// до выбывания команды.
        /// </summary>
        protected virtual LogicServerBattle CreateWorld()
        {
            if (LogicGameModeUtilNew.IsGemGrab(GetGameModeVariation()))
                return new LogicGemGrabBattle(this);

            return new LogicServerBattle(this);
        }

        /// <summary>
        /// Собирает мир: грузит карту по имени из локации и ставит бойцов на точки
        /// появления своей команды.
        ///
        /// Если карты с таким именем в таблице нет, мир остаётся несобранным, и бой
        /// об этом честно говорит в лог, а не падает посреди кадра.
        /// </summary>
        public override void AddGameObjects()
        {
            string mapName = Location != null ? Location.Map : null;

            if (!World.LoadMap(mapName))
            {
                Logger.Error($"[LogicBattleSession] battle {Id}: map '{mapName}' not found, world not built");
                WorldReady = false;
                return;
            }

            World.GameMode = GetGameModeVariation();
            World.SetRandomSeed(GetRandomSeed());

            // Вступление должно совпадать со старым боем: клиент рисует обратный
            // отсчёт по своему таймеру и ждёт, что бойцы тронутся ровно тогда же.
            World.IntroTicks = INTRO_TICKS;

            World.EventModifiers.Clear();
            World.EventModifiers.AddRange(EventModifiers);

            foreach (BattlePlayer player in m_players)
            {
                CharacterData data = player.CharacterData;
                if (data == null) continue;

                LogicHeroCharacter character = World.AddPlayer(data, player.TeamIndex, player.PlayerIndex);

                LogicPlayer worldPlayer = character.GetPlayer();
                if (worldPlayer != null) worldPlayer.HeroIndex = player.HeroIndex;

                if (player.HeroPowerLevel > 0) character.SetHeroLevel(player.HeroPowerLevel);

                character.EquipWeaponFromData();

                player.IsAlive = true;
                player.OwnObjectId = character.GlobalId;
                player.SetSpawnPoint(character.X, character.Y);

                _playersByCharacter[character.GlobalId] = player;
                _charactersByPlayer[player] = character;
            }

            LogicGemGrabBattle gemGrab = World as LogicGemGrabBattle;

            if (gemGrab != null && !gemGrab.CreateSpawner())
                Logger.Warning($"[LogicBattleSession] battle {Id}: gem grab items not found, battle runs without gems");

            WorldReady = true;

            Logger.Print($"[LogicBattleSession] battle {Id}: new engine world built, "
                + $"map '{mapName}' {World.Map.TileMap.Width}x{World.Map.TileMap.Height}, "
                + $"{m_players.Count} players, mode {World.GameMode}");
        }

        /// <summary>
        /// Кадр боя. Порядок тот же, что и в старом бою: сначала разбирается ввод,
        /// потом проверяется конец боя, потом крутится мир. Кадр клиенту уходит
        /// изнутри мира — там, где движок сам решает, что кадр пора кодировать.
        /// </summary>
        public override void ExecuteOneTick()
        {
            try
            {
                HandleInputs();

                if (!WorldReady)
                {
                    m_updateTimer?.Dispose();
                    IsGameOver = true;
                    return;
                }

                if (ShouldStopBattle())
                {
                    Logger.Warning($"[LogicBattleSession] battle {Id} stopped at tick {World.Tick}: "
                        + (World.Tick >= MaxBattleTicks ? "time limit" : "nobody left to watch"));

                    m_updateTimer?.Dispose();
                    GameOver();
                    IsGameOver = true;
                    return;
                }

                if (World.CheckIsGameOver())
                {
                    m_winnerTeam = World.GetWinnerTeam();
                    if (m_winnerTeam == -1) m_winnerTeam = FindWinnerTeam();

                    CopyScoresToServerPlayers();

                    m_updateTimer?.Dispose();
                    GameOver();
                    IsGameOver = true;
                    return;
                }

                World.Update();

                m_time.IncreaseTick();

                if (World.Tick % 100 == 0)
                {
                    int listeners = 0;
                    foreach (BattlePlayer p in m_players) if (p.GameListener != null) listeners++;

                    Logger.Print($"[NewEngine::tick] battle {Id} tick {World.Tick}: listeners {listeners}"
                        + $", objects {World.GameObjectManager.GameObjects.Count}"
                        + $", score {World.GetPlayer(0)?.Score ?? 0}:{(World.Players.Count > 1 ? World.Players[1].Score : 0)}");
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"[LogicBattleSession] battle {Id} stopped at tick {World.Tick}: {exception}");

                m_updateTimer?.Dispose();
                IsGameOver = true;
            }
        }

        /// <summary>Дольше этого бой не живёт ни при каких условиях.</summary>
        public const int MaxBattleTicks = 20 * 60 * 8;

        /// <summary>Сколько тиков бой терпит, когда его никто не смотрит.</summary>
        public const int EmptyBattleTicks = 20 * 30;

        /// <summary>Тик, на котором у боя в последний раз был хоть один слушатель.</summary>
        private int _lastWatchedTick;

        /// <summary>
        /// Пора ли прекращать бой помимо его собственных правил.
        ///
        /// Бой без единого слушателя не кончится сам: счёт в нём набирать некому,
        /// а спавнеры продолжают работать. Такой бой надо снимать, иначе он живёт
        /// до перезапуска сервера и копит объекты.
        /// </summary>
        private bool ShouldStopBattle()
        {
            if (World.Tick >= MaxBattleTicks) return true;

            int listeners = 0;
            foreach (BattlePlayer p in m_players) if (p.GameListener != null) listeners++;

            if (listeners > 0)
            {
                _lastWatchedTick = World.Tick;
                return false;
            }

            return World.Tick - _lastWatchedTick >= EmptyBattleTicks;
        }

        /// <summary>Разбирает весь накопившийся ввод.</summary>
        private void HandleInputs()
        {
            while (m_inputQueue.Count > 0) HandleInput(m_inputQueue.Dequeue());
        }

        /// <summary>
        /// Один приказ от клиента.
        ///
        /// Номера видов ввода те же, что и в старом бою: ноль это выстрел, единица
        /// ульта, двойка движение. Прицел у выстрела приходит смещением от бойца,
        /// если умение бьёт по направлению, и точкой на карте, если по месту.
        /// </summary>
        private void HandleInput(ClientInput input)
        {
            if (input == null || !WorldReady) return;

            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);
            if (player == null) return;

            if (player.LastHandledInput >= input.Index) return;
            player.LastHandledInput = input.Index;

            LogicHeroCharacter character;
            if (!_charactersByPlayer.TryGetValue(player, out character)) return;
            if (character == null || !character.IsAlive()) return;

            switch (input.Type)
            {
                case InputTypeAttack:
                    HandleAttackInput(character, input);
                    break;

                case InputTypeMove:
                    character.MoveTo(input.X, input.Y, false, 0, true, character.CanUseFastTravel());
                    break;
            }
        }

        /// <summary>Вид ввода: обычная атака.</summary>
        public const int InputTypeAttack = 0;

        /// <summary>Вид ввода: ульта.</summary>
        public const int InputTypeUlti = 1;

        /// <summary>Вид ввода: движение.</summary>
        public const int InputTypeMove = 2;

        private void HandleAttackInput(LogicHeroCharacter character, ClientInput input)
        {
            LogicWeaponSkill weapon = character.GetWeapon();
            if (weapon == null) return;

            int targetX = input.X;
            int targetY = input.Y;

            if (input.AutoAttack)
            {
                LogicCharacterServer target = FindAutoAttackTarget(character, input.AutoAttackTarget);

                if (target != null)
                {
                    targetX = target.X;
                    targetY = target.Y;
                }
            }
            else if (weapon.Data == null || !weapon.Data.IsPositionalTargeted())
            {
                // Умение, которое бьёт по направлению, получает от клиента смещение,
                // а не точку: считать её надо от самого бойца.
                targetX += character.X;
                targetY += character.Y;
            }

            weapon.Activate(targetX, targetY);
        }

        /// <summary>
        /// Кого клиент просит ударить. Он присылает того, кого видит сам, но пока
        /// приказ шёл, цель могла умереть — тогда берётся ближайший враг.
        /// </summary>
        private LogicCharacterServer FindAutoAttackTarget(LogicHeroCharacter character, int globalId)
        {
            LogicGameObjectServer named = World.GameObjectManager.GetGameObjectById(globalId);

            LogicCharacterServer target = named as LogicCharacterServer;
            if (target != null && target.IsAlive()) return target;

            return character.FindClosestVisibleEnemy();
        }

        /// <summary>
        /// Рассылает кадр каждому, кто его ждёт.
        ///
        /// Поток у каждого свой: бой у каждого игрока выглядит по-своему, и один
        /// и тот же кадр двум игрокам не годится.
        /// </summary>
        public void SendFrames()
        {
            int tick = World.Tick;

            foreach (BattlePlayer player in m_players)
            {
                if (player.GameListener == null) continue;

                BitStream stream = new BitStream(64);

                World.GameObjectManager.Encode(
                    stream,
                    World.TileMap,
                    player.OwnObjectId,
                    player.PlayerIndex,
                    player.TeamIndex,
                    World.GameMode,
                    World.GetCountdownForEncoding(),
                    World.Players,
                    World.GameObjectManager.GameObjects);

                // Первый кадр каждому игроку кладётся в лог целиком. Энкодер нового
                // движка ни разу не проверялся живым клиентом, и если тот не поймёт
                // кадр, разойтись с ним можно только по байтам.
                if (_framesLogged.TryAdd(player.SessionId, true))
                {
                    Logger.Print($"[NewEngine::vision] battle {Id} player {player.PlayerIndex} team {player.TeamIndex}"
                        + $": first frame {stream.GetLength()} bytes at tick {tick}, own object {player.OwnObjectId}"
                        + $", map {World.TileMap.Width}x{World.TileMap.Height}, objects {World.GameObjectManager.GameObjects.Count}");

                    System.Console.WriteLine("[NewEngine::hex] battle " + Id + " gmv " + World.GameMode
                        + " len " + stream.GetLength() + " "
                        + System.BitConverter.ToString(stream.GetByteArray(), 0, stream.GetLength()));
                }

                VisionUpdateMessage update = new VisionUpdateMessage();
                update.Tick = tick;
                update.HandledInputs = player.LastHandledInput;
                update.Viewers = 0;
                update.VisionBitStream = stream;

                try
                {
                    player.GameListener.SendMessage(update);
                }
                catch (Exception exception)
                {
                    Logger.Error($"[LogicBattleSession] battle {Id} player {player.PlayerIndex}: send failed: {exception}");
                    RemovePlayer(player);
                }
            }
        }

        /// <summary>
        /// Чья команда осталась на ногах. Никого живого — ничья, и бой закончится
        /// без победителя, как и в старом бою.
        /// </summary>
        private int FindWinnerTeam()
        {
            int winner = -1;

            foreach (KeyValuePair<BattlePlayer, LogicHeroCharacter> pair in _charactersByPlayer)
            {
                LogicHeroCharacter character = pair.Value;
                if (character == null || !character.IsAlive()) continue;

                int team = character.TeamIndex;

                // Живые сразу в двух командах означают, что бой кончился не победой:
                // такого быть не должно, но врать про победителя тем более не надо.
                if (winner != -1 && winner != team) return -1;

                winner = team;
            }

            return winner;
        }

        /// <summary>
        /// Переносит счёт из нового мира игрокам сервера.
        ///
        /// Итоги боя, трофеи и награды считает прежний код, и смотрит он в своего
        /// игрока: без этого переноса бой на новом движке закончился бы с нулями.
        /// </summary>
        private void CopyScoresToServerPlayers()
        {
            foreach (KeyValuePair<BattlePlayer, LogicHeroCharacter> pair in _charactersByPlayer)
            {
                BattlePlayer player = pair.Key;

                LogicPlayer worldPlayer = pair.Value != null ? pair.Value.GetPlayer() : null;
                if (worldPlayer == null) continue;

                player.AddScore(worldPlayer.Score - player.GetScore());
            }
        }

        /// <summary>Игрок сервера, которому принадлежит боец нового мира.</summary>
        public BattlePlayer GetServerPlayer(int characterGlobalId)
        {
            BattlePlayer player;
            return _playersByCharacter.TryGetValue(characterGlobalId, out player) ? player : null;
        }
    }

    /// <summary>
    /// Мир нового движка, знающий про сессию сервера.
    ///
    /// Всё, что бой обычно делает «в пустоту», здесь уходит наружу: кадр —
    /// клиенту, конец боя — серверу.
    /// </summary>
    public class LogicServerBattle : LogicBattle
    {
        private readonly LogicBattleSession _session;

        public LogicServerBattle(LogicBattleSession session)
        {
            _session = session;
        }

        /// <summary>
        /// Кадр уходит наружу ровно там, где движок его кодирует. Мир без сессии
        /// никуда его не шлёт — так его можно крутить и в проверках.
        /// </summary>
        protected override void EncodeState()
        {
            if (_session != null) _session.SendFrames();
            EncodedFrames++;
        }

        /// <summary>Конец боя решается тем же условием, что и у обычного боя.</summary>
        public bool CheckIsGameOver()
        {
            return base.CalculateIsGameOver();
        }

        /// <summary>
        /// Кого режим считает победителем. Общий мир этого не знает и отдаёт
        /// решение бою — там победителем станет последняя выжившая команда.
        /// </summary>
        public virtual int GetWinnerTeam()
        {
            return -1;
        }

        /// <summary>Обратный отсчёт, который уходит в кадр.</summary>
        public int GetCountdownForEncoding()
        {
            return GetCountdown();
        }
    }
}
