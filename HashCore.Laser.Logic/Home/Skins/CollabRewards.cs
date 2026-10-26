namespace HashCore.Laser.Logic.Home.Skins
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Reader;
    using HashCore.Laser.Server;

    /// <summary>
    /// Награды коллабов из collabs.csv.
    ///
    /// **Это и есть ответ на вопрос «что даёт контейнер».** У каждого коллаба в
    /// таблице свой список наград со своими весами:
    ///
    /// <list type="bullet">
    ///   <item>`SkinsInReward` и веса `TicketsInSkinDraw`,</item>
    ///   <item>`SpecialSkinsInReward` и `TicketsInSpecialSkinDraw`,</item>
    ///   <item>`SpraysInReward` и `TicketsInSprayDraw`,</item>
    ///   <item>`PinsInReward` и `TicketsInPinDraw`,</item>
    ///   <item>`ProfilePicsInReward` и `TicketsInProfilePicDraw`.</item>
    /// </list>
    ///
    /// Контейнер привязан к коллабу колонкой `CollabId` в
    /// random_reward_containers.csv, поэтому у мегаящика свои скины (коллаб 2,
    /// Classic), у мех-ящика свои (коллаб 14, Mechmas), у роллов свои (коллаб 8,
    /// Wasabi) — ровно как написано в таблицах.
    ///
    /// У каждого набора есть ВТОРОЙ столбец весов, `...AltDraw`. Его выбирает
    /// `TypeValue` строки награды: двойка означает запасной набор. Так устроены
    /// двусторонние коллабы — у DemonicStar строки идут именно с `TypeValue = 2`.
    ///
    /// collabs.csv сервер как таблицу данных не загружает, поэтому читаем файл
    /// напрямую — тем же способом, каким читается themes.csv. Один раз.
    /// </summary>
    public static class CollabRewards
    {
        /// <summary>
        /// Номер идущего коллаба. Выставляется сервером при старте. Берётся
        /// только тогда, когда у самого контейнера коллаб не проставлен.
        /// </summary>
        public static int ActiveCollabId;

        /// <summary>Набор наград одного вида: что может выпасть и с каким весом.</summary>
        public sealed class Pool
        {
            /// <summary>Номера строк в таблице предмета.</summary>
            public readonly List<int> InstanceIds = new List<int>();

            /// <summary>Основные веса, колонка `TicketsIn...Draw`.</summary>
            public readonly List<int> Tickets = new List<int>();

            /// <summary>Запасные веса, колонка `TicketsIn...AltDraw`.</summary>
            public readonly List<int> AltTickets = new List<int>();

            public int Count => InstanceIds.Count;

            public int GetTickets(int index, bool alternate)
            {
                List<int> list = alternate && AltTickets.Count > index ? AltTickets : Tickets;
                return index < list.Count ? list[index] : 0;
            }
        }

        private sealed class CollabPools
        {
            public readonly Pool Skins = new Pool();
            public readonly Pool SpecialSkins = new Pool();
            public readonly Pool Sprays = new Pool();
            public readonly Pool Pins = new Pool();
            public readonly Pool ProfilePics = new Pool();
        }

        private static readonly object Sync = new object();
        private static Dictionary<int, CollabPools> ByCollabId;

        public static Pool GetSkins(int collabId) => Get(collabId)?.Skins;
        public static Pool GetSpecialSkins(int collabId) => Get(collabId)?.SpecialSkins;
        public static Pool GetSprays(int collabId) => Get(collabId)?.Sprays;
        public static Pool GetPins(int collabId) => Get(collabId)?.Pins;
        public static Pool GetProfilePics(int collabId) => Get(collabId)?.ProfilePics;

        private static CollabPools Get(int collabId)
        {
            lock (Sync)
            {
                if (ByCollabId == null) ByCollabId = Read();
                return ByCollabId.TryGetValue(collabId, out CollabPools pools) ? pools : null;
            }
        }

        private static Dictionary<int, CollabPools> Read()
        {
            Dictionary<int, CollabPools> result = new Dictionary<int, CollabPools>();

            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets/csv_logic/collabs.csv");
                if (!File.Exists(path)) return result;

                Table table = new Table(path);

                int nameColumn = table.GetColumnIndexByName("Name");
                int idColumn = table.GetColumnIndexByName("CollabId");
                if (nameColumn == -1 || idColumn == -1) return result;

                // Запись коллаба занимает несколько физических строк: имя стоит
                // только в первой, а списки наград продолжаются в следующих.
                // GetRowCount считает ЗАПИСИ, а GetValueAt индексирует строки.
                CollabPools current = null;

                for (int row = 0; row < table.GetColumnRowCount(); row++)
                {
                    string name = table.GetValueAt(nameColumn, row);

                    if (!string.IsNullOrEmpty(name))
                    {
                        current = null;
                        if (int.TryParse(table.GetValueAt(idColumn, row), out int collabId))
                        {
                            if (!result.TryGetValue(collabId, out current))
                            {
                                current = new CollabPools();
                                result[collabId] = current;
                            }
                        }
                    }

                    if (current == null) continue;

                    Take(table, row, current.Skins, DataType.Skin,
                        "SkinsInReward", "TicketsInSkinDraw", "TicketsInSkinAltDraw");
                    Take(table, row, current.SpecialSkins, DataType.Skin,
                        "SpecialSkinsInReward", "TicketsInSpecialSkinDraw", null);
                    Take(table, row, current.Sprays, DataType.Spray,
                        "SpraysInReward", "TicketsInSprayDraw", "TicketsInSprayAltDraw");
                    Take(table, row, current.Pins, DataType.Emote,
                        "PinsInReward", "TicketsInPinDraw", "TicketsInPinAltDraw");
                    Take(table, row, current.ProfilePics, DataType.PlayerThumbnail,
                        "ProfilePicsInReward", "TicketsInProfilePicDraw", "TicketsInProfilePicAltDraw");
                }

                foreach (KeyValuePair<int, CollabPools> collab in result)
                {
                    CollabPools p = collab.Value;
                    if (p.Skins.Count == 0 && p.Sprays.Count == 0 && p.Pins.Count == 0
                        && p.ProfilePics.Count == 0) continue;

                    Logger.Print($"[collabRewards] коллаб {collab.Key}: скинов {p.Skins.Count}"
                        + $" (особых {p.SpecialSkins.Count}), спреев {p.Sprays.Count},"
                        + $" пинов {p.Pins.Count}, иконок {p.ProfilePics.Count}");
                }
            }
            catch (Exception exception)
            {
                Logger.Warning($"[collabRewards] прочитать не вышло: "
                    + $"{exception.GetType().Name}: {exception.Message}");
            }

            return result;
        }

        /// <summary>
        /// Забирает из одной физической строки имя награды и её веса. Имя и веса
        /// стоят в одной строке, поэтому индексы наборов сходятся сами.
        /// </summary>
        private static void Take(Table table, int row, Pool pool, DataType type,
            string nameColumn, string ticketColumn, string altTicketColumn)
        {
            int nameIndex = table.GetColumnIndexByName(nameColumn);
            if (nameIndex == -1) return;

            string itemName = table.GetValueAt(nameIndex, row);
            if (string.IsNullOrEmpty(itemName)) return;

            DataTable itemTable = DataTables.Get(type);
            LogicData item = itemTable?.GetData<LogicData>(itemName.Trim());
            if (item == null)
            {
                Logger.Warning($"[collabRewards] «{itemName.Trim()}» из {nameColumn} нет в таблице {type}");
                return;
            }

            pool.InstanceIds.Add(item.GetInstanceId());
            pool.Tickets.Add(ReadInt(table, ticketColumn, row));
            pool.AltTickets.Add(ReadInt(table, altTicketColumn, row));
        }

        private static int ReadInt(Table table, string column, int row)
        {
            if (string.IsNullOrEmpty(column)) return 0;

            int index = table.GetColumnIndexByName(column);
            if (index == -1) return 0;

            return int.TryParse(table.GetValueAt(index, row), out int value) ? value : 0;
        }
    }
}
