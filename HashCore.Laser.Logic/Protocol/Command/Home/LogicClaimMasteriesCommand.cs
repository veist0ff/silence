namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Data;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Gatcha;
     
    using HashCore.Laser.Logic.Message.Home;
     
    using HashCore.Laser.Logic.Util;
    using System.Runtime.InteropServices;
    using HashCore.Laser.Server;

    public class LogicClaimMasteriesCommand : Command
    {
        private int Tick1;
        private int Brawler;
        private int MasteryIndex;

        public override void Decode(ByteStream stream)
        {
            Tick1 = stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            Brawler = stream.ReadVInt();
            MasteryIndex = stream.ReadVInt()-1;
        }

        public override int Execute(HomeMode homeMode)
        {
            MasteryData masteryData = DataTables.Get(DataType.Mastery).GetData<MasteryData>(MasteryIndex);
            MasteryVanityData masteryVanityData = DataTables.Get(DataType.MasteryVanity).GetData<MasteryVanityData>(Brawler);
            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(Brawler);
            CardData cardData = DataTables.Get(DataType.Card).GetData<CardData>(characterData.Name + "_unlock");
            TitlesData titlesData = DataTables.Get(DataType.Titul).GetData<TitlesData>(masteryVanityData.RewardTitles);
            PlayerThumbnailData playerThumbnailData = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(masteryVanityData.RewardPlayerIcons);
            EmoteData emoteData = DataTables.Get(DataType.Emote).GetData<EmoteData>(masteryVanityData.RewardEmotes);

            int rewardCount;
            string rewardType;

            if (cardData.Rarity == "common" || cardData.Rarity == "Common")
            {
                rewardCount = masteryData.RewardCount_Common;
                rewardType = masteryData.RewardType_Common;
            }
            else if (cardData.Rarity == "rare" || cardData.Rarity == "Rare")
            {
                rewardCount = masteryData.RewardCount_Rare;
                rewardType = masteryData.RewardType_Rare;
            }
            else if (cardData.Rarity == "super_rare")
            {
                rewardCount = masteryData.RewardCount_SuperRare;
                rewardType = masteryData.RewardType_SuperRare;
            }
            else if (cardData.Rarity == "epic" || cardData.Rarity == "Epic")
            {
                rewardCount = masteryData.RewardCount_Epic;
                rewardType = masteryData.RewardType_Epic;
            }
            else if (cardData.Rarity == "mega_epic")
            {
                rewardCount = masteryData.RewardCount_Mythic;
                rewardType = masteryData.RewardType_Mythic;
            }
            else if (cardData.Rarity == "legendary" || cardData.Rarity == "Legendary")
            {
                rewardCount = masteryData.RewardCount_Legendary;
                rewardType = masteryData.RewardType_Legendary;
            }
            else
            {
                return -1;

            }

            // !!! говнокод щас будет !!! \\
            switch (rewardType)
            {
                case "Coins":
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);

                    GatchaDrop drop = new GatchaDrop(7);
                    drop.Count = rewardCount;
                    unit.AddDrop(drop);

                    command.DeliveryUnits.Add(unit);
                    command.Execute(homeMode);

                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    homeMode.GameListener.SendMessage(message);

                    break;
                case "PowerPoints":
                    LogicGiveDeliveryItemsCommand command1 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit1 = new DeliveryUnit(100);

                    GatchaDrop drop1 = new GatchaDrop(24);
                    drop1.Count = rewardCount;
                    unit1.AddDrop(drop1);

                    command1.DeliveryUnits.Add(unit1);
                    command1.Execute(homeMode);

                    AvailableServerCommandMessage message1 = new AvailableServerCommandMessage();
                    message1.Command = command1;
                    homeMode.GameListener.SendMessage(message1);

                    break;
                case "Credits":
                    LogicGiveDeliveryItemsCommand command2 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit12 = new DeliveryUnit(100);

                    GatchaDrop drop12 = new GatchaDrop(22);
                    drop12.Count = rewardCount;
                    unit12.AddDrop(drop12);

                    command2.DeliveryUnits.Add(unit12);
                    command2.Execute(homeMode);

                    AvailableServerCommandMessage message2 = new AvailableServerCommandMessage();
                    message2.Command = command2;
                    homeMode.GameListener.SendMessage(message2);

                    break;
                case "UniqueEmote":

                    LogicGiveDeliveryItemsCommand command4 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit2 = new DeliveryUnit(100);

                    GatchaDrop drop2 = new GatchaDrop(11);
                    drop2.DataGlobalId = emoteData.GetGlobalId();
                    drop2.Count = 1;
                    unit2.AddDrop(drop2);

                    command4.DeliveryUnits.Add(unit2);
                    command4.Execute(homeMode);

                    AvailableServerCommandMessage message2w = new AvailableServerCommandMessage();
                    message2w.Command = command4;
                    homeMode.GameListener.SendMessage(message2w);
                    break;
                case "UniquePlayerIcon":
                    LogicGiveDeliveryItemsCommand command44 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit4 = new DeliveryUnit(100);

                    GatchaDrop drop4 = new GatchaDrop(11);
                    drop4.DataGlobalId = playerThumbnailData.GetGlobalId();
                    Console.WriteLine(drop4.DataGlobalId);
                    drop4.Count = 1;
                    unit4.AddDrop(drop4);

                    command44.DeliveryUnits.Add(unit4);
                    command44.Execute(homeMode);

                    AvailableServerCommandMessage message4 = new AvailableServerCommandMessage();
                    message4.Command = command44;
                    homeMode.GameListener.SendMessage(message4);

                    break;
                case "UniqueTitle":
                    LogicGiveDeliveryItemsCommand command5 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit5 = new DeliveryUnit(100);

                    GatchaDrop drop5 = new GatchaDrop(11);
                    drop5.DataGlobalId = titlesData.GetGlobalId();
                    drop5.Count = 1;
                    unit5.AddDrop(drop5);

                    command5.DeliveryUnits.Add(unit5);
                    command5.Execute(homeMode);

                    AvailableServerCommandMessage message5 = new AvailableServerCommandMessage();
                    message5.Command = command5;
                    homeMode.GameListener.SendMessage(message5);
                    break;
            }
            Logger.Print($"Claim mastery reward: {Brawler}, MasteryIndex: {MasteryIndex} ");

            return 0;
        }

        public override int GetCommandType()
        {
            return 569;
        }
    }
}
