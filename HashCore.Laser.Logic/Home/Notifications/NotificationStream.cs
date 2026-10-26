namespace HashCore.Laser.Logic.Notification
{
    using HashCore.Laser.Logic.Home.Items;

    /// <summary>
    /// Вложенные структуры, из которых клиент собирает тела писем. Каждая —
    /// зеркало своего decode в клиенте.
    /// </summary>
    public static class NotificationStream
    {
        /// <summary>
        /// LogicRewards::decode (0x1004c76a0): VInt счётчик, затем на каждую
        /// награду булево «есть» и, если есть, LogicGemOffer. Так же клиент
        /// читает и безымянные списки наград внутри писем 26, 34 и 36.
        /// </summary>
        public static void WriteRewards(ByteStream stream, IList<LogicGemOffer> rewards)
        {
            int count = rewards?.Count ?? 0;
            stream.WriteVInt(count);
            for (int i = 0; i < count; i++)
            {
                LogicGemOffer offer = rewards[i];
                if (stream.WriteBoolean(offer != null)) offer.Encode(stream);
            }
        }

        /// <summary>Булево и, если true, LogicRewards.</summary>
        public static void WriteOptionalRewards(ByteStream stream, IList<LogicGemOffer> rewards)
        {
            if (stream.WriteBoolean(rewards != null)) WriteRewards(stream, rewards);
        }

        /// <summary>
        /// Список предложений без флагов: счётчик и сами LogicGemOffer —
        /// так клиент строит их конструктором LogicGemOffer(ByteStream).
        /// </summary>
        public static void WriteOfferList(ByteStream stream, IList<LogicGemOffer> offers)
        {
            int count = offers?.Count ?? 0;
            stream.WriteVInt(count);
            for (int i = 0; i < count; i++)
                (offers[i] ?? new LogicGemOffer()).Encode(stream);
        }

        public static void WriteOptionalOffer(ByteStream stream, LogicGemOffer offer)
        {
            if (stream.WriteBoolean(offer != null)) offer.Encode(stream);
        }

        /// <summary>ByteStreamHelper::decodeLogicLong — два VInt, старшая и младшая части.</summary>
        public static void WriteLogicLong(ByteStream stream, long value)
        {
            stream.WriteVInt((int)(value >> 32));
            stream.WriteVInt((int)value);
        }

        /// <summary>ByteStreamHelper::readLogicLong — булево и, если есть, два VInt.</summary>
        public static void WriteOptionalLogicLong(ByteStream stream, long? value)
        {
            if (stream.WriteBoolean(value.HasValue)) WriteLogicLong(stream, value.Value);
        }

        public static void WriteDataReferenceList(ByteStream stream, IList<int> globalIds)
        {
            int count = globalIds?.Count ?? 0;
            stream.WriteVInt(count);
            for (int i = 0; i < count; i++)
                ByteStreamHelper.WriteDataReference(stream, globalIds[i]);
        }
    }

    /// <summary>
    /// ChronosTextEntry::decode: строка-ссылка, затем VInt. В промо-окне так
    /// приходят заголовок, текст и надпись на кнопке.
    /// </summary>
    public class ChronosTextEntry
    {
        public string Text;
        public int Id = -1;

        public void Encode(ByteStream stream)
        {
            stream.WriteStringReference(Text ?? string.Empty);
            stream.WriteVInt(Id);
        }
    }

    /// <summary>
    /// PromoFileEntry::decode: две строки-ссылки. Первая ложится в +16 (хеш
    /// файла), вторая в +0 (путь).
    /// </summary>
    public class PromoFileEntry
    {
        public string Sha;
        public string Path;

        public void Encode(ByteStream stream)
        {
            stream.WriteStringReference(Sha ?? string.Empty);
            stream.WriteStringReference(Path ?? string.Empty);
        }
    }

    /// <summary>TimedIntValueEntry(ByteStream): четыре VInt. Четвёртый — сумма, которую
    /// клиент складывает в блинги при открытии письма 79.</summary>
    public class TimedIntValueEntry
    {
        public int Value0, Value4, Value8, Amount;

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Value0);
            stream.WriteVInt(Value4);
            stream.WriteVInt(Value8);
            stream.WriteVInt(Amount);
        }
    }

    /// <summary>TrophySeasonHeroEntry::decode: шесть VInt в порядке +16, +8, +20, +0, +4, +12.</summary>
    public class TrophySeasonHeroEntry
    {
        public int Value16, Value8, Value20, Value0, Value4, Value12;

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Value16);
            stream.WriteVInt(Value8);
            stream.WriteVInt(Value20);
            stream.WriteVInt(Value0);
            stream.WriteVInt(Value4);
            stream.WriteVInt(Value12);
        }
    }

    /// <summary>
    /// LogicRewardConfig::decode (0x1004fa7b8): булево + условие (два VInt,
    /// LogicDailyStreakEntry::decode), затем булево + LogicGemOffer.
    /// </summary>
    public class NotificationRewardConfig
    {
        public bool HasCondition;
        public int ConditionType;
        public int ConditionValue;
        public LogicGemOffer Offer;

        public void Encode(ByteStream stream)
        {
            if (stream.WriteBoolean(HasCondition))
            {
                stream.WriteVInt(ConditionType);
                stream.WriteVInt(ConditionValue);
            }
            NotificationStream.WriteOptionalOffer(stream, Offer);
        }
    }
}
