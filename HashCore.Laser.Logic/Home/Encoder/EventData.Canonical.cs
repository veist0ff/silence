// EventData.Canonical.cs
//
// 1:1 с EventData::decode из клиента v64 (arm64).
// Порядок вызовов здесь = порядок на проводе. В комментариях — смещение поля
// в объекте клиента, оно НЕ совпадает с порядком чтения (компилятор разложил поля иначе).
//
// Слоты vtable ByteStream в этом клиенте (проверено по декомпиляции):
//   24 -> readString(maxLen)   28 -> readBoolean   34 -> readVInt
// (в tutor64.md было 22 / 26 / 31 — таблица снята с v36, для v64 устарела)

namespace HashCore.Laser.Logic.Home.Items
{
    using System;
    using System.Collections.Generic;

    public partial class EventData
    {
        // +264 LogicRogueliteEvent. null => на провод уходит просто false.
        public RogueliteEvent Roguelite;

        // Значения ниже перенесены один в один из старого Encode() — они подобраны
        // под клиент, и обнулять их нельзя (иначе ивент едет пустым).
        // EncodeCanonical чинит ТОЛЬКО каркас, значения оставляет как были.
        public int Unk8  = 10;                                   // +8   старое WriteVInt(10)
        public int Unk40 = 2;                                    // +40  старое WriteVInt(2)
        public List<int> DataRefs184 = new() { 15_000_007 };     // +184 старое WriteDataReference(15, 7)
        public List<int> DataRefs192 = new() { 16_000_001 };     // +192 старое WriteDataReference(16, 1)

        // ---------------------------------------------------------------
        // ENCODE — ровно 40 операций, столько же ждёт EventData::decode
        // ---------------------------------------------------------------
        public const int RANKED_SLOT = 43;
        public const int RANKED_EVENT_TYPE = 14;
        /// <summary>Ранговый матч — слот 14 (тип события 14), данные сезона — слот 43.</summary>
        public bool IsRanked => Slot == RANKED_SLOT || Slot == RANKED_EVENT_TYPE;
        public static int[] RankedSeasonBrawlers = { 77, 71, 70 };

        /// <summary>
        /// LogicRankedSeason::decode (0x1004fa25c): номер сезона, строка, три числа,
        /// награды (LogicRewardConfig), две таблицы наград за ранги, список чисел,
        /// ссылка на данные, ещё список чисел и число. Пока сезон без наград.
        /// </summary>
        private void EncodeRankedSeason(ByteStream w)
        {
            w.WriteVInt(1);                                               // +8   номер сезона
            w.WriteString(null);                                          // +16
            w.WriteVInt(0);                                               // +24
            w.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);      // +28
            w.WriteVInt(0);                                               // +32
            w.WriteVInt(0);                                               // +40  награды
            w.WriteVInt(0);                                               // +56  награды за ранг
            w.WriteVInt(0);                                               // +72  награды за ранг
            // +88 — сезонные бойцы (номера строк characters.csv), в эталоне их три: 77, 71, 70.
            w.WriteVInt(RankedSeasonBrawlers.Length);
            foreach (int b in RankedSeasonBrawlers) w.WriteVInt(b);
            // +104 — локация, по ней слот показывает режим и карту (в эталоне 15/143).
            ByteStreamHelper.WriteDataReference(w, Location);
            w.WriteVInt(0);                                               // +112 int[]
            w.WriteVInt(0);                                               // +128
        }

        public void EncodeCanonical(ByteStream w)
        {
            //  #   поле клиента
            w.WriteVInt(Slot);                                            //  1  +0    slot
            // +4 — тип события: EventData::isRankedEvent проверяет здесь 14. Ранговый — слот 43.
            w.WriteVInt(Slot == RANKED_SLOT ? RANKED_EVENT_TYPE : Slot);  //  2  +4    eventType
            w.WriteVInt(Unk8);                                            //  3  +8
            w.WriteVInt(0);                                               //  4  +12   startTimeSeconds
            w.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);      //  5  +16   endTimeSeconds
            w.WriteVInt(0);                                               //  6  +20

