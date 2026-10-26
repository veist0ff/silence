namespace HashCore.Laser.Logic.Home.BattleLog
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Игрок в записи журнала боёв (BattleLogPlayerEntry, Android decode 0xA75300).
    /// </summary>
    public class BattleLogPlayer
    {
        [JsonProperty] public long AccountId;
        [JsonProperty] public bool IsBot;
        [JsonProperty] public string Name;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int NameColorId;
        [JsonProperty] public int CharacterId;
        [JsonProperty] public int PowerLevel;
        [JsonProperty] public int Trophies;
        [JsonProperty] public int TeamIndex;
        [JsonProperty] public int SkinId;

        // ТЕСТ: режим выдачи журнала из /root/v64/battlelog_mode.txt (0 полный, 1 без игроков, 2 без бойцов).
        public static int DebugMode
        {
            get { try { return int.Parse(System.IO.File.ReadAllText("/root/v64/battlelog_mode.txt").Trim()); } catch { return 0; } }
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(IsBot ? 0 : 1);                               // +0: 1 — живой игрок (getRealPlayers)
            new LogicLong((int)(AccountId >> 32), (int)AccountId).Encode(stream); // +8: id (readLogicLong)
            stream.WriteVInt(0);                                           // +16
            stream.WriteBoolean(false);                                    // +20

            // Бойцы игрока в бою: ссылка на бойца и три числа.
            if (DebugMode == 2) stream.WriteVInt(0);
            else
            {
                stream.WriteVInt(1);
                ByteStreamHelper.WriteDataReference(stream, CharacterId);
                stream.WriteVInt(Trophies);                                // +40 кубки (сравниваются с порогом повторов)
                stream.WriteVInt(PowerLevel);                              // +56 сила
                stream.WriteVInt(0);                                       // +72
            }

            stream.WriteVInt(TeamIndex);                                   // +88: команда; в выживании — место

            // Отображаемые данные — как PlayerDisplayData.
            stream.WriteString(Name ?? "Brawler");
            stream.WriteVInt(5000);
            stream.WriteVInt(ThumbnailId != 0 ? ThumbnailId : 28000000);
            stream.WriteVInt(NameColorId != 0 ? NameColorId : 43000000);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteBoolean(false);                                    // необязательный LogicLong
            int skin = SkinId;
            if (skin == 0)
            {
                var ch = HashCore.Laser.Logic.Data.DataTables.Get(HashCore.Laser.Logic.Data.DataType.Character)
                    .GetDataByGlobalId<HashCore.Laser.Logic.Data.CharacterData>(CharacterId);
                skin = HashCore.Laser.Logic.Data.DataTables.Get(HashCore.Laser.Logic.Data.DataType.Skin)
                    .GetData<HashCore.Laser.Logic.Data.SkinData>(ch?.DefaultSkin ?? "")?.GetGlobalId() ?? 0;
            }
            ByteStreamHelper.WriteDataReference(stream, skin);             // +112 (предположительно скин)
            stream.WriteVInt(0);                                           // +120
            stream.WriteVInt(0);                                           // +128
            stream.WriteVInt(0);                                           // список VInt: пусто
        }
    }

    /// <summary>
    /// Запись журнала боёв (BattleLogEntry, Android encode 0xA73E80 / decode 0xA74234).
    /// Смысл полей подтверждён для слота (+0) и локации (+24), остальные числа —
    /// по догадке и подлежат проверке по экрану журнала.
    /// </summary>
    public class BattleLogEntry
    {
        [JsonProperty] public int EventSlot;
        [JsonProperty] public int LocationId;
        [JsonProperty] public int Result;          // 0 победа, 1 поражение, 2 ничья (как в BattleEndMessage)
        [JsonProperty] public int Rank;            // место в выживании
        [JsonProperty] public int TrophiesChange;
        [JsonProperty] public int DurationSeconds;
        [JsonProperty] public long TimeUtcMs;
        [JsonProperty] public long OwnAccountId;
        [JsonProperty] public List<BattleLogPlayer> Players = new List<BattleLogPlayer>();

        public void Encode(ByteStream stream)
        {
            // Тип строки журнала (+8): клиент (BattleLogItem 0x7AD438) делает по нему switch —
            // 2 выживание одиночное (deathmatch), 7 парное (tagteam), 5 командные режимы.
            int entryType = 5;
            try
            {
                var loc = HashCore.Laser.Logic.Data.DataTables.Get(HashCore.Laser.Logic.Data.DataType.Location)
                    .GetDataByGlobalId<HashCore.Laser.Logic.Data.LocationData>(LocationId);
                int gmv = loc == null ? -1 : HashCore.Laser.Logic.Util.GameModeUtil.GetGameModeVariation(loc.GameModeVariation);
                if (gmv >= 0 && HashCore.Laser.Logic.Util.GameModeUtil.GetTeamCount(gmv) > 2)
                    entryType = HashCore.Laser.Logic.Util.GameModeUtil.GetTeamSize(gmv) >= 2 ? 7 : 2;
            }
            catch { }

            int ageSeconds = (int)System.Math.Max(0, (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - TimeUtcMs) / 1000);

            stream.WriteVInt(EventSlot);          // +0  слот (isRankedSlot: ==14)
            stream.WriteVInt(Result);             // +4
            stream.WriteVInt(ageSeconds);         // +36 сколько секунд назад (refreshTime 0x7B0134)
            stream.WriteVInt(entryType);          // +8  тип строки
            stream.WriteVInt(TrophiesChange);     // +12 изменение кубков (trophy_txt)
            stream.WriteVInt(0);                  // +16
            stream.WriteBoolean(false);           // +20
            ByteStreamHelper.WriteDataReference(stream, LocationId); // +24 локация
            stream.WriteVInt(0);                  // +32
            stream.WriteVInt(-2);                 // +40 повтор: -2 = «повтор недоступен»
            stream.WriteLong(TimeUtcMs);          // +48
            stream.WriteLong(0);                  // +56
            stream.WriteVInt(0);                  // +64
            stream.WriteBoolean(false);           // +68
            stream.WriteVInt(0);                  // список VInt: пусто

            // Экран журнала рисует полный состав команд режима и берёт игроков по местам
            // (getTeamMember). Пустое место — null и вылет, поэтому добиваем команды
            // ботами-заглушками до TeamCount x TeamSize режима этой локации.
            List<BattleLogPlayer> roster = new List<BattleLogPlayer>(Players);
            try
            {
                var loc = HashCore.Laser.Logic.Data.DataTables.Get(HashCore.Laser.Logic.Data.DataType.Location)
                    .GetDataByGlobalId<HashCore.Laser.Logic.Data.LocationData>(LocationId);
                int gmv = loc == null ? -1 : HashCore.Laser.Logic.Util.GameModeUtil.GetGameModeVariation(loc.GameModeVariation);
                if (gmv >= 0)
                {
                    int teams = HashCore.Laser.Logic.Util.GameModeUtil.GetTeamCount(gmv);
                    int size = HashCore.Laser.Logic.Util.GameModeUtil.GetTeamSize(gmv);
                    int filler = Players.Count > 0 ? Players[0].CharacterId : 16000000;
                    for (int t = 0; t < teams; t++)
                    {
                        int have = roster.FindAll(x => x.TeamIndex == t).Count;
                        for (int i = have; i < size; i++)
                            roster.Add(new BattleLogPlayer { IsBot = true, Name = "Bot", CharacterId = filler, PowerLevel = 1, TeamIndex = t });
                    }
                }
            }
            catch { }
            // +88 игрока — МЕСТО его команды (BattleLogItem::setDeathmatchResultTo выводит его как
            // <RANK>, getTeamMember группирует по нему). Командные режимы: победители 1, проигравшие 2,
            // ничья — обе 1. Выживание: у каждого своё место, пары — место пары.
            var own = roster.Find(x => x.AccountId == OwnAccountId && !x.IsBot);
            int ownTeam = own?.TeamIndex ?? 0;
            int ownPlace = entryType == 5 ? (Result == 1 ? 2 : 1) : Math.Max(1, Rank);
            var teamPlace = new Dictionary<int, int>();
            teamPlace[ownTeam] = ownPlace;
            int nextPlace = 1;
            foreach (BattleLogPlayer p in roster)
            {
                if (teamPlace.ContainsKey(p.TeamIndex)) continue;
                if (entryType == 5) teamPlace[p.TeamIndex] = Result == 1 ? 1 : (Result == 2 ? 1 : 2);
                else
                {
                    while (nextPlace == ownPlace) nextPlace++;
                    teamPlace[p.TeamIndex] = nextPlace++;
                }
            }
            var encoded = new List<BattleLogPlayer>();
            foreach (BattleLogPlayer p in roster)
            {
                encoded.Add(new BattleLogPlayer
                {
                    AccountId = p.AccountId, IsBot = p.IsBot, Name = p.Name, ThumbnailId = p.ThumbnailId,
                    NameColorId = p.NameColorId, CharacterId = p.CharacterId, PowerLevel = p.PowerLevel,
                    Trophies = p.Trophies, SkinId = p.SkinId, TeamIndex = teamPlace[p.TeamIndex],
                });
            }
            roster = encoded;
            stream.WriteVInt(roster.Count);
            foreach (BattleLogPlayer p in roster) p.Encode(stream);

            stream.WriteVInt(0);                  // +72
            stream.WriteBoolean(false);           // +76
            stream.WriteVInt(-1);                 // +80 (по умолчанию -1)
            stream.WriteBoolean(false);           // +84
            stream.WriteVInt(0);                  // +88
            stream.WriteVInt(0);                  // +96
            stream.WriteVInt(0);                  // +92
            stream.WriteVInt(0);                  // +100
            stream.WriteVInt(0);                  // +104
            stream.WriteVInt(0);                  // +108
            stream.WriteBoolean(false);           // блок повтора: нет
            stream.WriteBoolean(false);           // структура 0xB12EF8: нет
            stream.WriteVInt(0);                  // +200
            stream.WriteVInt(0);                  // +208 ссылка на данные: пусто
            stream.WriteBoolean(false);           // +216
        }
    }
}
