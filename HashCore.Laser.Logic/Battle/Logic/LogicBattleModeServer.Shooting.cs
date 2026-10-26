namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Запуск снарядов, 0x10047db3c.
    /// </summary>
    public partial class LogicBattleModeServer
    {
        /// <summary>
        /// На сколько выстрел портит счёт дружелюбия. Из shootProjectile:
        /// урон делится на минус пять.
        /// </summary>
        public const int TeamingScorePenaltyDivisor = -5;

        /// <summary>
        /// Признак того, что начальная точка не задана и снаряд стартует оттуда,
        /// где стоит тот, к кому он привязан. В оригинале это минус единица.
        /// </summary>
        public const int NoStartPosition = -1;

        /// <summary>Вид выстрела: обычная атака оружием.</summary>
        public const int AttackKindWeapon = 1;

        /// <summary>Рисунок атаки, при котором снаряд запоминает заряд стрелка.</summary>
        public const int ChargedAttackPattern = 17;

        /// <summary>
        /// 0x10047db3c. Выпускает снаряд.
        ///
        /// Снаряд забирает у хозяина команду, измерение и трейты в момент выстрела
        /// и дальше живёт сам: если хозяин умрёт, уже выпущенный снаряд долетит и
        /// нанесёт урон. Усиление урона тоже снимается здесь, один раз.
        ///
        /// Точка прицела запоминается отдельно от точки, куда снаряд полетит на
        /// самом деле: разброс уводит вторую в сторону, а первая нужна тем, кто
        /// потом спрашивает, куда целились.
        ///
        /// Расстояние выстрела считается от того, к кому снаряд привязан, а не от
        /// точки старта: у стрелка с вынесенным дулом это разные места.
        /// </summary>
        public virtual LogicProjectileServer ShootProjectile(
            int startX, int startY,
            LogicCharacterServer owner,
            LogicGameObjectServer linkedObject,
            ProjectileData data,
            int targetX, int targetY,
            int damage,
            int percentDamage,
            int spread,
            bool fixedSpread,
            int speedPenalty,
            int shotIndex,
            int attackKind,
            LogicAttackSpecialParams specialParams)
        {
            if (data == null) return null;

            if (owner != null)
            {
                LogicPlayer scored = owner.GetPlayer();
                if (scored != null) scored.TeamingScore += damage / TeamingScorePenaltyDivisor;
            }

            LogicProjectileServer projectile = CreateProjectile(data);
            if (projectile == null) return null;

            LogicGameObjectServer source = linkedObject != null ? linkedObject : owner;

            if (source != null)
            {
                projectile.PlayerIndex = source.PlayerIndex;
                projectile.OwnerPlayer = source.GetPlayer();
                projectile.TeamIndex = source.GetCurrentTeamIndex();
                projectile.DimensionIndex = source.DimensionIndex;
            }

            if (owner != null) projectile.Traits.CopyFrom(owner.Traits, true);

            int fromX = source != null ? source.X : startX;
            int fromY = source != null ? source.Y : startY;

            int aimDx = targetX - fromX;
            int aimDy = targetY - fromY;
            projectile.ShotDistance = LogicMath.Sqrt(aimDx * aimDx + aimDy * aimDy);

            int endX;
            int endY;
            LogicProjectileServer.GetSpreadEndLocation(
                data, TileMap, Random, fromX, fromY, targetX, targetY, spread, fixedSpread,
                out endX, out endY);

            GameObjectManager.AddGameObject(projectile);

            if (specialParams != null)
            {
                projectile.SpecialParams.CopyFrom(specialParams);
                projectile.SpecialParamsApplied = false;
            }

            projectile.Damage = damage;
            projectile.PercentDamage = percentDamage;
            projectile.AttackKind = attackKind;
            projectile.LinkedObject = source;

            projectile.Init(
                startX == NoStartPosition ? fromX : startX,
                startY == NoStartPosition ? fromY : startY,
                endX, endY, Tick, speedPenalty);

            projectile.ShotIndex = shotIndex;
            projectile.AimX = targetX;
            projectile.AimY = targetY;

            if (owner != null)
            {
                projectile.DamageBuffPercent = owner.GetDamageBuffForProjectile();

                if (attackKind == AttackKindWeapon)
                {
                    LogicSkillServer weapon = owner.GetSkillByIndex(0);
                    SkillData weaponData = weapon != null ? weapon.Data : null;

                    if (weaponData != null && weaponData.AttackPattern == ChargedAttackPattern)
                        projectile.ChargedShotValue = owner.ChargedShotValue;
                }
            }

            return projectile;
        }

        /// <summary>Короткая форма выстрела: без разброса, особых параметров и вариантов.</summary>
        public LogicProjectileServer ShootProjectile(
            LogicCharacterServer owner,
            ProjectileData data,
            int fromX, int fromY,
            int toX, int toY,
            int damage)
        {
            return ShootProjectile(
                fromX, fromY, owner, owner, data, toX, toY, damage,
                0, 0, false, 0, 0, AttackKindWeapon, null);
        }

        /// <summary>
        /// 0x1004e5000. Отзеркалено ли поле для этой команды.
        ///
        /// Зеркалится оно только второй команде и только там, где команд ровно
        /// две: при трёх и больше зеркалить нечего — «свой» стороны у поля нет.
        /// </summary>
        public virtual bool IsPlayFieldMirroredForPlayer(int teamIndex)
        {
            return teamIndex == 1 && !HasMoreThanTwoTeams();
        }

        /// <summary>Больше двух команд в бою.</summary>
        protected virtual bool HasMoreThanTwoTeams()
        {
            return false;
        }

        /// <summary>Создаёт объект снаряда. Переопределяется, когда нужен свой вид.</summary>
        protected virtual LogicProjectileServer CreateProjectile(ProjectileData data)
        {
            return new LogicProjectileServer(data);
        }
    }
}
