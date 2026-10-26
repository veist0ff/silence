namespace HashCore.Laser.Logic.Home
{
    using System;
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Command;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Home.Gatcha;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Listener;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Titan.Math;

    public class HomeMode
    {
        public const int UNLOCKABLE_HEROES_COUNT = 100;

        public readonly LogicGameListener GameListener;

        public ClientHome Home;
        public ClientAvatar Avatar;

        public Action<int> CharacterChanged;
        
        public int ActiveBoxType;

        public HomeMode(ClientHome home, ClientAvatar avatar, LogicGameListener gameListener)
        {
            Home = home;
            Avatar = avatar;

            Home.HomeMode = this;
            Avatar.HomeMode = this;

            GameListener = gameListener;
        }

        public static HomeMode LoadHomeState(LogicGameListener gameListener, ClientHome home, ClientAvatar avatar, EventData[] events)
        {
            home.Events = events;

            HomeMode homeMode = new HomeMode(home, avatar, gameListener);
            homeMode.Enter(DateTime.UtcNow);

            return homeMode;
        }

        public bool HasHeroUnlocked(int Brawler)
        {
            return Avatar.HasHero(Brawler);
        }

        private bool GetRandomBrawlerForGatcha(Random rand, DeliveryUnit unit)
        {
            int brawlersCount = UNLOCKABLE_HEROES_COUNT;
            int brawlerId = -1;

            bool done = false;
            int attempts = 0;
            while (!done && attempts < 25)
            {
                attempts++;
                brawlerId = GlobalId.CreateGlobalId(16, rand.Next(0, brawlersCount));

                CharacterData data = DataTables.Get(DataType.Character)?.GetDataByGlobalId<CharacterData>(brawlerId);
                if (data == null) continue;
                done = !data.Disabled && !Avatar.HasHero(brawlerId);

                if (done)
                {
                    CardData card = DataTables.Get(DataType.Card)?.GetData<CardData>(data.Name + "_unlock");
                    if (card == null) continue;
                    done = card.Rarity != "common";
                    if (done)
                    {
                        done = card.Name != "Blower_unlock";
                        if (done)
                        {
                            if (card.Rarity == "epic")
                            {
                                if (Avatar.RollsSinceGoodDrop > 6)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                            else if (card.Rarity == "mega_epic")
                            {
                                if (Avatar.RollsSinceGoodDrop > 25)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                            else if (card.Rarity == "legendary")
                            {
                                if (Avatar.RollsSinceGoodDrop > 100)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                        }
                    }
                }

                if (done)
                {
                    GatchaDrop drop = new GatchaDrop(1);
                    drop.DataGlobalId = brawlerId;
                    drop.Count = 1;
                    unit.AddDrop(drop);
                    break;
                }
            }

            return done;
        }

        public void SimulateGatcha(DeliveryUnit unit)
        {
            Avatar.RollsSinceGoodDrop++;
            Random rand = new Random();

            int unlockedBrawlersCount = Avatar.GetUnlockedHeroesCount();

            List<int> powerPoints = new List<int>();
            if (unit.Type == 10)
            {
                int brawlerChance = 10;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 70;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 40;
                }

                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                }

                if (!isBrawler)
                {
                    GatchaDrop coins = new GatchaDrop(7);
                    coins.Count = rand.Next(15, 40);
                    unit.AddDrop(coins);

                    for (int i = 0; i < 2; i++)
                    {
                        List<Hero> unlockedHeroes = Avatar.Heroes;
                        bool heroValid = false;
                        int generateAttempts = 0;
                        int idx = -1;
                        while (!heroValid && generateAttempts < 10 && unlockedHeroes != null && unlockedHeroes.Count > 0)
                        {
                            generateAttempts++;
                            idx = rand.Next(unlockedHeroes.Count);
                            heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                            if (heroValid)
                            {
                                GatchaDrop drop = new GatchaDrop(6);
                                drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                                if (powerPoints.Contains(drop.DataGlobalId))
                                {
                                    continue;
                                }
                                powerPoints.Add(drop.DataGlobalId);
                                drop.Count = LogicMath.Min(rand.Next(5, 25), (1410 - unlockedHeroes[idx].PowerPoints));
                                unit.AddDrop(drop);
                            }
                        }
                    }
                }
            }
            else if (unit.Type == 11)
            {
                GatchaDrop coins = new GatchaDrop(7);
                coins.Count = rand.Next(322, 600);
                unit.AddDrop(coins);

                for (int i = 0; i < 5; i++)
                {
                    List<Hero> unlockedHeroes = Avatar.Heroes;
                    bool heroValid = false;
                    int generateAttempts = 0;
                    int idx = -1;
                    while (!heroValid && generateAttempts < 10 && unlockedHeroes != null && unlockedHeroes.Count > 0)
                    {
                        generateAttempts++;
                        idx = rand.Next(unlockedHeroes.Count);
                        heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                        if (heroValid)
                        {
                            GatchaDrop drop = new GatchaDrop(6);
                            drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                            if (powerPoints.Contains(drop.DataGlobalId))
                            {
                                continue;
                            }
                            powerPoints.Add(drop.DataGlobalId);
                            drop.Count = LogicMath.Min(rand.Next(40, 80), (1410 - unlockedHeroes[idx].PowerPoints));
                            unit.AddDrop(drop);
                        }
                    }
                }

                int brawlerChance = 20;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 70;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 40;
                }

                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                }
            }
            else if (unit.Type == 12)
            {
                GatchaDrop coins = new GatchaDrop(7);
                coins.Count = rand.Next(60, 250);
                unit.AddDrop(coins);

                for (int i = 0; i < 3; i++)
                {
                    List<Hero> unlockedHeroes = Avatar.Heroes;
                    bool heroValid = false;
                    int generateAttempts = 0;
                    int idx = -1;
                    while (!heroValid && generateAttempts < 10 && unlockedHeroes != null && unlockedHeroes.Count > 0)
                    {
                        generateAttempts++;
                        idx = rand.Next(unlockedHeroes.Count);
                        heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                        if (heroValid)
                        {
                            GatchaDrop drop = new GatchaDrop(6);
                            drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                            if (powerPoints.Contains(drop.DataGlobalId))
                            {
                                continue;
                            }
                            powerPoints.Add(drop.DataGlobalId);
                            drop.Count = LogicMath.Min(rand.Next(10, 50), (1410 - unlockedHeroes[idx].PowerPoints));
                            unit.AddDrop(drop);
                        }
                    }
                }

                int brawlerChance = 20;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 70;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 40;
                }
                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                }
            }

            bool bonus = rand.Next(0, 100) < 50;
            if (bonus)
            {
                int count = rand.Next(2, 7) + 1;
                GatchaDrop drop = new GatchaDrop(8);
                drop.Count = count;
                unit.AddDrop(drop);
            }
        }

        public void Enter(DateTime dateTime)
        {
            // OwnHomeData is encoded after Enter; refresh the player snapshot first.
            Home.RefreshGigiSnapshot(dateTime.ToUniversalTime());
            Home.HomeVisited();
        }

        public void GenerateBoxReward(int boxType)
        {
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();

            DeliveryUnit unit = new DeliveryUnit(100);
            Random rand = new Random();

            if (boxType == 1)
            {
                int[] graffitySkins = new int[] { 101, 102, 103 };

                for (int i = 0; i < 5; i++)
                {
                    if (rand.Next(0, 2) == 0)
                    {
                        GatchaDrop coinDrop = new GatchaDrop(7);
                        coinDrop.Count = rand.Next(10, 50);
                        unit.AddDrop(coinDrop);
                        
                        if (this.Avatar != null) this.Avatar.AddGold(coinDrop.Count);
                    }
                    else
                    {
                        GatchaDrop gemDrop = new GatchaDrop(8);
                        gemDrop.Count = rand.Next(1, 5);
                        unit.AddDrop(gemDrop);
                        
                        if (this.Avatar != null) this.Avatar.AddDiamonds(gemDrop.Count);
                    }
                }

                GatchaDrop skinDrop = new GatchaDrop(9);
                skinDrop.SkinGlobalId = GlobalId.CreateGlobalId(29, graffitySkins[rand.Next(graffitySkins.Length)]);
                skinDrop.Count = 1;
                unit.AddDrop(skinDrop);
            }
            else if (boxType == 2)
            {
                int[] mechaSkins = new int[] { 1388, 1389, 1390, 1391, 1392, 1393 };

                for (int i = 0; i < 9; i++)
                {
                    if (rand.Next(0, 2) == 0)
                    {
                        GatchaDrop coinDrop = new GatchaDrop(7);
                        coinDrop.Count = rand.Next(25, 101);
                        unit.AddDrop(coinDrop);
                        
                        if (this.Avatar != null) this.Avatar.AddGold(coinDrop.Count);
                    }
                    else
                    {
                        GatchaDrop gemDrop = new GatchaDrop(8);
                        gemDrop.Count = rand.Next(1, 10);
                        unit.AddDrop(gemDrop);
                        
                        if (this.Avatar != null) this.Avatar.AddDiamonds(gemDrop.Count);
                    }
                }

                GatchaDrop skinDrop = new GatchaDrop(9);
                skinDrop.SkinGlobalId = GlobalId.CreateGlobalId(29, mechaSkins[rand.Next(mechaSkins.Length)]);
                skinDrop.Count = 1;
                unit.AddDrop(skinDrop);
            }

            command.DeliveryUnits.Add(unit);

            AvailableServerCommandMessage msg = new AvailableServerCommandMessage();
            msg.Command = command;
            this.GameListener.SendMessage(msg);
        }

        public void ClientTurnReceived(int tick, int checksum, List<Command> commands)
        {
            foreach (Command command in commands)
            {
                if (command.Execute(this) != 0)
                {
                    OutOfSyncMessage outOfSync = new OutOfSyncMessage();
                    GameListener.SendMessage(outOfSync);
                }
            }
            Home.Tick();
        }
    }
}
