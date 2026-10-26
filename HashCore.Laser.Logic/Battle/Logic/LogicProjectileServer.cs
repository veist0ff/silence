namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    /// <summary>Чем закончился шаг снаряда. Коды из LogicProjectileServer::tick.</summary>
    public enum ProjectileEndReason
    {
        /// <summary>Столкновений не было.</summary>
        None = 1,

        /// <summary>Долетел до конца пути, цели не было.</summary>
        RangeEnd = 2,

        /// <summary>Врезался в стену.</summary>
        Tile = 3,

        /// <summary>Попал в цель.</summary>
        Target = 4,
    }

    /// <summary>
    /// Снаряд, 1:1 с LogicProjectileServer.
    ///
    /// Смещения оригинала:
    ///   +116 снаряд остановлен
    ///   +192 сколько тиков он уже стоит
    ///   +224 помечен на снятие
    ///   +228 X текущего тика
    ///   +232 Y текущего тика
    ///   +236 X прошлого тика
    ///   +240 Y прошлого тика
    ///   +276 тик, на котором снаряд должен долететь
    ///   +284 цель, -1 если её нет
    ///   +292 прогресс разгона, 0..1000
    ///   +296 прибавка прогресса за тик
    ///   +332 список откатов попадания по целям
    ///   +852 текущий угол
    ///   +864 событие тика
    ///   +912 радиус вращения для кружащих снарядов
    /// </summary>
    public partial class LogicProjectileServer : LogicGameObjectServer
    {
        /// <summary>Потолок прогресса разгона из tick.</summary>
        public const int MaxSpeedProgress = 1000;

        /// <summary>Сколько тиков остановленный снаряд живёт до снятия. shouldDestruct: строго больше пяти.</summary>
        public const int StoppedTicksBeforeDestruct = 5;

        /// <summary>
        /// Поле +116. Причина, по которой полёт закончился; ноль означает, что
        /// снаряд ещё летит. Это не просто признак остановки: targetReached
        /// записывает сюда именно код причины и по нему решает, что делать дальше.
        /// </summary>
        public int EndReason;

        /// <summary>Снаряд уже остановлен.</summary>
        public bool Stopped
        {
            get { return EndReason != 0; }
        }

        /// <summary>Поле +192.</summary>
        public int StoppedTicks;

        /// <summary>Поле +224.</summary>
        public bool MarkedForDestruct;

        /// <summary>Поля +228 и +232.</summary>
        public int TickX;
        public int TickY;

        /// <summary>Поля +236 и +240.</summary>
        public int PrevTickX;
        public int PrevTickY;

        /// <summary>Поле +276. Ноль означает «время долёта не задано».</summary>
        public int TargetReachTick;

        /// <summary>Поле +284.</summary>
        public int TargetGlobalId = -1;

        /// <summary>Поле +292.</summary>
        public int SpeedProgress;

        /// <summary>Поле +296.</summary>
        public int SpeedProgressPerTick;

        /// <summary>Поле +852.</summary>
        public int Angle;

        /// <summary>Поле +864. Событие тика, чистится в начале кадра снаряда.</summary>
        public bool HitEventThisTick;

        /// <summary>
        /// Поле +332. Откаты повторного попадания по каждой цели: снаряд не бьёт
        /// одну и ту же цель каждый тик.
        /// </summary>
        public readonly List<int> TargetHitCooldowns = new List<int>();

        /// <summary>Снаряд привязан к объекту, поле +208.</summary>
        public LogicGameObjectServer LinkedObject;

        public ProjectileData ProjectileData;

        /// <summary>
        /// Колонка IgnoreLevelBorder. Читается из строки, а не из свойства данных:
        /// свойство там называется IgnoreLevelBoarder и с колонкой не сходится.
        /// </summary>
        public bool IgnoreLevelBorder;

        /// <summary>
        /// Колонка BoomerangType. Ноль — не бумеранг. В isBoomerang (0x100427330)
        /// проверка ровно такая: значение больше нуля.
        /// </summary>
        public int BoomerangType;

        public LogicProjectileServer(ProjectileData data) : base(data)
        {
            ProjectileData = data;

            IgnoreLevelBorder = LogicDataAccess.GetBool(data, "IgnoreLevelBorder");
            BoomerangType = LogicDataAccess.GetInt(data, "BoomerangType");
            EarlyTicks = LogicDataAccess.GetInt(data, "EarlyTicks");
        }

        public override LogicGameObjectType GetGameObjectType()
        {
            return LogicGameObjectType.Projectile;
        }

        /// <summary>
        /// 0x10047df1c. Снаряд снимается либо когда помечен и ни к чему не привязан,
        /// либо когда стоит на месте дольше пяти тиков.
        /// </summary>
        public override bool ShouldDestruct()
        {
            bool marked = LinkedObject == null && MarkedForDestruct;
            bool stuck = EndReason != 0 && StoppedTicks > StoppedTicksBeforeDestruct;
            return marked || stuck;
        }

        /// <summary>
        /// 0x1004778f4. Кадр снаряда.
        ///
        /// Остановленный снаряд не двигается и не проверяет столкновения — он только
        /// копит счётчик простоя, по которому его потом снимут. Обрезка по границе
        /// карты идёт в самом конце и для остановленного снаряда тоже.
        /// </summary>
        public override void Tick()
        {
            HitEventThisTick = false;

            TickEffects();

            if (Stopped)
            {
                StoppedTicks++;
                ClampToLevelBorder();
                return;
            }

            PrevTickX = TickX;
            PrevTickY = TickY;
            TickX = X;
            TickY = Y;

            ProcessSpeedChange();

            bool reachedEnd = TickMovement();

            // Разгон копится каждый тик и упирается в 1000.
            SpeedProgress += SpeedProgressPerTick;
            if (SpeedProgress >= MaxSpeedProgress) SpeedProgress = MaxSpeedProgress;

            ProjectileEndReason reason;
            bool noCollision;

            int collision = ShouldCheckCollision() ? HandleCollisions() : 0;

            if (collision != 0)
            {
                reason = collision == 2
                    ? ProjectileEndReason.Tile
                    : (TargetGlobalId == -1 ? ProjectileEndReason.RangeEnd : ProjectileEndReason.Target);
                noCollision = false;
                reachedEnd = true;
            }
            else
            {
                reason = ProjectileEndReason.None;
                noCollision = true;
            }

            if (reachedEnd || !noCollision) TargetReached(reason);

            TickTargetHitCooldowns();
            ClampToLevelBorder();
        }

        /// <summary>Откаты уменьшаются на единицу за тик, исчерпанные снимаются.</summary>
        private void TickTargetHitCooldowns()
        {
            for (int i = TargetHitCooldowns.Count - 1; i >= 0; i--)
            {
                int left = TargetHitCooldowns[i] - 1;
                if (left <= 0) TargetHitCooldowns.RemoveAt(i);
                else TargetHitCooldowns[i] = left;
            }
        }

        /// <summary>
        /// Хвост tick. Снаряд, которому не разрешено вылетать за карту, зажимается
        /// в границах: от единицы до размера карты минус два.
        /// </summary>
        private void ClampToLevelBorder()
        {
            if (IgnoreLevelBorder) return;

            LogicTileMap map = Manager != null && Manager.BattleMode != null
                ? Manager.BattleMode.TileMap
                : null;

            if (map == null) return;

            X = Clamp(X, map.LogicWidth - 2);
            Y = Clamp(Y, map.LogicHeight - 2);
        }

        private static int Clamp(int value, int max)
        {
            if (value <= 1) return 1;
            return value < max ? value : max;
        }

        /// <summary>Слот +120, 0x10047c2f8.</summary>
        protected virtual void TickEffects()
        {
        }

        /// <summary>0x100478018.</summary>
        protected virtual bool ShouldCheckCollision()
        {
            return true;
        }

    }
}
