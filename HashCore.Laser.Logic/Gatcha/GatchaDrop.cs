namespace HashCore.Laser.Logic.Home.Gatcha
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Recruit;

    public class GatchaDrop
    {
        public int Count;
        public int DataGlobalId;
        public int SkinGlobalId;
        public int Type;
        public int CardGlobalId;

        public int Unk40;
        public int Unk44;
        public int Unk48;
        public int Unk52;

        public GatchaDrop(int type)
        {
            Type = type;
        }

        public void DoDrop(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;

            switch (Type)
            {
                case 2: // Native LogicGatchaDrop execution at 0x1003d90ac.
                    homeMode.Home.TokenDoubler += Count;
                    break;
                case 1: // Unlock a hero
                    CharacterData characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(DataGlobalId);
                    if (characterData == null) return;

                    CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
                    if (cardData == null) return;

                    avatar.UnlockHero(characterData.GetGlobalId());
                    break;
                // Карточка бойца: гаджет, пассивка, гиперзаряд, мутация. У клиента
                // это LogicClientAvatar::setItem(LogicCardData) — один вид выдачи
                // на все четыре, различает их только MetaType в cards.csv.
                case 4:
                {
                    CardData card = DataTables.Get(DataType.Card)
                        ?.GetDataByGlobalId<CardData>(CardGlobalId);
                    if (card == null) return;

                    // Колода рогалика (MetaType 12) принадлежит событию, а не бойцу:
                    // её можно получить и на бойца, которого у игрока ещё нет.
                    // Поэтому она живёт отдельным списком в доме, иначе карточка
                    // выдавалась бы и пропадала при следующем входе.
                    if (card.MetaType == 12)
                    {
                        int deckId = card.GetInstanceId() + 23000000;
                        homeMode.Home.AddRogueliteDeckCopy(deckId);
                        break;
                    }

                    CharacterData owner = DataTables.Get(DataType.Character)
                        ?.GetData<CharacterData>(card.Target);
                    if (owner == null) return;

                    Hero cardHero = avatar.GetHero(owner.GetGlobalId());
                    if (cardHero == null) return;

                    int cardInstanceId = card.GetInstanceId();
                    if (cardHero.UnlockedCards == null) cardHero.UnlockedCards = new List<int>();
                    if (!cardHero.UnlockedCards.Contains(cardInstanceId))
                        cardHero.UnlockedCards.Add(cardInstanceId);

                    break;
                }
                case 6: // Add power points
                    Hero hero = avatar.GetHero(DataGlobalId);
                    if (hero == null) return;

                    hero.PowerPoints += Count;
                    break;
                case 7: // Add gold
                    avatar.AddGold(Count);
                    break;
                case 8: // Add Gems (Bonus)
                    avatar.AddDiamonds(Count);
                    break;
                case 9: // 
                    homeMode.Home.UnlockedSkins.Add(SkinGlobalId);
                    break;
                case 11:
                    if (GlobalId.GetClassId(DataGlobalId) == 68 && !homeMode.Home.UnlockedSprays.Contains(DataGlobalId))
                        homeMode.Home.UnlockedSprays.Add(DataGlobalId);
                    if (DataGlobalId > 28000000 && DataGlobalId < 29000000) { homeMode.Home.UnlockedThumbnails.Add(DataGlobalId); }
                    if (DataGlobalId > 52000000 && DataGlobalId < 53000000) { homeMode.Home.UnlockedEmotes.Add(DataGlobalId); }
                    if (DataGlobalId > 76000000 && DataGlobalId < 77000000) { homeMode.Home.UnlockedTituls.Add(DataGlobalId); }
                     if (DataGlobalId > 85000000 && DataGlobalId < 86000000) { homeMode.Home.UnlockedFrames.Add(DataGlobalId); }
                    break;
                // Звёздные очки. CreateGatchaDrop давно выдаёт их видом 12, а
                // применять было некому: клиент начислял, сервер нет.
                case 12:
                    avatar.StarPoints += Count;
                    break;
                // Хроматические кредиты.
                case 23:
                    homeMode.Home.ChromaticCoins += Count;
                    break;
                case 22: // Distribute recruit tokens through LogicBrawlerRecruitRoad.
                    BrawlerRecruitRoad.DistributeRecruitTokens(homeMode, Count);
                    break;
                case 24:
                    avatar.AddPowerPoints(Count);
                    break;
                case 25:
                    avatar.AddBlings(Count);
                    break;
                case 10:
                    homeMode.Home.UnlockedEmotes.Add(DataGlobalId);
                    break;
            }
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Count);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            stream.WriteVInt(Type);

            ByteStreamHelper.WriteDataReference(stream, SkinGlobalId);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            ByteStreamHelper.WriteDataReference(stream, CardGlobalId);
            stream.WriteVInt(Unk40);
            stream.WriteVInt(Unk44);
            stream.WriteVInt(Unk48);
            stream.WriteVInt(Unk52);
        }

        public void Decode(ByteStream stream)
        {
            Count = stream.ReadVInt();
            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Type = stream.ReadVInt();

            SkinGlobalId = ByteStreamHelper.ReadDataReference(stream);
            ByteStreamHelper.ReadDataReference(stream);
            CardGlobalId = ByteStreamHelper.ReadDataReference(stream);
            Unk40 = stream.ReadVInt();
            Unk44 = stream.ReadVInt();
            Unk48 = stream.ReadVInt();
            Unk52 = stream.ReadVInt();
        }
    }
}
