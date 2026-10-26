// RogueliteEvent.cs
//
// 1:1 с LogicRogueliteEvent::decode + LogicRogueliteEventStage::decode (v64, arm64).
// Это ивентовая половина рогалика — то, что сидит в EventData на смещении +264
// (последний boolean в EventData::decode).
//
// Размеры сходятся с аллокациями в родителях:
//   EventData::decode           -> operator new(0x30) == LogicRogueliteEvent      (48 байт)
//   LogicRogueliteEvent::decode -> operator new(0x58) == LogicRogueliteEventStage (88 байт)
//   везде для наград            -> operator new(0x18) == LogicGemOffer            (24 байта)
//
// LogicGemOffer уже есть в проекте: Home/Offers/Offer.cs, тот же namespace.
// Его Encode пишет 5 полей (Type +0, Count +4, dataRef ItemDataId +8/+12, SkinDataId +16),
// что ровно совпадает с раскладкой 0x18 и дефолтом -1 в +0 из клиентской инициализации.

namespace HashCore.Laser.Logic.Home.Items
{
    using System;
    using System.Collections.Generic;
    using HashCore.Laser.Titan;

    // =====================================================================
    // LogicRogueliteEvent — 0x30 (48 байт)
    //
    //   +0   int
    //   +8   LogicArrayList<LogicRogueliteEventStage*> m_stages   (ptr +8,  cap +16, count +20)
    //   +24  int
    //   +32  LogicArrayList<LogicGemOffer*> m_extraRewardsForRankInBattle
    //                                                             (ptr +32, cap +40, count +44)
    // =====================================================================
    public class RogueliteEvent
    {
        public const int MAX_EXTRA_REWARDS = 16;   // внимание: 16, а не 32
        public const int MAX_STAGES        = 32;

        /// <summary>
        /// +32. Награда за место в бою. Экран забега берёт отсюда элемент по индексу
        /// m_stageRanks[CurrentStage - 1] и показывает его как «epic_win_bonus»,
        /// но только если тип награды 76. Пустой список = бонус не рисуется.
        /// </summary>
        public List<LogicGemOffer> ExtraRewardsForRankInBattle = new();  // +32  m_extraRewardsForRankInBattle

        /// <summary>
        /// +0 — идентификатор коллаба, а не служебное поле. Конструктор экрана кладёт
        /// его в себя и передаёт в RogueliteUtil::loadCollabScAssetByCollabID, а тот
        /// через getCollabDataById и getDownloadedScFilename подгружает .sc события
        /// в область ресурсов экрана. С нулём коллаб не находится, и экран остаётся
        /// на голой ui_roguelite.sc без контента события.
        /// </summary>
        public int CollabId;                                             // +0
        public List<RogueliteEventStage> Stages = new();                 // +8   m_stages
        public int Unk24;                                                // +24

        public void Encode(ByteStream w)
        {
            // 1  m_extraRewardsForRankInBattle: count, затем count × (bool + [LogicGemOffer])
            WriteOfferList(w, ExtraRewardsForRankInBattle, MAX_EXTRA_REWARDS);

            // 2  +0
            w.WriteVInt(CollabId);

            // 3  m_stages: count, затем count × (bool + [LogicRogueliteEventStage])
            if (Stages == null)
            {
                w.WriteVInt(-1);
            }
            else
            {
                int count = Stages.Count;
                if (count > MAX_STAGES) throw new ArgumentOutOfRangeException(nameof(Stages));
                w.WriteVInt(count);
                for (int i = 0; i < count; i++)
                    if (w.WriteBoolean(Stages[i] != null))
                        Stages[i].Encode(w);
            }

            // 4  +24
            w.WriteVInt(Unk24);
        }

        // Минимальный валидный блок: ивент есть, но пустой.
        public static void EncodeEmpty(ByteStream w)
        {
            w.WriteVInt(0);   // m_extraRewardsForRankInBattle
            w.WriteVInt(0);   // +0
            w.WriteVInt(0);   // m_stages
            w.WriteVInt(0);   // +24
        }

        // count, затем count × (bool + [LogicGemOffer]).
        // false даёт валидный null-элемент — клиент положит в список нулевой указатель.
        internal static void WriteOfferList(ByteStream w, List<LogicGemOffer> list, int max)
        {
            if (list == null)
            {
                w.WriteVInt(-1);   // отрицательный count — клиент пропустит массив целиком
                return;
            }

            int count = list.Count;
            if (count > max) throw new ArgumentOutOfRangeException(nameof(list));
            w.WriteVInt(count);
            for (int i = 0; i < count; i++)
                if (w.WriteBoolean(list[i] != null))
                    list[i].Encode(w);
        }

        // Сахар для сборки наград.
        public static LogicGemOffer Offer(ShopItem type, int count, int itemGlobalId = 0, int skinGlobalId = 0)
            => new LogicGemOffer(type, count, itemGlobalId, skinGlobalId);

    }

