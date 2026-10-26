namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Карта боя, 1:1 с LogicTileMap.
    ///
    /// Смещения оригинала:
    ///   +32  массив клеток, индекс = x + ширина * y
    ///   +40  разрушаемые клетки, счётчик +52
    ///   +56  динамические клетки, счётчик +228
    ///   +128 очередь клеток на снос, счётчик +140
    ///   +196 ширина в клетках
    ///   +200 высота в клетках
    ///   +220 всего клеток
    ///   +232 просьба пересчитать коллизию и отскоки
    /// </summary>
    public partial class LogicTileMap
    {
        /// <summary>Поле +196.</summary>
        public int Width;

        /// <summary>Поле +200.</summary>
        public int Height;

        /// <summary>
        /// Поля +204 и +208. Размер карты в логических единицах. Именно по ним
        /// зажимаются координаты в moveTo и setCharacterPositionSafe, а не по
        /// размеру в клетках.
        /// </summary>
        public int LogicWidth;
        public int LogicHeight;

        /// <summary>Поле +32. Хранится строками: индекс = x + Width * y.</summary>
        private LogicTile[] _tiles = new LogicTile[0];

        /// <summary>Поле +40.</summary>
        public readonly List<LogicTile> DamageableTiles = new List<LogicTile>();

        /// <summary>Поле +56.</summary>
        public readonly List<LogicTile> DynamicTiles = new List<LogicTile>();

        /// <summary>Поле +128. Индексы клеток, которые надо снести ближайшим тиком.</summary>
        private readonly List<int> _tilesToDestroy = new List<int>();

        /// <summary>Поле +232.</summary>
        private bool _collisionDirty;

        public void Create(int width, int height)
        {
            Width = width;
            Height = height;
            LogicWidth = width * LogicTile.TileSize;
            LogicHeight = height * LogicTile.TileSize;
            _tiles = new LogicTile[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    LogicTile tile = new LogicTile();
                    tile.X = x;
                    tile.Y = y;
                    _tiles[x + width * y] = tile;
                }
            }
        }

        /// <summary>0x1003b31bc. Логические единицы в номер клетки.</summary>
        public static int LogicToTile(int logic)
        {
            return logic / LogicTile.TileSize;
        }

        /// <summary>0x1003b31d8. Номер клетки в центр клетки в логических единицах.</summary>
        public static int TileToLogic(int tile)
        {
            return LogicTile.TileSize * tile + LogicTile.TileSize / 2;
        }

        /// <summary>0x1003b2668. Логические единицы в подклетку пасфайндера.</summary>
        public static int LogicToPathFinderTile(int logic)
        {
            return logic / LogicTile.SubTileSize;
        }

        /// <summary>
        /// 0x1003b2684. Подклетка в логические единицы. Оригинал добавляет половину
        /// подклетки, только когда просят центр.
        /// </summary>
        public static int PathFinderTileToLogic(int subTile, bool center)
        {
            return LogicTile.SubTileSize * subTile + (center ? LogicTile.SubTileSize / 2 : 0);
        }

        /// <summary>0x1003b3184. Без проверки границ, как в оригинале.</summary>
        public LogicTile GetTileRaw(int x, int y)
        {
            return _tiles[x + Width * y];
        }

        /// <summary>0x1003b1870. С проверкой границ.</summary>
        public LogicTile GetTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return null;
            return _tiles[x + Width * y];
        }

        /// <summary>0x1003b3198. По плоскому индексу.</summary>
        public LogicTile GetTile(int index)
        {
            if (index < 0 || index >= _tiles.Length) return null;
            return _tiles[index];
        }

        public LogicTile GetTileByLogicPosition(int logicX, int logicY)
        {
            return GetTile(LogicToTile(logicX), LogicToTile(logicY));
        }

        /// <summary>
        /// 0x1003b2aa8. Порядок шагов взят из оригинала: временные запреты и зоны
        /// избегания обновляются ДО тика клеток, снос разбирается после, а пересчёт
        /// коллизии и отскоков идёт в самом конце и только по запросу.
        /// </summary>
        public void Tick(LogicGameObjectManagerServer manager)
        {
            ClearTempUnpassableTiles();
            RefreshTilesToAvoid();

            for (int i = 0; i < DynamicTiles.Count; i++) DynamicTiles[i].Tick(manager);

            for (int i = 0; i < DamageableTiles.Count; i++)
            {
                LogicTile tile = DamageableTiles[i];
                tile.Tick(manager);

                bool queued = _tilesToDestroy.Contains(tile.X + Width * tile.Y);
                bool expired = tile.Data != null && tile.Data.Lifetime >= 1 && tile.LifetimeCounter < 1;

                if (queued || expired) DestroyTile(tile.X, tile.Y);
            }

            _tilesToDestroy.Clear();

            if (_collisionDirty)
            {
                CalculateCollisionForLargerCharacters();
                RecalculateBounces();
                _collisionDirty = false;
            }
        }

        /// <summary>0x1003b2194. Временная маска живёт ровно один тик.</summary>
        public void ClearTempUnpassableTiles()
        {
            for (int i = 0; i < _tiles.Length; i++)
            {
                LogicTile tile = _tiles[i];
                if (tile != null) tile.TemporaryMask = 0;
            }
        }

        /// <summary>0x1003b2098.</summary>
        public void RefreshTilesToAvoid()
        {
        }

        /// <summary>0x1003b27e0.</summary>
        public void DestroyTile(int x, int y)
        {
            LogicTile tile = GetTile(x, y);
            if (tile == null || tile.Destroyed) return;

            tile.Destroyed = true;
            tile.BlockedMask = 0;
            tile.ConditionalMaskA = 0;
            tile.ConditionalMaskB = 0;
            tile.RespawnCountdown = tile.Data != null ? tile.Data.RespawnSeconds * 20 : 0;

            // Снос меняет геометрию, значит коллизию крупных персонажей и отскоки
            // снарядов надо пересчитать. Оригинал ставит тот же флаг +232.
            _collisionDirty = true;
        }

        /// <summary>0x1003b269c. Кладёт клетку в очередь на снос ближайшим тиком.</summary>
        public void DamageTile(int x, int y)
        {
            LogicTile tile = GetTile(x, y);
            if (tile == null || tile.Destroyed) return;

            int index = x + Width * y;
            if (!_tilesToDestroy.Contains(index)) _tilesToDestroy.Add(index);
        }

        /// <summary>0x1003b2d08.</summary>
        public void CalculateCollisionForLargerCharacters()
        {
        }

        /// <summary>0x1003b18b0.</summary>
        public void RecalculateBounces()
        {
        }

        /// <summary>
        /// 0x1003b3098. Клетка за границей карты непроходима, внутри решает маска.
        /// </summary>
        public bool IsPassablePathFinder(int subX, int subY, int layer, bool ignoreA, bool ignoreB)
        {
            int tileX = subX / LogicTile.SubTilesPerTile;
            int tileY = subY / LogicTile.SubTilesPerTile;

            LogicTile tile = GetTile(tileX, tileY);
            if (tile == null) return false;

            return tile.IsPassablePathFinder(
                layer,
                subX - tileX * LogicTile.SubTilesPerTile,
                subY - tileY * LogicTile.SubTilesPerTile,
                ignoreA,
                ignoreB);
        }

        /// <summary>0x1003b310c.</summary>
        public int GetPathFinderCost(int subX, int subY, int layer, bool ignoreA, bool ignoreB)
        {
            return IsPassablePathFinder(subX, subY, layer, ignoreA, ignoreB) ? 0 : int.MaxValue;
        }

        /// <summary>
        /// 0x1003b3ca0. Луч по клеткам Брезенхэмом: зрение перекрывает то же, что
        /// и снаряды, поэтому проверка идёт по blocksVision.
        /// </summary>
        public bool IsPlayerLineOfSightClear(int fromX, int fromY, int toX, int toY)
        {
            int x0 = LogicToTile(fromX);
            int y0 = LogicToTile(fromY);
            int x1 = LogicToTile(toX);
            int y1 = LogicToTile(toY);

            int dx = x1 > x0 ? x1 - x0 : x0 - x1;
            int dy = y1 > y0 ? y1 - y0 : y0 - y1;
            int stepX = x0 < x1 ? 1 : -1;
            int stepY = y0 < y1 ? 1 : -1;
            int error = dx - dy;

            while (true)
            {
                LogicTile tile = GetTile(x0, y0);
                if (tile == null) return false;
                if (tile.BlocksVision() && !tile.Destroyed) return false;

                if (x0 == x1 && y0 == y1) return true;

                int doubled = error * 2;
                if (doubled > -dy)
                {
                    error -= dy;
                    x0 += stepX;
                }
                if (doubled < dx)
                {
                    error += dx;
                    y0 += stepY;
                }
            }
        }
    }
}
