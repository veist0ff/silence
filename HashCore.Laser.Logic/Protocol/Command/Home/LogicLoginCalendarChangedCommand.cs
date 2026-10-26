using HashCore.Laser.Logic.Command;
using HashCore.Laser.Logic.Home;
using HashCore.Laser.Logic.Home.Calendar;

public class LogicLoginCalendarChangedCommand : Command
{
    public LoginCalendar Calendar;

    public WelcomeBackLoginCalendarEntry WelcomeBack;

    public int CalendarIndex;

    public int StreakFlag;

    public int ServerCommandId = -1;

    public bool Echo { get; private set; }

    public override void Encode(ByteStream stream)
    {
        stream.WriteBoolean(Calendar != null);
        if (Calendar != null) Calendar.Encode(stream);

        stream.WriteBoolean(WelcomeBack != null);
        if (WelcomeBack != null) WelcomeBack.Encode(stream);

        stream.WriteVInt(CalendarIndex);
        stream.WriteVInt(StreakFlag);

        stream.WriteVInt(ServerCommandId);
        base.Encode(stream);
    }

    public override void Decode(ByteStream stream)
    {
        Echo = true;

        Calendar = null;
        if (stream.ReadBoolean())
        {
            Calendar = new LoginCalendar();
            Calendar.Decode(stream);
        }

        if (stream.ReadBoolean())
        {
            WelcomeBack = new WelcomeBackLoginCalendarEntry();
            WelcomeBack.Decode(stream);
        }
        else
        {
            WelcomeBack = null;
        }

        CalendarIndex = stream.ReadVInt();
        StreakFlag = stream.ReadVInt();

        ServerCommandId = stream.ReadVInt();
        base.Decode(stream);
    }

    public override int Execute(HomeMode homeMode)
    {
        return 0;
    }

    public override int GetCommandType()
    {
        return 226;
    }
}
