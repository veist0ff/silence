namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Способ, которым боец раскладывает очередь автоатаки. Значения из
    /// createReferences таблицы бойцов, 0x1003f19c4.
    /// </summary>
    public static class LogicAutoAttackMode
    {
        /// <summary>Вся очередь уходит одним выстрелом.</summary>
        public const int Single = 0;

        /// <summary>Очередь растянута во времени: пули идут одна за другой.</summary>
        public const int Burst = 1;

        /// <summary>Вся очередь уходит разом веером.</summary>
        public const int Shotgun = 2;

        /// <summary>Пули расходятся кольцом вокруг цели.</summary>
        public const int AlwaysSpread = 4;

        /// <summary>
        /// Очередь во времени, но с упреждением: точка стрельбы сдвигается туда,
        /// куда цель успеет уйти.
        /// </summary>
        public const int BurstPredicting = 5;
    }

    /// <summary>
    /// Имена способов раскладки очереди из колонки AutoAttackMode. Разбор
    /// повторяет 0x1003f19c4: незнакомое имя — это одиночный выстрел.
    /// </summary>
    public static class LogicAutoAttackModeNames
    {
        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return LogicAutoAttackMode.Single;

            if (name == "Burst") return LogicAutoAttackMode.Burst;
            if (name == "Shotgun") return LogicAutoAttackMode.Shotgun;
            if (name == "AlwaysSpread") return LogicAutoAttackMode.AlwaysSpread;
            if (name == "BurstPredicting") return LogicAutoAttackMode.BurstPredicting;

            return LogicAutoAttackMode.Single;
        }
    }

    /// <summary>
    /// Автоатака, 0x1004528c8 и хвост attack.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Сколько тиков между пулями очереди, если в таблице не задано.</summary>
        public const int DefaultBurstDelayTicks = 3;

        /// <summary>Сколько градусов в круге. Кольцо автоатаки делится на это число.</summary>
        public const int FullCircleDegrees = 360;

        /// <summary>Поле +744. Кого боец бьёт.</summary>
        public LogicCharacterServer AutoAttackTarget;

        /// <summary>Поля +756 и +760. Куда боец целится.</summary>
        public int AutoAttackTargetX;
        public int AutoAttackTargetY;

        /// <summary>Поле +764. Цель в пределах досягаемости.</summary>
        public bool AutoAttackTargetInRange;

        /// <summary>Поле +1248. На каком тике вылетит следующая пуля очереди.</summary>
        public int BurstNextTick;

        /// <summary>Поле +1252. Сколько пуль очереди ещё не вылетело.</summary>
        public int BurstShotsLeft;

        /// <summary>Поля +1256 и +1260. Куда очередь досылает оставшиеся пули.</summary>
        public int BurstTargetX;
        public int BurstTargetY;

        /// <summary>Поле +1292. Прибавка к урону автоатаки.</summary>
        public int BonusAutoAttackDamage;

        /// <summary>Поле +1296. Способ раскладки очереди, заданный поверх таблицы.</summary>
        public int AutoAttackModeOverride;

        /// <summary>Поле +2140. Урон автоатаки идёт ещё и долей от здоровья цели.</summary>
        public bool AutoAttackDamagesByPercent;

        /// <summary>Снаряд автоатаки, найденный в таблице.</summary>
        public ProjectileData AutoAttackProjectile;

        /// <summary>0x100458d64. Способ раскладки очереди.</summary>
        public virtual int GetAutoAttackMode()
        {
            if (AutoAttackModeOverride > 0) return AutoAttackModeOverride;

            return GetAutoAttackModeFromData();
        }

        /// <summary>Способ раскладки, записанный в таблице бойцов.</summary>
        protected virtual int GetAutoAttackModeFromData()
        {
            return LogicAutoAttackMode.Single;
        }

        /// <summary>Сколько тиков между пулями очереди у этого бойца.</summary>
        protected virtual int GetBurstDelayTicks()
        {
            return 0;
        }

        /// <summary>Сколько пуль в одной очереди автоатаки.</summary>
        protected virtual int GetAutoAttackBulletCount()
        {
            return 1;
        }

        /// <summary>Разброс автоатаки.</summary>
        protected virtual int GetAutoAttackSpread()
        {
            return 0;
        }

        /// <summary>Дальность автоатаки в клетках.</summary>
        protected virtual int GetAutoAttackRange()
        {
            return 0;
        }

        /// <summary>Урон одной пули автоатаки, из таблицы.</summary>
        protected virtual int GetAutoAttackDamageFromData()
        {
            return 0;
        }

        /// <summary>
        /// 0x100452e84. Досылает пули очереди, начатой раньше.
        ///
        /// Очередь живёт сама по себе: как только она начата, пули уходят по
        /// расписанию, даже если боец отвернулся или цель уже умерла — стрельба
        /// идёт в запомненную точку, а не в цель.
        /// </summary>
        protected virtual void TickBurst()
        {
            if (BurstShotsLeft < 1) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            if (battle.Tick != BurstNextTick) return;

            int damage = GetAutoAttackDamageFromData() + BonusAutoAttackDamage;
            damage += GetTotalDamageBuffPercent() * damage / 100;

            LogicProjectileServer projectile = battle.ShootProjectile(
                LogicBattleModeServer.NoStartPosition,
                LogicBattleModeServer.NoStartPosition,
                this, this,
                AutoAttackProjectile,
                BurstTargetX, BurstTargetY,
                damage,
                GetAutoAttackDamageFromData(),
                GetAutoAttackSpread(),
                false,
                0,
                0,
                AttackKind,
                null);

            if (projectile != null) projectile.RunEarlyTicks();

            BurstShotsLeft--;

            int delay = GetBurstDelayTicks();
            if (delay <= 0) delay = DefaultBurstDelayTicks;

            BurstNextTick = delay + battle.Tick;

            // Стрельба с упреждением заново берёт прицел на каждой пуле, поэтому
            // накопленный доворот сбрасывается.
            if (GetAutoAttackMode() == LogicAutoAttackMode.BurstPredicting) AimAngle = 0;
        }

        /// <summary>
        /// 0x1004528c8. Обычная автоатака по выбранной цели.
        ///
        /// Урон берётся из таблицы, к нему добавляется прибавка бойца и усиление,
        /// а дальше всё уходит в общую атаку: она сама разложит очередь по способу,
        /// который у бойца задан.
        /// </summary>
        public virtual LogicProjectileServer AutoAttack()
        {
            int baseDamage = BonusAutoAttackDamage + GetAutoAttackDamageFromData();
            int damage = GetTotalDamageBuffPercent() * baseDamage / 100 + baseDamage;

            int percentDamage = AutoAttackDamagesByPercent ? GetAutoAttackDamageFromData() : 0;

            return Attack(
                AutoAttackTarget,
                AutoAttackTargetX, AutoAttackTargetY,
                damage, percentDamage,
                AutoAttackProjectile,
                GetAutoAttackSpread(),
                GetAutoAttackBulletCount(),
                GetAutoAttackMode());
        }

        /// <summary>
        /// Хвост attack, 0x10045ce44. Что делать с остальными пулями очереди.
        ///
        /// Веером они уходят все разом, очередью — по одной в следующие тики,
        /// кольцом — вокруг цели на расстоянии дальности автоатаки. У одиночного
        /// способа остальных пуль просто нет.
        /// </summary>
        protected virtual void FinishAttackBurst(
            ProjectileData data,
            int targetX, int targetY,
            int damage, int percentDamage,
            int spread, int bulletCount, int mode)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            if (mode == LogicAutoAttackMode.Shotgun)
            {
                for (int i = 1; i < bulletCount; i++)
                {
                    int side = (i & 1) != 0 ? -spread : spread;

                    LogicProjectileServer pellet = battle.ShootProjectile(
                        LogicBattleModeServer.NoStartPosition,
                        LogicBattleModeServer.NoStartPosition,
                        this, this,
                        data,
                        targetX, targetY,
                        damage, percentDamage,
                        side, true,
                        0, 0,
                        AttackKind,
                        null);

                    if (pellet != null) pellet.RunEarlyTicks();
                }

                return;
            }

            if (mode == LogicAutoAttackMode.Burst || mode == LogicAutoAttackMode.BurstPredicting)
            {
                int delay = GetBurstDelayTicks();
                if (delay <= 0) delay = DefaultBurstDelayTicks;

                BurstNextTick = delay + battle.Tick;
                BurstShotsLeft = bulletCount - 1;

                BurstTargetX = targetX;
                BurstTargetY = targetY;

                if (mode == LogicAutoAttackMode.BurstPredicting) PredictBurstTarget(delay, battle);

                return;
            }

            if (mode == LogicAutoAttackMode.AlwaysSpread && bulletCount >= 2)
                SpreadAttackInCircle(data, targetX, targetY, damage, percentDamage, bulletCount, battle);
        }

        /// <summary>
        /// 0x10045d280. Сдвигает точку очереди туда, куда цель успеет уйти.
        ///
        /// Упреждение берётся из скорости цели и задержки между пулями: куда цель
        /// уйдёт за это время, туда очередь и досылается. Точка при этом не может
        /// оказаться за краем карты.
        /// </summary>
        protected virtual void PredictBurstTarget(int delay, LogicBattleModeServer battle)
        {
            LogicCharacterServer target = AutoAttackTarget;
            if (target == null) return;

            if (target.MoveEndTick < battle.Tick) return;

            int angle = target.FacingAngle;
            int lead = target.GetBaseSpeed() * delay / 10;

            BurstTargetX += lead * LogicMath.Sin(angle + 90) / 1024;
            BurstTargetY += LogicMath.Sin(angle) * lead / 1024;

            LogicTileMap map = battle.TileMap;
            if (map == null) return;

            BurstTargetX = ClampInsideMap(BurstTargetX, map.LogicWidth);
            BurstTargetY = ClampInsideMap(BurstTargetY, map.LogicHeight);
        }

        /// <summary>
        /// 0x10045cf14. Раскладывает остальные пули кольцом вокруг цели.
        ///
        /// Каждая следующая пуля отворачивается на равную долю круга, а точка, в
        /// которую она уходит, подтягивается к ближайшему проходимому месту: иначе
        /// половина кольца утыкалась бы в стены.
        /// </summary>
        protected virtual void SpreadAttackInCircle(
            ProjectileData data,
            int targetX, int targetY,
            int damage, int percentDamage,
            int bulletCount,
            LogicBattleModeServer battle)
        {
            int step = FullCircleDegrees / bulletCount;

            for (int i = 1; i < bulletCount; i++)
            {
                int radius = 100 * GetAutoAttackRange();
                if (battle.IsPlayFieldMirroredForPlayer(GetCurrentTeamIndex())) radius = -radius;

                int angle = step * i;

                int offsetX = LogicMath.Sin(angle) * radius;
                if (offsetX < 0) offsetX += 1023;

                int spotX = targetX - (offsetX >> 10);
                int spotY = targetY - radius + LogicMath.Sin(angle + 90) * radius / 1024;

                int passableX;
                int passableY;
                LogicGamePlayUtil.GetClosestPassableSpot(
                    1, spotX, spotY, battle.TileMap, out passableX, out passableY, false, false);

                LogicProjectileServer projectile = battle.ShootProjectile(
                    LogicBattleModeServer.NoStartPosition,
                    LogicBattleModeServer.NoStartPosition,
                    this, this,
                    data,
                    AttackOriginX, AttackOriginY,
                    damage, percentDamage,
                    0, false,
                    0, 0,
                    AttackKind,
                    null);

                if (projectile != null) projectile.RunEarlyTicks();
            }
        }

        private static int ClampInsideMap(int value, int size)
        {
            int high = size - 2;
            int clamped = value < high ? value : high;
            return value <= 1 ? 1 : clamped;
        }
    }
}
