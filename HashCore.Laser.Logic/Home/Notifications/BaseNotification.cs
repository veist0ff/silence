namespace HashCore.Laser.Logic.Notification
{
    using Newtonsoft.Json;
    using HashCore.Laser.Logic.Home;

    /// <summary>
    /// Письмо во входящих. Раскладка снята с клиента, BaseNotification::decode
    /// (0x1004d1bf8) и encode (0x1004d1b78):
    ///
    ///   int     +8   номер письма — по нему клиент ищет письмо в командах 514/528
    ///   bool    +12  просмотрено
    ///   int     +16  сколько секунд назад пришло (InboxItem::refreshTime
    ///                прибавляет к нему время, прошедшее с получения дома)
    ///   string  +24  текст
    ///   vint    +48
    ///
    /// Дальше каждый тип пишет свои поля. Перед письмом всегда идёт VInt его
    /// типа — и в списке дома, и в LogicAddNotificationCommand: по нему клиент
    /// создаёт объект через NotificationFactory::create (0x1004d4140). Лишний или
    /// недостающий байт в теле ломает весь остаток пакета.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseNotification
    {
        [JsonProperty] public int Id;
        [JsonProperty] public bool IsViewed;
        [JsonProperty] public long CreatedAt;
        [JsonProperty] public string Text;
        [JsonProperty] public int Extra48;

        protected BaseNotification()
        {
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public abstract int GetNotificationType();

        /// <summary>
        /// Хранить ли письмо во входящих. Всплывающий текст (66) клиент только
        /// показывает, в список дома его класть незачем.
        /// </summary>
        public virtual bool IsPersistent => true;

        public int GetSecondsPassed()
        {
            long passed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - CreatedAt;
            if (passed < 0) return 0;
            return passed > int.MaxValue ? int.MaxValue : (int)passed;
        }

        public virtual void Encode(ByteStream stream)
        {
            stream.WriteInt(Id);
            stream.WriteBoolean(IsViewed);
            stream.WriteInt(GetSecondsPassed());
            stream.WriteString(Text);
            stream.WriteVInt(Extra48);
        }

        /// <summary>Тип и тело — ровно то, что клиент читает за одно письмо.</summary>
        public void EncodeWithType(ByteStream stream)
        {
            stream.WriteVInt(GetNotificationType());
            Encode(stream);
        }

        /// <summary>
        /// Игрок открыл письмо (LogicViewInboxNotificationCommand, 528). Клиент в
        /// этот момент выдаёт награду сам или ждёт её от сервера — см.
        /// NotificationRewards и переопределения в типах.
        /// </summary>
        public virtual void OnView(HomeMode homeMode)
        {
        }
    }
}
