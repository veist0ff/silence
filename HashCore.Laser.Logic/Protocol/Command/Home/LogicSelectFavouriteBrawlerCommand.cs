namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
     
    public class LogicSelectFavouriteBrawlerCommand : Command
    {
        public int CharacterId;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CharacterId=ByteStreamHelper.ReadDataReference(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            if (GlobalId.GetClassId(CharacterId) != (int)DataType.Character) return -1;
            if (DataTables.Get(DataType.Character)?.GetDataByGlobalId<CharacterData>(CharacterId) == null) return -1;

            if (homeMode.Avatar.HasHero(CharacterId))
            {
                homeMode.Home.FavouriteCharacter = CharacterId;
                return 0;
            }

            return -1;
        }

        public override int GetCommandType()
        {
            return 570;
        }
    }
}
