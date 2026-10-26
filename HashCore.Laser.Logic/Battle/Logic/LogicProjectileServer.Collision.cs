namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>Столкновения снаряда: со стенами и с объектами.</summary>
    public partial class LogicProjectileServer
    {
        /// <summary>Столкновений не было.</summary>
        public const int CollisionNone = 0;

        /// <summary>Снаряд врезался в объект.</summary>
        public const int CollisionObject = 1;

        /// <summary>Снаряд врезался в стену.</summary>
        public const int CollisionTile = 2;

        /// <summary>
        /// На сколько отрезок полёта продлевается вперёд перед проверкой.
        /// Из handleCollisions (0x1004780c8): шаг нормируется ровно до двадцати
        /// логических единиц, чтобы быстрый снаряд не проскочил тонкую цель.
        /// </summary>
        public const int CollisionLookAhead = 20;

        /// <summary>Точка, где снаряд столкнулся. Поля +120 и +124 оригинала.</summary>
        public int CollisionX;
        public int CollisionY;

        /// <summary>Поле +1361. Снаряд уткнулся в стену на этом тике.</summary>
        public bool HitWallThisTick;

        /// <summary>С чем именно столкнулись.</summary>
        public LogicGameObjectServer CollisionTarget;

        /// <summary>
        /// 0x1004780c8. Ищет ближайшее столкновение на отрезке полёта.
        ///
        /// Отрезок берётся от позиции прошлого тика до текущей и продлевается
        /// вперёд на двадцать единиц — иначе быстрый снаряд успевал бы за тик
        /// перескочить цель насквозь.
        ///
        /// Из всех найденных столкновений выбирается ближайшее к началу отрезка:
        /// снаряд обязан попасть в то, до чего долетел первым, а не в то, что
        /// первым нашлось в списке.
        /// </summary>
        protected virtual int HandleCollisions()
        {
            HitWallThisTick = false;
            CollisionTarget = null;
            CollisionX = 0;
            CollisionY = 0;

            LogicTileMap map = Manager != null && Manager.BattleMode != null
                ? Manager.BattleMode.TileMap
                : null;

            if (map == null) return CollisionNone;

            int fromX = TickX;
            int fromY = TickY;

            int toX;
            int toY;
            GetLookAheadEnd(out toX, out toY);

            if (!IgnoreLevelBorder)
            {
                ClampToMap(map, ref fromX, ref fromY);
                ClampToMap(map, ref toX, ref toY);
            }

            int best = int.MaxValue;
            int result = CollisionNone;

            result = ScanTiles(map, fromX, fromY, toX, toY, ref best, result);
            result = ScanObjects(fromX, fromY, toX, toY, ref best, result);

            return result;
        }

        /// <summary>
        /// Конец отрезка проверки: текущая позиция плюс двадцать единиц вдоль
        /// направления шага. Нулевой шаг оставляет отрезок вырожденным.
        /// </summary>
        private void GetLookAheadEnd(out int endX, out int endY)
        {
            int stepX = X - TickX;
            int stepY = Y - TickY;

            int length = LogicMath.Sqrt(stepX * stepX + stepY * stepY);

            if (length != 0)
            {
                stepX = CollisionLookAhead * stepX / length;
                stepY = CollisionLookAhead * stepY / length;
            }

            endX = X + stepX;
            endY = Y + stepY;
        }

        private static void ClampToMap(LogicTileMap map, ref int x, ref int y)
        {
            int limitX = map.LogicWidth + 1;
            int limitY = map.LogicHeight + 1;

            if (x < 1) x = 1;
            else if (x > limitX) x = limitX;

            if (y < 1) y = 1;
            else if (y > limitY) y = limitY;
        }

        /// <summary>Радиус снаряда с учётом его текущего масштаба.</summary>
        public int GetCollisionRadius()
        {
            int radius = ProjectileData != null ? ProjectileData.Radius : 0;
            return GetModifiedScale() * radius / 100;
        }

        /// <summary>Поля +1352 и +1356. Переопределение масштаба, ноль означает «не задано».</summary>
        public int ScaleOverrideStart;
        public int ScaleOverrideEnd;

        /// <summary>
        /// 0x10047d5f8. Масштаб снаряда в процентах на текущем прогрессе полёта.
        ///
        /// Масштаб плавно идёт от начального к конечному по ходу полёта. Если оба
        /// равны сотне, оригинал коротит расчёт и сразу отдаёт сотню — иначе на
        /// целочисленном делении получилось бы 99 в середине пути.
        /// Сверху накладывается трейт AddBulletSizePercent.
        /// </summary>
        protected virtual int GetModifiedScale()
        {
            int scale;

            if (ScaleOverrideStart >= 1 && ScaleOverrideEnd >= 1)
            {
                scale = ScaleOverrideEnd * SpeedProgress / 1000
                      + ScaleOverrideStart * (1000 - SpeedProgress) / 1000;
            }
            else
            {
                // В таблице ноль означает «сто процентов», это учитывают
                // готовые геттеры данных.
                int start = ProjectileData != null ? ProjectileData.GetScaleStart() : 100;
                int end = ProjectileData != null ? ProjectileData.GetScaleEnd() : 100;

                if (start == 100 && end == 100) scale = 100;
                else scale = end * SpeedProgress / 1000 + start * (1000 - SpeedProgress) / 1000;
            }

            int bulletSize;
            if (Traits.TryGetValue(LogicTraitType.AddBulletSizePercent, out bulletSize))
                return (bulletSize + 100) * scale / 100;

            return scale;
        }

        /// <summary>
        /// Перебор клеток вокруг отрезка. Ширина полосы берётся по радиусу снаряда,
        /// переведённому в клетки с запасом в одну.
        /// </summary>
        private int ScanTiles(
            LogicTileMap map, int fromX, int fromY, int toX, int toY, ref int best, int result)
        {
            int margin = (GetCollisionRadius() + LogicTile.TileSize / 2) / LogicTile.TileSize + 1;

            int fromTileX = LogicTileMap.LogicToTile(fromX);
            int fromTileY = LogicTileMap.LogicToTile(fromY);
            int toTileX = LogicTileMap.LogicToTile(toX);
            int toTileY = LogicTileMap.LogicToTile(toY);

            int minTileX = (fromTileX < toTileX ? fromTileX : toTileX) - margin;
            int maxTileX = (fromTileX > toTileX ? fromTileX : toTileX) + margin;
            int minTileY = (fromTileY < toTileY ? fromTileY : toTileY) - margin;
            int maxTileY = (fromTileY > toTileY ? fromTileY : toTileY) + margin;

            bool pierces = PiercesEnvironment();

            for (int tileX = minTileX; tileX <= maxTileX; tileX++)
            {
                for (int tileY = minTileY; tileY <= maxTileY; tileY++)
                {
                    LogicTile tile = map.GetTile(tileX, tileY);
                    if (tile == null) continue;

                    TileData data = tile.Data;
                    if (data == null || !data.BlocksProjectiles) continue;
                    if (tile.Destroyed) continue;

                    int hitX;
                    int hitY;
                    RectangleSide side;

                    if (!IsTileIntersection(map, tileX, tileY, data, fromX, fromY, toX, toY,
                                            out hitX, out hitY, out side))
                        continue;

                    HitWallThisTick = true;

                    // Пробивающий снаряд отмечает стену, но не останавливается на ней.
                    if (pierces) continue;

                    int distance = (hitX - fromX) * (hitX - fromX) + (hitY - fromY) * (hitY - fromY);
                    if (distance >= best) continue;

                    best = distance;
                    result = CollisionTile;
                    CollisionTarget = null;
                    CollisionX = hitX;
                    CollisionY = hitY;
                }
            }

            return result;
        }

        /// <summary>
        /// Перебор персонажей. Сначала дешёвая отсечка по осям, потом точная
        /// проверка расстояния от центра цели до отрезка полёта, и только если
        /// центр вне досягаемости — пересечение отрезка с окружностью цели.
        /// </summary>
        private int ScanObjects(int fromX, int fromY, int toX, int toY, ref int best, int result)
        {
            if (Manager == null) return result;

            int radius = GetCollisionRadius();

            for (int i = 0; i < Manager.Characters.Count; i++)
            {
                LogicCharacterServer target = Manager.Characters[i] as LogicCharacterServer;
                if (target == null) continue;
                if (!CanHit(target)) continue;

                int reach = radius + target.GetRadius();

                int dx = target.X - X;
                int dy = target.Y - Y;

                int absX = dx >= 0 ? dx : -dx;
                int absY = dy >= 0 ? dy : -dy;

                if (absX > reach || absY > reach) continue;

                int hitX;
                int hitY;

                if (LogicMath.GetDistanceSquareToLine(fromX, fromY, toX, toY, target.X, target.Y)
                    <= reach * reach)
                {
                    // Цель уже накрыта отрезком — точкой попадания считается его начало.
                    hitX = fromX;
                    hitY = fromY;
                }
                else if (!LogicGamePlayUtil.LineSegmentIntersectsCircle(
                             fromX, fromY, toX, toY, target.X, target.Y, reach, out hitX, out hitY))
                {
                    continue;
                }

                int distance = (hitX - fromX) * (hitX - fromX) + (hitY - fromY) * (hitY - fromY);
                if (distance >= best) continue;

                best = distance;
                result = CollisionObject;
                CollisionTarget = target;
                CollisionX = hitX;
                CollisionY = hitY;
            }

            return result;
        }

        /// <summary>
        /// Может ли снаряд задеть эту цель. Проверяются измерение, жизнь, команда
        /// и список уже задетых целей.
        /// </summary>
        protected virtual bool CanHit(LogicCharacterServer target)
        {
            if (target.DimensionIndex != DimensionIndex) return false;
            if (!target.IsAlive()) return false;
            if (target.TeamIndex == TeamIndex) return false;

            return true;
        }

        /// <summary>
        /// Пробивает ли снаряд стены. Колонка PiercesEnvironment, но трейт
        /// OverridePierceEnvironment может её перебить.
        /// </summary>
        protected virtual bool PiercesEnvironment()
        {
            int traitValue;
            if (Traits.TryGetValue(LogicTraitType.OverridePierceEnvironment, out traitValue))
                return traitValue == 1;

            return ProjectileData != null && ProjectileData.PiercesEnvironment;
        }

        /// <summary>
        /// 0x10047d730. Пересекает ли отрезок полёта прямоугольник клетки.
        ///
        /// Прямоугольник строится с отступом CollisionMargin внутрь клетки, но с той
        /// стороны, где соседняя клетка тоже держит снаряды, отступ снимается.
        /// Из-за этого сплошная стена работает как одна поверхность, а не как ряд
        /// отдельных коробок со щелями между ними. Клетка за краем карты считается
        /// держащей снаряды — иначе стена у границы получила бы щель наружу.
        /// </summary>
        public static bool IsTileIntersection(
            LogicTileMap map, int tileX, int tileY, TileData tileData,
            int fromX, int fromY, int toX, int toY,
            out int hitX, out int hitY, out RectangleSide side)
        {
            int margin = tileData != null ? tileData.CollisionMargin : 0;

            int tileLeft = LogicTile.TileSize * tileX;
            int tileTop = LogicTile.TileSize * tileY;

            int left = margin + tileLeft;
            int top = margin + tileTop;
            int right = tileLeft - margin + LogicTile.TileSize;
            int bottom = tileTop - margin + LogicTile.TileSize;

            int grow = margin + 1;

            if (NeighbourBlocksProjectiles(map, tileX - 1, tileY)) left = tileLeft - 1;
            if (NeighbourBlocksProjectiles(map, tileX + 1, tileY)) right += grow;
            if (NeighbourBlocksProjectiles(map, tileX, tileY - 1)) top = tileTop - 1;
            if (NeighbourBlocksProjectiles(map, tileX, tileY + 1)) bottom += grow;

            side = LogicGamePlayUtil.LineSegmentIntersectsRectangle(
                fromX, fromY, toX, toY, left, top, right, bottom, out hitX, out hitY);

            return side != RectangleSide.None;
        }

        private static bool NeighbourBlocksProjectiles(LogicTileMap map, int tileX, int tileY)
        {
            LogicTile tile = map != null ? map.GetTile(tileX, tileY) : null;

            // Оригинал считает отсутствующую клетку держащей снаряды.
            if (tile == null) return true;

            return tile.Data != null && tile.Data.BlocksProjectiles;
        }
    }
}
