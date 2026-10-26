namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Эту запись данных я посмотрел», тип 583.
    ///
    /// Формат снят с клиента (`LogicSetDataSeenStateCommand::decode` 0x1003d2040):
    /// ссылка на запись данных и VInt с состоянием. `execute` (0x1003d2120) кладёт
    /// пару в `LogicDataSeenStates` дома, а тот уезжает обратно в блоке
    /// LogicDataSeenStates (карта «ссылка → состояние»).
    ///
    /// На сервере команды не было — в логе она и падала как «command 583 not found»,
    /// а блок состояний уходил клиенту всегда пустым. Из-за этого всё, что клиент
    /// помечает через него — новые бойцы, предметы, звенья мега-квестов — после
    /// каждого перезахода снова показывалось новым.
    /// </summary>
    public class LogicSetDataSeenStateCommand : Command
    {
        /// <summary>Глобальный идентификатор записи данных.</summary>
        public int DataGlobalId;

        /// <summary>Состояние просмотра, как его понимает клиент.</summary>
        public int SeenState;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            DataGlobalId = ByteStreamHelper.ReadDataReference(stream);
            SeenState = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home == null) return -1;
            if (DataGlobalId <= 0) return -1;

            if (homeMode.Home.DataSeenStates == null)
                homeMode.Home.DataSeenStates = new Dictionary<int, int>();

            homeMode.Home.DataSeenStates[DataGlobalId] = SeenState;
            return 0;
        }

        public override int GetCommandType()
        {
            return 583;
        }
    }
}
