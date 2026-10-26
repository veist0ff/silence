// PlayerRogueliteEventData.cs
//
// 1:1 с LogicPlayerRogueliteEventData::decode из клиента v64 (arm64).
// Это ИГРОКОВАЯ половина рогалика (лежит в OwnHomeData/ClientHome),
// а не LogicRogueliteEvent — тот сидит в EventData +264 и описывает сам ивент.
//
// Слоты vtable в этом дампе даны сырыми смещениями:
//   *(_QWORD *)a2 + 224  -> 0xE0 -> слот 28 -> readBoolean
//   *(_QWORD *)a2 + 272  -> 0x110 -> слот 34 -> readVInt
// Совпадает с тем, что мы вывели по EventData::decode.

namespace HashCore.Laser.Logic.Home.Items
{
    using System.Collections.Generic;
    using HashCore.Laser.Titan;

    public class PlayerRogueliteEventData
    {
        // Клиент падает (Debugger::fatal) на count > 32 для всех трёх массивов.
        public const int MAX_ARRAY_ITEMS = 32;

        // Names confirmed by LogicPlayerRogueliteEventData::toString, 0x1004fbb54.
        public bool CanClaimReward;                       // +64
        public int EventSlotCode;                         // +68
        public int CurrentEventId;                        // +0
        public int SelectedHeroGlobalId;                  // +56, dataRef
        public int EventEndTime;                          // +4
        public int ClaimedTotalCollabEventCurrency;        // +48
        public int CurrentStage;                          // +8

        // --- массивы (имена достоверные, из строк Debugger::fatal) ---
        public List<int> BattleDurations = new();        // +72  m_battleDurations
        public List<int> EquippedCardGlobalIds = new();  // +16  m_equippedCardGlobalIds
        public List<int> StageRanks = new();             // +32  m_stageRanks

        // ---------------------------------------------------------------
        // ENCODE — порядок строго как в ::decode
        // ---------------------------------------------------------------
        public void Encode(ByteStream w)
        {
            w.WriteBoolean(CanClaimReward);                       //  1  +64
            w.WriteVInt(EventSlotCode);                           //  2  +68
            WriteIntArray(w, BattleDurations);                    //  3  +72  m_battleDurations

            w.WriteVInt(CurrentEventId);                          //  4  +0
            ByteStreamHelper.WriteDataReference(w, SelectedHeroGlobalId); //  5  +56
            w.WriteVInt(EventEndTime);                            //  6  +4
            WriteIntArray(w, EquippedCardGlobalIds);              //  7  +16  m_equippedCardGlobalIds

            w.WriteVInt(ClaimedTotalCollabEventCurrency);          //  8  +48
            w.WriteVInt(CurrentStage);                           //  9  +8
            WriteIntArray(w, StageRanks);                         // 10  +32  m_stageRanks
        }

        // Пустой блок — если ивент есть, но у игрока ещё нет забега.
        public static void EncodeEmpty(ByteStream w)
        {
            w.WriteBoolean(false);   // +64
            w.WriteVInt(0);          // +68
            w.WriteVInt(0);          // m_battleDurations: 0 элементов
            w.WriteVInt(0);          // +0
            w.WriteVInt(0);          // +56 dataRef = null
            w.WriteVInt(0);          // +4
            w.WriteVInt(0);          // m_equippedCardGlobalIds
            w.WriteVInt(0);          // +48
            w.WriteVInt(0);          // +8
            w.WriteVInt(0);          // m_stageRanks
        }

        // ---------------------------------------------------------------
        // DECODE — зеркало клиентского, для round-trip проверки
        // ---------------------------------------------------------------
        public void Decode(ByteStream r)
        {
            CanClaimReward = r.ReadBoolean();                     //  1  +64
            EventSlotCode = r.ReadVInt();                         //  2  +68
            BattleDurations = ReadIntArray(r);                    //  3  +72

            CurrentEventId = r.ReadVInt();                        //  4  +0
            SelectedHeroGlobalId = ByteStreamHelper.ReadDataReference(r); //  5  +56
            EventEndTime = r.ReadVInt();                          //  6  +4
            EquippedCardGlobalIds = ReadIntArray(r);              //  7  +16

            ClaimedTotalCollabEventCurrency = r.ReadVInt();        //  8  +48
            CurrentStage = r.ReadVInt();                          //  9  +8
            StageRanks = ReadIntArray(r);                         // 10  +32
        }

        // ---------------------------------------------------------------
        private static void WriteIntArray(ByteStream w, List<int> list)
        {
            if (list == null)
            {
                w.WriteVInt(-1);   // отрицательный count = массива нет вовсе, клиент его пропустит
                return;
            }

            int count = list.Count;
            if (count > MAX_ARRAY_ITEMS) throw new System.ArgumentOutOfRangeException(nameof(list));

            w.WriteVInt(count);
            for (int i = 0; i < count; i++) w.WriteVInt(list[i]);
        }

        private static List<int> ReadIntArray(ByteStream r)
        {
            int count = r.ReadVInt();
            if (count > MAX_ARRAY_ITEMS) throw new System.IO.InvalidDataException("Roguelite array limit is 32.");
            var list = new List<int>();
            if (count < 0) return list;                            // клиент здесь делает goto/return
            for (int i = 0; i < count; i++) list.Add(r.ReadVInt());
            return list;
        }
    }
}

/*  Раскладка объекта в клиенте (LogicArrayList = 16 байт: ptr +0, capacity +8, count +12)

    +0   int
    +4   int
    +8   int
    +16  LogicArrayList<int>  m_equippedCardGlobalIds   (ptr +16, cap +24, count +28)
    +32  LogicArrayList<int>  m_stageRanks              (ptr +32, cap +40, count +44)
    +48  int
    +56  dataRef (globalId)
    +64  bool
    +68  int
    +72  LogicArrayList<int>  m_battleDurations         (ptr +72, cap +80, count +84)
    ----
    размер ~0x58 (88)

    Порядок на проводе НЕ совпадает с раскладкой — читать только по потоку.
*/
