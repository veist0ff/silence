namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Умение, собранное из строки skills.csv и умеющее стрелять.
    ///
    /// Это замыкание цикла боя: боец приказывает умению выстрелить, умение по
    /// готовности выпускает снаряд, снаряд летит и наносит урон.
    /// </summary>
    public class LogicWeaponSkill : LogicSkillServer
    {
        /// <summary>Кому умение принадлежит.</summary>
        public LogicCharacterServer Owner;

        /// <summary>Снаряд, который умение выпускает.</summary>
        public ProjectileData Projectile;

        /// <summary>
        /// Все снаряды умения. В attack (0x10045aab0) снаряд берётся из этого
        /// списка по номеру варианта выстрела, а нулевой — обычный.
        /// </summary>
        public readonly List<ProjectileData> Projectiles = new List<ProjectileData>();

        /// <summary>Сколько выстрелов умение уже сделало за бой.</summary>
        public int ShotsFired;

        public LogicWeaponSkill(SkillData data, LogicCharacterServer owner) : base(data)
        {
            Owner = owner;

            if (data == null) return;

            AmmoCost = 1;
            Ammo = AmmoPerShot * GetMaxAmmo(owner);
        }

        /// <summary>
        /// Приказ выстрелить в точку.
        ///
        /// Патрон списывается сразу, а сам выстрел происходит после подготовки:
        /// если подготовки нет, он случится на ближайшем тике умения.
        /// </summary>
        public bool Activate(int targetX, int targetY)
        {
            if (!CanActivate(Owner)) return false;

            TargetX = targetX;
            TargetY = targetY;

            ConsumeAmmo(Owner);

            Charge = GetCastTicks();

            // Мгновенное умение стреляет прямо сейчас: ждать нечего, а откладывать
            // на тик значило бы терять кадр.
            if (Charge <= 0) Fire();
            else Casting = true;

            return true;
        }

        /// <summary>Сколько времени умение готовится перед выстрелом.</summary>
        protected virtual int GetCastTicks()
        {
            return Data != null ? Data.MsBetweenAttacks : 0;
        }

        protected override void OnChargeFinished(LogicCharacterServer character)
        {
            Casting = false;
            Fire();
        }

        /// <summary>Выпускает снаряд в запомненную точку.</summary>
        protected virtual void Fire()
        {
            if (Owner == null) return;

            LogicBattleModeServer battle = Owner.Manager != null ? Owner.Manager.BattleMode : null;
            if (battle == null) return;

            ProjectileData projectile = Projectile;
            if (projectile == null) return;

            battle.ShootProjectile(Owner, projectile, Owner.X, Owner.Y, TargetX, TargetY, GetDamage());

            ShotsFired++;
            ApplyCooldown();
        }

        /// <summary>Урон одного выстрела.</summary>
        public virtual int GetDamage()
        {
            return Data != null ? Data.Damage : 0;
        }

        /// <summary>Ставит откат после выстрела.</summary>
        protected virtual void ApplyCooldown()
        {
            if (Data == null) return;
            Cooldown = Data.MsBetweenAttacks;
        }
    }
}
