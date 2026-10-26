namespace HashCore.Laser.Logic.Home.Items
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Command.Home;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Logic.Message.Home;
    using HashCore.Laser.Logic.Notification;

    /// <summary>
    /// Входящие игрока — зеркало списка LogicClientHome +40.
    ///
    /// В доме клиент читает его сразу после LogicConfData и восьми байт +16:
    /// VInt счётчик, затем на каждое письмо VInt типа и тело
    /// (LogicClientHome::decode). Письмо, пришедшее позже, клиент добавляет через
    /// LogicAddNotificationCommand (206) — в НАЧАЛО списка
    /// (LogicClientHome::addNotification вставляет с индексом 0). Поэтому новые
    /// письма и здесь идут первыми, иначе после перезахода порядок менялся бы.
    ///
    /// Ищет клиент письмо по его номеру (+8), поэтому номера уникальны в пределах
    /// аккаунта и никогда не переиспользуются.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class NotificationFactory
    {
        /// <summary>Сколько писем хранить. Лишние просмотренные уходят первыми.</summary>
        public const int MAX_NOTIFICATIONS = 100;

        // Имя ключа новое: в старом «notification_list» лежали записи другой
        // раскладки, читать их незачем.
        [JsonProperty("notifications", ItemTypeNameHandling = TypeNameHandling.Auto)]
        public List<BaseNotification> Notifications = new List<BaseNotification>();

        [JsonProperty("next_id")]
        public int NextId = 1;

        public int Count => Notifications.Count;

        public BaseNotification Get(int id)
        {
            foreach (BaseNotification notification in Notifications)
                if (notification != null && notification.Id == id) return notification;
            return null;
        }

        public BaseNotification Remove(int id)
        {
            BaseNotification notification = Get(id);
            if (notification != null) Notifications.Remove(notification);
            return notification;
        }

        /// <summary>Выдать номер и положить в начало списка (если письмо хранимое).</summary>
        public void Add(BaseNotification notification)
        {
            if (notification == null) return;

            if (notification.Id <= 0)
            {
                if (NextId < 1) NextId = 1;
                notification.Id = NextId++;
            }

            if (!notification.IsPersistent) return;

            Notifications.Insert(0, notification);
            Trim();
        }

        private void Trim()
        {
            while (Notifications.Count > MAX_NOTIFICATIONS)
            {
                int index = Notifications.FindLastIndex(n => n == null || n.IsViewed);
                if (index < 0) index = Notifications.Count - 1;
                Notifications.RemoveAt(index);
            }
        }

        public void Encode(ByteStream stream)
        {
            Notifications.RemoveAll(n => n == null);

            stream.WriteVInt(Notifications.Count);
            foreach (BaseNotification notification in Notifications)
                notification.EncodeWithType(stream);
        }

        /// <summary>
        /// Положить письмо во входящие и, если игрок в сети, сразу отправить его
        /// клиенту командой 206. Без сети письмо придёт с домом при следующем
        /// входе.
        /// </summary>
        public static void Send(HomeMode homeMode, BaseNotification notification)
        {
            if (homeMode?.Home == null || notification == null) return;

            if (homeMode.Home.NotificationFactory == null)
                homeMode.Home.NotificationFactory = new NotificationFactory();
            homeMode.Home.NotificationFactory.Add(notification);
            Server.Logger.Print($"[inbox] отправлено письмо {notification.Id} (тип {notification.GetNotificationType()})");

            homeMode.GameListener?.SendMessage(new AvailableServerCommandMessage
            {
                Command = new LogicAddNotificationCommand { Notification = notification }
            });
        }

        /// <summary>Положить письмо в дом без отправки (игрок не в сети).</summary>
        public static void Store(ClientHome home, BaseNotification notification)
        {
            if (home == null || notification == null) return;
            if (home.NotificationFactory == null) home.NotificationFactory = new NotificationFactory();
            home.NotificationFactory.Add(notification);
        }
    }
}
