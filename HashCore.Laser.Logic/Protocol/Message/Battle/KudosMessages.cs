namespace HashCore.Laser.Logic.Message.Battle
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Battle.Structures;

    /// <summary>
    /// Похвалы одного боя (KudosStatus клиента): пары «кто → кого» номерами записей в
    /// итогах боя (PlayerEntry). Общие для всех игроков боя.
    /// </summary>
    public class KudosBattle
    {
        public readonly List<(int From, int To)> Pairs = new List<(int, int)>();
        public readonly List<BattlePlayer> Players;
        public KudosBattle(List<BattlePlayer> players) { Players = players; }

        public void Encode(ByteStream stream)
        {
            lock (Pairs)
            {
                stream.WriteVInt(Pairs.Count);
                foreach (var p in Pairs)
                {
                    stream.WriteByte((byte)p.From);
                    stream.WriteByte((byte)p.To);
                }
            }
        }

        private static readonly ConcurrentDictionary<long, KudosBattle> s_byAccount = new ConcurrentDictionary<long, KudosBattle>();
        public static void Bind(long accountId, KudosBattle battle) => s_byAccount[accountId] = battle;
        public static KudosBattle Of(long accountId) => s_byAccount.TryGetValue(accountId, out var b) ? b : null;
    }

    /// <summary>15349 KudosMessage: Int8 — кто хвалит, Int8 — кого (номера в итогах боя).</summary>
    public class KudosMessage : GameMessage
    {
        public int From;
        public int To;

        public override void Decode()
        {
            From = Stream.ReadByte();
            To = Stream.ReadByte();
        }

        public override int GetMessageType() => 15349;
        public override int GetServiceNodeType() => 27;
    }

    /// <summary>25634 KudosStatusMessage: обновлённый список похвал боя.</summary>
    public class KudosStatusMessage : GameMessage
    {
        public KudosBattle Battle;

        public override void Encode()
        {
            if (Battle != null) Battle.Encode(Stream);
            else Stream.WriteVInt(0);
        }

        public override int GetMessageType() => 25634;
        public override int GetServiceNodeType() => 27;
    }
}
