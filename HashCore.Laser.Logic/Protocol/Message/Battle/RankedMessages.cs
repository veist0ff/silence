using System.Collections.Generic;
using HashCore.Laser.Logic.Avatar.Structures;
using HashCore.Laser.Titan.Math;

namespace HashCore.Laser.Logic.Message.Battle
{
    /// <summary>Игрок рангового матча — LogicRankedMatchPlayer::decode.</summary>
    public class RankedMatchPlayer
    {
        public long AccountId;
        public string Name;
        public int Unk24;            // клиент кладёт сюда значение сезона (+488 → +16)
        public int Team;             // +28
        public int TeamIndex;        // +32 — место в команде
        public int HeroGlobalId;     // +40 выбранный боец (0 — ещё не выбран)
        public int SkinGlobalId;     // +56

        public void Encode(ByteStream s)
        {
            s.WriteVInt((int)(AccountId >> 32));
            s.WriteVInt((int)AccountId);
            s.WriteBoolean(true);
            new PlayerDisplayData(0, 0, Name).Encode(s);
            s.WriteVInt(Unk24);
            s.WriteVInt(Team);
            s.WriteVInt(TeamIndex);
            ByteStreamHelper.WriteDataReference(s, HeroGlobalId);   // +40
            s.WriteVInt(0);                                         // +48
            ByteStreamHelper.WriteDataReference(s, SkinGlobalId);   // +56
            ByteStreamHelper.WriteDataReference(s, 0);              // +64
            ByteStreamHelper.WriteDataReference(s, 0);              // +72
            ByteStreamHelper.WriteDataReference(s, 0);              // +80
            s.WriteBoolean(false);                                  // +128
            s.WriteVInt(0);                                         // +104
            ByteStreamHelper.WriteDataReference(s, 0);              // +88
            s.WriteVInt(0);                                         // +108
            ByteStreamHelper.WriteDataReference(s, 0);              // +96
            s.WriteVInt(0);                                         // открытые бойцы (LogicRankedUnlockedHero)
        }
    }

    /// <summary>
    /// 22150 — старт рангового матча (экран драфта). Раскладка — LogicRankedMatch::decode;
    /// значения — как у офлайн-сборки клиента в HomePage::startGame: uuid 0/0, ранговая
    /// локация, +52 = тип события 14, +64 — команда, которая пикает первой.
    /// </summary>
    public class RankedMatchStartedMessage : GameMessage
    {
        public int LocationGlobalId;
        public int EventType = 14;
        public int FirstPickTeam;
        public List<RankedMatchPlayer> Players = new List<RankedMatchPlayer>();

        public override int GetMessageType() => 22150;
        public override int GetServiceNodeType() => 9;

        public override void Encode()
        {
            Stream.WriteBoolean(true);
            Stream.WriteLong(0, 0);                                 // LogicUuid: два long
            Stream.WriteLong(0, 0);
            ByteStreamHelper.WriteDataReference(Stream, LocationGlobalId); // +32
            Stream.WriteVInt(0);                                    // +40 int[]
            Stream.WriteVInt(0);                                    // +48
            Stream.WriteVInt(EventType);                            // +52
            Stream.WriteVInt(Players.Count);
            foreach (RankedMatchPlayer p in Players) p.Encode(Stream);
            Stream.WriteVInt(FirstPickTeam);                        // +64
            Stream.WriteVInt(0);                                    // +68
            Stream.WriteVInt(0);                                    // +72
            Stream.WriteVInt(0);                                    // +76
            Stream.WriteVInt(0);                                    // хвост: 0 — списка нет
        }
    }

    /// <summary>22151 — начало банов: секунды на бан (+144), ещё два числа (+148, +152).</summary>
    public class RankedMatchBanStartedMessage : GameMessage
    {
        public int Seconds = 15, Unk148, Unk152;
        public override int GetMessageType() => 22151;
        public override int GetServiceNodeType() => 9;
        public override void Encode() { Stream.WriteVInt(Seconds); Stream.WriteVInt(Unk148); Stream.WriteVInt(Unk152); }
    }

