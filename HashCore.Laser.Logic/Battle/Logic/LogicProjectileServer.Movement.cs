namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Движение снаряда: скорость, пересчёт времени полёта и траектория.
    ///
    /// Все смещения полей данных снаряда взяты из LogicProjectileData::
    /// createReferences и сверены с именами колонок из resetStatics:
    ///   +160 Indirect, +168 Gravity, +180 Speed, +196 ConstantFlyTime,
    ///   +464 TravelType, +468 TravelTypeVariable.
    /// Кривизна траектории, которую декомпилятор показывает безымянным вызовом, —
    /// это колонка BouncePercent.
    /// </summary>
    public partial class LogicProjectileServer
    {
        /// <summary>Тип движения «привязан к источнику». Значение колонки TravelType.</summary>
        public const int TravelTypeAttached = 8;

        /// <summary>Тип движения со сдвигом времени старта на TravelTypeVariable.</summary>
        public const int TravelTypeTimeShifted = 11;

        /// <summary>Тип движения, который не переставляет позицию сам.</summary>
        public const int TravelTypeNoReposition = 12;

        /// <summary>Тип движения, который едет только вперёд по своему направлению.</summary>
        public const int TravelTypeForwardOnly = 1;

        /// <summary>Потолок высоты из хвоста getPosAtTick.</summary>
        public const int MaxZ = 3000;

        /// <summary>Поле +32. Тик, на котором снаряд стартовал.</summary>
        public int StartTick;

        /// <summary>Поле +36. Тик, на котором снаряд долетает.</summary>
        public int EndTick;

        /// <summary>Поля +148 и +152.</summary>
        public int StartX;
        public int StartY;

        /// <summary>Поля +156 и +160.</summary>
        public int EndX;
        public int EndY;

        /// <summary>Поле +180. Высота снаряда.</summary>
        public int Z;

        /// <summary>Поле +300. Базовая скорость, взятая из данных при запуске.</summary>
        public int BaseSpeed;

        /// <summary>Поле +304. Во сколько тысячных текущая скорость отличается от прошлой.</summary>
        public int SpeedScale = 1000;

        /// <summary>Поле +308. Скорость на прошлом тике.</summary>
        public int LastSpeed;

        /// <summary>Поле +860. Сдвиг тиков, с которым снаряд считает своё время.</summary>
        public int TickOffset;

        /// <summary>Поле +988. Переопределение TravelTypeVariable, ноль — берём из данных.</summary>
        public int TravelTypeVariableOverride;

        /// <summary>
        /// Кривая изменения скорости. Оригинал (0x10047e5b0) переключает её глобальным
        /// значением, которое в обычной сборке равно нулю, — тогда скорость постоянна.
        /// Ветки оставлены, чтобы поведение совпадало, если значение выставят.
        /// </summary>
        public static int SpeedCurveMode;

        /// <summary>0x10047e5b0.</summary>
        public int GetCurrentSpeed()
        {
            int speed = BaseSpeed;

            switch (SpeedCurveMode)
            {
                case 1:
                    return SpeedProgress > 500 ? speed << 1 : speed;

                case 2:
                    return SpeedProgress >= 501 ? speed / 4 : speed;

                case 3:
                    if (SpeedProgress >= 2) return SpeedProgress * speed / 1000;
                    break;
            }

            return speed;
        }

        /// <summary>
        /// 0x100477cec. Когда скорость изменилась, весь оставшийся полёт
        /// пересчитывается: время старта и время долёта растягиваются или сжимаются
        /// вокруг текущего тика, а не обнуляются. Поэтому ускоренный на середине
        /// снаряд не прыгает, а плавно меняет темп.
        /// </summary>
        protected virtual void ProcessSpeedChange()
        {
            if (ProjectileData != null && ProjectileData.TravelType == TravelTypeAttached) return;

            int speed = GetCurrentSpeed();
            if (LastSpeed == speed) return;

            if (LastSpeed == 0)
            {
                LastSpeed = speed;
                return;
            }

            int now = GetProjectileTick();
            int remaining = EndTick - now;
            int elapsed = now - StartTick;

            int scale = 1000 * speed / LastSpeed;
            if (scale >= 1000000) scale = 1000000;
            if (scale <= 10) scale = 10;
            SpeedScale = scale;

            int newEnd = 1000 * remaining / scale + now;
            int newStart = now - 1000 * elapsed / scale;

            StartTick = newStart;
            EndTick = newEnd;

            if (newEnd <= newStart) EndTick = newStart + 1;

            SpeedProgressPerTick = 1000 / (EndTick - StartTick);

            if (TargetReachTick >= 1)
                TargetReachTick = 1000 * (TargetReachTick - now) / scale + now;

            LastSpeed = speed;
        }

        /// <summary>Собственное время снаряда: тик боя плюс его сдвиг.</summary>
        public int GetProjectileTick()
        {
            int battleTick = Manager != null && Manager.BattleMode != null ? Manager.BattleMode.Tick : 0;
            return TickOffset + battleTick;
        }

        /// <summary>
        /// 0x100477de0. Возвращает true, когда полёт на этом тике закончился.
        ///
        /// Привязанный снаряд живёт, пока жив источник и пока они в одном измерении.
        /// Непрямой снаряд, вышедший за край карты, дожимается внутрь и считается
        /// долетевшим. Самонаводящийся снаряд, чья цель стала неуязвимой и
        /// прозрачной для пуль, продлевает себе полёт на тик.
        /// </summary>
        protected virtual bool TickMovement()
        {
            ProjectileData data = ProjectileData;
            int travelType = data != null ? data.TravelType : 0;

            if (travelType == TravelTypeAttached)
            {
                if (LinkedObject == null) return true;
                if (!LinkedObject.IsAlive()) return true;
                if (LinkedObject.DimensionIndex != DimensionIndex) return true;
            }

            int now = GetProjectileTick();
            if (now > EndTick) return true;

            int z;

            if (GetSteerStrengthThatCanBeOverridden() >= 1 || travelType == TravelTypeAttached)
                z = Z;
            else
                z = GetPosAtTick(now, out PositionX, out PositionY);

            LogicTileMap map = Manager != null && Manager.BattleMode != null
                ? Manager.BattleMode.TileMap
                : null;

            if (data != null && data.Indirect && map != null)
            {
                int limitX = map.LogicWidth - 2;
                int limitY = map.LogicHeight - 2;

                if (PositionX < 2 || PositionX >= limitX || PositionY < 2 || PositionY >= limitY)
                {
                    int clampedX = PositionX < limitX ? PositionX : limitX;
                    if (PositionX <= 1) clampedX = 1;

                    int clampedY = PositionY < limitY ? PositionY : limitY;
                    if (PositionY <= 1) clampedY = 1;

                    SetPosition(clampedX, clampedY);
                    Z = z;
                    return true;
                }
            }

            if (travelType == TravelTypeForwardOnly)
            {
                // Снаряд этого типа не пятится: позиция обновляется, только если шаг
                // идёт вдоль его собственного направления.
                int stepX = PositionX - X;
                int stepY = PositionY - Y;
                int dirX = EndX - StartX;
                int dirY = EndY - StartY;

                if (dirX * stepX + dirY * stepY > 0)
                {
                    SetPosition(PositionX, PositionY);
                    Z = z;
                }
            }
            else if (travelType == TravelTypeNoReposition)
            {
                if (LinkedObject == null) return true;
            }
            else
            {
                SetPosition(PositionX, PositionY);
                Z = z;
            }

            if (now == EndTick)
            {
                if (data != null && data.IsHomingMissile
                    && LinkedObject != null
                    && IsTargetImmuneAndBulletsGoThrough())
                {
                    EndTick++;
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>Позиция, посчитанная на этом тике. Поля +120 и +124 оригинала.</summary>
        public int PositionX;
        public int PositionY;

        /// <summary>0x10047c3c4.</summary>
        protected virtual int GetSteerStrengthThatCanBeOverridden()
        {
            return ProjectileData != null ? ProjectileData.SteerStrength : 0;
        }

        /// <summary>Цель неуязвима и пули проходят насквозь.</summary>
        protected virtual bool IsTargetImmuneAndBulletsGoThrough()
        {
            return false;
        }

        /// <summary>
        /// 0x10047c4a8. Положение снаряда на заданном тике и его высота.
        ///
        /// Прямой снаряд просто линейно идёт от старта к концу. Снаряд с ненулевым
        /// BouncePercent летит дальше цели на часть пути, а потом возвращается —
        /// это бумеранг. Поверх этого Gravity добавляет параболу по высоте.
        ///
        /// Все деления на 1000 в оригинале сделаны умножением на 274877907 со
        /// сдвигом на 38 — это ровно деление на тысячу, тут оно записано прямо.
        /// </summary>
        public int GetPosAtTick(int tick, out int outX, out int outY)
        {
            ProjectileData data = ProjectileData;

            int deltaX = EndX - StartX;
            int deltaY = EndY - StartY;
            int distanceSquare = deltaX * deltaX + deltaY * deltaY;

            int distance = LogicMath.Sqrt(distanceSquare);
            if (distance <= 1) distance = 1;

            int gravity = data != null ? data.Gravity : 0;
            int bouncePercent = data != null ? data.BouncePercent : 0;

            int x;
            int y;
            int z;

            if (EndTick >= tick)
            {
                int total = EndTick - StartTick;
                int elapsed = tick - StartTick;

                if (bouncePercent < 1)
                {
                    if (data != null && data.TravelType == TravelTypeTimeShifted)
                    {
                        int variable = TravelTypeVariableOverride != 0
                            ? TravelTypeVariableOverride
                            : data.TravelTypeVariable;

                        int shift = -(variable * SpeedScale) / 1000;

                        elapsed += shift;
                        if (elapsed < 0) elapsed = 0;

                        total += shift;
                        if (total <= 1) total = 1;
                    }

                    x = (total - elapsed) * StartX / total + elapsed * EndX / total;
                    y = (total - elapsed) * StartY / total + elapsed * EndY / total;

                    z = Z;
                    if (gravity != 0) z = z * (total - elapsed) / total;
                }
                else
                {
                    int progress = 1000 * elapsed / total;
                    int backPart = 10 * bouncePercent;
                    int forwardPart = 1000 - backPart;

                    // Точка, до которой снаряд перелетает цель.
                    int overshoot = backPart * distance;
                    int farX = EndX;
                    int farY = EndY;

                    if (overshoot + 1499 >= 3000)
                    {
                        int step = overshoot / 1500;
                        farX += step * deltaX / distance;
                        farY += step * deltaY / distance;
                    }

                    if (progress <= forwardPart)
                    {
                        x = ((1000 - progress) * StartX + farX * progress) / 1000;
                        y = ((1000 - progress) * StartY + farY * progress) / 1000;

                        int forwardTicks = forwardPart * total / 1000;
                        if (forwardTicks <= 1) forwardTicks = 1;

                        z = (1000 - 1000 * elapsed / forwardTicks) * Z / 1000;
                    }
                    else
                    {
                        z = 0;

                        int back = progress - forwardPart;

                        int turnX = (backPart * StartX + farX * forwardPart) / 1000;
                        int turnY = (backPart * StartY + farY * forwardPart) / 1000;

                        int pullX = (farX - StartX) * forwardPart / 1000 + StartX
                                  + (farX - StartX) * backPart / 2000;
                        int pullY = (farY - StartY) * forwardPart / 1000 + StartY
                                  + (farY - StartY) * backPart / 2000;

                        x = (turnX * (backPart - back) + pullX * back) / backPart;
                        y = (turnY * (backPart - back) + pullY * back) / backPart;
                    }
                }
            }
            else
            {
                x = EndX;
                y = EndY;
                z = gravity != 0 ? 0 : Z;
            }

            outX = x;
            outY = y;

            if (gravity != 0) z += 300 * GetGravityHeight(x, y, deltaX, deltaY, distance, distanceSquare) / 1000;

            if (z >= MaxZ) z = MaxZ;
            return z < 0 ? 0 : z;
        }

        /// <summary>
        /// Параболическая добавка по высоте из хвоста getPosAtTick.
        ///
        /// Наклон берётся из Gravity: при ConstantFlyTime он идёт как есть, иначе
        /// делится на корень из длины полёта — тогда дальний бросок получается
        /// настолько же высоким, а не круче. Время считается не в тиках, а в долях
        /// пройденного расстояния, поделённого на скорость.
        /// </summary>
        private int GetGravityHeight(
            int x, int y, int deltaX, int deltaY, int distance, int distanceSquare)
        {
            ProjectileData data = ProjectileData;

            int gravity = data.Gravity;
            int slope = data.ConstantFlyTime
                ? -gravity
                : -1000 * gravity / (40 * LogicMath.Sqrt(distance));

            int speedUnits = 100 * data.Speed;
            if (speedUnits <= 100) speedUnits = 100;

            int bouncePercent = data.BouncePercent;

            int elapsedUnits;
            int totalUnits;

            if (bouncePercent < 1)
            {
                int totalDistance = LogicMath.GetDistance(StartX, StartY, EndX, EndY);

                totalUnits = (int)(1000000L * totalDistance / speedUnits);
                if (totalUnits <= 100) totalUnits = 100;

                elapsedUnits = (int)(1000000L * LogicMath.GetDistance(StartX, StartY, x, y) / speedUnits);

                if (data.ConstantFlyTime)
                {
                    // Постоянное время полёта: парабола строится по длине броска,
                    // а не по времени, поэтому коэффициент делится на квадрат длины.
                    int k = totalDistance != 0
                        ? (int)(2000000L * slope / ((long)totalDistance * totalDistance))
                        : 0;

                    int start = -(totalUnits * k) / 1000;
                    return start * elapsedUnits / 1000 + k * (elapsedUnits * elapsedUnits / 1000) / 1000;
                }
            }
            else
            {
                int straight = LogicMath.Sqrt(distanceSquare);
                int pull = straight * bouncePercent;

                int backX = 0;
                int backY = 0;

                if (pull + 149 >= 299)
                {
                    int step = pull / 150;
                    backX = step * deltaX / straight;
                    backY = step * deltaY / straight;
                }

                int apexX = EndX - backX;
                int apexY = EndY - backY;

                int fromX;
                int fromY;
                int segment;

                if ((x - apexX) * backX + (y - apexY) * backY < 0)
                {
                    segment = LogicMath.GetDistance(StartX, StartY, apexX, apexY);
                    fromX = StartX;
                    fromY = StartY;
                }
                else
                {
                    segment = LogicMath.GetDistance(apexX, apexY, EndX, EndY);
                    fromX = apexX;
                    fromY = apexY;
                }

                totalUnits = (int)(1000000L * segment / speedUnits);
                if (totalUnits <= 100) totalUnits = 100;

                elapsedUnits = (int)(1000000L * LogicMath.GetDistance(fromX, fromY, x, y) / speedUnits);
            }

            int speedAtStart = -(slope * totalUnits);
            return elapsedUnits * elapsedUnits / 1000 * slope / 1000
                 + speedAtStart / 1000 * elapsedUnits / 1000;
        }
    }
}
