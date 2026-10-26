namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Подтики персонажа: стазис, регенерация и отложенная смерть.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>
        /// Сколько тиков подряд регенерация не идёт после урона. Базовое значение
        /// из tickHeals (0x1004501d0) — шестьдесят тиков, то есть три секунды.
        /// </summary>
        public const int RegenerationDelayTicks = 60;

        /// <summary>Нижняя граница задержки регенерации, сколько бы её ни срезали.</summary>
        public const int MinRegenerationDelayTicks = 20;

        /// <summary>Минимальный промежуток между двумя тиками лечения.</summary>
        public const int HealIntervalTicks = 20;

        /// <summary>Модификатор боя, который держит регенерацию выключенной.</summary>
        public const int NoRegenerationModifier = 19;

        /// <summary>Трейт AddRegenSpeed: срезает задержку регенерации, в процентах.</summary>
        public const LogicTraitType RegenerationDelayTrait = LogicTraitType.AddRegenSpeed;

        /// <summary>Поле +584. Тик последнего лечения.</summary>
        public int LastHealTick;

        /// <summary>Поле +588. Персонаж повреждён и ещё не долечился.</summary>
        public bool Damaged;

        /// <summary>Поле +600. Пока стоит, регенерация не начинается.</summary>
        public bool RegenerationBlocked;

        /// <summary>Поле +1520. Тик, от которого отсчитывается задержка регенерации.</summary>
        public int RegenerationBlockedUntilTick;

        /// <summary>
        /// Поле +1532. Тик, на котором персонаж окончательно умрёт. Ноль означает,
        /// что отложенной смерти нет.
        /// </summary>
        public int DelayedDeathTick;

        /// <summary>Поле +1292. Прибавка к урону подрыва.</summary>
        public int SelfDestructDamageBonus;

        /// <summary>Поле +2140.</summary>
        public bool SelfDestructKeepsBaseDamage;

        /// <summary>
        /// Таймеры, которые стазис двигает вперёд. В оригинале (0x10044f160) это
        /// поля +32, +36, +416, +420, +424, +628, +632, +768, +1248, +1520, +1532
        /// и +2196: каждое, если оно взведено, увеличивается на единицу.
        ///
        /// Стазис не замораживает время, а сдвигает все сроки на тик вперёд —
        /// поэтому после выхода из него у умений остаётся ровно столько же отката,
        /// сколько было на входе. Ключ словаря — смещение поля в оригинале.
        /// </summary>
        public readonly Dictionary<int, int> StasisTimers = new Dictionary<int, int>();

        /// <summary>0x10044f160.</summary>
        protected virtual void TickStasis()
        {
            List<int> keys = new List<int>(StasisTimers.Keys);

            for (int i = 0; i < keys.Count; i++)
            {
                int value = StasisTimers[keys[i]];
                if (value >= 1) StasisTimers[keys[i]] = value + 1;
            }

            if (RegenerationBlockedUntilTick >= 1) RegenerationBlockedUntilTick++;
            if (DelayedDeathTick >= 1) DelayedDeathTick++;
        }

        /// <summary>
        /// 0x1004501d0. Регенерация здоровья.
        ///
        /// Условий три, и все обязаны сойтись: прошло не меньше двадцати тиков с
        /// прошлого лечения, задержка после урона вышла, и бой уже начался.
        /// Отрицательная регенерация — это яд: она не ждёт задержки после урона.
        /// </summary>
        protected virtual void TickHeals()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int now = battle != null ? battle.Tick : 0;

            if (RegenerationBlocked || HasEventModifier(NoRegenerationModifier))
                RegenerationBlockedUntilTick = now;

            if (IsMegaBoss()) return;

            int regeneration = GetRegeneratePerSecond();

            int delay = RegenerationDelayTicks;
            int traitPercent;

            if (Traits.TryGetValue(RegenerationDelayTrait, out traitPercent))
            {
                int left = 100 - traitPercent;
                if (left <= 1) left = 1;

                delay = RegenerationDelayTicks * left / 100;
                if (delay <= MinRegenerationDelayTicks) delay = MinRegenerationDelayTicks;
            }

            if (now < LastHealTick + HealIntervalTicks) return;

            if (now >= RegenerationBlockedUntilTick + delay || regeneration < 0)
            {
                // Пока идёт вступление, лечение не работает — но только если бой
                // вообще считает вступление; иначе граница равна нулю.
                int introBoundary = UsesIntroForHealing && battle != null ? GetIntroTicks() - 1 : 0;

                if (now >= introBoundary && regeneration != 0)
                {
                    if (regeneration < 0) ApplySelfDamage(-regeneration);
                    else
                    {
                        Heal(regeneration);
                        LastHealTick = now;
                    }
                }
            }

            if (Health == MaxHealth) Damaged = false;
        }

        /// <summary>Глобальный признак, что лечение ждёт конца вступления.</summary>
        public static bool UsesIntroForHealing = true;

        /// <summary>
        /// 0x100453984. Отложенная смерть.
        ///
        /// Персонаж, которому назначен тик смерти, доживает до него с текущим
        /// здоровьем, а на самом тике здоровье обнуляется и разворачивается зона
        /// смерти. У самоподрывающихся урон зоны считается от урона автоатаки с
        /// учётом всех баффов урона.
        /// </summary>
        protected virtual void HandleDelayedDeath()
        {
            if (DelayedDeathTick < 1) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int now = battle != null ? battle.Tick : 0;

            if (now < DelayedDeathTick) return;

            Health = 0;
            DelayedDeathTick = 0;

            int damage = 0;
            int secondaryDamage = 0;

            if (HasSelfDestruct())
            {
                int baseDamage = SelfDestructDamageBonus + GetAutoAttackDamage();
                damage = GetTotalDamageBuffPercent() * baseDamage / 100 + baseDamage;

                if (SelfDestructKeepsBaseDamage) secondaryDamage = GetAutoAttackDamage();
            }

            SpawnDeathAreaEffect(damage, secondaryDamage);
        }

        /// <summary>0x100457b54.</summary>
        protected virtual int GetRegeneratePerSecond()
        {
            return 0;
        }

        /// <summary>0x10045a7ac.</summary>
        protected virtual int GetTotalDamageBuffPercent()
        {
            return 0;
        }

        protected virtual int GetAutoAttackDamage()
        {
            return 0;
        }

        protected virtual bool HasSelfDestruct()
        {
            return false;
        }

        protected virtual bool IsMegaBoss()
        {
            return false;
        }

        protected virtual int GetIntroTicks()
        {
            return 0;
        }

        protected virtual bool HasEventModifier(int modifier)
        {
            return false;
        }

        /// <summary>Значение трейта. Ложь означает, что трейта на персонаже нет.</summary>
        protected virtual bool TryGetTraitValue(int traitId, out int value)
        {
            return Traits.TryGetValue((LogicTraitType)traitId, out value);
        }

        /// <summary>Урон самому себе, например от яда с отрицательной регенерацией.</summary>
        protected virtual void ApplySelfDamage(int damage)
        {
            Health -= damage;
            if (Health < 0) Health = 0;
        }

        /// <summary>Разворачивает зону смерти, если она задана у персонажа.</summary>
        protected virtual void SpawnDeathAreaEffect(int damage, int secondaryDamage)
        {
        }
    }
}
