using System.Net;

namespace HashCore.Laser.Logic.Message.Udp
{
    public class UdpConnectionInfoMessage : GameMessage
    {
        public int ServerPort;
        public string ServerAddress;

        public long SessionId;

        /// <summary>80 байт: ключ (32) + два nonce по 24. null — шифрование не включаем.</summary>
        public byte[] Nonce;

        public UdpConnectionInfoMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Stream.WriteVInt(ServerPort);
            Stream.WriteString(ServerAddress);

            // Блоб 1 — заголовок UDP-пакета: клиент копирует ровно 10 байт
            // (UdpLaserSocket::createSocket 0x100242108) и ставит их в начало каждой датаграммы.
            Stream.WriteInt(10);
            Stream.WriteLong(SessionId);
            Stream.WriteShort(0);

            // Блоб 2 — nonce. Клиент читает из него 80 байт:
            //   [0..31]  ключ
            //   [32..55] nonce для PepperPerMessageEncrypter #1 (приём)
            //   [56..79] nonce для PepperPerMessageEncrypter #2 (отправка)
            // При null шифровальщики не создаются. Проверяем, не это ли держит сокет.
            if (Nonce != null && Nonce.Length == 80)
            {
                Stream.WriteInt(80);
                Stream.WriteBytesWithoutLength(Nonce, 80);
            }
            else
            {
                Stream.WriteInt(-1);
            }
        }

        public override int GetMessageType()
        {
            return 24112;
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}
