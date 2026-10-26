namespace HashCore.Laser.Server.Handler
{
    using Al;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.RandomReward;
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message.Account;
    using HashCore.Laser.Logic.Message.Account.Auth;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server.Database;
    using HashCore.Laser.Server.Database.Cache;
    using HashCore.Laser.Server.Networking.Session;
    using System.Reflection;

    public static class CmdHandler
    {
        public static void Start()
        {
            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();
                    if (cmd == null) continue;
                    if (!cmd.StartsWith("/")) continue;
                    HandleCmd(cmd);
                }
                catch (Exception) { }
            }
        }
        public static void HandleCmd(string cmd, long OwnAccountId = -1)
        {
            cmd = cmd.Substring(1);
            string[] args = cmd.Split(" ");
            if (args.Length < 1) return;
            switch (args[0])
            {
                case "premium":
                    ExecuteGivePremiumToAccount(args);
                    break;
                case "ban":
                    ExecuteBanAccount(args);
                    break;
                case "ToID":
                    Console.WriteLine(LogicLongCodeGenerator.ToId(args[1]));
                    break;
                case "unban":
                    ExecuteUnbanAccount(args);
                    break;
                case "changename":
                    ExecuteChangeNameForAccount(args);
                    break;
                case "getvalue":
                    ExecuteGetFieldValue(args);
                    break;
                case "changevalue":
                    ExecuteChangeValueForAccount(args);
                    break;
                case "unlockall":
                    ExecuteUnlockAllForAccount(args);
                    break;
                case "trophiesall":
                    ExecuteTrophiesAll(args);
                    break;
                case "removehero":
                    ExecuteRemoveHero(args);
                    break;
                case "removeall":
                    ExecuteRemoveAllForAccount(args);
                    break;
                case "mail":
                    MailHandler.Execute(args, OwnAccountId);
                    break;
                case "starrdrop":
                    ExecuteGiveRandomReward(OwnAccountId, null);
                    break;
                case "box":
                    ExecuteGiveRandomReward(OwnAccountId, args.Length > 1 ? args[1] : null);
                    break;
                case "maintenance":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "m":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "md":
                    List<LogicData> logicDatas = DataTables.Get(DataType.Map).GetDatas();
                    LogicData logicData = logicDatas[0];
                    string s = logicData.GetCSVRow().GetValueAt(1);
                    int o = logicData.GetCSVRow().GetArraySizeAt(1);
                  //  string[] m = MapLoader.InitWithMapFromDataTable(null, DataTables.Get(19), "Tutorial");
                    break;
                case "login":
                    if (OwnAccountId == -1) break;
                    long id = LogicLongCodeGenerator.ToId(args[1]);
                    Account account = Accounts.Load(id);
                    if (account == null)
                    {
                        Console.WriteLine("Fail: account not found!");
                        return;
                    }
                    if (LogicServerListener.Instance.IsPlayerOnline(OwnAccountId))
                    {
                        LogicServerListener.Instance.GetGameListener(OwnAccountId).SendTCPMessage(new UnlockAccountOkMessage()
                        {
                            AccountId = account.AccountId,
                            PassToken = account.PassToken
                        });
                    }
                    break;
                case "changetheme":
                    if (OwnAccountId == -1) break;
                    Account account1 = Accounts.Load(OwnAccountId);
                    int i = -1;
                    try
                    {
                        i = int.Parse(args[1]);
                    }
                    catch(Exception) { }
                    account1.Home.PreferredThemeId = i;
                    break;
            }
        }
        
        private static void ExecuteGiveRandomReward(long ownAccountId, string containerName)
        {
            if (ownAccountId == -1)
            {
                Console.WriteLine("Usage: in-game /starrdrop or /box [Name]");
                return;
            }

            HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(ownAccountId);
            if (homeMode == null) return;

            if (string.IsNullOrEmpty(containerName))
            {
                RandomRewardManager.GiveStarrDrop(homeMode);
                return;
            }

            RandomRewardContainerData container = RandomRewardManager.GetContainer(containerName);
            if (container == null)
            {
                Console.WriteLine($"Fail: container {containerName} not found!");
                return;
            }
            RandomRewardManager.Give(homeMode, container);
        }

        private static void ExecuteUnlockAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            // Надежно перебираем всех персонажей в памяти
            foreach (CharacterData character in DataTables.Get(DataType.Character).GetDatas())
            {
                // Проверяем, что это не пустышка, не отключен и является героем
                if (character != null && !character.Disabled && character.IsHero())
                {
                    int heroId = character.GetGlobalId();
                    if (!account.Avatar.HasHero(heroId))
                    {
                        account.Avatar.UnlockHero(heroId); // Используем твой метод разблокировки
                    }
                }
            }

            AccountCache.SaveAll();
            Console.WriteLine($"Successfully unlocked all brawlers for account {args[1]}");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Все бойцы разблокированы! Перезайдите в игру." // Сообщение, которое увидит игрок
                });
                Sessions.Remove(id);
            }
        }

        /// <summary>/trophiesall [N] — всем аккаунтам +N кубков (первому бойцу).</summary>
        private static void ExecuteTrophiesAll(string[] args)
        {
            if (args.Length != 2 || !int.TryParse(args[1], out int n)) { Console.WriteLine("Usage: /trophiesall [N]"); return; }
            long max = Accounts.GetMaxAvatarId();
            int done = 0;
            for (long id = 1; id <= max; id++)
            {
                try
                {
                    Account account = Accounts.Load(id);
                    if (account?.Avatar?.Heroes == null || account.Avatar.Heroes.Count == 0) continue;
                    var hero = account.Avatar.Heroes[0];
                    hero.Trophies += n;
                    if (hero.Trophies > hero.HighestTrophies) hero.HighestTrophies = hero.Trophies;
                    account.Avatar.RefreshHighestTrophies();
                    Accounts.Save(account);
                    done++;
                    if (Sessions.IsSessionActive(id))
                    {
                        var session = Sessions.GetSession(id);
                        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage() { Message = "Your account updated!" });
                        Sessions.Remove(id);
                    }
                }
                catch (Exception e) { Console.WriteLine($"[trophiesall] {id}: {e.Message}"); }
            }
            Console.WriteLine($"[trophiesall] +{n} кубков выдано {done} аккаунтам");
        }

        /// <summary>/removehero [числовой id аккаунта] [GlobalId бойца] — убрать одного бойца.</summary>
        private static void ExecuteRemoveHero(string[] args)
        {
            if (args.Length != 3 || !long.TryParse(args[1], out long id) || !int.TryParse(args[2], out int heroId))
            {
                Console.WriteLine("Usage: /removehero [accountId] [heroGlobalId]");
                return;
            }
            Account account = Accounts.Load(id);
            if (account == null) { Console.WriteLine("Fail: account not found!"); return; }
            account.Avatar.RemoveHero(heroId);
            if (account.Home.CharacterIds != null)
                for (int i = 0; i < account.Home.CharacterIds.Length; i++)
                    if (account.Home.CharacterIds[i] == heroId) account.Home.CharacterIds[i] = 16000000;
            Accounts.Save(account);
            Console.WriteLine($"[removehero] аккаунт {id}: боец {heroId} убран");
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage() { Message = "Your account updated!" });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteRemoveAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /removeall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            for (int i = 1; ; i++)
            {
                if (account.Avatar.HasHero(16000000 + i))
                {
                    CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                    if (!character.IsHero()) return;

                    account.Avatar.RemoveHero(character.GetGlobalId());
                }
            }

          //  Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGivePremiumToAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /premium [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.IsPremium = true;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteUnbanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = false;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteBanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /ban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = true;
            account.Avatar.ResetTrophies();
            account.Avatar.Name = "Brawler";
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteChangeNameForAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [NewName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Name = args[2];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGetFieldValue(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            int value = (int)field.GetValue(account.Avatar);
            Console.WriteLine($"LogicClientAvatar::{args[2]} = {value}");
        }

        private static void ExecuteChangeValueForAccount(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName] [Value]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            field.SetValue(account.Avatar, int.Parse(args[3]));
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteShutdown()
        {
            Sessions.StartShutdown();
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;
        }
    }
}
