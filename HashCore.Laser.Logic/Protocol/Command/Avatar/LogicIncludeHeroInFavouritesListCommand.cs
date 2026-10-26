namespace HashCore.Laser.Logic.Command.Avatar
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// Добавить бойца в список любимых или убрать из него, тип 949.
    ///
    /// Порядок полей снят с клиента (`LogicIncludeHeroInFavouritesListCommand::decode`
    /// 0x1003c885c): СНАЧАЛА булев признак, и только потом ссылка на бойца. Сервер
    /// читал одну ссылку, булев байт уходил в её первый VInt, и дальше весь ход
    /// клиента разбирался со сдвигом — дом ломался, стоило поменять любимого бойца.
    ///
    /// `execute` (0x1003c86c4) зовёт `setHeroInFavouriteList(боец, признак)`; на
    /// проводе список уезжает обратно в товаре 7 с битом 0x20.
    /// </summary>
    public class LogicIncludeHeroInFavouritesListCommand : Command
    {
        /// <summary>Добавить (true) или убрать (false).</summary>
        public bool Include;

        /// <summary>Боец, глобальный идентификатор таблицы 16.</summary>
        public int CharacterId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Include = stream.ReadBoolean();
            CharacterId = ByteStreamHelper.ReadDataReference(stream);
        }

        public override void Encode(ByteStream stream)
        {
            stream.WriteBoolean(Include);
            ByteStreamHelper.WriteDataReference(stream, CharacterId);
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null || homeMode.Avatar == null) return -1;
            if (GlobalId.GetClassId(CharacterId) != (int)DataType.Character) return -1;
            if (!homeMode.Avatar.HasHero(CharacterId)) return -1;

            if (homeMode.Home.FavouriteHeroes == null)
                homeMode.Home.FavouriteHeroes = new List<int>();

            if (Include)
            {
                if (!homeMode.Home.FavouriteHeroes.Contains(CharacterId))
                    homeMode.Home.FavouriteHeroes.Add(CharacterId);
            }
            else
            {
                homeMode.Home.FavouriteHeroes.Remove(CharacterId);
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 949;
        }
    }
}
