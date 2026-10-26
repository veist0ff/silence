namespace HashCore.Laser.Server.Logic.Game
{
    using Masuda.Net;
    using HashCore.Laser.Logic.Battle;
    using HashCore.Laser.Logic.Battle.Logic;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message.Battle;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Logic.Team;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server.Networking;
    using HashCore.Laser.Server.Networking.Session;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Matchmaking
    {
        private static Dictionary<int, MatchmakingSlot> Slots;
        private static Thread UpdateThread;

        public static void Init()
        {
            Slots = new Dictionary<int, MatchmakingSlot>();

            foreach (EventData e in Events.GetEvents())
            {
                if (Slots.ContainsKey(e.Slot)) continue;

                LocationData location = e.Location;
                if (location == null)
                {
                    Logger.Warning($"[Matchmaking::Init] slot {e.Slot} skipped: location {e.LocationId} not found");
                    continue;
                }

                int mode = GameModeUtil.GetGameModeVariation(location.GameModeVariation);
                if (mode < 0)
                {
                    Logger.Warning($"[Matchmaking::Init] slot {e.Slot} skipped: unknown game mode {location.GameModeVariation}");
                    continue;
                }

                int playersRequired = GamePlayUtil.GetPlayerCountWithGameModeVariation(mode);
                if (playersRequired < 1) playersRequired = 1;

                Slots.Add(e.Slot, new MatchmakingSlot(e, playersRequired));
            }

            UpdateThread = new Thread(Update);
            UpdateThread.Start();
        }

        private static void Update()
        {
            while (true)
            {
                foreach (MatchmakingSlot slot in Slots.Values)
                {
                    try
                    {
                        slot.Update();
                    }
                    catch (Exception exception)
                    {
                        Logger.Error($"[Matchmaking::Update] {exception}");
                    }
                }
                Thread.Sleep(250);
            }
        }

        public static void RequestMatchmake(Connection connection, int slot, long team = -1)
        {
            if (!Slots.ContainsKey(slot)) return;

            connection.MatchmakeSlot = slot;
            MatchmakingEntry entry = new MatchmakingEntry(connection);
            entry.PlayerTeamId = team;
            connection.MatchmakingEntry = entry;
            Slots[slot].Add(entry);
        }

        public static void CancelMatchmake(Connection connection)
        {

            int slot = connection.MatchmakeSlot;
            if (Slots.ContainsKey(slot))
            {
                connection.MatchmakeSlot = -1;
                Slots[slot].Remove(connection.MatchmakingEntry);

                if (connection.MatchmakingEntry.PlayerTeamId > 0)
                {
                    TeamEntry team = Teams.Get(connection.MatchmakingEntry.PlayerTeamId);

                    foreach (TeamMember member in team.Members)
                    {
                        member.IsReady = false;
                        Session session = Sessions.GetSession(member.AccountId);
                        if (session != null)
                        {
                            session.Connection.MatchmakingEntry.PlayerTeamId = -1;
                            CancelMatchmake(session.Connection);
                            MatchMakingCancelledMessage cancelled = new MatchMakingCancelledMessage();
                            session.Connection.Send(cancelled);
                        }
                    }

                    team.TeamUpdated();
                }
            }
        }
    }

    public class MatchmakingSlot
    {
        private Queue<MatchmakingEntry> RequestQueue;
        private Queue<MatchmakingEntry> RemoveQueue;

        private List<MatchmakingEntry> Queue;

        private int PlayersRequired;
        private EventData EventData;
        private readonly int Slot;

        public int SecondsLeft;
        private int Turns;

        public const int SEARCH_TIMEOUT = 15;

        /// <summary>
        /// Добивать ли бой ботами до полного состава. Пока выключено: бои идут
        /// как в награде за поимку — сразу, только реальные игроки. С ботами
        /// выживание и захват кристаллов у клиента вылетают.
        /// </summary>
        public const bool FillWithBots = true;

        public MatchmakingSlot(EventData eventData, int playersRequired)
        {
            PlayersRequired = FillWithBots ? playersRequired : 1;
            if (GameModeUtil.GetGameModeVariation(eventData.Location.GameModeVariation) == 30)
            {
                PlayersRequired = 1;
            }
            if (PlayersRequired < 1) PlayersRequired = 1;
            EventData = eventData;
            Slot = eventData.Slot;
            Queue = new List<MatchmakingEntry>();

            RequestQueue = new Queue<MatchmakingEntry>();
            RemoveQueue = new Queue<MatchmakingEntry>();

            SecondsLeft = SEARCH_TIMEOUT;
        }

        public void Update()
        {
            {
                if (Sessions.Maintenance) return;

                foreach (MatchmakingEntry entry in Queue)
                {
                    if (!entry.Connection.IsOpen) Remove(entry);
                }

                while (RemoveQueue.Count > 0)
                {
                    Queue.Remove(RemoveQueue.Dequeue());
                }

                while (RequestQueue.Count > 0)
                {
                    MatchmakingEntry entry = RequestQueue.Dequeue();
                    MatchmakingEntry check = Queue.Find(x => x.Connection == entry.Connection);
                    if (check != null) continue;

                    Queue.Add(entry);
                }

                while (Queue.Count >= PlayersRequired)
                {
                    try { StartGame(Queue.Take(PlayersRequired).ToList()); } catch (Exception e) { Logger.Error($"[Matchmaking] slot {EventData.Slot}: {e}"); }
                    Queue.RemoveRange(0, PlayersRequired);
                }

                if (Queue.Count > 0 && Queue.Count < PlayersRequired && SecondsLeft <= 0)
                {
                    SecondsLeft = SEARCH_TIMEOUT;
                    StartGame(Queue.Take(Queue.Count).ToList());
                    Queue.RemoveRange(0, Queue.Count);
                }

                if (Queue.Count > 0)
                {
                    Turns++;
                    if (Turns >= 4)
                    {
                        Turns = 0;
                        SecondsLeft--;
                    }
                }
                else
                {
                    Turns = 0;
                    SecondsLeft = SEARCH_TIMEOUT;
                }

                if (Queue.Count > 0)
                {
                    MatchMakingStatusMessage message = new MatchMakingStatusMessage();

                    message.Seconds = SecondsLeft;
                    message.Found = Queue.Count;
                    message.Max = PlayersRequired;
                    message.ShowTips = true;

                    foreach (MatchmakingEntry entry in Queue)
                    {
                        entry.Connection.Messaging.Send(message);
                    }
                }
            } //finally
            {
            }
        }

        public void Remove(MatchmakingEntry entry)
        {
            RemoveQueue.Enqueue(entry);
        }

        public void Add(MatchmakingEntry entry)
        {
            RequestQueue.Enqueue(entry);
        }

        public static readonly int[] botBrawlers = { 0, 1, 2, 3, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 54, 56, 57, 58, 59, 60, 61, 62 }; // боты на разных бойцах (вылеты были из-за кадра, а не бойцов)

        /// <summary>
        /// Карта этапа рогалика. Клиент показывает игроку карту текущего этапа
        /// (LogicRogueliteEventStage +0, findLocationData), поэтому и бой должен
        /// идти на ней, а не на карте слота. Этап берётся по забегу первого игрока.
        /// </summary>
        private static bool IsRogueliteSlot(int slot) =>
            slot >= EventData.ROGUELITE_FIRST_SLOT && slot <= EventData.ROGUELITE_LAST_SLOT;

        /// <summary>Забег игрока для слота рогалика.</summary>
        private static HashCore.Laser.Logic.Home.Items.PlayerRogueliteEventData FindRun(EventData ev, List<MatchmakingEntry> entries)
        {
            var runs = entries[0]?.Connection?.Home?.PlayerRoguelite;
            if (runs == null) return null;
            return runs.Find(r => r != null && r.EventSlotCode == ev.Slot);
        }

        private static int? RogueliteStageLocation(EventData ev, List<MatchmakingEntry> entries)
        {
            if (ev?.Roguelite?.Stages == null || ev.Roguelite.Stages.Count == 0) return null;
            if (!IsRogueliteSlot(ev.Slot)) return null;

            var run = FindRun(ev, entries);
            int stage = run?.CurrentStage ?? 0;
            if (stage < 0) stage = 0;
            if (stage >= ev.Roguelite.Stages.Count) stage = ev.Roguelite.Stages.Count - 1;

            int location = ev.Roguelite.Stages[stage]?.LocationGlobalId ?? 0;
            if (location <= 0) return null;
            if (DataTables.Get(DataType.Location)?.GetDataByGlobalId<LocationData>(location) == null) return null;
            return location;
        }

        /// <summary>Карта боя после рангового драфта (null — обычная карта слота).</summary>
        public int? RankedLocation;

        private static int? RandomRankedLocation(Random rnd)
        {
            try
            {
                var names = File.ReadAllLines("Assets/csv_logic/ranked_locations.csv").Skip(2)
                    .Select(l => l.Split(',')[0].Trim().Trim('"'))
                    .Where(n => n.Length > 0).ToList();
                var ids = names.Select(n => DataTables.Get(DataType.Location)?.GetData<LocationData>(n)?.GetGlobalId() ?? 0)
                    .Where(id => id > 0).ToList();
                return ids.Count > 0 ? ids[rnd.Next(ids.Count)] : null;
            }
            catch (Exception e) { Logger.Error($"[Ranked] ranked_locations.csv: {e.Message}"); return null; }
        }

        private void SendRankedMatchStarted(List<MatchmakingEntry> entries)
        {
            var rnd = new Random();
            var msg = new RankedMatchStartedMessage
            {
                // Как LogicDataTables::getAllDefaultRankedLocations: карта из ranked_locations.csv.
                // Локация события (не из этого списка) роняла клиент на старте драфта.
                LocationGlobalId = RandomRankedLocation(rnd) ?? EventData.Location?.GetGlobalId() ?? 0,
                FirstPickTeam = rnd.Next(2),
            };
            int myTeam = rnd.Next(2);
            var avatar = entries[0].Connection.Avatar;
            msg.Players.Add(new RankedMatchPlayer { AccountId = avatar.AccountId, Name = avatar.Name, Team = myTeam, TeamIndex = 0 });
            for (int i = 1; i < 6; i++)
            {
                int team = (myTeam == 1) ^ (i > 2) ? 1 : 0;
                int idx = i < 3 ? i : i - 3;
                msg.Players.Add(new RankedMatchPlayer { AccountId = i + 1, Name = "player " + (i + 1), Team = team, TeamIndex = idx });
            }
            RankedLocation = msg.LocationGlobalId > 0 ? msg.LocationGlobalId : null;
            new RankedDraft(this, entries, msg).Start();
            Logger.Print($"[Ranked] драфт: локация {msg.LocationGlobalId}, первой пикает команда {msg.FirstPickTeam}, моя команда {myTeam}");
        }

        public void StartGame(List<MatchmakingEntry> entries, bool skipDraft = false)
        {
            if (entries == null || entries.Count == 0) return;

            // Events пересобирает слоты по таймеру, и карта в них меняется. Раньше
            // очередь держала EventData со старта сервера, поэтому слот всегда уходил
            // в бой на одной и той же карте. Берём текущую запись слота, а на неё
            // откатываемся только если слот пропал из конфигурации.
            EventData current = Events.GetEvent(Slot);
            if (current?.Location != null) EventData = current;

            // Ранговые (слот 43, тип события 14): сначала экран драфта — 22150, как в
            // офлайн-сборке клиента (HomePage::startGame): игрок + 5 ботов, 3 на 3.
            if (EventData.IsRanked && !skipDraft)
            {
                SendRankedMatchStarted(entries);
                return;
            }

            // Слоты, перечисленные в конфиге, идут на новом движке. Всё остальное
            // — на прежнем, и он этого переключателя даже не замечает.
            bool newEngine = Configuration.IsNewEngineSlot(EventData.Slot);

            int locationId = (skipDraft ? RankedLocation : null) ?? RogueliteStageLocation(EventData, entries) ?? EventData.LocationId;

            // Слоты из new_engine_slots идут на новый минимальный бой (Battle/Arena),
            // написанный заново по разбору клиента.
            BattleMode battle = newEngine
                ? new HashCore.Laser.Logic.Battle.Arena.ArenaBattle(locationId)
                : new BattleMode(locationId);

            if (newEngine)
                Logger.Print($"[Matchmaking] slot {EventData.Slot}: battle runs on the new engine");

            battle.EventSlot = EventData.Slot;
            battle.BossCharacterId = EventData.BossCharacterId;
            // Бой рогалика привязываем к слоту забега: по нему начисляется этаж и хранятся карты.
            if (IsRogueliteSlot(EventData.Slot) && FindRun(EventData, entries) is { } rogueRun) battle.EventSlot = rogueRun.EventSlotCode;
            battle.Id = Battles.Add(battle);

            Random rand = new Random();

            List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();
            Dictionary<long, List<MatchmakingEntry>> teamEntries = new Dictionary<long, List<MatchmakingEntry>>();

            if (battle.GetGameModeVariation() == 7)
            {
                sortedEntries = entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;
                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, 0);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    player.HeroPowerLevel = 21;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                BattlePlayer bot1 = BattlePlayer.CreateStoryModeDummy("首领", battle.GetPlayersCountWithGameModeVariation(), 1, 32, 310, 254);
                battle.AddPlayer(bot1, -1);
                bot1.Bot = 2;
                bot1.HeroPowerLevel = 391;
            }
            else if (GameModeUtil.HasTwoTeams(battle.GetGameModeVariation()))
            {
                //先排车队
                MatchmakingEntry[] entriesWithTeam = entries.FindAll(entry => entry.PlayerTeamId > 0).ToArray();
                for (int i = 0; i < entriesWithTeam.Length; i++)
                {
                    MatchmakingEntry entry = entriesWithTeam[i];
                    long teamId = entry.PlayerTeamId;
                    if (!teamEntries.ContainsKey(teamId)) teamEntries.Add(teamId, new List<MatchmakingEntry>());
                    teamEntries[teamId].Add(entry);
                    entries.Remove(entry);
                }

                long[] ids = teamEntries.Keys.ToArray();
                for (int i = 0; i < ids.Length; i++)
                {
                    List<MatchmakingEntry> teamedEntries = teamEntries[ids[i]];
                    for (int j = 0; j < teamedEntries.Count; j++)
                    {
                        sortedEntries.Add(teamedEntries[j]);
                        teamedEntries[j].PrefferedTeam = i;
                    }
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    sortedEntries.Add(entries[i]);
                }

                for (int i = 0; i < sortedEntries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                    // Раскладка по командам берётся из game_mode_variations.csv:
                    // GemGrab это 2 команды по 3, а выживание — 10 команд по одному.
                    // Жёсткое `i % 2` паковало десятерых в две команды, и клиент считал
                    // половину лобби союзниками (владелец пакуется как team*16 + index).
                    int teamCount = GameModeUtil.GetTeamCount(battle.GetGameModeVariation());
                    int teamSize = GameModeUtil.GetTeamSize(battle.GetGameModeVariation());
                    int teamIndex = i % teamCount;
                    if (sortedEntries[i].PrefferedTeam != -1 && sortedEntries[i].PrefferedTeam < teamCount)
                    {
                        teamIndex = sortedEntries[i].PrefferedTeam;
                    }

                    if (battle.GetTeamPlayersCount(teamIndex) >= teamSize)
                    {
                        for (int t = 0; t < teamCount; t++)
                        {
                            if (battle.GetTeamPlayersCount(t) < teamSize) { teamIndex = t; break; }
                        }
                    }
                    // Индекс игрока должен быть ВНУТРИКОМАНДНЫМ: клиент пакует его как
                    // team*16 + index и сравнивает с getPlayerCount (3 слота на команду).
                    // Сквозной 0..5 давал команде 0 индексы 0,2,4 — выход за массив у клиента.
                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, battle.GetTeamPlayersCount(teamIndex), teamIndex);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                List<int> team1BotsCharacters = new List<int>();
                List<int> team2BotsCharacters = new List<int>();

                for (int i = sortedEntries.Count; FillWithBots && i < battle.GetPlayersCountWithGameModeVariation(); i++)
                {
                    bool isBotValid = false;

                    int botCharacter = -1;
                    for (int tries = 0; !isBotValid && tries < 50; tries++)
                    {
                        botCharacter = 16000000 + botBrawlers[rand.Next(0, botBrawlers.Length)];
                        if (i % 2 == 0)
                        {
                            isBotValid = !team1BotsCharacters.Contains(botCharacter);
                        }
                        else
                        {
                            isBotValid = !team2BotsCharacters.Contains(botCharacter);
                        }
                        if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                    }

                    int botTeamCount = GameModeUtil.GetTeamCount(battle.GetGameModeVariation());
                    int botTeamSize = GameModeUtil.GetTeamSize(battle.GetGameModeVariation());
                    int teamIndex = i % botTeamCount;
                    if (battle.GetTeamPlayersCount(teamIndex) >= botTeamSize)
                    {
                        for (int t = 0; t < botTeamCount; t++)
                        {
                            if (battle.GetTeamPlayersCount(t) < botTeamSize) { teamIndex = t; break; }
                        }
                    }

                    if (teamIndex == 0)
                    {
                        team1BotsCharacters.Add(botCharacter);
                    }
                    else
                    {
                        team2BotsCharacters.Add(botCharacter);
                    }
                    CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                    BattlePlayer bot = BattlePlayer.CreateBotInfo((i - sortedEntries.Count + 1).ToString(), battle.GetTeamPlayersCount(teamIndex), teamIndex, botCharacter);
                    battle.AddPlayer(bot, -1);
                }
            }
            else
            {
                sortedEntries = entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    UDPSocket socket = UDPGateway.CreateSocket();
                    socket.TCPConnection = sortedEntries[i].Connection;
                    socket.Battle = battle;
                    sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                    int teamIndex = battle.IsBossFightMode ? 0 : i; // бой с боссом: все игроки — одна команда
                    BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, battle.GetTeamPlayersCount(teamIndex), teamIndex);
                    player.TeamId = sortedEntries[i].PlayerTeamId;
                    sortedEntries[i].Player = player;
                    battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                }

                for (int i = entries.Count; FillWithBots && i < battle.GetPlayersCountWithGameModeVariation(); i++)
                {
                    int botCharacter = 16000000 + botBrawlers[rand.Next(0, botBrawlers.Length)];

                    int teamIndex = battle.IsBossFightMode ? 0 : i; // напарники — в команду игрока

                    CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                    BattlePlayer bot = BattlePlayer.CreateBotInfo(data.ItemName.ToUpper(), battle.GetTeamPlayersCount(teamIndex), teamIndex, botCharacter);
                    battle.AddPlayer(bot, -1);
                }

            }
            battle.AddBossPlayer(); // бой с боссом: босс — игрок-бот команды 1
            if (battle.m_players.Count > 0 && battle.m_players[0].IsAdmin) for (int i = 0; i < 120; i++) battle.m_time.IncreaseTick();
            if (battle.StoryMode != null) battle.StoryMode.Init();
            battle.AddGameObjects();

            battle.BattleWithTrophies = true;

            Logger.Print($"[Battle::create] battle {battle.Id}: slot {EventData.Slot} location {EventData.LocationId} gmv {battle.GetGameModeVariation()} map {battle.Location.Map} {battle.GetTileMap().Width}x{battle.GetTileMap().Height} players {battle.m_players.Count} (real {sortedEntries.Count}) objects {battle.GetGameObjectManager().GameObjects.Count + battle.GetGameObjectManager().AddObjects.Count}");

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                StartLoadingMessage startLoading = new StartLoadingMessage();
                startLoading.LocationId = battle.Location.GetGlobalId();
                startLoading.TeamIndex = sortedEntries[i].Player.TeamIndex;
                startLoading.OwnIndex = sortedEntries[i].Player.PlayerIndex;
                startLoading.GameMode = battle.GetGameModeVariation();
                startLoading.BossCharacterId = battle.BossCharacterGlobalId;
                if (battle.GetGameModeVariation() == 67)
                {
                    battle.DealRogueliteCards();        // карты должны быть уже в сообщении загрузки
                    startLoading.Modifiers.Add(47);     // event_modifiers.csv: Roguelite = 47
                }

                sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
                startLoading.Players.AddRange(battle.GetPlayers());
                // UDP-эндпоинт должен приехать ДО StartLoading, иначе клиент
                // инициализирует бой без UDP-сессии и уходит на TCP.
                HashCore.Laser.Server.Message.MessageManager.SendUdpConnectionInfo(sortedEntries[i].Connection);
                sortedEntries[i].Connection.Send(startLoading);

                // Слушатель здесь НЕ цепляется. Он появляется в ответ на первый
                // ClientInput, и так было всегда: клиент присылает ввод сам, не
                // дожидаясь кадра. Предварительная привязка гнала ему кадры ещё
                // на загрузке — тогда он отваливался на четвёртой секунде во всех
                // режимах, включая те, что до этого заходили.

                if (battle.Dummy == null) battle.Dummy = startLoading;
            }
            battle.Start();

        }
    }

    public class MatchmakingEntry
    {
        public readonly Connection Connection;
        public BattlePlayer Player;
        public long PlayerTeamId;

        public int PrefferedTeam;

        public MatchmakingEntry(Connection connection)
        {
            PrefferedTeam = -1;
            this.Connection = connection;
        }
    }
}
