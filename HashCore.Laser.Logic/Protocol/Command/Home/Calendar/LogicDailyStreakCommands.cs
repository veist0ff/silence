namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Calendar;

    public class LogicKeepDailyStreakCommand : Command
    {
        public int CalendarIndex;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(CalendarIndex);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CalendarIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            LoginCalendar.KeepStreak(homeMode, CalendarIndex);
            return 0;
        }

        public override int GetCommandType()
        {
            return 595;
        }
    }

    public class LogicResetDailyStreakCommand : Command
    {
        public int CalendarIndex;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(CalendarIndex);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            CalendarIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            LoginCalendar.ResetStreak(homeMode, CalendarIndex);
            return 0;
        }

        public override int GetCommandType()
        {
            return 596;
        }
    }
}
