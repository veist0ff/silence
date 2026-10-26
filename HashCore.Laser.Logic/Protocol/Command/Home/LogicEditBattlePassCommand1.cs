namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;
     
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Structures;
     
    /// <summary>
    /// Флаги просмотренных всплывающих окон, тип 567.
    ///
    /// Класс у клиента называется `LogicSetInfoPopupSeenFlagsCommand`, а его
    /// `decode` (0x1003d2408) читает ДВА сырых четырёхбайтовых int-а. Сервер
    /// держал тут «правку боевого пропуска» и читал одно булево — восемь байт
    /// оставались в потоке, и весь остаток хода клиента разбирался мусором.
    /// </summary>
    public class LogicEditBattlePassCommand1 : Command
    {
        public int SeenFlags;
        public int SeenFlags2;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            SeenFlags = stream.ReadInt();
            SeenFlags2 = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 567;
        }
    }
}
