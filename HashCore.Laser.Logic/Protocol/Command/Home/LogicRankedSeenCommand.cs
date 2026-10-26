namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
     
    /// <summary>
    /// «Ранговый сезон посмотрели», тип 576.
    ///
    /// Формат по клиенту (`LogicRankedSeasonSeenCommand::decode`): заголовок и два
    /// сырых int-а. Сервер читал VInt ДО заголовка и на этом останавливался —
    /// восемь байт оставались в потоке, и разбор пакета дальше шёл по мусору.
    /// </summary>
    public class LogicRankedSeenCommand : Command
    {
        public int Season;
        public int SeenState;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            Season = stream.ReadInt();
            SeenState = stream.ReadInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 576;
        }
    }
}