    /// <summary>22152 — ответ на бан: LogicLong аккаунта, vint, боец, bool (успех), vint.</summary>
    public class RankedMatchBanHeroResponseMessage : GameMessage
    {
        public long AccountId; public int HeroGlobalId; public bool Ok = true;
        public override int GetMessageType() => 22152;
        public override int GetServiceNodeType() => 9;
        public override void Encode()
        {
            Stream.WriteVInt((int)(AccountId >> 32)); Stream.WriteVInt((int)AccountId);
            Stream.WriteVInt(0);
            ByteStreamHelper.WriteDataReference(Stream, HeroGlobalId);
            Stream.WriteBoolean(Ok);
            Stream.WriteVInt(0);
        }
    }

    /// <summary>22153 — конец банов: по игроку {long аккаунта, забаненный боец}.</summary>
    public class RankedMatchBanEndedMessage : GameMessage
    {
        public List<KeyValuePair<long, int>> Bans = new List<KeyValuePair<long, int>>();
        public override int GetMessageType() => 22153;
        public override int GetServiceNodeType() => 9;
        public override void Encode()
        {
            Stream.WriteVInt(Bans.Count);
            foreach (var b in Bans) { Stream.WriteLong(b.Key); ByteStreamHelper.WriteDataReference(Stream, b.Value); }
        }
    }

    /// <summary>22164 — состояние банов: {long, боец} предварительные баны, затем {long, bool} «забанил».</summary>
    public class RankedMatchBanStatusUpdateMessage : GameMessage
    {
        public List<KeyValuePair<long, int>> PreBans = new List<KeyValuePair<long, int>>();
        public List<KeyValuePair<long, bool>> Done = new List<KeyValuePair<long, bool>>();
        public override int GetMessageType() => 22164;
        public override int GetServiceNodeType() => 9;
        public override void Encode()
        {
            Stream.WriteVInt(PreBans.Count);
            foreach (var b in PreBans) { Stream.WriteLong(b.Key); ByteStreamHelper.WriteDataReference(Stream, b.Value); }
            Stream.WriteVInt(Done.Count);
            foreach (var d in Done) { Stream.WriteLong(d.Key); Stream.WriteBoolean(d.Value); }
        }
    }

    /// <summary>
    /// 22154 — ход пика: секунды (+144), текущий игрок (bool + LogicRankedMatchPlayer),
    /// сколько пикает за ход (+160), +164, LogicLong следующего игрока (+168).
    /// </summary>
    public class RankedMatchPickStartedMessage : GameMessage
    {
        public int Seconds = 20; public RankedMatchPlayer Current; public int PicksThisTurn = 1; public long NextAccountId;
        public override int GetMessageType() => 22154;
        public override int GetServiceNodeType() => 9;
        public override void Encode()
        {
            Stream.WriteVInt(Seconds);
            Stream.WriteBoolean(Current != null);
            Current?.Encode(Stream);
            Stream.WriteVInt(PicksThisTurn);
            Stream.WriteVInt(0);
            Stream.WriteVInt((int)(NextAccountId >> 32)); Stream.WriteVInt((int)NextAccountId);
        }
    }

    /// <summary>22156 — боец выбран: bool + LogicRankedMatchPlayer (боец в +40).</summary>
    public class RankedMatchHeroPickedMessage : GameMessage
    {
        public RankedMatchPlayer Player;
        public override int GetMessageType() => 22156;
        public override int GetServiceNodeType() => 9;
        public override void Encode() { Stream.WriteBoolean(Player != null); Player?.Encode(Stream); }
    }

    /// <summary>12152 — клиент банит бойца (dataRef).</summary>
    public class RankedMatchBanHeroMessage : GameMessage
    {
        public int HeroGlobalId;
        public override int GetMessageType() => 12152;
        public override int GetServiceNodeType() => 9;
        public override void Decode() { HeroGlobalId = ByteStreamHelper.ReadDataReference(Stream); }
    }

    /// <summary>12155 — клиент выбирает бойца: vint, dataRef, bool.</summary>
    public class RankedMatchPickHeroMessage : GameMessage
    {
        public int Unk; public int HeroGlobalId; public bool Flag;
        public override int GetMessageType() => 12155;
        public override int GetServiceNodeType() => 9;
        public override void Decode() { Unk = Stream.ReadVInt(); HeroGlobalId = ByteStreamHelper.ReadDataReference(Stream); Flag = Stream.ReadBoolean(); }
    }
}
