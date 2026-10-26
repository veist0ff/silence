namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Видимость: 0x1003b481c, 0x1003b48a0, 0x1003b4938, 0x1003b49d0, 0x1003b4524.
    /// </summary>
    public partial class LogicTileMap
    {
        /// <summary>Сколько подклеток видно вбок от середины экрана.</summary>
        public const int VisibilityRadiusX = 43;

        /// <summary>Сколько подклеток видно вверх. Вторая величина — та же при зеркальном поле.</summary>
        public const int VisibilityRadiusUp = 33;
        public const int VisibilityRadiusUpMirrored = 28;

        /// <summary>Отступ от края карты, с которого окно обзора начинает съезжать вбок.</summary>
        public const int VisibilityEdgeSlackX = 6;

        /// <summary>То же по вертикали.</summary>
        public const int VisibilityEdgeSlackUp = 26;
        public const int VisibilityEdgeSlackUpMirrored = 18;

        /// <summary>Сколько логических единиц в одной подклетке обзора.</summary>
        public const int VisibilitySubTileSize = 100;

        /// <summary>Поля +212 и +216. Размер карты в подклетках обзора.</summary>
        public int SubTileWidth
        {
            get { return LogicWidth / VisibilitySubTileSize; }
        }

        public int SubTileHeight
        {
            get { return LogicHeight / VisibilitySubTileSize; }
        }

        /// <summary>
        /// 0x1003b481c. Насколько далеко видно вбок от заданной точки.
        ///
        /// Окно обзора не выходит за карту: у левого края видно меньше слева и
        /// ровно на столько же больше справа. Знак и говорит, какую из двух
        /// половин спрашивают — минус единица левую, плюс единица правую.
        /// </summary>
        public int GetVisibilityRadiusX(int logicX, int sign)
        {
            int sub = logicX / VisibilitySubTileSize;
            int radius = VisibilityRadiusX;

            if (sub < radius)
            {
                int over = ClampPositive(radius - VisibilityEdgeSlackX - sub);
                radius += over * sign;
            }
            else
            {
                int limit = SubTileWidth - radius;

                if (limit <= sub)
                {
                    int over = ClampPositive(sub - (VisibilityEdgeSlackX + limit) + 1);
                    radius -= over * sign;
                }
            }

            return VisibilitySubTileSize * radius;
        }

        /// <summary>
        /// 0x1003b48a0. Насколько далеко видно вверх от заданной точки.
        ///
        /// Вверх и вниз видно по-разному: камера смотрит сверху вниз, и поле
        /// перед бойцом всегда больше поля за спиной. На зеркальном поле эти две
        /// величины меняются местами.
        /// </summary>
        public int GetVisibilityRadiusUp(int logicY, bool mirrored)
        {
            int radius = mirrored ? VisibilityRadiusUpMirrored : VisibilityRadiusUp;
            int slack = mirrored ? VisibilityEdgeSlackUpMirrored : VisibilityEdgeSlackUp;

            int sub = logicY / VisibilitySubTileSize;

            if (sub >= radius)
            {
                int limit = SubTileHeight - radius;
                if (limit <= sub) radius += ClampPositive(sub - slack - limit + 1);
            }
            else
            {
                radius -= ClampPositive(radius - (sub + slack));
            }

            return VisibilitySubTileSize * radius;
        }

        /// <summary>0x1003b4938. Насколько далеко видно вниз от заданной точки.</summary>
        public int GetVisibilityRadiusDown(int logicY, bool mirrored)
        {
            int radius = mirrored ? VisibilityRadiusUp : VisibilityRadiusUpMirrored;
            int slack = mirrored ? VisibilityEdgeSlackUp : VisibilityEdgeSlackUpMirrored;

            int sub = logicY / VisibilitySubTileSize;

            if (sub >= radius)
            {
                int limit = SubTileHeight - radius;
                if (limit <= sub) radius -= ClampPositive(sub - slack - limit + 1);
            }
            else
            {
                radius += ClampPositive(radius - (sub + slack));
            }

            return VisibilitySubTileSize * radius;
        }

        /// <summary>
        /// 0x1003b49d0. Попадает ли точка в окно обзора того, кто смотрит.
        ///
        /// Это не круг, а прямоугольник экрана: по нему решается, видно ли цель
        /// вообще. Бьющий не может стрелять в того, кто за краем его экрана, даже
        /// если тот совсем рядом по прямой.
        /// </summary>
        public bool IsInsideVisibilityRect(
            int fromX, int fromY,
            int toX, int toY,
            int marginX, int marginUp, int marginDown,
            bool mirrored)
        {
            if (fromX - marginX - GetVisibilityRadiusX(fromX, -1) > toX) return false;

            int down = marginDown + fromY + GetVisibilityRadiusDown(fromY, mirrored);
            int up = fromY - marginUp - GetVisibilityRadiusUp(fromY, mirrored);

            if (marginX + fromX + GetVisibilityRadiusX(fromX, 1) < toX) return false;

            return down >= toY && up <= toY;
        }

        /// <summary>
        /// 0x1003b4524. Видно ли точку, если не считать стен.
        ///
        /// Открытую точку видно всегда. Спрятавшегося в кустах видно, только если
        /// подойти вплотную: насколько именно — задаёт радиус в клетках, а форма
        /// у этой близости бывает круглой или ромбом.
        /// </summary>
        public bool CanPlayerSeeFromToNoLOS(
            int fromX, int fromY, int toX, int toY, int radiusTiles, bool circular)
        {
            int tileX = toX / LogicTile.TileSize;
            if (Width <= tileX) return true;

            int tileY = toY / LogicTile.TileSize;
            if (tileX < 0 || tileY < 0 || Height <= tileY) return true;

            LogicTile tile = GetTile(tileX, tileY);
            if (tile == null || !tile.HidesHero()) return true;

            int dx = tileX - fromX / LogicTile.TileSize;
            int dy = tileY - fromY / LogicTile.TileSize;

            int absX = dx >= 0 ? dx : -dx;
            int absY = dy >= 0 ? dy : -dy;

            if (absX > radiusTiles || absY > radiusTiles) return false;

            if (circular) return dx * dx + dy * dy <= radiusTiles * radiusTiles;

            return absX + absY < 2 * radiusTiles;
        }

        private static int ClampPositive(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
