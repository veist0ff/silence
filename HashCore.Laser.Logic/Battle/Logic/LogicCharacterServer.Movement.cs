namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Движение персонажа: направление, безопасная установка позиции и приказ идти.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Длина, к которой приводится вектор направления. Из updateMoveDirection.</summary>
        public const int MoveDirectionLength = 256;

        /// <summary>Режим игры, в котором moveTo обходит зону особого противника.</summary>
        public const int DodgeZoneGameMode = 11;

        /// <summary>Особое свойство того самого противника, чью зону обходят.</summary>
        public const int DodgeZoneCharacterProperty = 6;

        /// <summary>Поля +380 и +384. Направление движения длиной 256.</summary>
        public LogicVector2 MoveDirection;

        /// <summary>Поле +352. Угол, куда персонаж повёрнут. Конвенция atan2(y, x).</summary>
        public int FacingAngle;

        /// <summary>Поле +360. Угол прицела. Это FacingAngle со сдвигом на девяносто.</summary>
        public int AimAngle;

        /// <summary>Поля +32 и +36. Тики начала и конца текущего перехода.</summary>
        public int MoveStartTick;
        public int MoveEndTick;

        /// <summary>Поле +572. Длина найденного пути в логических единицах.</summary>
        public int PathLength;

        /// <summary>Поля +689 и +691. Персонаж повёрнут спиной к направлению движения.</summary>
        public bool ReversedFacingA;
        public bool ReversedFacingB;

        /// <summary>Поле +665.</summary>
        public bool DodgeZoneEnabled;

        /// <summary>
        /// 0x10045e0a4. Пересчитывает направление и угол поворота.
        ///
        /// Из двух заданных точек берётся ДАЛЬНЯЯ, и вектор строится от неё к
        /// персонажу, а не наоборот. Потом он приводится к длине 256 — так все
        /// дальнейшие расчёты идут в одном масштабе независимо от расстояния.
        /// </summary>
        public int UpdateMoveDirection(int fromX, int fromY, int firstX, int firstY, int secondX, int secondY)
        {
            LogicVector2 position = new LogicVector2(fromX, fromY);

            int toFirst = LogicVector2.GetDistanceSquared(position, firstX, firstY);
            int toSecond = LogicVector2.GetDistanceSquared(position, secondX, secondY);

            MoveDirection.Set(0, 0);

            if (toFirst >= toSecond) MoveDirection.Set(fromX - firstX, fromY - firstY);
            else MoveDirection.Set(fromX - secondX, fromY - secondY);

            MoveDirection.Normalize(MoveDirectionLength);

            int angle = LogicMath.GetAngle(MoveDirection.X, MoveDirection.Y);
            FacingAngle = angle;

            // Персонаж, который движется задом наперёд, смотрит в противоположную
            // сторону: оригинал прибавляет или отнимает ровно сто восемьдесят.
            if (ReversedFacingA && ReversedFacingB)
                FacingAngle = angle >= 180 ? angle - 180 : angle + 180;

            return angle;
        }

        /// <summary>
        /// 0x10045819c. Ставит позицию, не выпуская персонажа за край карты.
        ///
        /// Поезда исключение: их ведёт своя логика по рельсам, и зажимать их
        /// нельзя. Для остальных координата держится в пределах от единицы до
        /// размера карты минус два.
        /// </summary>
        public void SetCharacterPositionSafe(int x, int y)
        {
            LogicTileMap map = Manager != null && Manager.BattleMode != null
                ? Manager.BattleMode.TileMap
                : null;

            if (map == null || IsTrain())
            {
                SetPosition(x, y);
                return;
            }

            SetPosition(ClampToMap(x, map.LogicWidth), ClampToMap(y, map.LogicHeight));
            OnNewPositionSet(X, Y);
        }

        private static int ClampToMap(int value, int logicSize)
        {
            int limit = logicSize - 2;
            if (value < limit) limit = value;
            return value <= 1 ? 1 : limit;
        }

        /// <summary>
        /// 0x100458dcc. Приказ идти в точку.
        ///
        /// Персонаж, который проходит сквозь стены и по воде, идёт по прямой; всем
        /// остальным ищется путь. Длительность перехода считается как
        /// двадцать тиков на путь, делённые на скорость, и не может быть меньше
        /// одного тика — двадцать это число тиков в секунде, а скорость задана
        /// в логических единицах за секунду.
        /// </summary>
        public void MoveTo(int x, int y, bool hasSpeedOverride, int speedOverride, bool allowPartial, bool flag)
        {
            if (!AllowMovementControl()) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            LogicTileMap map = battle != null ? battle.TileMap : null;
            if (map == null) return;

            // Пока идёт умение, которое нельзя совмещать с движением, запоминается
            // точка старта: с неё персонажа вернут, если движение прервёт умение.
            if (IsCastingSkillThatBlocksMovement()) SetStartPosition();

            int targetX = ClampToMap(x, map.LogicWidth);
            int targetY = ClampToMap(y, map.LogicHeight);

            if (DodgeZoneEnabled && battle.GameMode == DodgeZoneGameMode)
                ClipDestinationOutOfDodgeZone(ref targetX, ref targetY);

            targetX = ClampToMap(targetX, map.LogicWidth);
            targetY = ClampToMap(targetY, map.LogicHeight);

            if (CanMoveThroughWalls() && CanWalkOnWater()) SetStraightPath(targetX, targetY);
            else FindPathTo(targetX, targetY, allowPartial, flag);

            PathLength = GetPathLength();
            if (PathLength < 1) return;

            int speed = hasSpeedOverride ? speedOverride : GetBaseSpeed();
            if (speed <= 1) speed = 1;

            int ticks = TicksPerSecond * PathLength / speed;
            if (ticks <= 1) ticks = 1;

            MoveStartTick = battle.Tick;
            MoveEndTick = MoveStartTick + ticks;
        }

        /// <summary>
        /// Обрезает цель движения по кругу особого противника: если отрезок до цели
        /// входит в круг, точкой назначения становится место входа в него.
        /// </summary>
        private void ClipDestinationOutOfDodgeZone(ref int targetX, ref int targetY)
        {
            if (Manager == null) return;

            LogicCharacterServer zone = null;

            for (int i = 0; i < Manager.Characters.Count; i++)
            {
                LogicCharacterServer other = Manager.Characters[i] as LogicCharacterServer;
                if (other == null || !other.IsAlive()) continue;
                if (other.TeamIndex == TeamIndex) continue;
                if (other.GetUniqueProperty() != DodgeZoneCharacterProperty) continue;

                zone = other;
            }

            if (zone == null) return;

            int radius = 100 * zone.GetTileRange() + LogicTile.TileSize;
            int radiusSquare = radius * radius;

            int dx = targetX - zone.X;
            int dy = targetY - zone.Y;

            if (dx * dx + dy * dy >= radiusSquare) return;

            int hitX;
            int hitY;

            if (LogicGamePlayUtil.LineSegmentIntersectsCircle(
                    X, Y, targetX, targetY, zone.X, zone.Y, LogicMath.Sqrt(radiusSquare),
                    out hitX, out hitY))
            {
                targetX = hitX;
                targetY = hitY;
            }
        }

        /// <summary>0x10045ea7c.</summary>
        protected virtual bool AllowMovementControl()
        {
            return true;
        }

        protected virtual bool IsTrain()
        {
            return false;
        }

        protected virtual bool IsCastingSkillThatBlocksMovement()
        {
            return false;
        }

        /// <summary>0x10045f1c0.</summary>
        protected virtual void SetStartPosition()
        {
        }

        /// <summary>0x10045f268.</summary>
        protected virtual bool CanMoveThroughWalls()
        {
            return false;
        }

        /// <summary>0x10045f2bc.</summary>
        protected virtual bool CanWalkOnWater()
        {
            return false;
        }

        /// <summary>0x10045f314.</summary>
        protected virtual void SetStraightPath(int x, int y)
        {
        }

        /// <summary>0x10045f40c.</summary>
        protected virtual void FindPathTo(int x, int y, bool allowPartial, bool flag)
        {
        }

        /// <summary>Длина текущего пути в логических единицах.</summary>
        protected virtual int GetPathLength()
        {
            return 0;
        }

        /// <summary>Базовая скорость персонажа из данных.</summary>
        protected virtual int GetBaseSpeed()
        {
            return 0;
        }

        protected virtual int GetTileRange()
        {
            return 0;
        }

        /// <summary>0x10045eb48.</summary>
        protected virtual void OnNewPositionSet(int x, int y)
        {
        }
    }
}
