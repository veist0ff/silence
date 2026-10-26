namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Задания просмотрены», тип 877.
    ///
    /// Сервер держал этот класс под номером 533, которого клиент не шлёт вовсе.
    /// Настоящий номер — 877 (`getCommandType` 0x1003ce804), и у команды есть поле:
    /// `decode` (0x1003ce844) читает один VInt.
    ///
    /// Логика снята с клиентского `execute` (0x1003ce7ac -> 0x1004c7094): помечаются
    /// просмотренными все задания, кроме мега-квестов, у которых поле +0 не равно
    /// пришедшему числу. Клиент шлёт сюда −1, когда просто закрыл список заданий, —
    /// тогда мега-квесты остаются непросмотренными, у них своя витрина.
    ///
    /// Пока команда была неизвестна, сервер на ней обрывал весь ход клиента
    /// («Команда под индексом 0 не была декодирована»), и всё, что шло следом,
    /// применялось только у клиента — отсюда и расхождение состояний.
    /// </summary>
    public class LogicQuestsSeenCommand : Command
    {
        /// <summary>
        /// Какую цепочку мега-квестов заодно пометить просмотренной.
        /// −1 — только обычные задания.
        /// </summary>
        public int MegaQuestFilter = -1;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            MegaQuestFilter = stream.ReadVInt();
        }

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);

            stream.WriteVInt(MegaQuestFilter);
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode?.Home?.questsList == null) return 2;

            foreach (Quest quest in homeMode.Home.questsList)
            {
                if (quest == null || quest.Checknuto) continue;

                // Мега-квест помечается только своей цепочкой.
                if (quest.IsMegaQuest && quest.MegaChain != MegaQuestFilter) continue;

                quest.Checknuto = true;
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 877;
        }
    }
}
