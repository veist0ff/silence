namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Поиск свободного места, 0x100507480.
    /// </summary>
    public static partial class LogicGamePlayUtil
    {
        /// <summary>Цена, которой оценивается заведомо недостижимое место.</summary>
        private const int UnreachableSpotScore = 99999999;

        /// <summary>
        /// Во сколько раз хуже место, до которого от исходной точки нет прямой
        /// дороги. Оно не отбрасывается совсем — просто уступает любому другому.
        /// </summary>
        private const int BlockedPathScorePenalty = 10;

        /// <summary>
        /// Сколько колец вокруг исходной точки просматривается целиком, даже если
        /// подходящее место уже нашлось. Дальше поиск останавливается на первом же.
        /// </summary>
        private const int FullSearchRings = 5;

        /// <summary>
        /// 0x100507480. Ближайшая клетка, куда можно встать.
        ///
        /// Если в исходной точке стоять можно, она и возвращается. Иначе клетки
        /// просматриваются кольцами: сначала соседние, потом следующие за ними, и
        /// так пока место не найдётся или карта не кончится.
        ///
        /// Из равных по расстоянию мест лучше то, до которого от исходной точки
        /// есть прямая дорога: место за стеной считается вдесятеро дальше, чем
        /// оно есть, и проигрывает любому открытому.
        /// </summary>
        public static bool GetClosestPassableSpot(
            int layer,
            int logicX, int logicY,
            LogicTileMap map,
            out int outX, out int outY,
            bool preferClearPath,
            bool allowWater)
        {
            outX = logicX;
            outY = logicY;

            if (map == null) return false;

            int subX = LogicTileMap.LogicToPathFinderTile(logicX);
            int subY = LogicTileMap.LogicToPathFinderTile(logicY);

            if (map.IsPassablePathFinder(subX, subY, layer, allowWater, false)) return true;

            int width = map.Width;
            int height = map.Height;

            int startTileX = Clamp(LogicTileMap.LogicToTile(logicX), width - 1);
            int startTileY = Clamp(LogicTileMap.LogicToTile(logicY), height - 1);

            int maxRing = width > height ? width : height;
            if (maxRing < 1) return false;

            int bestX = -1;
            int bestY = -1;
            int bestScore = UnreachableSpotScore;

            for (int ring = 0; ring < maxRing; ring++)
            {
                for (int tileX = startTileX - ring; tileX <= startTileX + ring; tileX++)
                {
                    for (int tileY = startTileY - ring; tileY <= startTileY + ring; tileY++)
                    {
                        // Клетки внутри кольца уже просмотрены на прошлых заходах:
                        // берутся только те, что лежат ровно на его краю.
                        int distX = tileX - startTileX;
                        if (distX < 0) distX = -distX;

                        int distY = tileY - startTileY;
                        if (distY < 0) distY = -distY;

                        if (distX != ring && distY != ring) continue;

                        if (tileX < 0 || tileX >= width || tileY < 0 || tileY >= height) continue;

                        if (!map.IsPassablePathFinder(3 * tileX + 1, 3 * tileY + 1, layer, false, false))
                            continue;

                        int dx = tileX - startTileX;
                        int dy = tileY - startTileY;
                        int score = dx * dx + dy * dy;

                        bool sameTile = tileX == startTileX && tileY == startTileY;

                        if (preferClearPath && !sameTile
                            && !IsWalkLineClear(map, tileX, tileY, startTileX, startTileY))
                        {
                            score *= BlockedPathScorePenalty;
                        }

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestX = tileX;
                            bestY = tileY;
                        }
                    }
                }

                bool keepSearching = ring < FullSearchRings && preferClearPath;
                if (!keepSearching && bestX != -1) break;
            }

            if (bestX == -1) return false;

            outX = LogicTileMap.PathFinderTileToLogic(3 * bestX + 1, false);
            outY = LogicTileMap.PathFinderTileToLogic(3 * bestY + 1, false);

            return true;
        }

        /// <summary>
        /// Есть ли прямая дорога от клетки до клетки. Шаг делается по той оси, по
        /// которой до цели дальше, — получается ровно та же ломаная, по которой
        /// пойдёт и сам персонаж.
        /// </summary>
        private static bool IsWalkLineClear(LogicTileMap map, int fromX, int fromY, int toX, int toY)
        {
            int x = fromX;
            int y = fromY;

            while (true)
            {
                int dx = toX - x;
                int dy = toY - y;

                int absX = dx >= 0 ? dx : -dx;
                int absY = dy >= 0 ? dy : -dy;

                int stepX = dx >= 1 ? 1 : dx >> 31;
                int stepY = dy >= 1 ? 1 : dy >> 31;

                if (absX > absY) x += stepX;
                else y += stepY;

                LogicTile tile = map.GetTile(x, y);
                if (tile == null) return false;

                bool passable = (tile.Data == null || tile.Data.DynamicCode <= 0)
                    && (!tile.IsWater() || tile.WaterWalkable);

                if (passable) return true;

                if (x == toX && y == toY) return false;
            }
        }

        private static int Clamp(int value, int high)
        {
            int clamped = value < high ? value : high;
            return value <= 0 ? 0 : clamped;
        }
    }
}
