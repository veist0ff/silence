using System.Collections.Generic;
using HashCore.Laser.Logic.Home.Items;
using Newtonsoft.Json;

/// <summary>
/// Зеркало QuestData::decode (0x1004c96bc). Порядок операций снят с клиента и
/// совпадает один в один: 31 чтение, из них три списка со счётчиком впереди.
/// Имена полей взяты из строк Debugger::fatal внутри самого декодера
/// (m_eventSlots, m_gmvs, m_pHeroes, m_rewards, m_fallbackRewards), смещения
/// подписаны для сверки. Размер объекта в клиенте 0xA8.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public class Quest
{
    /// <summary>Слоты событий, к которым привязан квест (+56). Пусто — квест общий.</summary>
    [JsonProperty] public List<int> EventSlots = new List<int>();

    /// <summary>Режимы, к которым привязан квест (+40). Пусто — квест без привязки.</summary>
    [JsonProperty] public List<int> GameModes = new List<int>();

    /// <summary>Бойцы, на которых засчитывается квест (+24), ссылками на таблицу 16.</summary>
    [JsonProperty] public List<int> Heroes = new List<int>();

    /// <summary>
    /// +156. Индекс цепочки хроноса. isChronosQuest это просто «+156 &gt;= 0», а
    /// isDailyQuest отдельно требует отрицательного значения. Ноль здесь означал,
    /// что любое задание считалось хроносным и попадало в «особые» — поэтому по
    /// умолчанию тут минус единица.
    /// </summary>
    [JsonProperty] public int ChronosIndex = -1;

    /// <summary>
    /// +132. Одно из двух полей, по которым isDailyQuest узнаёт ежедневное задание:
    /// нужно либо +132 больше нуля, либо +164 не меньше единицы.
    /// </summary>
    [JsonProperty] public int DailyMarker;

    /// <summary>+164, второй признак ежедневного задания.</summary>
    [JsonProperty] public int BatchMarker;

    /// <summary>+143. Единица уводит задание во вкладку трофейного мира.</summary>
    [JsonProperty] public bool TrophyWorldExtra;

    /// <summary>
    /// +144. Признак мега-квеста. QuestUtil::isActiveMegaQuest (0x10039ceb0) первым
    /// делом требует здесь единицу, а QuestsPopup::sortQuestsByType такие задания
    /// пропускает — у них своя витрина, поэтому во вкладках они и не показываются.
    /// </summary>
    [JsonProperty] public bool IsMegaQuest;

    /// <summary>
    /// +148. Номер цепочки мега-квестов, от 1 до 4. По нему QuestUtil::getMegaQuestChain
    /// собирает звенья, а getScFilenameForMegaQuestChainSlotImpl строит имя
    /// MegaQuestSlot%dSC для поиска файла в chronos_asset_ids.csv.
    /// </summary>
    [JsonProperty] public int MegaChain;

    /// <summary>
    /// +152. Номер звена в цепочке, считая с единицы. Первое звено активно сразу,
    /// каждое следующее открывается, когда завершено предыдущее.
    /// </summary>
    [JsonProperty] public int MegaIndex;

    /// <summary>
    /// Номер шаблона в пуле заданий сервера. На проводе его нет, он нужен только
    /// чтобы при входе досыпать ровно недостающие задания и не путать их местами.
    /// Минус один — задание не из пула (мега-квест или старое сохранение).
    /// </summary>
    [JsonProperty] public int PoolIndex = -1;

    [JsonProperty] public bool Checknuto;        // +141, отметка «просмотрено»
    [JsonProperty] public bool isUpdateQuest;    // +142
    [JsonProperty] public int QuestsProgress;    // +16, цель
    [JsonProperty] public int Missia;            // +8, тип задания
    [JsonProperty] public int Progress;          // +12, текущий прогресс
    [JsonProperty] public LogicGemOffer GemOffer;

    /// <summary>
    /// +88, m_rewards. Список, а не одна награда: счётчик впереди, дальше на каждую
    /// булев признак и сам оффер. Мега-квесты выдают сразу несколько предметов,
    /// поэтому одной GemOffer здесь мало.
    /// </summary>
    [JsonProperty] public List<LogicGemOffer> Rewards = new List<LogicGemOffer>();

    /// <summary>Совместимость со старыми сохранениями: один режим вместо списка.</summary>
    [JsonProperty] public int GameMode;

    /// <summary>Совместимость со старыми сохранениями: один боец вместо списка.</summary>
    [JsonProperty] public int BrawlerId;

    public void encode(ByteStream stream)
    {
        List<int> modes = GameModes != null && GameModes.Count > 0
            ? GameModes
            : (GameMode != 0 ? new List<int> { GameMode } : new List<int>());

        List<int> heroes = Heroes != null && Heroes.Count > 0
            ? Heroes
            : (BrawlerId != 0 ? new List<int> { BrawlerId } : new List<int>());

        //  1  +56  m_eventSlots
        stream.WriteVInt(EventSlots?.Count ?? 0);
        if (EventSlots != null) foreach (int slot in EventSlots) stream.WriteVInt(slot);

        stream.WriteBoolean(Checknuto);          //  2  +141

        //  3  +40  m_gmvs
        stream.WriteVInt(modes.Count);
        foreach (int mode in modes) stream.WriteVInt(mode);

        stream.WriteVInt(0);                     //  4  +76
        stream.WriteVInt(0);                     //  5  +4
        stream.WriteVInt(0);                     //  6  +72
        stream.WriteVInt(0);                     //  7  +128
        stream.WriteBoolean(false);              //  8  +140
        stream.WriteVInt(BatchMarker);           //  9  +164
        stream.WriteBoolean(isUpdateQuest);      // 10  +142
        stream.WriteVInt(MegaIndex);             // 11  +152  звено цепочки
        stream.WriteVInt(ChronosIndex);          // 12  +156  < 0 = не хронос

        // 13  +24  m_pHeroes, ссылками на таблицу персонажей
        stream.WriteVInt(heroes.Count);
        foreach (int hero in heroes) stream.WriteDataReference(16, hero);

        stream.WriteBoolean(IsMegaQuest);        // 14  +144  признак мега-квеста
        stream.WriteBoolean(false);              // 15  +145
        stream.WriteBoolean(false);              // 16  +81
        stream.WriteVInt(DailyMarker);           // 17  +132

        // 18  +88  m_rewards: счётчик, затем bool + LogicGemOffer на каждую
        List<LogicGemOffer> rewards = Rewards != null && Rewards.Count > 0
            ? Rewards
            : (GemOffer != null ? new List<LogicGemOffer> { GemOffer } : new List<LogicGemOffer>());

        stream.WriteVInt(rewards.Count);
        foreach (LogicGemOffer reward in rewards)
        {
            stream.WriteBoolean(true);
            reward.Encode(stream);
        }

        stream.WriteVInt(0);                     // 19  +104 m_fallbackRewards
        stream.WriteVInt(MegaChain);             // 20  +148  номер цепочки
        stream.WriteVInt(QuestsProgress);        // 21  +16  цель
        stream.WriteVInt(0);                     // 22  +0
        stream.WriteVInt(Missia);                // 23  +8   тип
        stream.WriteVInt(0);                     // 24  +136
        stream.WriteVInt(0);                     // 25  +20
        stream.WriteVInt(0);                     // 26  +160
        stream.WriteVInt(0);                     // 27  +124
        stream.WriteVInt(0);                     // 28  +120
        stream.WriteVInt(Progress);              // 29  +12  прогресс
        stream.WriteBoolean(false);              // 30  +80
        stream.WriteBoolean(TrophyWorldExtra);    // 31  +143
    }
}
