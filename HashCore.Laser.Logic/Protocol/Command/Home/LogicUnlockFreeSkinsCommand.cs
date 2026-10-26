namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Забрать бесплатные скины», тип 526.
    ///
    /// Своего `decode` у класса в клиенте нет — значит на проводе он не несёт
    /// ничего сверх общей шапки команды. Сервер её не знал, и каждый раз, когда
    /// клиент её присылал, весь ход обрывался и состояния расходились.
    ///
    /// Выдачу пока не делаем: какие именно скины считаются бесплатными, задаётся
    /// в `canExecute`/`encode` клиента (0x1003d44e0, 0x1003d4390) и ещё не разобрано.
    /// Главное — поток больше не рвётся.
    /// </summary>
    public class LogicUnlockFreeSkinsCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 526;
        }
    }
}
