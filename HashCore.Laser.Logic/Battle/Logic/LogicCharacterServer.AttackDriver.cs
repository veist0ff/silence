namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Что гоняет автоатаку каждый кадр. Часть handleMoveAndAttack, 0x1004528c8.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Потолок счётчика анимации атаки. Из attack, 0x10045b514.</summary>
        public const int MaxAttackAnimationTicks = 63;

        /// <summary>На сколько тиков атака закрывает бойцу право спрятаться.</summary>
        public const int AttackRevealExtraTicks = 2;

        /// <summary>Поле +416 в паре. Тик, на котором можно будет ударить снова.</summary>
        public int NextAttackTick;

        /// <summary>Автоатака выключена снаружи, например на время особого умения.</summary>
        public bool AutoAttackDisabled;

        /// <summary>
        /// Сколько тиков между ударами автоатаки. Из таблицы это миллисекунды,
        /// делённые на пятьдесят, — ровно один тик на каждые пятьдесят.
        /// </summary>
        public virtual int GetCurrentAttackSpeedTicks()
        {
            return 0;
        }

        /// <summary>
        /// Ход автоатаки за кадр.
        ///
        /// Сначала досылаются пули начатой очереди — она идёт своим чередом. Потом,
        /// если пора бить и цель на месте, начинается новая очередь. Цели нет —
        /// боец бредёт дальше искать.
        /// </summary>
        protected virtual void TickAutoAttack()
        {
            TickBurst();

            if (AttackAnimationTicks > 0) AttackAnimationTicks--;

            if (AutoAttackDisabled || !HasAutoAttack()) return;
            if (BurstShotsLeft >= 1) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            AcquireAutoAttackTarget();

            if (AutoAttackTarget == null || !AutoAttackTargetInRange)
            {
                PathFindToNextTarget();
                return;
            }

            if (battle.Tick < NextAttackTick) return;

            AutoAttackTargetX = AutoAttackTarget.X;
            AutoAttackTargetY = AutoAttackTarget.Y;

            AttackOriginX = X;
            AttackOriginY = Y;

            AttackBulletCount = GetAutoAttackBulletCount();
            AttackShotIndex = 0;

            AutoAttack();

            int speed = GetCurrentAttackSpeedTicks();
            NextAttackTick = battle.Tick + (speed > 0 ? speed : 1);

            LastAttackTick = battle.Tick;
            LastRevealTickB = battle.Tick + AttackRevealExtraTicks;

            int animation = NextAttackTick - battle.Tick;
            if (animation > MaxAttackAnimationTicks) animation = MaxAttackAnimationTicks;
            AttackAnimationTicks = animation < 0 ? 0 : animation;
        }

        /// <summary>Есть ли у бойца автоатака вообще.</summary>
        protected virtual bool HasAutoAttack()
        {
            return AutoAttackProjectile != null && GetAutoAttackDamageFromData() > 0;
        }

        /// <summary>
        /// Выбирает, кого бить. Держится за прежнюю цель, пока та годится: иначе
        /// боец на каждом кадре перескакивал бы между двумя одинаково близкими
        /// противниками и не успевал ударить ни одного.
        /// </summary>
        protected virtual void AcquireAutoAttackTarget()
        {
            int range = GetAutoAttackRange();

            if (AutoAttackTarget != null && IsInRange(range, AutoAttackTarget, false, false, false, false))
            {
                AutoAttackTargetInRange = true;
                return;
            }

            AutoAttackTarget = FindClosestEnemy(range);
            AutoAttackTargetInRange = AutoAttackTarget != null;

            if (AutoAttackTarget != null) PreferredTargetGlobalId = AutoAttackTarget.GlobalId;
        }

        /// <summary>
        /// Ближайший противник, которого боец видит. Дальность здесь своя —
        /// столько, сколько достаёт его оружие.
        /// </summary>
        public virtual LogicCharacterServer FindClosestVisibleEnemy()
        {
            int range = GetAutoAttackRange();
            if (range <= 0) range = GetWeaponRange();

            return FindClosestEnemy(range);
        }

        /// <summary>Дальность основного оружия в тех же единицах, что и дальность автоатаки.</summary>
        protected virtual int GetWeaponRange()
        {
            LogicSkillServer weapon = GetSkillByIndex(0);
            if (weapon == null || weapon.Data == null) return 0;

            return weapon.Data.CastingRange / RangeUnitSize;
        }

        /// <summary>
        /// Ближайший противник в пределах дальности. Перебор идёт по всем бойцам:
        /// их в бою десятки, а не тысячи, и оригинал считает так же.
        /// </summary>
        protected virtual LogicCharacterServer FindClosestEnemy(int range)
        {
            if (Manager == null) return null;

            List<LogicGameObjectServer> characters = Manager.Characters;

            LogicCharacterServer best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < characters.Count; i++)
            {
                LogicCharacterServer other = characters[i] as LogicCharacterServer;
                if (other == null || other == this) continue;

                if (!IsInRange(range, other, false, false, false, false)) continue;

                int distance = LogicGamePlayUtil.GetDistanceSquaredBetween(X, Y, other.X, other.Y);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = other;
                }
            }

            return best;
        }
    }
}
