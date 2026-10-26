namespace HashCore.Laser.Logic.Stream.Entry
{
    using HashCore.Laser.Logic.Avatar;
    using HashCore.Laser.Logic.Avatar.Structures;

    public class AllianceStreamEntry
    {
        public const int TYPE_CHAT = 2;

        public const int TYPE_JOIN_REQUEST = 3;

        public const int TYPE_EVENT = 4;

        /// <summary>
        /// Пин (быстрое сообщение). Номер типа тот же, что у командного потока:
        /// `QuickChatStreamEntry::GetStreamEntryType` возвращает 8, а разбирает его
        /// клиент в `QuickChatStreamEntry::decode` (0x1004aa7b8).
        /// </summary>
        public const int TYPE_QUICK_CHAT = 8;

        public long Id;
        public long AuthorId;
        public string AuthorName;
        public string Message;
        public AllianceRole AuthorRole;

        public DateTime SendTime;

        public long PlayerId;
        public string PlayerName;

        public int Type;
        public int Event;

        public int EventValue;

        public int RequestState;

        public PlayerDisplayData RequestDisplayData;

        /// <summary>Пин: строка в messages.csv.</summary>
        public int MessageDataId;

        /// <summary>Пин: кому адресован, ноль — всем.</summary>
        public long TargetId;

        /// <summary>Пин: имя адресата.</summary>
        public string TargetPlayerName;

        public int QuickChatUnknown1;
        public int QuickChatUnknown2;

        public AllianceStreamEntry()
        {
            SendTime = DateTime.UtcNow;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Type);

            EncodeHeader(stream);

            switch (Type)
            {
                case TYPE_EVENT:
                    EncodeEvent(stream);
                    break;

                case TYPE_JOIN_REQUEST:
                    EncodeJoinRequest(stream);
                    break;

                case TYPE_QUICK_CHAT:
                    EncodeQuickChat(stream);
                    break;

                default:
                    EncodeChat(stream);
                    break;
            }
        }

        private void EncodeHeader(ByteStream stream)
        {
            stream.WriteVLong(Id);
            stream.WriteVLong(AuthorId);
            stream.WriteString(AuthorName);
            stream.WriteVInt((int)AuthorRole);
            stream.WriteVInt((int)(DateTime.UtcNow - SendTime).TotalSeconds);
            stream.WriteBoolean(false);
        }

        private void EncodeChat(ByteStream stream)
        {
            stream.WriteString(Message);
            stream.WriteBoolean(false);
        }

        private void EncodeEvent(ByteStream stream)
        {
            stream.WriteVInt(Event);
            stream.WriteVInt(EventValue);

            if (stream.WriteBoolean(PlayerId != 0))
            {
                stream.WriteVLong(PlayerId);
                stream.WriteString(PlayerName);
            }
        }

        /// <summary>
        /// Порядок полей — `QuickChatStreamEntry::encode` (0x1004aa714):
        /// ссылка на сообщение, признак адресата с его идентификатором, имя
        /// адресата и два числа.
        /// </summary>
        private void EncodeQuickChat(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, MessageDataId);

            if (stream.WriteBoolean(TargetId > 0)) stream.WriteLong(TargetId);

            stream.WriteString(TargetPlayerName ?? string.Empty);
            stream.WriteVInt(QuickChatUnknown1);
            stream.WriteVInt(QuickChatUnknown2);
        }

        private void EncodeJoinRequest(ByteStream stream)
        {
            stream.WriteString(Message);
            stream.WriteString(PlayerName);
            stream.WriteVInt(RequestState);
            (RequestDisplayData ?? new PlayerDisplayData(0, 0, PlayerName)).Encode(stream);
        }
    }
}