            if (Slot == 12 || Slot == 13)
                ByteStreamHelper.WriteDataReference(w, null);             //  7  +24   LocationData
            else
                ByteStreamHelper.WriteDataReference(w, Location);

            ByteStreamHelper.WriteDataReference(w, null);                 //  8  +32   dataRef (GameModeVariation)

            w.WriteVInt(Unk40);                                           //  9  +40
            w.WriteString(null);                                          // 10  +48   readString(900000)
            w.WriteVInt(0);                                               // 11  +56
            bool challenge = Challenge != null;
            bool challengeHead = challenge && Slot == Challenges.ChallengeDefinition.FIRST_SLOT;
            w.WriteVInt(challengeHead ? ChallengeLivesUsed : 0);          // 12  +60   испытание: потрачено жизней
            w.WriteVInt(challenge ? ChallengeStageWins : 0);              // 13  +64   испытание: побед на этапе

            w.WriteVInt(0);                                               // 14  +72   int[] — count, дальше count × vint

            w.WriteVInt(challengeHead ? ChallengeTotalWins : 0);          // 15  +88   испытание: всего побед
            w.WriteVInt(challenge ? Challenge.ChallengeId : 0);           // 16  +100  испытание: ChallengeId
            // +104 у испытания — бесконечные жизни (hasRemainingLives), а не бонус.
            w.WriteBoolean(challenge ? Challenge.InfiniteLives : IsBonusCollected); // 17  +104

            w.WriteBoolean(false);                                        // 18  +144  BattlePlayerMap (readBattlePlayerMap)
            w.WriteVInt(challengeHead ? ChallengeLivesPurchased : 0);     // 19  +68   испытание: докупок жизней
            if (w.WriteBoolean(IsRanked)) EncodeRankedSeason(w);          // 20  +152  LogicRankedSeason
            w.WriteVInt(0);                                               // 21  +92
            w.WriteVInt(0);                                               // 22  +96

            w.WriteBoolean(false);                                        // 23  +112  ChronosTextEntry
            w.WriteBoolean(false);                                        // 24  +120  ChronosTextEntry
            w.WriteBoolean(false);                                        // 25  +128  ChronosTextEntry
            // +160 — награда за прохождение испытания (getCompletionRewardFromEvent).
            LogicGemOffer completion = challenge ? Challenge.CompletionReward : null;
            if (w.WriteBoolean(completion != null)) completion!.Encode(w); // 26  +160  LogicGemOffer

            // +80 — цены докупки жизней (getPriceForChallengeLives), только у слота 20.
            if (challengeHead)
            {
                w.WriteVInt(Challenge.LivesPrices.Count);                 // 27  +80   int[]
                foreach (int price in Challenge.LivesPrices) w.WriteVInt(price);
            }
            else
            {
                w.WriteVInt(-1);                                          // 27  +80   int[] (-1 = списка нет)
            }

            if (w.WriteBoolean(ChronosFileEntry != null)) ChronosFileEntry!.Encode(w); // 28 +136

            w.WriteBoolean(false);                                        // 29  +168  LogicClubTrophyConfig
            // ВНИМАНИЕ: если здесь true, то ПОСЛЕ ::decode идёт ещё один vint в +176 —
            // он внутри if, а не снаружи:
            //     w.WriteBoolean(true); WriteClubTrophyConfig(w); w.WriteVInt(0 /* +176 */);

            WriteDataRefList(w, DataRefs184);                             // 30  +184  dataRef[] — count, дальше count × dataRef
            WriteDataRefList(w, DataRefs192);                             // 31  +192  dataRef[]
            w.WriteVInt(0);                                               // 32  +200  int[]
            w.WriteVInt(0);                                               // 33  +208

