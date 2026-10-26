namespace HashCore.Laser.Logic.Home.Recruit
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    public class BrawlerRecruit
    {
        public int CharacterGlobalId;
        public int TokenPrice;
        public int GemPrice;
        public int CollectedTokens;
        public int GroupingId;

        public readonly List<int> OtherChoices = new List<int>();

        public static BrawlerRecruit Create(int characterGlobalId, int collectedTokens)
        {
            BrawlerRecruit recruit = new BrawlerRecruit();
            recruit.CharacterGlobalId = characterGlobalId;

            switch (GetRarity(characterGlobalId))
            {
                case "rare": recruit.TokenPrice = 160; recruit.GemPrice = 29; recruit.GroupingId = 1; break;
                case "super_rare": recruit.TokenPrice = 430; recruit.GemPrice = 79; recruit.GroupingId = 2; break;
                case "epic": recruit.TokenPrice = 925; recruit.GemPrice = 169; recruit.GroupingId = 3; break;
                case "mega_epic": recruit.TokenPrice = 1900; recruit.GemPrice = 349; recruit.GroupingId = 4; break;
                case "legendary": recruit.TokenPrice = 3800; recruit.GemPrice = 699; recruit.GroupingId = 5; break;
                case "ultra_legendary": recruit.TokenPrice = 5500; recruit.GemPrice = 999; recruit.GroupingId = 6; break;
                default: recruit.TokenPrice = 160; recruit.GemPrice = 29; recruit.GroupingId = 1; break;
            }

            recruit.CollectedTokens = collectedTokens < 0 ? 0 : collectedTokens;
            if (recruit.CollectedTokens > recruit.TokenPrice) recruit.CollectedTokens = recruit.TokenPrice;

            return recruit;
        }

        public static string GetRarity(int characterGlobalId)
        {
            CharacterData characterData = DataTables.Get(DataType.Character)?.GetDataByGlobalId<CharacterData>(characterGlobalId);
            if (characterData == null) return null;

            CardData cardData = DataTables.Get(DataType.Card)?.GetData<CardData>(characterData.Name + "_unlock");
            return cardData?.Rarity;
        }

        public int GetRemainingTokens()
        {
            int remaining = TokenPrice - CollectedTokens;
            return remaining < 0 ? 0 : remaining;
        }

        public int GetGemCostToUnlock()
        {
            if (TokenPrice < 1) return GemPrice;

            int remaining = GetRemainingTokens();
            if (remaining < 1) return 0;

            return (remaining * GemPrice + TokenPrice - 1) / TokenPrice;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteDataReference(CharacterGlobalId);
            stream.WriteVInt(TokenPrice);
            stream.WriteVInt(GemPrice);
            stream.WriteVInt(-1); // milestones: null list
            stream.WriteVInt(CollectedTokens);
            stream.WriteVInt(GroupingId);

            stream.WriteVInt(OtherChoices.Count);
            foreach (int choice in OtherChoices)
            {
                stream.WriteDataReference(choice);
            }
        }

        public void Decode(ByteStream stream)
        {
            CharacterGlobalId = ByteStreamHelper.ReadDataReference(stream);

            if (CharacterGlobalId > 0)
            {
                TokenPrice = stream.ReadVInt();
                GemPrice = stream.ReadVInt();
                DecodeMilestones(stream);
                CollectedTokens = stream.ReadVInt();
                GroupingId = stream.ReadVInt();

                OtherChoices.Clear();
                int choices = stream.ReadVInt();
                for (int i = 0; i < choices; i++)
                {
                    OtherChoices.Add(ByteStreamHelper.ReadDataReference(stream));
                }
            }
            else
            {
                stream.ReadVInt(); // +16
                TokenPrice = stream.ReadVInt();
                GemPrice = stream.ReadVInt();
                DecodeMilestones(stream);
                GroupingId = stream.ReadVInt();
            }
        }

        private static void DecodeMilestones(ByteStream stream)
        {
            int count = stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                stream.ReadVInt();
                if (stream.ReadBoolean())
                {
                    stream.ReadVInt();
                    stream.ReadVInt();
                    ByteStreamHelper.ReadDataReference(stream);
                    stream.ReadVInt();
                }
            }
        }
    }
}
