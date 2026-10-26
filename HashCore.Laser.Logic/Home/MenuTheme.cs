namespace HashCore.Laser.Logic.Home
{
    using System;
    using System.IO;

    using HashCore.Laser.Logic.Data.Reader;
    using HashCore.Laser.Server;

    /// <summary>
    /// Тема меню из themes.csv.
    ///
    /// В таблице у каждой темы два поля, которые нас интересуют: `FileName` — файл
    /// с фоном, и `ExportName` — имя самого клипа фона (для темы Gigi это
    /// `bgr_gigi`). Номер активной темы лежит в конфиге сервера ключом `ThemeID`,
    /// но до сих пор он нигде не читался, а фоны предложений были либо пустыми,
    /// либо зашиты строкой от чужой темы.
    /// </summary>
    public static class MenuTheme
    {
        /// <summary>
        /// Откуда берётся номер темы. Сервер подставляет сюда чтение конфига,
        /// и тогда правка ThemeID подхватывается БЕЗ перезапуска: конфиг сам
        /// перечитывается по времени изменения файла.
        /// </summary>
        public static Func<int> ThemeIdProvider;

        private static int FallbackThemeId = 152;

        /// <summary>Номер строки в themes.csv. Берётся из конфига на каждом обращении.</summary>
        public static int ActiveThemeId
        {
            get
            {
                Func<int> provider = ThemeIdProvider;
                if (provider == null) return FallbackThemeId;

                try
                {
                    int id = provider();
                    return id > 0 ? id : FallbackThemeId;
                }
                catch
                {
                    return FallbackThemeId;
                }
            }

            set { FallbackThemeId = value; }
        }


        private static string CachedBackground;
        private static string CachedName;
        private static int CachedForTheme = -1;
        private static readonly object Sync = new object();

        /// <summary>
        /// Фон предложений.
        ///
        /// Клипа вида `offer_bgr_<тема>` НЕ СУЩЕСТВУЕТ — проверено по самим
        /// файлам: background_gigi.sc экспортирует только `bgr_gigi`,
        /// background_strangerthings.sc — только `bgr_strangerthings`, и ни в
        /// одном файле нет ни одного имени с префиксом offer_bgr. Именно поэтому
        /// фон предложений всегда выходил пустым.
        ///
        /// Сейчас сюда идёт `bgr_hub_palace` — клип из событийного файла
        /// коллаба (8a1bf81c-…_collab_stranger_things.sc), он загружен, пока
        /// событие активно. Меняется одной строкой.
        /// </summary>
        /// <summary>
        /// Фон предложений — тот же клип, что и фон лобби активной темы.
        ///
        /// Клипа вида `offer_bgr_<тема>` НЕ СУЩЕСТВУЕТ: проверено по самим
        /// файлам, background_gigi.sc экспортирует только `bgr_gigi`,
        /// background_strangerthings.sc — только `bgr_strangerthings`, и ни в
        /// одном файле нет имени с таким префиксом. Отсюда и пустой фон.
        ///
        /// Привязка именно к теме ЛОББИ не случайна: клиент держит загруженным
        /// ровно один файл фона — тот, что у активной темы. Клип из чужой темы
        /// в памяти не лежит, и фон снова выйдет пустым. При теме 152 это
        /// `bgr_strangerthings`.
        /// </summary>
        public static string OfferBackgroundExportName => BackgroundExportName;


        /// <summary>Имя активной темы из themes.csv, колонка Name.</summary>
        public static string ThemeName
        {
            get
            {
                lock (Sync)
                {
                    Refresh();
                    return CachedName;
                }
            }
        }

        /// <summary>
        /// Имя клипа фона активной темы. Пустая строка, если тему прочитать не
        /// удалось — тогда клиент нарисует свой фон по умолчанию.
        /// </summary>
        public static string BackgroundExportName
        {
            get
            {
                lock (Sync)
                {
                    Refresh();
                    return CachedBackground;
                }
            }
        }

        private static void Refresh()
        {
            if (CachedForTheme == ActiveThemeId) return;

            CachedForTheme = ActiveThemeId;
            CachedBackground = ReadColumn(ActiveThemeId, "ExportName");
            CachedName = ReadColumn(ActiveThemeId, "Name");

            Logger.Print($"[MenuTheme] тема лобби {ActiveThemeId} «{CachedName}», фон «{CachedBackground}»");
        }

        private static string ReadColumn(int themeId, string columnName)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets/csv_logic/themes.csv");
                if (!File.Exists(path)) return string.Empty;

                Table table = new Table(path);

                int column = table.GetColumnIndexByName(columnName);
                if (column == -1) return string.Empty;
                if (themeId < 0 || themeId >= table.GetRowCount()) return string.Empty;

                return table.GetValueAt(column, themeId) ?? string.Empty;
            }
            catch (Exception exception)
            {
                Logger.Warning($"[MenuTheme] тему прочитать не вышло: {exception.GetType().Name}: {exception.Message}");
                return string.Empty;
            }
        }
    }
}
