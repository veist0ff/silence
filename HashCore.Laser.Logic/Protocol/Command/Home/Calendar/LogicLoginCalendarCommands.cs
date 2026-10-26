namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Calendar;

    public class LogicClaimLoginCalendarRewardCommand : Command
    {
        public int Unk28;
        public int Day;
        public int OptionIndex;
        public bool Fallback;
        public int CalendarIndex;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(Unk28);
            stream.WriteVInt(Day);
            stream.WriteVInt(OptionIndex);
            stream.WriteBoolean(Fallback);
            stream.WriteVInt(CalendarIndex);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Unk28 = stream.ReadVInt();
            Day = stream.ReadVInt();
            OptionIndex = stream.ReadVInt();
            Fallback = stream.ReadBoolean();
            CalendarIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            LoginCalendar.Claim(homeMode, Day, OptionIndex, CalendarIndex);
            return 0;
        }

        public override int GetCommandType()
        {
            return 550;
        }
    }

    public class LogicSetLoginCalendarStateCommand : Command
    {
        public int State;
        public int CalendarIndex;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(State);
            stream.WriteVInt(CalendarIndex);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            State = stream.ReadVInt();
            CalendarIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            LoginCalendar.SetState(homeMode, State, CalendarIndex);
            return 0;
        }

        public override int GetCommandType()
        {
            return 556;
        }
    }
}
