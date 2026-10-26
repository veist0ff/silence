namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>Завершение полёта снаряда.</summary>
    public partial class LogicProjectileServer
    {
        /// <summary>
        /// Причина «снаряд вернулся к хозяину». Только она возвращает патрон
        /// бумерангу и не разворачивает ничего на месте.
        /// </summary>
        public const int EndReasonRecalled = 5;

        /// <summary>Особые свойства снаряда, при которых возврат бумеранга даёт патрон.</summary>
        public const int UniquePropertyBoomerangA = 16;
        public const int UniquePropertyBoomerangB = 6;

        /// <summary>Поле +416. Пет, которого снаряд призывает на месте падения.</summary>
        public object PetToSummon;

        /// <summary>Поле +424. Зона, которую снаряд разворачивает на месте падения.</summary>
        public object AreaEffectToSpawn;

        /// <summary>
        /// Поле +432. При какой причине завершения разворачивается зона.
        /// Ноль означает «при любой».
        /// </summary>
        public int AreaEffectSpawnReason;

        /// <summary>Поле +828. Сколько объектов снаряд разворачивает.</summary>
        public int SpawnCount = 1;

        /// <summary>Поле +868. С каким значением снаряд переинициализируется.</summary>
        public int ReinitValue;

        /// <summary>Поля +849, +857 и +1273. Запреты на призыв пета.</summary>
        public bool PetSummonBlockedA;
        public bool PetSummonBlockedB;
        public bool PetSummonBlockedC;

        /// <summary>
        /// 0x10047bd28. Полёт закончился.
        ///
        /// Повторный вызов ничего не делает: причина пишется один раз, и по её
        /// нулевому значению снаряд отличает «ещё летит» от «уже приземлился».
        ///
        /// Возврат бумеранга обрабатывается отдельной короткой веткой: он не
        /// разворачивает ни зону, ни пета, а только возвращает хозяину патрон.
        /// </summary>
        protected virtual void TargetReached(ProjectileEndReason reason)
        {
            TargetReached((int)reason);
        }

        /// <summary>0x10047bd28.</summary>
        protected virtual void TargetReached(int reason)
        {
            if (EndReason != 0) return;

            EndReason = reason;

            if (reason == EndReasonRecalled)
            {
                RefundBoomerangAmmo();
                return;
            }

            ApplyDamageToTarget();

            Reinitialise(ReinitValue);

            if (PetToSummon != null && !PetSummonBlockedA && !PetSummonBlockedB && !PetSummonBlockedC)
                SummonPet();

            if (AreaEffectToSpawn == null) return;

            // Зона разворачивается либо всегда, либо только при своей причине.
            if (AreaEffectSpawnReason != 0 && AreaEffectSpawnReason != reason) return;

            SpawnAreaEffects(GetSpawnCount());
        }

        /// <summary>
        /// Наносит урон тому, в кого попали.
        ///
        /// Усиление урона взято у хозяина ещё при выстреле, поэтому здесь оно
        /// применяется к уже зафиксированному числу: снаряд, выпущенный усиленным
        /// бойцом, бьёт усиленно даже если бафф с хозяина уже спал.
        /// </summary>
        private void ApplyDamageToTarget()
        {
            LogicCharacterServer target = CollisionTarget as LogicCharacterServer;
            if (target == null || Damage < 1) return;

            int damage = Damage + Damage * DamageBuffPercent / 100;

            target.CauseDamage(
                damage,
                PlayerIndex,
                LinkedObject as LogicCharacterServer,
                ProjectileData,
                LogicCharacterServer.DamageTypeBasicAttack,
                false);

            HitEventThisTick = true;
        }

        /// <summary>
        /// Сколько объектов разворачивать. Трейт AddNumMaxItems прибавляет к
        /// числу из снаряда, а OverrideNumMaxItems задаёт его целиком и перебивает
        /// прибавку — порядок проверок в оригинале именно такой.
        /// </summary>
        private int GetSpawnCount()
        {
            int count = SpawnCount;

            int add;
            if (Traits.TryGetValue(LogicTraitType.AddNumMaxItems, out add)) count += add;

            int over;
            if (Traits.TryGetValue(LogicTraitType.OverrideNumMaxItems, out over)) count = over;

            return count;
        }

        /// <summary>
        /// Бумеранг, вернувшийся к живому хозяину, возвращает ему полный патрон.
        /// Работает только для двух особых свойств снаряда.
        /// </summary>
        private void RefundBoomerangAmmo()
        {
            LogicCharacterServer owner = LinkedObject as LogicCharacterServer;
            if (owner == null || !owner.IsAlive()) return;
            if (!GetIsBoomerang()) return;

            int property = ProjectileData != null ? ProjectileData.UniqueProperty : 0;
            if (property != UniquePropertyBoomerangA && property != UniquePropertyBoomerangB) return;

            LogicSkillServer weapon = owner.GetSkillByIndex(0);
            if (weapon != null) weapon.AddAmmo(owner, 100);
        }

        /// <summary>
        /// 0x10047cb00. Бумеранг ли это.
        ///
        /// Признак берётся из данных и может быть добавлен трейтом; кроме того,
        /// отдельный трейт способен включить или выключить его для конкретного
        /// снаряда, но только если снаряд совпадает с тем, который этот трейт
        /// назвал.
        ///
        /// </summary>
        protected virtual bool GetIsBoomerang()
        {
            bool fromData = BoomerangType > 0;

            int added;
            if (Traits.TryGetValue(LogicTraitType.OverrideBasicAttackIsBoomerang, out added) && added != 0)
                fromData = true;

            return fromData;
        }

        /// <summary>0x10047cb94.</summary>
        protected virtual void Reinitialise(int value)
        {
        }

        /// <summary>0x10047d0c4.</summary>
        protected virtual void SummonPet()
        {
        }

        /// <summary>0x10047daa0.</summary>
        protected virtual void SpawnAreaEffects(int count)
        {
        }
    }
}
