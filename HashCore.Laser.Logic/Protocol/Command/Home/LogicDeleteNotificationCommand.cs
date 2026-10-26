namespace HashCore.Laser.Logic.Command.Home
{
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Server;

    /// <summary>
    /// Удаление письма, 514. LogicDeleteNotificationCommand::decode (0x1003c7c7c):
    /// номер письма (+28) и ожидаемый тип (+32), оба VInt. Клиент убирает
    /// письмо по номеру (LogicClientHome::removeNotification) — сервер тоже.
    /// </summary>
    public class LogicDeleteNotificationCommand : Command
    {
        public int NotificationId;
        public int ExpectedType;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(NotificationId);
            stream.WriteVInt(ExpectedType);
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            NotificationId = stream.ReadVInt();
            ExpectedType = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            NotificationFactory factory = homeMode?.Home?.NotificationFactory;
            if (factory == null) return 0;

            if (factory.Remove(NotificationId) == null)
                Logger.Print($"[inbox] удаление: письма {NotificationId} (тип {ExpectedType}) нет");
            return 0;
        }

        public override int GetCommandType()
        {
            return 514;
        }
    }
}
