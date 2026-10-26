namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Загрузка карты боя, по мотивам LogicMapLoader.
    ///
    /// Карта в таблице лежит как набор строк, по символу на клетку. Символ ищется
    /// среди клеток таблицы tiles по первой букве колонки TileCode; если такого
    /// символа нет, берётся нулевая строка таблицы — пустая земля.
    /// </summary>
    public static class LogicMapLoader
    {
        /// <summary>Символ точки появления первой команды.</summary>
        public const char SpawnTeamFirstCode = '1';

        /// <summary>Символ точки появления второй команды.</summary>
        public const char SpawnTeamSecondCode = '2';

        /// <summary>Символ точки появления базы.</summary>
        public const char SpawnBaseCode = '8';

        /// <summary>Результат загрузки: карта и снятые с неё точки появления.</summary>
        public sealed class LoadedMap
        {
            public LogicTileMap TileMap;

            public readonly List<LogicVector2> SpawnPointsFirstTeam = new List<LogicVector2>();
            public readonly List<LogicVector2> SpawnPointsSecondTeam = new List<LogicVector2>();
            public readonly List<LogicVector2> SpawnPointsBases = new List<LogicVector2>();

            /// <summary>Точка появления для команды, по кругу если их меньше, чем бойцов.</summary>
            public LogicVector2 GetSpawnPoint(int teamIndex, int slot)
            {
                List<LogicVector2> points = teamIndex == 0 ? SpawnPointsFirstTeam : SpawnPointsSecondTeam;
                if (points.Count == 0) points = SpawnPointsFirstTeam;

                if (points.Count == 0)
                    return new LogicVector2(
                        LogicTileMap.TileToLogic(TileMap.Width / 2),
                        LogicTileMap.TileToLogic(TileMap.Height / 2));

                return points[slot % points.Count];
            }
        }

        /// <summary>Строки карты из таблицы по её имени.</summary>
        public static string[] GetMapRows(string mapName)
        {
            DataTable table = DataTables.Get(DataType.Map);
            if (table == null || table.Count < 1) return null;

            for (int i = 0; i < table.Count; i++)
            {
                LogicData data = table.GetData<LogicData>(i);
                if (data == null) continue;
                if (data.GetCSVRow().GetValueAt(0) != mapName) continue;

                int rows = data.GetCSVRow().GetArraySizeAt(1);
                if (rows < 1) return null;

                string[] result = new string[rows];
                for (int row = 0; row < rows; row++) result[row] = data.GetCSVRow().GetValueAt(1, row);

                return result;
            }

            return null;
        }

        /// <summary>
        /// Собирает карту из строк. Ширина берётся по первой строке, высота по их
        /// количеству — так же, как это делает старый загрузчик.
        /// </summary>
        public static LoadedMap Load(string mapName)
        {
            string[] rows = GetMapRows(mapName);
            if (rows == null || rows.Length == 0) return null;

            return Load(rows);
        }

        public static LoadedMap Load(string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;

            LoadedMap loaded = new LoadedMap();
            loaded.TileMap = new LogicTileMap();
            loaded.TileMap.Create(width, height);

            Dictionary<char, TileData> byCode = BuildTileLookup();
            TileData fallback = GetFallbackTile();

            for (int y = 0; y < height; y++)
            {
                string row = rows[y];

                for (int x = 0; x < width; x++)
                {
                    char code = x < row.Length ? row[x] : ' ';

                    TileData tileData;
                    if (!byCode.TryGetValue(code, out tileData)) tileData = fallback;

                    LogicTile tile = loaded.TileMap.GetTile(x, y);
                    if (tile == null) continue;

                    tile.OriginalData = tileData;
                    tile.SetData(tileData);

                    // Клетка, через которую нельзя пройти, закрывается для размера
                    // один — а значит и для двойки: разметка идёт от заданного
                    // слоя вверх. Столкновения снарядов от этих масок не зависят,
                    // они смотрят прямо в данные клетки.
                    if (tile.BlocksMovement())
                        tile.MarkWholeTileBlockable(1, LogicTile.MaskKind.Blocked);

                    if (tileData != null && tileData.IsDestructible) loaded.TileMap.DamageableTiles.Add(tile);
                    if (tile.IsDynamic) loaded.TileMap.DynamicTiles.Add(tile);

                    LogicVector2 point = new LogicVector2(
                        LogicTileMap.TileToLogic(x), LogicTileMap.TileToLogic(y));

                    if (code == SpawnTeamFirstCode) loaded.SpawnPointsFirstTeam.Add(point);
                    else if (code == SpawnTeamSecondCode) loaded.SpawnPointsSecondTeam.Add(point);
                    else if (code == SpawnBaseCode) loaded.SpawnPointsBases.Add(point);
                }
            }

            return loaded;
        }

        private static Dictionary<char, TileData> BuildTileLookup()
        {
            Dictionary<char, TileData> result = new Dictionary<char, TileData>();

            DataTable table = DataTables.Get(DataType.Tile);
            if (table == null) return result;

            foreach (LogicData data in table.GetDatas())
            {
                TileData tileData = data as TileData;
                if (tileData == null) continue;
                if (string.IsNullOrEmpty(tileData.TileCode)) continue;

                char code = tileData.TileCode[0];
                if (!result.ContainsKey(code)) result.Add(code, tileData);
            }

            return result;
        }

        private static TileData GetFallbackTile()
        {
            DataTable table = DataTables.Get(DataType.Tile);
            return table != null ? table.GetData<TileData>(0) : null;
        }
    }
}
