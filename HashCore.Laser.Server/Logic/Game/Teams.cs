namespace HashCore.Laser.Server.Logic.Game
{
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Battle;
    using HashCore.Laser.Logic.Command.Home;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Message.Battle;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Logic.Message.Team;
    using HashCore.Laser.Logic.Notification;
    using HashCore.Laser.Logic.Team;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server.Networking;
    using HashCore.Laser.Server.Networking.Session;
    using Masuda.Net.Models;
    using System.Xml.Linq;
    using System.Reflection.Metadata.Ecma335;

    public static class Teams
    {
        private static Dictionary<long, TeamEntry> Entries;
        private static long TeamIdCounter;

        public static void Init()
        {
            Entries = new Dictionary<long, TeamEntry>();
            TeamIdCounter = 0;
        }

        public static int Count
        {
            get
            {
                return Entries.Count;
            }
        }

        public static TeamEntry Create()
        {
            TeamEntry entry = new TeamEntry();
            entry.Id = ++TeamIdCounter;
            Entries.Add(entry.Id, entry);
            return entry;
        }

        public static void Remove(long id)
        {
            Entries.Remove(id);
        }

        public static void StartGame(TeamEntry team)
        {
            try
            {
                if (team.Type == 0)
                {
                    foreach (TeamMember member in team.Members)
                    {
                        Connection connection = Sessions.GetSession(member.AccountId).Connection;
                        connection.Send(new TeamGameStartingMessage()
                        {
                            LocationId = team.LocationId
                        });
                        Matchmaking.RequestMatchmake(connection, team.EventSlot, team.Id);
                        member.IsReady = false;
                    }
                }
                if (team.Type == 1)
                {
                    string[] supportedModes = { "GemGrab", "Showdown", "Bounty", "Heist", "ShowdownSolo", "ShowdownDuo", "LoneStar" };
                    string gmv = DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(team.LocationId).GameModeVariation;
                    if (!supportedModes.Contains(gmv))
                    {
                        LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                        //logicAddNotificationCommand.Notification = new FloaterTextNotification("Not avaible gamemode.");
                        AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                        availableServerCommandMessage.Command = logicAddNotificationCommand;
                        foreach (TeamMember member in team.Members)
                        {
                            Sessions.GetSession(member.AccountId).Connection.Send(availableServerCommandMessage);
                        }
                        return;
                    }
                    BattleMode battle = new BattleMode(team.LocationId);
                    battle.Id = Battles.Add(battle);
                  //  if (team.BattlePlayerMap != null) battle.SetPlayerMap(team.BattlePlayerMap);
                    battle.SetEventModifiers(team.CustomModifiers);
                    List<MatchmakingEntry> entries = new List<MatchmakingEntry>();
                    List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();
                    foreach (TeamMember member in team.Members)
                    {
                        Connection connection = Sessions.GetSession(member.AccountId).Connection;
                        MatchmakingEntry entry = new MatchmakingEntry(connection);
                        entry.PrefferedTeam = member.TeamIndex;
                        entries.Add(entry);
                        member.IsReady = false;
                    }

                    if (GameModeUtil.HasTwoTeams(battle.GetGameModeVariation())) {

                        for (int i = 0; i < entries.Count; i++)
                        {
                            sortedEntries.Add(entries[i]);
                        }
                        Console.WriteLine("lore");
                        for (int i = 0; i < sortedEntries.Count; i++)
                        {
                            UDPSocket socket = UDPGateway.CreateSocket();
                            socket.TCPConnection = sortedEntries[i].Connection;
                            socket.Battle = battle;
                            sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                            int teamIndex = sortedEntries[i].PrefferedTeam;
                            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                            player.TeamId = sortedEntries[i].PlayerTeamId;
                            sortedEntries[i].Player = player;
                            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                        }
                        List<int> team1BotsCharacters = new List<int>();
                        List<int> team2BotsCharacters = new List<int>();
                        for (int i = battle.GetTeamPlayersCount(0); i < battle.GetPlayersCountWithGameModeVariation() / 2; i++)
                        {
                            if (team.DisabledBots.Contains(i)) continue;
                            
                            bool isBotValid = false;

                            int botCharacter = -1;
                            while (!isBotValid)
                            {
                                botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();
                                isBotValid = !team1BotsCharacters.Contains(botCharacter);
                                if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                            }

                            team1BotsCharacters.Add(botCharacter);
                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo((i - entries.Count + 2).ToString(), battle.GetPlayers().Count(), 0, botCharacter);
                            battle.AddPlayer(bot, -1);
                        }
                        for (int i = battle.GetPlayersCountWithGameModeVariation() / 2 + battle.GetTeamPlayersCount(1); i < battle.GetPlayersCountWithGameModeVariation(); i++)
                        {
                            if (team.DisabledBots.Contains(i)) continue;
                            bool isBotValid = false;
                            
                            int botCharacter = -1;
                            while (!isBotValid)
                            {
                                botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();
                                isBotValid = !team2BotsCharacters.Contains(botCharacter);
                                if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                            }
                            team2BotsCharacters.Add(botCharacter);
                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo((i - entries.Count + 2).ToString(), battle.GetPlayers().Count(), 1, botCharacter);
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

                            int teamIndex = i;
                            Console.WriteLine(teamIndex);
                            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                            player.TeamId = sortedEntries[i].PlayerTeamId;
                            sortedEntries[i].Player = player;
                            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                        }
                        for (int i = entries.Count; i < battle.GetPlayersCountWithGameModeVariation(); i++)
                        {
                            int botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();

                            int teamIndex = i;
                            Console.WriteLine(teamIndex);

                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo(data.ItemName.ToUpper(), i, teamIndex, botCharacter);
                            battle.AddPlayer(bot, -1);
                        }
                    }

                    for (int i = 0; i < sortedEntries.Count; i++)
                    {
                        StartLoadingMessage startLoading = new StartLoadingMessage();
                        startLoading.LocationId = battle.Location.GetGlobalId();
                        startLoading.TeamIndex = sortedEntries[i].Player.TeamIndex;
                        startLoading.OwnIndex = sortedEntries[i].Player.PlayerIndex;
                        startLoading.GameMode = battle.GetGameModeVariation();
                        startLoading.SetPlayerMap(battle.BattlePlayerMap);
                        startLoading.Modifiers = battle.EventModifiers;
                   //     if (sortedEntries.Count <= 1) { startLoading.LeaveButton = true; }
                        sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
                        startLoading.Players.AddRange(battle.GetPlayers());
                        sortedEntries[i].Connection.Send(startLoading);
                        battle.Dummy = startLoading;
                        battle.BattleWithTrophies = false;
                    }
                    battle.AddGameObjects();
                    battle.Start();

                }
            }
            catch (Exception)
            {
                ;
            }
        }

        public static TeamEntry Get(long id)
        {
            if (Entries.ContainsKey(id))
            {
                return Entries[id];
            }
            return null;
        }
    }
}
