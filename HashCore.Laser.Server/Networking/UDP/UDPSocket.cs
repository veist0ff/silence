namespace HashCore.Laser.Server.Networking
{
    using HashCore.Laser.Logic.Battle;
    using HashCore.Laser.Logic.Message;
    using HashCore.Laser.Logic.Message.Battle;

    using HashCore.Laser.Server.Message;
    using System.Net;
    using System.Linq;
    using System.Threading.Tasks;
    using HashCore.Laser.Logic.Battle.Input;

    public class UDPSocket
    {
        public readonly long SessionId;
        private EndPoint EndPoint;

        public BattleMode Battle;
        public bool IsConnected => EndPoint != null;

        public Connection TCPConnection;
        public bool IsSpectator;

        public UDPSocket(long sessionId)
        {
            SessionId = sessionId;
        }

        public void SetEndPoint(EndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        /// <summary>
        /// Сколько байт клиент вообще способен принять в одной датаграмме.
        ///
        /// UdpLaserSocket::update (0x1002423d0) читает recvfrom в буфер на 0x578
        /// байт и всё, что больше, выбрасывает с "Received too big udp packet".
        /// Десятибайтовая шапка сессии входит в этот же размер.
        /// </summary>
        public const int MaxDatagramSize = 0x578;

        /// <summary>
        /// Что в датаграмме занято не телом кадра: десять байт шапки сессии, два
        /// vint конверта пакета и сам заголовок VisionUpdateMessage вместе с
        /// четырёхбайтовой длиной битстрима. Считаем с запасом.
        /// </summary>
        public const int DatagramOverhead = 48;

        /// <summary>
        /// Датаграмма боя, разобранная по UdpLaserSocket::isValidConnection
        /// (на деле отправка, 0x1002425fc) и UdpSingleMessagePacket::decode
        /// (0x10049b350):
        ///
        ///   [8 байт id сессии, big-endian][2 байта][vint тип][vint длина][тело]
        ///
        /// Шапка идёт открытым текстом, шифруется только всё, что после неё, и то
        /// лишь когда сервер прислал nonce. Мы nonce не шлём, поэтому здесь
        /// открытый текст в обе стороны.
        /// </summary>
        public void SendMessage(GameMessage message)
        {
            if (message.GetEncodingLength() == 0) message.Encode();

            ByteStream stream = new ByteStream(64);
            stream.WriteLong(SessionId);
            stream.WriteShort(0);
            stream.WriteVInt(message.GetMessageType());
            stream.WriteVInt(message.GetEncodingLength());
            stream.WriteBytesWithoutLength(message.GetMessageBytes(), message.GetEncodingLength());

            if (stream.GetOffset() <= MaxDatagramSize)
            {
                UDPGateway.SendTo(stream.GetByteArray(), 0, stream.GetOffset(), EndPoint);
                return;
            }

            // Такой кадр клиент молча выбросит ("Received too big udp packet"),
            // и его таймер без входящих датаграмм добежит до десяти секунд —
            // после этого он перестанет слать инпуты по UDP.
            if (message is VisionUpdateMessage vision && SendFragmented(vision)) return;

            // Резать нечего или кусков не хватило — уходим по TCP, канал живой.
            if (TCPConnection != null) TCPConnection.Send(message);
        }

        /// <summary>
        /// Режет кадр на куски ровно так, как их склеивает клиент
        /// (MessageManager::combineVisionUpdates 0x10023d814): у всех кусков один
        /// тик, каждый несёт описатель `всего * 8 + номер` с номером от единицы, и
        /// клиент склеивает их битстримы по порядку слотов, как только соберёт все.
        ///
        /// Возвращает false, если кадр не влезает и в семь кусков, — тогда его
        /// надо отправлять по TCP.
        /// </summary>
        private bool SendFragmented(VisionUpdateMessage vision)
        {
            if (vision.VisionBitStream == null) return false;

            byte[] payload = vision.VisionBitStream.GetByteArray();
            int length = vision.VisionBitStream.GetLength();
            if (length <= 0) return false;

            int budget = MaxDatagramSize - DatagramOverhead;
            if (budget < 1) return false;

            int count = (length + budget - 1) / budget;
            if (count > VisionUpdateMessage.MaxFragments) return false;

            for (int i = 0; i < count; i++)
            {
                int offset = i * budget;
                int size = length - offset;
                if (size > budget) size = budget;

                byte[] chunk = new byte[size];
                System.Array.Copy(payload, offset, chunk, 0, size);

                VisionUpdateMessage fragment = new VisionUpdateMessage();
                fragment.Tick = vision.Tick;
                fragment.HandledInputs = vision.HandledInputs;
                fragment.Viewers = vision.Viewers;
                fragment.VisionBitStream = new BitStream(chunk, size);
                fragment.IsFragment = true;
                fragment.FragmentDescriptor = VisionUpdateMessage.MakeFragmentDescriptor(count, i + 1);

                fragment.Encode();

                ByteStream stream = new ByteStream(64);
                stream.WriteLong(SessionId);
                stream.WriteShort(0);
                stream.WriteVInt(fragment.GetMessageType());
                stream.WriteVInt(fragment.GetEncodingLength());
                stream.WriteBytesWithoutLength(fragment.GetMessageBytes(), fragment.GetEncodingLength());

                if (stream.GetOffset() > MaxDatagramSize) return false;

                UDPGateway.SendTo(stream.GetByteArray(), 0, stream.GetOffset(), EndPoint);
            }

            return true;
        }

        public void ProcessReceive(ByteStream stream)
        {
            // async!
            Task.Run(() =>
            {
                int type = stream.ReadVInt();
                int length = stream.ReadVInt();
                byte[] data = stream.ReadBytes(length, 2000);

                GameMessage message = MessageFactory.Instance.CreateMessageByType(type);
                if (message != null)
                {
                    message.GetByteStream().SetByteArray(data, length);
                    message.Decode();
                    HandleMessage(message);
                }
            });
        }

        private void HandleMessage(GameMessage message)
        {
            if (message.GetMessageType() == 10555)
            {
                TCPConnection.MessageManager.LastKeepAlive= DateTime.UtcNow;
                ClientInputMessage clientInputMessage = (ClientInputMessage)message;
                while (clientInputMessage.Inputs.TryDequeue(out ClientInput clientInput))
                {
                    if (!IsSpectator)
                        Battle.AddClientInput(clientInput, SessionId);
                    else
                        Battle.HandleSpectatorInput(clientInput, SessionId);
                }
            }
        }
    }
}
