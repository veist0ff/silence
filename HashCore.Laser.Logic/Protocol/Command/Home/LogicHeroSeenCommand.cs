namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Бойца посмотрели», тип 522.
    ///
    /// Формат по клиенту (0x1003c9084): заголовок, ссылка на данные бойца и сырой
    /// четырёхбайтовый int. Сервер **не вызывал базовый Decode** и потому
    /// недочитывал четыре VInt-а заголовка — поток съезжал, и весь остаток хода
    /// клиента терялся.
    /// </summary>
    public class LogicHeroSeenCommand : Command
    {
        public int HeroGlobalId;
        public int SeenState;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            HeroGlobalId = ByteStreamHelper.ReadDataReference(stream);
            SeenState = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 522;
        }
    }
}
