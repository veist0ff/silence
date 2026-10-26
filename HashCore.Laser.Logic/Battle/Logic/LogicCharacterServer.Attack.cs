namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Атака персонажа, 0x10045a8a4.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Делитель, переводящий разброс в поправку дальности. Из attack.</summary>
        public const int SpreadRangeDivisor = 450;

        /// <summary>С какого числа пуль в очереди веер раскладывается по синусу.</summary>
        public const int SineFanMinBullets = 5;

        /// <summary>Рисунок атаки: ровный веер.</summary>
        public const int AttackPatternEvenFan = 2;

        /// <summary>Рисунок атаки: веер по синусу.</summary>
        public const int AttackPatternSineFan = 3;

        /// <summary>Рисунок атаки: лесенка в стороны от середины.</summary>
        public const int AttackPatternStaircase = 6;

        /// <summary>
        /// Рисунки атаки, у которых пули расходятся. Маска из attack: единицы
        /// стоят на местах один, два, три и шесть.
        /// </summary>
        public const int SpreadingAttackPatternMask = 0x4E;

        /// <summary>Поле +364. Откуда полетит снаряд на этом кадре.</summary>
        public int AttackOriginX;
        public int AttackOriginY;

        /// <summary>Поле +372. Поправка к точке прицела на этом кадре.</summary>
        public int AttackAimOffsetX;
        public int AttackAimOffsetY;

        /// <summary>Поле +508. Сколько пуль в текущей очереди.</summary>
        public int AttackBulletCount;

        /// <summary>Поле +512. Какая пуля очереди сейчас вылетает.</summary>
        public int AttackShotIndex;

        /// <summary>Поле +520. Поправка скорости снаряда от снаряжения.</summary>
        public int AttackSpeedBonus;

        /// <summary>Поле +1216. Каким видом атаки идёт выстрел.</summary>
        public int AttackKind = LogicBattleModeServer.AttackKindWeapon;

        /// <summary>Поле +2648. Набранный заряд выстрела.</summary>
        public int ChargedShotValue;

        /// <summary>
        /// 0x10045a8a4. Выпускает одну пулю очереди.
        ///
        /// За один вызов вылетает ровно одна пуля: очередь набирается тем, что
        /// вызов повторяется, а счётчик выстрела растёт. От него зависит и куда
        /// пуля уйдёт, и каким снарядом она будет, и насколько сильнее ударит
        /// предыдущей.
        ///
        /// Разброс здесь не случайный: рисунок атаки раскладывает пули по вееру
        /// или лесенке, а поправка дальности вдобавок делает крайние пули очереди
        /// короче или длиннее средних.
        /// </summary>
        public virtual LogicProjectileServer Attack(
            LogicCharacterServer target,
            int targetX, int targetY,
            int damage, int percentDamage,
            ProjectileData projectileData,
            int spread, int bulletCount, int attackPattern)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return null;

            LogicSkillServer skill = GetCurrentActiveOrCastingSkill();
            SkillData skillData = skill != null ? skill.Data : null;

            ProjectileData data = SelectProjectile(skillData, projectileData);
            if (data == null)
            {
                AttackShotIndex++;
                return null;
            }

            // Каждая следующая пуля очереди бьёт сильнее предыдущей на заданную
            // в умении долю: так задан разгон урона у очередей.
            if (skillData != null)
                damage += AttackShotIndex * damage * skillData.DamageModifier / 100;

            int aimX = targetX;
            int aimY = targetY;
            int angleOffset = 0;

            int pattern = skillData != null ? skillData.AttackPattern : attackPattern;

            if ((uint)pattern <= 6 && ((1 << pattern) & SpreadingAttackPatternMask) != 0)
                ApplyAttackPattern(pattern, spread, ref aimX, ref aimY, out angleOffset);

            LogicProjectileServer projectile = battle.ShootProjectile(
                AttackOriginX, AttackOriginY,
                this, this,
                data,
                AttackAimOffsetX + aimX,
                AttackAimOffsetY + aimY,
                damage, percentDamage,
                angleOffset, true,
                -AttackSpeedBonus,
                AttackShotIndex,
                AttackKind,
                null);

            AttackShotIndex++;

            // Остальные пули очереди уходят отсюда же: часть способов стрельбы
            // выпускает их разом, часть откладывает на следующие тики.
            FinishAttackBurst(
                data, targetX, targetY, damage, percentDamage, spread, bulletCount, attackPattern);

            return projectile;
        }

        /// <summary>
        /// Раскладка пуль очереди. Из attack, 0x10045b7c4.
        ///
        /// Сначала пуля сдвигается по кругу вокруг стрелка: нечётные уходят в одну
        /// сторону, чётные в другую, а каждая третья летит прямо. Поправка дальности
        /// при этом такая же по величине, но со знаком, зависящим от стороны: веер
        /// получается не плоским, а выгнутым.
        ///
        /// Дальше, если очередь длинная или у неё задан особый рисунок, боковой
        /// сдвиг пересчитывается заново — уже по номеру пули внутри очереди.
        /// </summary>
        protected virtual void ApplyAttackPattern(
            int pattern, int spread, ref int aimX, ref int aimY, out int angleOffset)
        {
            int dx = aimX - X;
            int dy = aimY - Y;

            int distance = LogicMath.Sqrt(dx * dx + dy * dy);

            int slot = AttackShotIndex % 3;
            int rangeShift;

            if (slot == 2)
            {
                rangeShift = distance * spread / -SpreadRangeDivisor;
                angleOffset = -spread;
            }
            else if (slot == 1)
            {
                rangeShift = distance * spread / -SpreadRangeDivisor;
                angleOffset = spread;
            }
            else
            {
                rangeShift = distance * spread / SpreadRangeDivisor;
                angleOffset = 0;
            }

            if (IsRangeShiftedAttack())
            {
                if (distance != 0)
                {
                    dx = (rangeShift + distance) * dx / distance;
                    dy = (rangeShift + distance) * dy / distance;
                }

                aimX = X + dx;
                aimY = Y + dy;
            }

            if (AttackBulletCount >= SineFanMinBullets)
                angleOffset = LogicMath.Sin(1080 * AttackShotIndex / (AttackBulletCount - 1)) * spread >> 10;

            switch (pattern)
            {
                case AttackPatternEvenFan:
                    angleOffset = AttackShotIndex * (2 * spread / (AttackBulletCount - 1)) - spread;
                    break;

                case AttackPatternSineFan:
                    angleOffset = LogicMath.Sin(720 * AttackShotIndex / (AttackBulletCount - 1)) * spread >> 10;
                    break;

                case AttackPatternStaircase:
                    angleOffset = GetStaircaseOffset(spread);
                    break;
            }
        }

        /// <summary>
        /// Лесенка: пули уходят всё дальше от середины, чередуя стороны. Шаг равен
        /// разбросу, делённому на половину очереди, а середина стоит на нуле.
        /// </summary>
        private int GetStaircaseOffset(int spread)
        {
            int index = AttackShotIndex;
            if (index == 0) return 0;

            int half = AttackBulletCount / 2;
            if (half == 0) return 0;

            int step = spread / half;
            int value = step + step * ((index - ((index & 1) == 0 ? 1 : 0)) / 2);

            return (index & 1) != 0 ? -value : value;
        }

        /// <summary>
        /// Летит ли атака по укороченной или удлинённой дуге. В оригинале это
        /// признак Indirect у снаряда либо особый вид снаряда номер шесть.
        /// </summary>
        protected virtual bool IsRangeShiftedAttack()
        {
            return false;
        }

        /// <summary>
        /// Снаряд очереди. У умения их список, и номер выстрела выбирает, каким
        /// стреляет именно эта пуля; если такого нет, берётся первый.
        /// </summary>
        protected virtual ProjectileData SelectProjectile(SkillData skillData, ProjectileData fallback)
        {
            LogicWeaponSkill weapon = GetCurrentActiveOrCastingSkill() as LogicWeaponSkill;
            if (weapon == null || weapon.Projectiles.Count == 0) return fallback;

            int index = AttackShotIndex;
            if (index < 0 || index >= weapon.Projectiles.Count) index = 0;

            return weapon.Projectiles[index];
        }

        /// <summary>Умение, которое сейчас применяется или готовится.</summary>
        public virtual LogicSkillServer GetCurrentActiveOrCastingSkill()
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                LogicSkillServer skill = Skills[i];
                if (skill != null && (skill.Casting || skill.Charge > 0)) return skill;
            }

            return Skills.Length > 0 ? Skills[0] : null;
        }
    }
}
