namespace HashCore.Laser.Logic.Message.Battle
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Battle.Structures;

    public class BattleEndMessage : GameMessage
    {
        public BattleEndMessage() : base()
        {
        }

        public int Result;
        public int TokensReward = 10000;
        public int TrophiesReward;
        public List<BattlePlayer> Players;

        public BattlePlayer OwnPlayer;
        public bool StarToken;

        public int GameMode;
        /// <summary>+160: тип экрана результатов (2 и 5 — испытание, иначе обычный бой).</summary>
        public int BattleEndType;
        /// <summary>+364: тип боя, читается в BattleEndPopup::getOutcomeCont.</summary>
        public int GameType;

        /// <summary>+268: идентификатор испытания; -1 означает «обычный бой».</summary>
        public int ChallengeId = -1;

        /// <summary>+468: общий прогресс рогалика. При 0 секция события скрыта целиком.</summary>
        public int RogueliteProgress;
        /// <summary>+472: базовая часть прогресса; разница с <see cref="RogueliteProgress"/> идёт в бонус "+N".</summary>
        public int RogueliteBase;
        /// <summary>+520: номер пройденного этапа рогалика (подстановка в TID_ROGUELITE_STAGE_COMPLETED).</summary>
        public int RogueliteStage;
        public bool BattleWithoutTrophies;
        public bool IsPvP;
        public KudosBattle Kudos;

        public override void Encode()
        {
            // === Главная структура (sub_8738BC) ===
            
            Stream.WriteLong(OwnPlayer.AccountId); // a1 + 96, 100
            Stream.WriteLong(OwnPlayer.AccountId); // a1 + 104, 108
            
            // +160 — это НЕ игровой режим, а тип экрана результатов. Клиент в
            // BattleEndPopup::isChallengeWin/isVictory трактует значения 2 и 5 как
            // испытание, поэтому запись сюда номера режима превращала Heist (2) и
            // BrawlBall (5) в «испытание» вместо обычного боя на кубки.
            // Настоящий режим уходит ниже, в +360.
            Stream.WriteVInt(BattleEndType);  // +160
            Stream.WriteVInt(Result);         // +164
            Stream.WriteVInt(TokensReward);   // a1 + 120
            Stream.WriteVInt(8); // a1 + 124
            Stream.WriteVInt(0); // a1 + 128
            Stream.WriteVInt(0); // a1 + 132
            Stream.WriteVInt(0); // a1 + 136
            Stream.WriteVInt(0); // a1 + 140
            Stream.WriteVInt(0); // a1 + 144
            Stream.WriteVInt(0); // a1 + 148
            Stream.WriteVInt(0); // a1 + 152

            // Опциональная структура sub_8DF30C
            bool hasSub8DF30C = false;
            Stream.WriteBoolean(hasSub8DF30C); // a1 + 168 (bool)
            if (hasSub8DF30C)
            {
                // TODO: Encode sub_8DF30C(a1 + 168)
            }

            Stream.WriteVInt(0); // a1 + 156
            Stream.WriteVInt(0); // a1 + 160
            Stream.WriteBoolean(false); // a1 + 164
            Stream.WriteBoolean(false); // a1 + 165
            
            Stream.WriteVInt(0); // a1 + 172
            Stream.WriteVInt(0); // a1 + 176
            Stream.WriteVInt(0); // a1 + 180
            Stream.WriteVInt(0); // a1 + 184
            Stream.WriteVInt(0); // a1 + 188
            Stream.WriteVInt(0); // a1 + 192
            Stream.WriteVInt(0); // a1 + 196
            Stream.WriteVInt(0); // a1 + 200

            Stream.WriteBoolean(false); // a1 + 209
            Stream.WriteBoolean(false); // a1 + 210
            Stream.WriteBoolean(false); // a1 + 211
            Stream.WriteBoolean(false); // a1 + 212
            Stream.WriteBoolean(false); // a1 + 213
            Stream.WriteBoolean(false); // a1 + 214
            
            // +268 — идентификатор испытания. Клиент в BattleEndPopup::getOutcomeCont
            // сверяет его именно с -1: только тогда берётся обычный контейнер результата
            // (result_container / result_container_ranked). Ноль трактуется как валидное
            // испытание, из-за чего экран показывался испытанием даже в выживании.
            Stream.WriteVInt(ChallengeId); // +268
            Stream.WriteBoolean(false); // +272

            // --- Массив 1: Игроки (a1 + 224) ---
            Stream.WriteVInt(Players.Count);
            foreach (BattlePlayer player in Players)
            {
                // 1. Флаги
                Stream.WriteBoolean(player.AccountId == OwnPlayer.AccountId); // *a1
                Stream.WriteBoolean(player.TeamIndex != OwnPlayer.TeamIndex); // a1[1]
                Stream.WriteBoolean(false); // a1[2]

                // 2. Вложенные массивы DataRef и VInt
                Stream.WriteByte(1); // Int8 count, PlayerEntry +20 (vtable 88 = writeInt8)
                ByteStreamHelper.WriteDataReference(Stream, player.CharacterId);
                
                Stream.WriteByte(1); // Int8 count, PlayerEntry +36
                ByteStreamHelper.WriteDataReference(Stream, player.SkinId);
                
                Stream.WriteByte(1); // Int8 count, PlayerEntry +52
                Stream.WriteVInt(player.Trophies);
                
                Stream.WriteByte(1); // Int8 count, PlayerEntry +84
                Stream.WriteVInt(player.HeroPowerLevel);
                
                // Третий список (PlayerEntry +56) — это ИЗМЕНЕНИЕ кубков: клиент в
                // BattleEndTeamMember печатает его как "+%d" при положительном значении
                // и "%d" при нуле/минусе. Раньше сюда уходил ноль, поэтому начисление
                // применялось к герою на сервере, но на экране результатов не появлялось.
                Stream.WriteByte(1); // Int8 count, PlayerEntry +68
                Stream.WriteVInt(player.AccountId == OwnPlayer.AccountId ? TrophiesReward : 0);

                // 3. Данные игрока
                Stream.WriteVInt(0); // PlayerEntry +88
                
                // PlayerEntry +96: опциональный LogicLong (Int32 high + Int32 low).
                // Флаг относится именно к нему, а не к дисплей-дате.
                // +96: номер аккаунта — по нему клиент добавляет в друзья и хвалит. Ботам нет.
                bool hasEntryLogicLong = player.AccountId > 0; // и ботам: у них свой номер и профиль (BotProfiles)
                Stream.WriteBoolean(hasEntryLogicLong);
                if (hasEntryLogicLong)
                {
                    Stream.WriteLong(player.AccountId);
                }

                // PlayerEntry +104: PlayerDisplayData пишется всегда, без флага.
                player.DisplayData.Encode(Stream);

                // PlayerEntry +112: LogicLong(VInt,VInt) + String(лимит 900000) + DataRef
                bool hasEntryOwnerInfo = false;
                Stream.WriteBoolean(hasEntryOwnerInfo);
                if (hasEntryOwnerInfo)
                {
                    // ByteStreamHelper.EncodeLogicLong(Stream, 0, 0);
                    // Stream.WriteString(string.Empty);
                    // ByteStreamHelper.WriteDataReference(Stream, 0);
                }

                Stream.WriteInt16((short)player.Kills); // PlayerEntry +136: убийства (stat_kills)
                Stream.WriteInt16((short)player.Deaths); // PlayerEntry +140: смерти (stat_deaths)
                Stream.WriteInt(player.Damage); // PlayerEntry +144: урон (stat_extra)
                Stream.WriteInt(player.Heals); // PlayerEntry +148: лечение (stat_extra, если больше урона)
                
                ByteStreamHelper.WriteDataReference(Stream, 0); // PlayerEntry +152
                ByteStreamHelper.WriteDataReference(Stream, 0); // PlayerEntry +160
                
                Stream.WriteVInt(0); // PlayerEntry +168
                Stream.WriteVInt(0); // PlayerEntry +172
            }

            // --- Массив 2: sub_879620 (a1 + 228) ---
            Stream.WriteVInt(0); 

            // --- Массив 3: DataReferences (a1 + 232) ---
            Stream.WriteVInt(0); 

            // --- Массив 4: sub_89B8B0 (a1 + 236) ---
            Stream.WriteVInt(0); 

            // Продолжение главной структуры
            ByteStreamHelper.WriteDataReference(Stream, 0); // a1 + 240
            
            bool hasSub877F90 = false;
            Stream.WriteBoolean(hasSub877F90); // a1 + 244 != 0
            if (hasSub877F90)
            {
                // TODO: Encode sub_877F90
            }

            bool hasLogicQuests = false;
            Stream.WriteBoolean(hasLogicQuests); // a1 + 252 != 0
            if (hasLogicQuests)
            {
                // TODO: LogicQuests.Encode()
            }

            Stream.WriteBoolean(false); // +344
            Stream.WriteBoolean(false); // +345
            Stream.WriteVInt(GameMode);  // +360 — игровой режим (isSurvival/isVictory читают его отсюда)
            Stream.WriteVInt(GameType);  // +364 — тип боя (getOutcomeCont)
            Stream.WriteVInt(0);         // +368

            bool hasSub8E85A4 = false; // Важно: этот флаг используется дважды!
            Stream.WriteBoolean(hasSub8E85A4); // a1 + 260 != 0
            if (hasSub8E85A4)
            {
                // TODO: Encode sub_8E85A4
            }

            Stream.WriteVInt(0); // a1 + 276

            bool hasChronosTextEntry = false;
            Stream.WriteBoolean(hasChronosTextEntry); // a1 + 280 != 0
            if (hasChronosTextEntry)
            {
                // TODO: ChronosTextEntry.Encode()
            }

            Stream.WriteVInt(0); // a1 + 204
            Stream.WriteBoolean(false); // a1 + 208
            Stream.WriteBoolean(false); // a1 + 284
            Stream.WriteBoolean(false); // a1 + 285

            // +328 KudosStatus: без него клиент не даёт хвалить (BattleEndPopup::onGivenKudos).
            bool hasSub8775E4 = Kudos != null;
            Stream.WriteBoolean(hasSub8775E4); // a1 + 248 != 0
            if (hasSub8775E4)
            {
                Kudos.Encode(Stream);
                // TODO: Encode sub_8775E4
            }

            Stream.WriteBoolean(false); // a1 + 286
            Stream.WriteVInt(0); // a1 + 288
            Stream.WriteVInt(0); // a1 + 292
            Stream.WriteVInt(0); // a1 + 296

            // --- Массив 5: (a1 + 308) ---
            Stream.WriteVInt(0); // Количество (a1 + 308)
            // Здесь был бы цикл с Stream.WriteVInt(...)

            Stream.WriteVInt(0); // a1 + 312
            Stream.WriteVInt(0); // a1 + 316

            bool isValueGreaterThanZero = false; // Флаг для (a1 + 320 >= 0)
            Stream.WriteBoolean(isValueGreaterThanZero);
            if (isValueGreaterThanZero)
            {
                Stream.WriteVInt(0); // a1 + 320
                Stream.WriteVInt(0); // a1 + 324
                Stream.WriteVInt(0); // a1 + 328
                Stream.WriteVInt(0); // a1 + 332
            }

            // Зависит от флага a1 + 260, который мы читали выше
            if (hasSub8E85A4)
            {
                Stream.WriteVInt(0); // a1 + 336
                int val340 = 0;
                Stream.WriteVInt(val340); // a1 + 340
                if (val340 >= 1)
                {
                    Stream.WriteVInt(0); // a1 + 344
                }
            }

            Stream.WriteBoolean(false); // +452
            Stream.WriteVInt(0); // +456
            Stream.WriteVInt(0); // +460
            Stream.WriteVInt(0); // +464
            // Блок события-рогалика (BattleEndPopup::setUpRogueliteCont).
            // Секция "roguelite_container" рисуется только когда +468 >= 1;
            // "+468 - +472" показывается как "+N" в epic_win_bonus.
            Stream.WriteVInt(RogueliteProgress); // +468
            Stream.WriteVInt(RogueliteBase);     // +472

            bool hasLogicRewards1 = false;
            Stream.WriteBoolean(hasLogicRewards1); // a1 + 372 != 0
            if (hasLogicRewards1)
            {
                // TODO: LogicRewards.Encode()
            }

            bool hasLogicRewards2 = false;
            Stream.WriteBoolean(hasLogicRewards2); // a1 + 376 != 0
            if (hasLogicRewards2)
            {
                // TODO: LogicRewards.Encode()
            }

            Stream.WriteVInt(0); // a1 + 380
            Stream.WriteBoolean(false); // a1 + 392
            Stream.WriteVInt(0); // a1 + 384
            ByteStreamHelper.WriteDataReference(Stream, 0); // a1 + 388

            bool hasSub8AC188 = false;
            Stream.WriteBoolean(hasSub8AC188); // a1 + 404 != 0
            if (hasSub8AC188)
            {
                // TODO: Encode sub_8AC188
            }

            Stream.WriteVInt(0); // +516
            // Номер этапа рогалика: подставляется вместо <LEVEL>
            // в строку TID_ROGUELITE_STAGE_COMPLETED.
            Stream.WriteVInt(RogueliteStage); // +520
        }

        public override int GetMessageType()
        {
            return 23456; 
        }

        public override int GetServiceNodeType()
        {
            return 27;
        }
    }
}