            w.WriteBoolean(false);                                        // 34  +216  LogicAlliancePiggyBankEvent
            w.WriteBoolean(false);                                        // 35  +224  LogicCollabEvent
            // +232 LogicSpecialEvent. Пишется только для слотов боссов: именно это поле
            // проверяет LogicGameModeUtil::isBossRushEvent, и раньше здесь всегда стоял
            // false, поэтому слоты 45..52 не могли появиться в принципе.
            if (w.WriteBoolean(BossCharacterId != 0)) WriteSpecialEvent(w); // 36  +232  LogicSpecialEvent
            w.WriteBoolean(false);                                        // 37  +240  LogicContestEvent
            if (w.WriteBoolean(BrawlerEarlyAccess != null)) BrawlerEarlyAccess!.Encode(w); // 38 +248
            if (w.WriteBoolean(SpecialRush != null)) SpecialRush!.Encode(w); // 39 +256 LogicSpecialRushEvent

            if (w.WriteBoolean(Roguelite != null)) Roguelite.Encode(w);   // 40  +264  LogicRogueliteEvent
        }

        // ---------------------------------------------------------------
        // DECODE — зеркало клиентского. Нужен для round-trip теста:
        // EncodeCanonical -> DecodeCanonical -> сверить, что стрим вычитан ровно до конца.
        // ---------------------------------------------------------------
        public void DecodeCanonical(ByteStream r)
        {
            Slot = r.ReadVInt();                                          //  1  +0
            r.ReadVInt();                                                 //  2  +4
            r.ReadVInt();                                                 //  3  +8
            r.ReadVInt();                                                 //  4  +12
            int endSeconds = r.ReadVInt();                                //  5  +16
            EndTime = DateTime.Now.AddSeconds(endSeconds);
            r.ReadVInt();                                                 //  6  +20

            LocationId = ByteStreamHelper.ReadDataReference(r);           //  7  +24
            ByteStreamHelper.ReadDataReference(r);                        //  8  +32

            r.ReadVInt();                                                 //  9  +40
            r.ReadString(900000);                                         // 10  +48
            r.ReadVInt();                                                 // 11  +56
            r.ReadVInt();                                                 // 12  +60
            r.ReadVInt();                                                 // 13  +64

            for (int n = r.ReadVInt(); n > 0; n--) r.ReadVInt();          // 14  +72

            r.ReadVInt();                                                 // 15  +88
            r.ReadVInt();                                                 // 16  +100
            IsBonusCollected = r.ReadBoolean();                           // 17  +104

            if (r.ReadBoolean()) ReadBattlePlayerMap(r);                  // 18  +144
            r.ReadVInt();                                                 // 19  +68
            if (r.ReadBoolean()) ReadRankedSeason(r);                     // 20  +152
            r.ReadVInt();                                                 // 21  +92
            r.ReadVInt();                                                 // 22  +96

            if (r.ReadBoolean()) ReadChronosTextEntry(r);                 // 23  +112
            if (r.ReadBoolean()) ReadChronosTextEntry(r);                 // 24  +120
            if (r.ReadBoolean()) ReadChronosTextEntry(r);                 // 25  +128
            if (r.ReadBoolean()) ReadGemOffer(r);                         // 26  +160

            int extra = r.ReadVInt();                                     // 27  +80
            if (extra >= 0) for (int i = 0; i < extra; i++) r.ReadVInt();

            if (r.ReadBoolean())                                          // 28  +136
            {
                r.ReadStringReference(1024);
                r.ReadStringReference(1024);
            }

            if (r.ReadBoolean())                                          // 29  +168
            {
                ReadClubTrophyConfig(r);
                r.ReadVInt();                                             //     +176 — ВНУТРИ if
            }

            int n30 = r.ReadVInt();                                       // 30  +184
            if (n30 >= 0) for (int i = 0; i < n30; i++) ByteStreamHelper.ReadDataReference(r);

            int n31 = r.ReadVInt();                                       // 31  +192
            if (n31 >= 0) for (int i = 0; i < n31; i++) ByteStreamHelper.ReadDataReference(r);

            int n32 = r.ReadVInt();                                       // 32  +200
            if (n32 >= 0) for (int i = 0; i < n32; i++) r.ReadVInt();

            r.ReadVInt();                                                 // 33  +208

            if (r.ReadBoolean()) ReadPiggyBankEvent(r);                   // 34  +216
            if (r.ReadBoolean()) ReadCollabEvent(r);                      // 35  +224
            if (r.ReadBoolean()) ReadSpecialEvent(r);                     // 36  +232
            if (r.ReadBoolean()) ReadContestEvent(r);                     // 37  +240
            if (r.ReadBoolean()) ReadBrawlerEarlyAccessEvent(r);           // 38 +248
            if (r.ReadBoolean()) ReadSpecialRushEvent(r);                 // 39  +256
            if (r.ReadBoolean()) ReadRogueliteEvent(r);                   // 40  +264
        }

        private static void WriteDataRefList(ByteStream w, List<int> list)
        {
            if (list == null) { w.WriteVInt(0); return; }
            w.WriteVInt(list.Count);
            foreach (int globalId in list) ByteStreamHelper.WriteDataReference(w, globalId);
        }

        // Заглушки — заполнять по мере реверса соответствующих ::decode.
        private static void ReadBattlePlayerMap(ByteStream r) => throw new NotImplementedException();
        /// <summary>LogicRankedSeason::decode — зеркало EncodeRankedSeason (списки наград пока пустые).</summary>
        private static void ReadRankedSeason(ByteStream r)
        {
            r.ReadVInt(); r.ReadStringReference(900000); r.ReadVInt(); r.ReadVInt(); r.ReadVInt();
            if (r.ReadVInt() > 0 || r.ReadVInt() > 0 || r.ReadVInt() > 0)
                throw new NotImplementedException("награды рангового сезона");
            ReadIntList(r);
            ByteStreamHelper.ReadDataReference(r);
            ReadIntList(r);
            r.ReadVInt();
        }
        /// <summary>ChronosTextEntry::decode (0x1004d27fc): строка, затем VInt.</summary>
        private static void ReadChronosTextEntry(ByteStream r)
        {
            r.ReadStringReference(900000);
            r.ReadVInt();
        }
        /// <summary>LogicGemOffer::decode (0x1004f2ef0): VInt, VInt, dataRef, VInt.</summary>
        private static void ReadGemOffer(ByteStream r)
        {
            r.ReadVInt();
            r.ReadVInt();
            ByteStreamHelper.ReadDataReference(r);
            r.ReadVInt();
        }

        /// <summary>Список наград: счётчик, затем на каждую bool и, если true, оффер.</summary>
        private static void ReadGemOfferList(ByteStream r)
        {
            int count = r.ReadVInt();
            for (int i = 0; i < count; i++)
                if (r.ReadBoolean()) ReadGemOffer(r);
        }

        private static void ReadIntList(ByteStream r)
        {
            int count = r.ReadVInt();
            for (int i = 0; i < count; i++) r.ReadVInt();
        }
        private static void ReadClubTrophyConfig(ByteStream r) => throw new NotImplementedException();
        private static void ReadPiggyBankEvent(ByteStream r) => throw new NotImplementedException();
        /// <summary>
        /// LogicCollabEvent::decode (0x1003b8e98). Два булевых поля, затем семнадцать
        /// VInt: одиннадцать счётчиков списков и шесть скаляров. Имена списков взяты
        /// из строк Debugger::fatal внутри самого декодера.
        /// </summary>
        private static void ReadCollabEvent(ByteStream r)
        {
            r.ReadBoolean();                            //  1  +32
            if (r.ReadBoolean()) ReadGemOffer(r);       //  2  +144

            ReadIntList(r);                             //  3  treatUpgradePeriodRemainingSeconds
            ReadRewardMilestoneList(r);                 //  4  scoreRewardMilestones
            r.ReadVInt();                               //  5  +96
            ReadGemOfferList(r);                        //  6  treatUpgradeBoxRewards
            r.ReadVInt();                               //  7  +136
            r.ReadVInt();                               //  8  +56
            ReadIntList(r);                             //  9  treatUpgradeCosts
            ReadIntList(r);                             // 10  winProgressRewards
            ReadGemOfferList(r);                        // 11  scoreRewardsRepeatingScoreRewards
            r.ReadVInt();                               // 12  +232
            ReadIntList(r);                             // 13  treatUpgradePeriodStartTimes

            int tokens = r.ReadVInt();                  // 14  tokenRewardMilestones
            for (int i = 0; i < tokens; i++)
                if (r.ReadBoolean()) { r.ReadVInt(); r.ReadVInt(); }

            int wins = r.ReadVInt();                    // 15  winMilestoneRewards
            for (int i = 0; i < wins; i++)
                if (r.ReadBoolean()) { if (r.ReadBoolean()) ReadGemOffer(r); r.ReadVInt(); }

            ReadIntList(r);                             // 16  treatUpgradePeriodEndTimes
            ReadRewardMilestoneList(r);                 // 17  clubRewardMilestones
            r.ReadVInt();                               // 18  +8  id коллаба
            r.ReadVInt();                               // 19  +12
        }

        /// <summary>LogicCollabEventRewardMilestone::decode: список наград, затем VInt.</summary>
        private static void ReadRewardMilestoneList(ByteStream r)
        {
            int count = r.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                if (!r.ReadBoolean()) continue;
                ReadGemOfferList(r);
                r.ReadVInt();
            }
        }
        /// <summary>
        /// LogicSpecialEvent::decode (0x1004ff9ac): одиннадцать операций, три списка
        /// наград и три ссылки. Вторая ссылка (+72) — боец для рейда боссов.
        /// </summary>
        private static void ReadSpecialEvent(ByteStream r)
        {
            ReadGemOfferList(r);                        //  1  bossFightLevelRewards
            ByteStreamHelper.ReadDataReference(r);      //  2  +72
            ReadGemOfferList(r);                        //  3  bonusRewardWithFallbacks
            r.ReadVInt();                               //  4  +4
            ByteStreamHelper.ReadDataReference(r);      //  5  +88
            ByteStreamHelper.ReadDataReference(r);      //  6  +80
            r.ReadVInt();                               //  7  +32
            r.ReadBoolean();                            //  8  +96
            ReadGemOfferList(r);                        //  9  bossFightLevelFallBackRewards
            r.ReadVInt();                               // 10  +8
            r.ReadVInt();                               // 11  +0
        }
        private static void ReadContestEvent(ByteStream r) => throw new NotImplementedException();
        private static void ReadBrawlerEarlyAccessEvent(ByteStream r)
        {
            for (int i = 0; i < 5; i++) r.ReadVInt();
            if (r.ReadBoolean())
            {
                int n = r.ReadVInt();
                if (n > 32) throw new System.IO.InvalidDataException("EarlyAccess reward limit is 32.");
                for (int i = 0; i < n; i++)
                {
                    if (!r.ReadBoolean()) continue;
                    r.ReadVInt();
                    r.ReadVInt();
                    ByteStreamHelper.ReadDataReference(r);
                    r.ReadVInt();
                }
            }
            ByteStreamHelper.ReadDataReference(r);
            for (int i = 0; i < 5; i++) r.ReadVInt();
            ByteStreamHelper.ReadDataReference(r);
            if (r.ReadBoolean()) { r.ReadString(900000); r.ReadVInt(); }
        }
        private static void ReadSpecialRushEvent(ByteStream r)
        {
            for (int array = 0; array < 2; array++)
            {
                int n = r.ReadVInt();
                if (n > 32) throw new System.IO.InvalidDataException("SpecialRush slot array limit is 32.");
                for (int i = 0; i < n; i++) r.ReadVInt();
            }
            int overrides = r.ReadVInt();
            if (overrides > 32) throw new System.IO.InvalidDataException("SpecialRush override array limit is 32.");
            for (int i = 0; i < overrides; i++)
            {
                if (!r.ReadBoolean()) continue;
                int n = r.ReadVInt();
                for (int j = 0; j < n; j++)
                {
                    if (!r.ReadBoolean()) continue;
                    r.ReadVInt();
                    r.ReadVInt();
                    ByteStreamHelper.ReadDataReference(r);
                    r.ReadVInt();
                }
            }
            r.ReadVInt(); // +0 follows all override lists, including absent ones.
            int rewards = r.ReadVInt();
            if (rewards > 32) throw new System.IO.InvalidDataException("SpecialRush reward array limit is 32.");
            for (int i = 0; i < rewards; i++)
            {
                if (!r.ReadBoolean()) continue;
                r.ReadVInt();
                r.ReadVInt();
                ByteStreamHelper.ReadDataReference(r);
                r.ReadVInt();
            }
        }
        /// <summary>
        /// Зеркало RogueliteEvent.Encode. Порядок снят с LogicRogueliteEvent::decode
        /// (0x1004fc4e4) и LogicRogueliteEventStage::decode (0x1004fcad0), результат
        /// кладётся в Roguelite, чтобы round-trip показывал реальное число этапов.
        /// </summary>
        private void ReadRogueliteEvent(ByteStream r)
        {
            RogueliteEvent ev = new RogueliteEvent();

            ReadOfferList(r, RogueliteEvent.MAX_EXTRA_REWARDS, ev.ExtraRewardsForRankInBattle);
            ev.CollabId = r.ReadVInt();

            int stages = r.ReadVInt();
            if (stages > RogueliteEvent.MAX_STAGES)
                throw new System.IO.InvalidDataException("Roguelite stage limit is 32.");

            for (int i = 0; i < stages; i++)
            {
                if (!r.ReadBoolean()) { ev.Stages.Add(null); continue; }

                RogueliteEventStage stage = new RogueliteEventStage();
                stage.DataRef32 = ByteStreamHelper.ReadDataReference(r);
                stage.Unk48 = r.ReadVInt();

                int modifies = r.ReadVInt();
                if (modifies > RogueliteEventStage.MAX_MODIFIES)
                    throw new System.IO.InvalidDataException("Roguelite modifies limit is 32.");
                for (int m = 0; m < modifies; m++) stage.Modifies.Add(r.ReadVInt());

                ReadOfferList(r, RogueliteEventStage.MAX_REWARDS, stage.Rewards);
                ReadOfferList(r, RogueliteEventStage.MAX_FALLBACK_REWARDS, stage.FallbackRewards);

                stage.LocationGlobalId = r.ReadVInt();
                stage.DataRef40 = ByteStreamHelper.ReadDataReference(r);
                stage.DataRef24 = ByteStreamHelper.ReadDataReference(r);

                ev.Stages.Add(stage);
            }

            ev.Unk24 = r.ReadVInt();
            Roguelite = ev;
        }

        private static void ReadOfferList(ByteStream r, int max, List<LogicGemOffer> into)
        {
            int count = r.ReadVInt();
            if (count > max) throw new System.IO.InvalidDataException("Roguelite offer array limit exceeded.");
            for (int i = 0; i < count; i++)
            {
                if (!r.ReadBoolean()) { into?.Add(null); continue; }

                LogicGemOffer offer = new LogicGemOffer
                {
                    Type = (ShopItem)r.ReadVInt(),
                    Count = r.ReadVInt()
                };
                offer.ItemDataId = ByteStreamHelper.ReadDataReference(r);
                offer.SkinDataId = r.ReadVInt();
                into?.Add(offer);
            }
        }
    }
}
