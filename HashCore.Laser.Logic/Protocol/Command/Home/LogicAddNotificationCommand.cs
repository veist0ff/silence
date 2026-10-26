namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Notification;

    /// <summary>
    /// Новое письмо, 206. LogicAddNotificationCommand::decode (0x1003d6e10):
    /// булево «есть письмо», VInt типа, тело письма, затем
    /// LogicServerCommand::decode — VInt номера серверной команды и базовые поля.
    /// </summary>
    public class LogicAddNotificationCommand : Command
    {
        public BaseNotification Notification;

        public override void Encode(ByteStream stream)
        {
            if (stream.WriteBoolean(Notification != null))
                Notification.EncodeWithType(stream);

            stream.WriteVInt(0);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 206;
        }
    }
}
