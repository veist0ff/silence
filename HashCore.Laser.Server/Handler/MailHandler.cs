namespace HashCore.Laser.Server.Handler
{
    using Al;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Notification;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server.Database;
    using HashCore.Laser.Server.Networking.Session;

    /// <summary>
    /// /mail — письма во входящие.
    ///
    ///   /mail &lt;кому&gt; &lt;что&gt; [значения] [текст письма]
    ///
    /// кому: me (из игры), all (все в сети), тег (#2PP…) или числовой id.
    /// что:
    ///   text &lt;текст&gt;             письмо-текст (81)
    ///   floater &lt;текст&gt;          всплывающая надпись в доме (66)
    ///   important &lt;текст&gt;        важное сообщение (57)
    ///   gems &lt;n&gt;                  гемы (89)
    ///   coins|pp|bling|credits|chroma|fame|starpoints &lt;n&gt;   ресурс (90)
    ///   doubler &lt;n&gt;               удвоитель жетонов (88)
    ///   brawler &lt;id&gt;             боец (93); id — номер строки characters.csv или глобальный
    ///   pp_hero &lt;id&gt; &lt;n&gt;         очки силы конкретному бойцу (92)
    ///   skin &lt;id&gt;                скин (94)
    ///   card &lt;id&gt;                карточка: гаджет/пассивка/гипер (84)
    ///   vanity &lt;глобальный id&gt;    спрей, значок, иконка, титул или рамка (72)
    ///   box &lt;имя контейнера&gt; [n]  ящик из random_reward_containers (43)
    ///   offer &lt;тип&gt; &lt;n&gt;          любое предложение магазина по типу (64)
    ///
    /// Игроку в сети письмо приходит сразу, остальным — с домом при входе.
    /// </summary>
    public static class MailHandler
    {
        public static void Execute(string[] args, long ownAccountId)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: /mail [me|all|TAG|id] [text|floater|important|gems|coins|pp|bling|credits|chroma|fame|starpoints|doubler|brawler|pp_hero|skin|card|vanity|box|offer] [values] [text]");
                return;
            }

            string kind = args[2].ToLowerInvariant();
            int rest = 3;

            Func<BaseNotification> factory;
            try
            {
                factory = Build(kind, args, ref rest);
            }
            catch (Exception e)
            {
                Console.WriteLine("Fail: " + e.Message);
                return;
            }
            if (factory == null)
            {
                Console.WriteLine($"Fail: unknown mail kind {kind}");
                return;
            }

            string text = rest < args.Length ? string.Join(" ", args, rest, args.Length - rest) : null;

            int sent = 0;
            foreach (long accountId in ResolveTargets(args[1], ownAccountId))
            {
                BaseNotification notification = factory();
                if (text != null && string.IsNullOrEmpty(notification.Text)) notification.Text = text;
                if (Deliver(accountId, notification)) sent++;
            }

            Console.WriteLine($"[mail] {kind}: отправлено {sent}");
        }

        private static Func<BaseNotification> Build(string kind, string[] args, ref int rest)
        {
            int i = rest;
            int Int() => int.Parse(args[i++]);
            string Tail() { string t = string.Join(" ", args, i, args.Length - i); i = args.Length; return t; }

            Func<BaseNotification> result;
            switch (kind)
            {
                case "text":
                {
                    string t = Tail();
                    result = () => new FreeTextNotification(t);
                    break;
                }
                case "floater":
                {
                    string t = Tail();
                    result = () => new FloaterTextNotification(t);
                    break;
                }
                case "important":
                {
                    string t = Tail();
                    result = () => new ImportantMessageNotification { Text = t, Message = t };
                    break;
                }
                case "gems":
                {
                    int n = Int();
                    result = () => new GemRewardNotification(n);
                    break;
                }
                case "doubler":
                {
                    int n = Int();
                    result = () => new CoinDoublerRewardNotification { Count = n };
                    break;
                }
                case "coins": result = Resource(NotificationRewards.RESOURCE_COINS, Int()); break;
                case "pp": result = Resource(NotificationRewards.RESOURCE_POWER_POINTS, Int()); break;
                case "bling": result = Resource(NotificationRewards.RESOURCE_BLING, Int()); break;
                case "credits": result = Resource(NotificationRewards.RESOURCE_RECRUIT_TOKENS, Int()); break;
                case "chroma": result = Resource(NotificationRewards.RESOURCE_CHROMATIC_TOKENS, Int()); break;
                case "fame": result = Resource(NotificationRewards.RESOURCE_FAME, Int()); break;
                case "starpoints":
                {
                    int n = Int();
                    result = () => new StarPointMigrationNotification { StarPoints = n };
                    break;
                }
                case "brawler":
                {
                    int hero = ToGlobal(16, Int());
                    CheckData(DataType.Character, hero);
                    result = () => new HeroRewardNotification { HeroGlobalId = hero };
                    break;
                }
                case "pp_hero":
                {
                    int hero = ToGlobal(16, Int());
                    int n = Int();
                    CheckData(DataType.Character, hero);
                    result = () => new HeroPowerRewardNotification { HeroGlobalId = hero, Count = n };
                    break;
                }
                case "skin":
                {
                    int skin = ToGlobal(29, Int());
                    CheckData(DataType.Skin, skin);
                    result = () => new SkinRewardNotification { SkinGlobalId = skin };
                    break;
                }
                case "card":
                {
                    int card = ToGlobal(23, Int());
                    CheckData(DataType.Card, card);
                    result = () => new StarPowerRewardNotification { CardGlobalId = card };
                    break;
                }
                case "vanity":
                {
                    int item = Int();
                    result = () => new VanityItemRewardNotification { ItemGlobalId = item };
                    break;
                }
                case "box":
                {
                    string name = args[i++];
                    int count = 1;
                    if (i < args.Length && int.TryParse(args[i], out int parsed))
                    {
                        count = parsed;
                        i++;
                    }
                    RandomRewardContainerData container = DataTables.Get(DataType.RandomRewardContainer)
                        ?.GetData<RandomRewardContainerData>(name);
                    if (container == null) throw new Exception($"container {name} not found");
                    int id = container.GetGlobalId();
                    result = () => new StarrDropContainerRewardNotification { ContainerGlobalId = id, Count = count };
                    break;
                }
                case "offer":
                {
                    int type = Int();
                    int n = Int();
                    result = () => new GemOfferRewardNotification { OfferType = type, Count = n };
                    break;
                }
                default:
                    result = null;
                    break;
            }

            rest = i;
            return result;
        }

        private static Func<BaseNotification> Resource(int resourceGlobalId, int count)
            => () => new ResourceRewardNotification { ResourceGlobalId = resourceGlobalId, Count = count };

        private static int ToGlobal(int classId, int value)
            => value >= 1000000 ? value : GlobalId.CreateGlobalId(classId, value);

        private static void CheckData(DataType type, int globalId)
        {
            if (DataTables.Get(type)?.GetDataByGlobalId<LogicData>(globalId) == null)
                throw new Exception($"{type} {globalId} not found");
        }

        private static IEnumerable<long> ResolveTargets(string target, long ownAccountId)
        {
            switch (target.ToLowerInvariant())
            {
                case "me":
                    if (ownAccountId > 0) yield return ownAccountId;
                    else Console.WriteLine("Fail: 'me' works only from the game chat");
                    yield break;
                case "all":
                    foreach (Session session in Sessions.GetAll())
                        if (session?.Home?.Avatar != null) yield return session.Home.Avatar.AccountId;
                    yield break;
            }

            if (long.TryParse(target, out long numeric))
            {
                yield return numeric;
                yield break;
            }

            yield return LogicLongCodeGenerator.ToId(target);
        }

        private static bool Deliver(long accountId, BaseNotification notification)
        {
            HomeMode homeMode = Sessions.IsSessionActive(accountId) ? Sessions.GetSession(accountId)?.Home : null;
            if (homeMode != null)
            {
                NotificationFactory.Send(homeMode, notification);
                return true;
            }

            Account account = Accounts.Load(accountId);
            if (account?.Home == null)
            {
                Console.WriteLine($"Fail: account {accountId} not found!");
                return false;
            }

            NotificationFactory.Store(account.Home, notification);
            return true;
        }
    }
}