    // =====================================================================
    // LogicRogueliteEventStage — 0x58 (88 байт)
    //
    //   +0   Location global ID (VInt, not dataRef)
    //   +8   LogicArrayList<int> m_modifies                    (ptr +8,  cap +16, count +20)
    //   +24  dataRef
    //   +32  dataRef
    //   +40  dataRef
    //   +48  int
    //   +56  LogicArrayList<LogicGemOffer*> m_rewards          (ptr +56, cap +64, count +68)
    //   +72  LogicArrayList<LogicGemOffer*> m_fallbackRewards  (ptr +72, cap +80, count +84)
    //
    // Порядок на проводе НЕ равен раскладке: dataRef +32 читается первым,
    // а +40 и +24 — последними.
    // =====================================================================
    public class RogueliteEventStage
    {
        public const int MAX_MODIFIES         = 32;
        public const int MAX_REWARDS          = 32;
        public const int MAX_FALLBACK_REWARDS = 32;

        public int DataRef32;                              // +32  читается первым
        public int Unk48;                                  // +48
        public List<int> Modifies = new();                 // +8   m_modifies
        public List<LogicGemOffer> Rewards = new();        // +56  m_rewards
        public List<LogicGemOffer> FallbackRewards = new();// +72  m_fallbackRewards
        public int LocationGlobalId;                       // +0, findLocationData 0x1004fc900
        public int DataRef40;                              // +40
        public int DataRef24;                              // +24  читается последним

        public void Encode(ByteStream w)
        {
            ByteStreamHelper.WriteDataReference(w, DataRef32);        //  1  +32
            w.WriteVInt(Unk48);                                       //  2  +48

            // 3  m_modifies: count, затем count × vint
            if (Modifies == null)
            {
                w.WriteVInt(-1);
            }
            else
            {
                int count = Modifies.Count;
                if (count > MAX_MODIFIES) throw new ArgumentOutOfRangeException(nameof(Modifies));
                w.WriteVInt(count);
                for (int i = 0; i < count; i++) w.WriteVInt(Modifies[i]);
            }

            RogueliteEvent.WriteOfferList(w, Rewards, MAX_REWARDS);                   // 4  +56
            RogueliteEvent.WriteOfferList(w, FallbackRewards, MAX_FALLBACK_REWARDS);  // 5  +72

            w.WriteVInt(LocationGlobalId);                            //  6  +0, NOT a dataRef
            ByteStreamHelper.WriteDataReference(w, DataRef40);        //  7  +40
            ByteStreamHelper.WriteDataReference(w, DataRef24);        //  8  +24
        }

        public static void EncodeEmpty(ByteStream w)
        {
            w.WriteVInt(0);   // +32 dataRef = null
            w.WriteVInt(0);   // +48
            w.WriteVInt(0);   // m_modifies
            w.WriteVInt(0);   // m_rewards
            w.WriteVInt(0);   // m_fallbackRewards
            w.WriteVInt(0);   // +0
            w.WriteVInt(0);   // +40 dataRef = null
            w.WriteVInt(0);   // +24 dataRef = null
        }
    }
}

/*  ПОРЯДОК НА ПРОВОДЕ

    LogicRogueliteEvent (4 операции):
      1  vint count (<=16) + count × (bool + [GemOffer])   m_extraRewardsForRankInBattle
      2  vint                                              +0
      3  vint count (<=32) + count × (bool + [Stage])      m_stages
      4  vint                                              +24

    LogicRogueliteEventStage (8 операций):
      1  dataRef                                           +32
      2  vint                                              +48
      3  vint count (<=32) + count × vint                  m_modifies
      4  vint count (<=32) + count × (bool + [GemOffer])   m_rewards
      5  vint count (<=32) + count × (bool + [GemOffer])   m_fallbackRewards
      6  vint                                              +0
      7  dataRef                                           +40
      8  dataRef                                           +24

    LogicGemOffer (Home/Offers/Offer.cs), 0x18:
      vint Type (+0, дефолт -1) | vint Count (+4)
      | dataRef ItemDataId (+8, +12) | vint SkinDataId (+16)

    Лимиты массивов: count > лимита -> Debugger::fatal, count < 0 -> массив пропускается.
    Клампинг на 1024 применяется только к преаллокации (toSize), цикл чтения крутится
    на полный count — так что 1024 это не лимит, лимит именно 16 / 32.


    ПРИМЕР

    var ev = new RogueliteEvent { CollabId = 13, Unk24 = 0 };
    ev.ExtraRewardsForRankInBattle.Add(RogueliteEvent.Offer(ShopItem.Bling, 50));
    ev.Stages.Add(new RogueliteEventStage
    {
        Unk48   = 1,
        Modifies = { 0 },
        Rewards  = { RogueliteEvent.Offer(ShopItem.Coin, 100) },
    });

    // в EventData на месте +264:
    if (w.WriteBoolean(true)) ev.Encode(w);
*/
