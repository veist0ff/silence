namespace HashCore.Laser.Logic.Message.Home
{
    using System.Collections.Generic;

    /// <summary>Запрос списка каналов Brawl TV, тип 14700 (iOS ListBrawlTvChannelsMessage): VInt, bool.</summary>
    public class ListBrawlTvChannelsMessage : GameMessage
    {
        public int Unknown;
        public bool Flag;

        public override void Decode()
        {
            Unknown = Stream.ReadVInt();
            Flag = Stream.ReadBoolean();
        }

        public override int GetMessageType() => 14700;

        public override int GetServiceNodeType() => 9;
    }

    /// <summary>Включить канал, тип 14701 (iOS TuneBrawlTvChannelMessage): VInt канал, VInt (-1).</summary>
    public class TuneBrawlTvChannelMessage : GameMessage
    {
        public int Channel;
        public int Unknown;

        public override void Decode()
        {
            Channel = Stream.ReadVInt();
            Unknown = Stream.ReadVInt();
        }

        public override int GetMessageType() => 14701;

        public override int GetServiceNodeType() => 9;
    }

    /// <summary>
    /// Список каналов, тип 24700 (iOS BrawlTvChannelListMessage::encode 0x1004b1c80):
    /// VInt число, каждый канал — VInt id и две строки (BrawlTvChannelInfo 0x1004b1ad4).
    /// </summary>
    public class BrawlTvChannelListMessage : GameMessage
    {
        public List<(int Id, string A, string B)> Channels = new();

        public override void Encode()
        {
            Stream.WriteVInt(Channels.Count);
            foreach (var c in Channels)
            {
                Stream.WriteVInt(c.Id);
                Stream.WriteString(c.A);
                Stream.WriteString(c.B);
            }
        }

        public override int GetMessageType() => 24700;

        public override int GetServiceNodeType() => 9;
    }

    /// <summary>
    /// Следующий бой на канале, тип 24701 (iOS BrawlTvChannelNextUpMessage::encode 0x1004b1df0):
    /// VInt канал, VInt, bool «прямой эфир»; при true — LogicLong игрока (клиент шлёт
    /// StartSpectate 14104 с ним), при false — строка id повтора. Отрицательный старший
    /// int id — «на канале пусто», клиент запросит список заново.
    /// </summary>
    public class BrawlTvChannelNextUpMessage : GameMessage
    {
        public int Channel;
        public long AccountId = -1;

        public override void Encode()
        {
            Stream.WriteVInt(Channel);
            Stream.WriteVInt(0);
            Stream.WriteBoolean(true);
            Stream.WriteInt(AccountId < 0 ? -1 : (int)(AccountId >> 32));
            Stream.WriteInt(AccountId < 0 ? -1 : (int)AccountId);
        }

        public override int GetMessageType() => 24701;

        public override int GetServiceNodeType() => 9;
    }
}
