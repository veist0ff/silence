namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// «Дорожку ежедневных побед посмотрели», тип 750.
    ///
    /// В клиенте это `LogicDailyWinTrackSeenCommand` (0x1003c7ac0): `encode` и
    /// `decode` у неё пустые — на проводе нет ни одного собственного поля, — а
    /// `execute` (0x1003c7b30) ставит единицу в байт +24 менеджера случайных наград
    /// (`LogicRandomRewardManager`). Мы делаем ровно то же и запоминаем отметку в
    /// доме, чтобы она уехала обратно в следующем блоке менеджера.
    /// </summary>
    public class LogicDailyWinTrackSeenCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
        }

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            if (homeMode == null || homeMode.Home == null) return 2;

            homeMode.Home.DailyWinTrackSeen = true;
            return 0;
        }

        public override int GetCommandType()
        {
            return 750;
        }
    }
}
