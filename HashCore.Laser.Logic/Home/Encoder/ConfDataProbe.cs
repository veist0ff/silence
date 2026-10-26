namespace HashCore.Laser.Logic.Home.Items
{
    using System;
    using HashCore.Laser.Titan;

    /// <summary>
    /// Зеркало LogicConfData::decode (0x1004bf0ac). Нужно ровно для одного: прогнать
    /// уже записанные сервером байты конфигурации обратно и убедиться, что поток
    /// сходится позиция в позицию. У событий такая проверка есть давно, а блок
    /// конфигурации правился вслепую — и каждая правка стоила перезахода в клиент
    /// с разъехавшимся домом.
    ///
    /// Разбор совпадает с клиентом по порядку и по типам: двадцать шесть операций,
    /// из них три списка событий, четыре списка целых через decodeIntList и
    /// одиннадцать списков записей. Тела читаются только у тех записей, которые
    /// сервер действительно отправляет; для остальных счётчик обязан быть нулевым,
    /// и ненулевой сразу выбрасывает исключение с именем записи — это тоже ответ.
    /// </summary>
    public static class ConfDataProbe
    {
        public sealed class Result
        {
            public int Start;
            public int End;
            public int Consumed;
            public int ChronosAssetListOffset = -1;
            public int ChronosAssetEntryCount;
            public string Error;
            public readonly System.Collections.Generic.List<string> Trace = new System.Collections.Generic.List<string>();

            public bool Ok => Error == null && Consumed == End;
        }

        public static Result Verify(byte[] raw, int start, int end)
        {
            Result result = new Result { Start = start, End = end };

            try
            {
                ByteStream r = new ByteStream(raw, end);
                r.SetOffset(start);

                void Mark(string name) => result.Trace.Add($"{name}@{r.GetOffset() - start}");

                Mark("1.head");
                r.ReadVInt();                                  //  1  +0

                Mark("2.privacy");
                ReadEntryList(r, "AvatarPrivacyPreferences", _ => _.ReadVInt());

                for (int list = 0; list < 3; list++)           //  3-5  три списка событий
                {
                    int count = r.ReadVInt();
                    for (int i = 0; i < count; i++)
                        new EventData().DecodeCanonical(r);
                }

                ReadIntList(r);                                //  6
                ReadIntList(r);                                //  7
                ReadIntList(r);                                //  8

                Mark("9.release");
                ReadEntryList(r, "ReleaseEntry", s =>          //  9
                {
                    ByteStreamHelper.ReadDataReference(s);
                    s.ReadInt(); s.ReadInt(); s.ReadInt();
                    s.ReadBoolean();
                });

                Mark("10.intvalue");
                ReadEntryList(r, "IntValueEntry", s =>         // 10  значение, затем ключ
                {
                    s.ReadVInt();
                    s.ReadVInt();
                });

                Mark("11.timed");
                ReadEmptyList(r, "TimedIntValueEntry");        // 11
                ReadEmptyList(r, "CustomEvent");               // 12
                ReadEmptyList(r, "ShopChainOfferThemeEntry");  // 13
                ReadEmptyList(r, "AdPlacementEntry");          // 14
                ReadEmptyList(r, "ThemeOverrideEntry");        // 15
                ReadEmptyList(r, "JoinClubEventEntry");        // 16

                Mark("17.fortune");
                ReadEntryList(r, "DailyFortuneCookieEntry", s =>   // 17
                {
                    s.ReadVInt();                              // +4
                    int cookies = s.ReadVInt();                // m_fortuneCookies
                    for (int i = 0; i < cookies; i++) { s.ReadStringReference(900000); s.ReadVInt(); }
                    s.ReadVInt();                              // +0
                    s.ReadVInt();                              // +8
                });

                Mark("18.chronos");
                result.ChronosAssetListOffset = r.GetOffset();
                ReadEntryList(r, "ChronosAssetListEvent", s => // 18
                {
                    int entries = s.ReadVInt();
                    result.ChronosAssetEntryCount += entries;
                    for (int i = 0; i < entries; i++)
                    {
                        if (s.ReadBoolean())
                        {
                            s.ReadStringReference(1024);       // хеш
                            s.ReadStringReference(1024);       // имя файла
                        }
                        ByteStreamHelper.ReadDataReference(s); // строка chronos_asset_ids
                    }
                    s.ReadVInt();                              // +24
                    s.ReadVInt();                              // +4
                    s.ReadVInt();                              // +0
                });

                Mark("19.visualoffer");
                ReadEmptyList(r, "ShopVisualOfferGroupingEntry"); // 19

                ReadIntList(r);                                // 20
                ReadIntList(r);                                // 21
                ReadIntList(r);                                // 22

                Mark("23.news");
                ReadEntryList(r, "NewsInboxLinkEntry", s =>    // 23
                {
                    s.ReadVInt();                              // +24
                    s.ReadStringReference(900000);             // ссылка
                    if (s.ReadBoolean()) { s.ReadStringReference(900000); s.ReadVInt(); }
                });

                Mark("24.storepromo");
                ReadEmptyList(r, "StorePromoEntry");           // 24
                ReadEmptyList(r, "StoreLinkEntry");            // 25
                Mark("26.dailywin");
                ReadEntryList(r, "DailyWinTrackEventEntry", s =>   // 26
                {
                    // DailyWinTrackEventEntry::decode 0x1004bb158
                    s.ReadVInt();                              // +4
                    ByteStreamHelper.ReadDataReference(s);     // +40, тип дорожки
                    s.ReadVInt();                              // +0

                    for (int i = s.ReadVInt(); i > 0; i--) s.ReadVInt();   // ступени

                    for (int i = s.ReadVInt(); i > 0; i--)                 // награды
                    {
                        if (!s.ReadBoolean()) continue;

                        for (int j = s.ReadVInt(); j > 0; j--)
                        {
                            if (!s.ReadBoolean()) continue;

                            s.ReadVInt();                          // тип
                            s.ReadVInt();                          // количество
                            ByteStreamHelper.ReadDataReference(s); // предмет
                            s.ReadVInt();                          // скин
                        }
                    }
                });

                Mark("end");
                result.Consumed = r.GetOffset();
            }
            catch (Exception ex)
            {
                result.Error = ex.GetType().Name + ": " + ex.Message;
            }

            return result;
        }

        private static void ReadIntList(ByteStream r)
        {
            int count = r.ReadVInt();
            for (int i = 0; i < count; i++) r.ReadVInt();
        }

        private static void ReadEntryList(ByteStream r, string name, Action<ByteStream> body)
        {
            int count = r.ReadVInt();
            if (count > 4096) throw new InvalidOperationException($"{name}: счётчик {count} — поток разъехался.");
            for (int i = 0; i < count; i++) body(r);
        }

        private static void ReadEmptyList(ByteStream r, string name)
        {
            int count = r.ReadVInt();
            if (count != 0)
                throw new InvalidOperationException(
                    $"{name}: счётчик {count}, а тело этой записи ещё не разобрано.");
        }
    }
}
