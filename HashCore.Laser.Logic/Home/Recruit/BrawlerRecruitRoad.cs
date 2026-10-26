namespace HashCore.Laser.Logic.Home.Recruit
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Command.Home;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Message.Home;

    public class BrawlerRecruitRoad
    {
        private const int MAX_CHOICES = 2;

        private static readonly int[] GROUP_SIZES = { 1, 1, 2, 1, 1, 3 };

        public readonly List<int> AbandonableBrawlers = new List<int>();
        public int AbandonedBrawler;
        public readonly List<BrawlerRecruit> Active = new List<BrawlerRecruit>();
        public readonly List<BrawlerRecruit> Next = new List<BrawlerRecruit>();
        public int ExtraTokens;

        public BrawlerRecruit Current
        {
            get { return Active.Count > 0 ? Active[0] : null; }
        }

        public BrawlerRecruit Find(int characterGlobalId)
        {
            foreach (BrawlerRecruit recruit in Active)
            {
                if (recruit.CharacterGlobalId == characterGlobalId) return recruit;
            }
            foreach (BrawlerRecruit recruit in Next)
            {
                if (recruit.CharacterGlobalId == characterGlobalId) return recruit;
            }
            return null;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(0); // +8: recruiting list, kept empty

            stream.WriteVInt(AbandonableBrawlers.Count);
            foreach (int brawler in AbandonableBrawlers)
            {
                stream.WriteDataReference(brawler);
            }

            stream.WriteDataReference(AbandonedBrawler);

            stream.WriteVInt(Active.Count);
            foreach (BrawlerRecruit recruit in Active)
            {
                recruit.Encode(stream);
            }

            stream.WriteVInt(Next.Count);
            foreach (BrawlerRecruit recruit in Next)
            {
                recruit.Encode(stream);
            }

            stream.WriteVInt(0); // +48: gem offer longs
            stream.WriteVInt(ExtraTokens);
        }

        public void Decode(ByteStream stream)
        {
            ReadDataReferences(stream, null); // +8: recruiting list

            AbandonableBrawlers.Clear();
            ReadDataReferences(stream, AbandonableBrawlers);

            AbandonedBrawler = ByteStreamHelper.ReadDataReference(stream);

            ReadRecruits(stream, Active);
            ReadRecruits(stream, Next);

            int longs = stream.ReadVInt();
            for (int i = 0; i < longs; i++)
            {
                stream.ReadVInt();
                stream.ReadVInt();
            }

            ExtraTokens = stream.ReadVInt();
        }

        private static void ReadDataReferences(ByteStream stream, List<int> target)
        {
            int count = stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                int globalId = ByteStreamHelper.ReadDataReference(stream);
                if (target != null) target.Add(globalId);
            }
        }

        private static void ReadRecruits(ByteStream stream, List<BrawlerRecruit> target)
        {
            target.Clear();

            int count = stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                BrawlerRecruit recruit = new BrawlerRecruit();
                recruit.Decode(stream);
                target.Add(recruit);
            }
        }

        public static BrawlerRecruitRoad Build(ClientHome home)
        {
            BrawlerRecruitRoad road = new BrawlerRecruitRoad();
            ClientAvatar avatar = home.HomeMode?.Avatar;

            road.ExtraTokens = avatar != null ? avatar.RareTokens : 0;
            road.AbandonedBrawler = home.AbandonedRecruitBrawler;

            List<int> queue = GetQueue(home, avatar);
            MigrateRecruitState(home, queue);
            if (queue.Count == 0) return road;

            BrawlerRecruit current = null;
            if (home.ActiveRecruitBrawler != 0)
            {
                current = BrawlerRecruit.Create(home.ActiveRecruitBrawler, home.RecruitTokens);
                string rarity = BrawlerRecruit.GetRarity(home.ActiveRecruitBrawler);

                for (int i = 0; i < queue.Count && current.OtherChoices.Count < MAX_CHOICES; i++)
                {
                    int candidate = queue[i];
                    if (candidate == home.ActiveRecruitBrawler) continue;
                    if (BrawlerRecruit.GetRarity(candidate) == rarity) current.OtherChoices.Add(candidate);
                }

                road.Active.Add(current);
                road.AbandonableBrawlers.Add(home.ActiveRecruitBrawler);
            }

            List<BrawlerRecruit> pending = new List<BrawlerRecruit>();
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i] == home.ActiveRecruitBrawler) continue;
                if (current != null && current.OtherChoices.Contains(queue[i])) continue;
                pending.Add(BrawlerRecruit.Create(queue[i], 0));
            }

            List<BrawlerRecruit> ordered = new List<BrawlerRecruit>();
            for (int group = 1; group <= 6; group++)
            {
                foreach (BrawlerRecruit recruit in pending)
                {
                    if (recruit.GroupingId == group) ordered.Add(recruit);
                }
            }

            int groupingId = 0;
            int groupRarity = 0;
            int groupSize = 0;
            int sizeIndex = 0;
            int groupLimit = GROUP_SIZES[0];
            foreach (BrawlerRecruit recruit in ordered)
            {
                int recruitRarity = recruit.GroupingId;
                if (recruitRarity != groupRarity || groupSize >= groupLimit)
                {
                    groupRarity = recruitRarity;
                    groupSize = 0;
                    groupingId++;
                    groupLimit = GROUP_SIZES[sizeIndex % GROUP_SIZES.Length];
                    sizeIndex++;
                }

                recruit.GroupingId = groupingId;
                groupSize++;
                road.Next.Add(recruit);
            }

            return road;
        }

        private static void MigrateRecruitState(ClientHome home, List<int> queue)
        {
            if (home.RecruitRoadStateVersion < 1)
            {
                home.ActiveRecruitBrawler = queue.Count > 0 ? queue[0] : 0;
                home.RecruitRoadStateVersion = 1;
            }

            if (home.ActiveRecruitBrawler != 0 && !queue.Contains(home.ActiveRecruitBrawler))
            {
                if (home.RecruitTokens > 0 && home.HomeMode?.Avatar != null)
                {
                    home.HomeMode.Avatar.AddRareTokens(home.RecruitTokens);
                }

                home.ActiveRecruitBrawler = 0;
                home.RecruitTokens = 0;
            }

            if (queue.Count == 0)
            {
                home.ActiveRecruitBrawler = 0;
                home.RecruitTokens = 0;
            }
        }

        public static List<int> GetQueue(ClientHome home, ClientAvatar avatar)
        {
            List<int> queue = new List<int>();
            int stored = home.UnlockStarRoad != null ? home.UnlockStarRoad.Length : 0;

            for (int i = 0; i < stored; i++)
            {
                int brawler = home.UnlockStarRoad[i];
                if (brawler <= 0) continue;
                if (queue.Contains(brawler)) continue;
                if (!IsAvailableRecruit(brawler)) continue;
                if (avatar != null && avatar.HasHero(brawler)) continue;
                queue.Add(brawler);
            }

            foreach (int brawler in DefaultRoad)
            {
                if (queue.Contains(brawler)) continue;
                if (!IsAvailableRecruit(brawler)) continue;
                if (avatar != null && avatar.HasHero(brawler)) continue;
                queue.Add(brawler);
            }

            if (queue.Count != stored) home.UnlockStarRoad = queue.ToArray();
            return queue;
        }

        private static int[] DefaultRoadCache;

        public static int[] DefaultRoad
        {
            get
            {
                if (DefaultRoadCache == null) DefaultRoadCache = BuildDefaultRoad();
                return DefaultRoadCache;
            }
        }

        private static int[] BuildDefaultRoad()
        {
            DataTable table = DataTables.Get(DataType.Character);
            if (table == null) return new int[0];

            List<int> road = new List<int>();
            foreach (CharacterData character in table.GetDatas())
            {
                if (character == null || !IsAvailableRecruit(character.GetGlobalId())) continue;

                int globalId = character.GetGlobalId();
                road.Add(globalId);
            }

            road.Sort(delegate (int a, int b)
            {
                int rank = GetRarityRank(a).CompareTo(GetRarityRank(b));
                return rank != 0 ? rank : a.CompareTo(b);
            });

            return road.ToArray();
        }

        private static bool IsAvailableRecruit(int characterGlobalId)
        {
            CharacterData character = DataTables.Get(DataType.Character)?.GetDataByGlobalId<CharacterData>(characterGlobalId);
            if (character == null || character.Disabled || character.LockedForChronos || !character.IsHero()) return false;

            CardData unlockCard = DataTables.Get(DataType.Card)?.GetData<CardData>(character.Name + "_unlock");
            if (unlockCard == null || unlockCard.Disabled || unlockCard.LockedForChronos) return false;

            return GetRarityRank(characterGlobalId) != 0;
        }

        private static int GetRarityRank(int characterGlobalId)
        {
            switch (BrawlerRecruit.GetRarity(characterGlobalId))
            {
                case "rare": return 1;
                case "super_rare": return 2;
                case "epic": return 3;
                case "mega_epic": return 4;
                case "legendary": return 5;
                case "ultra_legendary": return 6;
            }
            return 0;
        }

        private static void MoveToBack(ClientHome home, int characterGlobalId)
        {
            List<int> queue = GetQueue(home, home.HomeMode?.Avatar);
            if (!queue.Remove(characterGlobalId)) return;

            queue.Add(characterGlobalId);
            home.UnlockStarRoad = queue.ToArray();
        }

        private static void GrantBrawler(HomeMode homeMode, int characterGlobalId, bool completesActiveRecruit,
            bool sendDelivery = true)
        {
            ClientHome home = homeMode.Home;
            ClientAvatar avatar = homeMode.Avatar;

            if (avatar != null && !avatar.HasHero(characterGlobalId))
            {
                avatar.UnlockHero(characterGlobalId);
            }

            List<int> queue = GetQueue(home, avatar);
            queue.Remove(characterGlobalId);
            home.UnlockStarRoad = queue.ToArray();

            if (completesActiveRecruit)
            {
                home.ActiveRecruitBrawler = 0;
                home.RecruitTokens = 0;
                home.AbandonedRecruitBrawler = 0;
            }

            SendRoadChanged(homeMode);

            // Доставку шлём не всегда. Когда бойца забрал сам игрок, клиент уже
            // показал выдачу на своей же команде: LogicRecruitRoadClaimBrawlerCommand::execute
            // (0x1003cf600) при успешном canExecute дёргает слушатель +512 с этим
            // бойцом. Досылать сверху доставку — значит показать выдачу ВТОРОЙ раз,
            // ровно это и происходило: анимация, «продолжить», и она же снова.
            if (sendDelivery) SendBrawlerDelivery(homeMode, characterGlobalId);
        }

        public static void SendRoadChanged(HomeMode homeMode)
        {
            if (homeMode?.GameListener == null) return;

            LogicBrawlerRecruitRoadChangedCommand command = new LogicBrawlerRecruitRoadChangedCommand();
            command.homeMode = homeMode;

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        private static void SendBrawlerDelivery(HomeMode homeMode, int characterGlobalId)
        {
            if (homeMode?.GameListener == null) return;

            GatchaDrop drop = new GatchaDrop(1);
            drop.DataGlobalId = characterGlobalId;
            drop.Count = 1;

            DeliveryUnit unit = new DeliveryUnit(100);
            unit.AddDrop(drop);

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            command.DeliveryUnits.Add(unit);

            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
        }

        public static int ProcessExtraTokens(HomeMode homeMode)
        {
            int result = Pour(homeMode);
            if (result == 0) SendRoadChanged(homeMode);
            return result;
        }

        /// <summary>
        /// Курс обмена кредитов на славу. Должен совпадать с ключом конфигурации
        /// 10054, который уходит клиенту: иначе он посчитает своё, сервер своё, и
        /// игрока выбьет рассинхроном прямо в лобби.
        /// </summary>
        public const int FameExchangeRate = 100;

        /// <summary>
        /// Начисление кредитов вербовки.
        ///
        /// Когда открывать больше некого, кредиты в игру не идут — они переводятся
        /// в славу. Правило снято с клиента
        /// (LogicBrawlerRecruitRoad::distributeRecruitTokens, 0x1003b7760): при
        /// отсутствии текущей вербовки берётся ключ 10054 и считается
        /// (кредиты * курс + 99) / 100, то есть с округлением вверх, после чего
        /// кредиты обнуляются.
        ///
        /// Сервер же всегда просто добавлял кредиты. У клиента они превращались в
        /// славу, у сервера оставались кредитами — состояния расходились, и лобби
        /// встречало ошибкой рассинхрона.
        /// </summary>
        public static void DistributeRecruitTokens(HomeMode homeMode, int count)
        {
            if (homeMode?.Avatar == null || count < 1) return;

            if (HasRecruitToUnlock(homeMode))
            {
                homeMode.Avatar.AddRareTokens(count);
                return;
            }

            // Обмениваем ВЕСЬ остаток вместе с новым начислением — так же, как клиент:
            // он складывает в свой счётчик и переводит его целиком.
            int tokens = homeMode.Avatar.RareTokens + count;
            if (tokens < 1) return;

            int fame = (tokens * FameExchangeRate + 99) / 100;

            homeMode.Avatar.RareTokens = 0;
            homeMode.Avatar.FameCredits += fame;

            HashCore.Laser.Server.Logger.Print($"[recruitRoad] все бойцы открыты: "
                + $"{tokens} кредитов обменяны на {fame} славы");
        }

        /// <summary>Есть ли кого вербовать. Пока есть — кредиты остаются кредитами.</summary>
        private static bool HasRecruitToUnlock(HomeMode homeMode)
        {
            if (homeMode?.Home == null) return false;

            BrawlerRecruit current = Build(homeMode.Home).Current;
            return current != null;
        }

        private static int Pour(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;
            if (avatar == null) return 2;

            BrawlerRecruit current = Build(homeMode.Home).Current;
            if (current == null) return 3;

            int need = current.GetRemainingTokens();
            if (need < 1) return 4;

            int take = avatar.RareTokens < need ? avatar.RareTokens : need;
            if (take < 1) return 4;

            avatar.UseRareTokens(take);
            homeMode.Home.RecruitTokens += take;
            return 0;
        }

        public static int Claim(HomeMode homeMode, int characterGlobalId)
        {
            if (homeMode?.Avatar != null && homeMode.Avatar.HasHero(characterGlobalId)) return 0;

            Pour(homeMode);

            BrawlerRecruit current = Build(homeMode.Home).Current;
            if (current == null) return 3;
            if (current.GetRemainingTokens() > 0) return 4;

            if (characterGlobalId != current.CharacterGlobalId && !current.OtherChoices.Contains(characterGlobalId))
            {
                return 5;
            }

            // Выдачу не досылаем: её уже показал сам клиент, см. GrantBrawler.
            GrantBrawler(homeMode, characterGlobalId, true, false);
            return 0;
        }

        public static int Purchase(HomeMode homeMode, int characterGlobalId, int resourceGlobalId)
        {
            ClientHome home = homeMode.Home;
            ClientAvatar avatar = homeMode.Avatar;
            if (avatar == null) return 2;

            BrawlerRecruit recruit = Build(home).Find(characterGlobalId);
            if (recruit == null) return 5;

            switch (resourceGlobalId % 1000000)
            {
                case 19:
                {
                    int cost = recruit.GetRemainingTokens();
                    if (avatar.RareTokens < cost) return 3;
                    if (cost > 0) avatar.UseRareTokens(cost);
                    break;
                }
                case 20:
                {
                    int cost = recruit.TokenPrice;
                    if (home.ChromaticCoins < cost) return 3;
                    home.ChromaticCoins -= cost;
                    break;
                }
                default:
                {
                    int cost = recruit.GetGemCostToUnlock();
                    if (avatar.Diamonds < cost) return 3;
                    if (cost > 0) avatar.UseDiamonds(cost);
                    break;
                }
            }

            bool completesActiveRecruit = home.ActiveRecruitBrawler == characterGlobalId;
            GrantBrawler(homeMode, characterGlobalId, completesActiveRecruit);
            return 0;
        }

        public static int Switch(HomeMode homeMode, int characterGlobalId)
        {
            BrawlerRecruit current = Build(homeMode.Home).Current;
            if (current == null) return 3;
            if (current.CharacterGlobalId == characterGlobalId) return 4;
            if (!current.OtherChoices.Contains(characterGlobalId)) return 5;

            if (BrawlerRecruit.GetRarity(characterGlobalId) != BrawlerRecruit.GetRarity(current.CharacterGlobalId))
            {
                return 5;
            }

            homeMode.Home.ActiveRecruitBrawler = characterGlobalId;
            SendRoadChanged(homeMode);
            return 0;
        }

        public static int Select(HomeMode homeMode, int characterGlobalId)
        {
            BrawlerRecruitRoad road = Build(homeMode.Home);
            if (road.Current != null) return 3;
            if (road.Next.Count == 0) return 4;

            BrawlerRecruit selected = road.Find(characterGlobalId);
            if (selected == null || selected.GroupingId != road.Next[0].GroupingId) return 4;

            homeMode.Home.ActiveRecruitBrawler = characterGlobalId;
            homeMode.Home.AbandonedRecruitBrawler = 0;
            Pour(homeMode);
            SendRoadChanged(homeMode);
            return 0;
        }

        public static int Abandon(HomeMode homeMode, int characterGlobalId)
        {
            ClientHome home = homeMode.Home;

            BrawlerRecruitRoad road = Build(home);
            BrawlerRecruit current = road.Current;
            if (current == null || current.CharacterGlobalId != characterGlobalId) return 3;
            if (road.Next.Count == 0) return 3;

            if (homeMode.Avatar != null && current.CollectedTokens > 0)
            {
                homeMode.Avatar.AddRareTokens(current.CollectedTokens);
            }

            home.RecruitTokens = 0;
            home.AbandonedRecruitBrawler = characterGlobalId;
            home.ActiveRecruitBrawler = 0;

            MoveToBack(home, characterGlobalId);
            SendRoadChanged(homeMode);
            return 0;
        }
    }
}
