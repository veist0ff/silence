namespace HashCore.Laser.Logic.Home.DailyWin
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HashCore.Laser.Logic.Data.Reader;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Server;

    /// <summary>
    /// Дорожка ежедневных побед — последний список блока LogicConf.
    ///
    /// Разобрано по клиенту: `DailyWinTrackEventEntry::decode` (0x1004bb158) читает
    /// запись, `LogicConfData::decode` (хвост, 0x1004bf850) — список этих записей
    /// последним полем конфигурации, `getActiveDailyWinTrack` (0x100341d90) берёт из
    /// списка ПЕРВУЮ запись, а `HomePageDailyWinTrack::refreshButtonGraphics`
    /// (0x10034049c) рисует панель только если у записи есть хотя бы одна ступень.
    ///
    /// Панель появляется, когда сошлось всё сразу:
    ///   - `getIntValue(10056) == 1` (LogicRandomRewardManager::isEnabled 0x100492b60);
    ///   - уровень игрока не ниже `getIntValue(10057)` (isUnlocked 0x100492b88);
    ///   - в доме есть LogicRandomRewardManager (он и так уходит);
    ///   - в конфигурации есть хотя бы одна запись с непустым списком ступеней.
    /// </summary>
    public class DailyWinTrack
    {
        /// <summary>
        /// Таблица daily_win_track_type.csv. Номер взят из клиента:
        /// `getTrackTypeSafe` (0x1004baf00) на пустой ссылке берёт
        /// `LogicDataTables::getTable(123)` и её нулевую строку.
        /// </summary>
        public const int TrackTypeClassId = 123;

        /// <summary>Строка Normal, без суффикса в имени клипа.</summary>
        public const int TrackTypeNormal = 0;

        /// <summary>Строка Lucky, клип получает суффикс `_lucky`.</summary>
        public const int TrackTypeLucky = 1;

        /// <summary>
        /// Клиент ищет клип `daily_wins_track_6` (+ суффикс типа), шестёрка в имени
        /// зашита. Столбцов Win в таблице восемь, но больше шести ступеней рисовать
        /// нечем.
        /// </summary>
        public const int MaxSteps = 6;

        /// <summary>Поле +4 записи. Уходит нулём: клиент им панель не гейтит.</summary>
        public int StartTime;

        /// <summary>Поле +0 записи.</summary>
        public int Id;

        /// <summary>
        /// Какой тип дорожки показывать. Lucky — «удачный день»: клиент берёт клип
        /// `daily_wins_track_6_lucky` и заголовок TID_DAILY_WINS_LUCKY_DAY.
        /// </summary>
        public static int DefaultTrackTypeIndex = TrackTypeLucky;

        /// <summary>Награда одной ступени: валюта события.</summary>
        public static ShopItem DefaultRewardType = ShopItem.CollabEventCurrency;

        /// <summary>Сколько валюты события даёт каждая ступень.</summary>
        public static int DefaultRewardCount = 500;

        /// <summary>Строка daily_win_track_type.csv: Normal или Lucky.</summary>
        public int TrackTypeIndex = TrackTypeNormal;

        /// <summary>Сколько побед нужно на каждой ступени.</summary>
        public readonly List<int> WinCounts = new List<int>();

        /// <summary>Награда каждой ступени. Индексы совпадают с <see cref="WinCounts"/>.</summary>
        public readonly List<List<LogicGemOffer>> Rewards = new List<List<LogicGemOffer>>();

        /// <summary>
        /// Порядок полей — `DailyWinTrackEventEntry::decode` (0x1004bb158) один в один.
        /// Оба списка клиент ограничивает 32 элементами и на превышении падает
        /// с «Too many items in array decode», так что режем на своей стороне.
        /// </summary>
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(StartTime);
            ByteStreamHelper.WriteDataReference(stream, TrackTypeClassId * 1000000 + TrackTypeIndex);
            stream.WriteVInt(Id);

            int steps = WinCounts.Count;
            if (steps > 32) steps = 32;

            stream.WriteVInt(steps);
            for (int i = 0; i < steps; i++) stream.WriteVInt(WinCounts[i]);

            stream.WriteVInt(steps);
            for (int i = 0; i < steps; i++)
            {
                List<LogicGemOffer> step = i < Rewards.Count ? Rewards[i] : null;

                if (!stream.WriteBoolean(step != null)) continue;

                stream.WriteVInt(step.Count);
                foreach (LogicGemOffer offer in step)
                {
                    if (!stream.WriteBoolean(offer != null)) continue;
                    offer.Encode(stream);
                }
            }
        }

        private static DailyWinTrack Cached;
        private static bool CacheBuilt;
        private static readonly object CacheSync = new object();

        /// <summary>
        /// Дорожка из daily_win_track.csv. Таблица в игру не загружается (сервер её
        /// не разбирает), поэтому читаем файл тем же ридером, что и остальные csv.
        /// Возвращает null, если дорожку собрать не из чего — тогда список в
        /// конфигурации уходит пустым и панели просто нет.
        /// </summary>
        public static DailyWinTrack GetDefault()
        {
            lock (CacheSync)
            {
                if (CacheBuilt) return Cached;
                CacheBuilt = true;

                try
                {
                    Cached = Build();
                }
                catch (Exception exception)
                {
                    Logger.Warning($"[DailyWinTrack] дорожку собрать не вышло: {exception.GetType().Name}: {exception.Message}");
                    Cached = null;
                }

                if (Cached != null)
                {
                    Logger.Print($"[DailyWinTrack] ступеней {Cached.WinCounts.Count}, "
                        + $"наград на первой {(Cached.Rewards.Count > 0 ? Cached.Rewards[0].Count : 0)}");
                }

                return Cached;
            }
        }

        private static DailyWinTrack Build()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets/csv_logic/daily_win_track.csv");
            if (!File.Exists(path)) return null;

            Table table = new Table(path);
            if (table.GetRowCount() < 1) return null;

            DailyWinTrack track = new DailyWinTrack();
            track.TrackTypeIndex = DefaultTrackTypeIndex;

            for (int step = 1; step <= MaxSteps; step++)
            {
                // Сколько ступеней рисовать, берём из таблицы: там столбцы Win1..Win8,
                // и клиент точно так же отрезает пустые с конца
                // (LogicDailyWinTrackRewardsData::createReferencesCustom 0x1003ff890).
                int column = table.GetColumnIndexByName("Win" + step);
                if (column == -1) break;

                string value = table.GetValueAt(column, 0);
                if (string.IsNullOrWhiteSpace(value)) break;

                // А вот сама награда своя: в записи конфигурации мы отдаём клиенту
                // готовые предложения, и он показывает их, а не строку из таблицы.
                track.WinCounts.Add(step);
                track.Rewards.Add(new List<LogicGemOffer>
                {
                    new LogicGemOffer(DefaultRewardType, DefaultRewardCount)
                });
            }

            return track.WinCounts.Count > 0 ? track : null;
        }

        /// <summary>
        /// Победа в бою: двигает дневной счётчик и выдаёт награду ступени.
        ///
        /// Дорожка дневная, поэтому счётчик сбрасывается при смене даты по UTC.
        /// Возвращает, сколько валюты события выдали (ноль — ступени на этот номер
        /// победы нет или дорожка уже пройдена).
        /// </summary>
        public static int RegisterWin(HomeMode homeMode)
        {
            ClientHome home = homeMode != null ? homeMode.Home : null;
            if (home == null) return 0;

            DailyWinTrack track = GetDefault();
            if (track == null) return 0;

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (home.DailyWinDay != today)
            {
                home.DailyWinDay = today;
                home.DailyWinCount = 0;
            }

            home.DailyWinCount++;

            int index = track.WinCounts.IndexOf(home.DailyWinCount);
            if (index == -1 || index >= track.Rewards.Count) return 0;

            int granted = 0;
            foreach (LogicGemOffer offer in track.Rewards[index])
            {
                if (offer == null || offer.Count <= 0) continue;
                if (offer.Type != ShopItem.CollabEventCurrency) continue;

                home.CollabCurrency += offer.Count;
                granted += offer.Count;
            }

            if (granted > 0)
            {
                Logger.Print($"[DailyWinTrack] победа {home.DailyWinCount}: выдано {granted} валюты события");
            }

            return granted;
        }
    }
}
