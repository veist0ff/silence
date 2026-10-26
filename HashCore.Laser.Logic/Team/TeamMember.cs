namespace HashCore.Laser.Logic.Team
{
    using HashCore.Laser.Logic.Avatar.Structures;

    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    public class TeamMember
    {
        public bool IsOwner;
        public long AccountId;

        public int CharacterId;
        public int SkinId;

        public int HeroTrophies;
        public int HeroHighestTrophies;
        public int HeroLevel;

        public int State;
        public bool IsReady;

        public int SelectedStarPowerId;

        public int SelectedGadgetId;
        public int SelectedHyperchargeId;

        public int SelectedGearId1;
        public int SelectedGearId2;

        public PlayerDisplayData DisplayData;

        public HomeMode homeMode;

        public int TeamIndex;

        public void SetHero(Hero hero)
        {
            if (hero == null) return;

            CharacterId = hero.CharacterId;

            HeroTrophies = hero.Trophies;
            HeroHighestTrophies = hero.HighestTrophies;
            HeroLevel = hero.PowerLevel;

            SkinId = hero.SelectedSkinId > 0 ? GlobalId.CreateGlobalId((int)DataType.Skin, hero.SelectedSkinId) : 0;

            SelectedStarPowerId = ToCardId(hero.SelectedStarPowerId);
            SelectedGadgetId = ToCardId(hero.SelectedGadgetId);
            SelectedHyperchargeId = ToCardId(hero.SelectedOverChargeId);

            SelectedGearId1 = GlobalId.CreateGlobalId((int)DataType.Gear, hero.SelectedGearId1);
            SelectedGearId2 = GlobalId.CreateGlobalId((int)DataType.Gear, hero.SelectedGearId2);
        }

        private static int ToCardId(int instanceId)
        {
            return instanceId > 0 ? GlobalId.CreateGlobalId((int)DataType.Card, instanceId) : 0;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteBoolean(IsOwner);
            stream.WriteLong(AccountId);

            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            ByteStreamHelper.WriteDataReference(stream, SkinId);

            stream.WriteVInt(0);
            stream.WriteVInt(HeroTrophies);
            stream.WriteVInt(HeroHighestTrophies);

            stream.WriteVInt(HeroTrophies);

            stream.WriteVInt(HeroLevel);  // sprintf'd into upgrade_count_container/txt
            stream.WriteVInt(State);      // HomePageTeamMember::shouldShowPlayerStatusText

            stream.WriteBoolean(IsReady);

            stream.WriteVInt(TeamIndex); // team
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk

            DisplayData.Encode(stream);

            WriteSlot(stream, SelectedStarPowerId);
            WriteSlot(stream, SelectedGadgetId);
            WriteSlot(stream, 0);
            WriteSlot(stream, 0);
            WriteSlot(stream, SelectedHyperchargeId);
            WriteSlot(stream, SelectedGearId1);
            WriteSlot(stream, SelectedGearId2);
            WriteSlot(stream, 0);
            WriteSlot(stream, 0);

            stream.WriteVInt(0); // data reference list
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0); // int list
            stream.WriteVInt(0); // int list
            stream.WriteVInt(0);
        }

        private static void WriteSlot(ByteStream stream, int globalId)
        {
            ByteStreamHelper.WriteDataReference(stream, globalId >= 1000000 ? globalId : 0);
        }

        public void EncodeSocialEntry(ByteStream stream)
        {
            stream.WriteLong(AccountId);

            stream.WriteVInt(homeMode?.Avatar?.Trophies ?? HeroTrophies);
            stream.WriteVInt(0);

            (DisplayData ?? new PlayerDisplayData(0, 0, string.Empty)).Encode(stream);

            stream.WriteVInt(0);
        }
    }
}
