namespace HashCore.Laser.Logic.Command.Home
{
    using System.Runtime.InteropServices;
    using HashCore.Laser.Logic.Data;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
    
    public class LogicSelectEmoteCommand : Command
    {
        private int EmoteID;
        private int EmoteSlot;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            EmoteID = stream.ReadVInt();
            EmoteSlot =stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (EmoteSlot < 0 || EmoteSlot > 6) return -1;
            switch (EmoteSlot)
            {
                case 4:
                    homeMode.Home.PlayerSelectedEmotes[4] = EmoteID;
                    break;
                case 5:
                    homeMode.Home.PlayerSelectedEmotes[5] = EmoteID;
                    break;
            }

            EmoteData emoteData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(EmoteID);

            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character);
            if (characterData == null) return 0;
            Hero hero = homeMode.Avatar.GetHero(characterData.GetGlobalId()) ?? null;
            
            if (emoteData.Character != null && characterData != null) homeMode.Avatar.SetEmoteForBrawler(characterData.GetGlobalId(), EmoteSlot, EmoteID);
            if (hero.emote[1] == hero.emote[2] || hero.emote[1] == hero.emote[3]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[1] = 160 - 3;
            if (hero.emote[2] == hero.emote[1] || hero.emote[1] == hero.emote[3]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[2] = 148 - 3;
            if (hero.emote[3] == hero.emote[1] || hero.emote[3] == hero.emote[2]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).emote[3] = 137 - 3;
            return 0;
        }

        public override int GetCommandType()
        {
            return 538;
        }
    }
}
