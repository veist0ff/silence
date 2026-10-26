namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Персонаж боя, 1:1 с LogicCharacterServer.
    ///
    /// Смещения оригинала:
    ///   +488  текущее здоровье
    ///   +492  максимальное здоровье
    ///   +496  здоровье, выставленное при спавне
    ///   +1288 счётчик отложенной смерти
    ///   +1854 персонаж в стазисе
    /// </summary>
    public partial class LogicCharacterServer : LogicGameObjectServer
    {
        /// <summary>Поле +488.</summary>
        public int Health;

        /// <summary>Поле +492.</summary>
        public int MaxHealth;

        /// <summary>Поле +496. Оригинальный максимум, от него считаются проценты.</summary>
        public int BaseHealth;

        /// <summary>
        /// Поле +1288. Пока он больше нуля, персонаж уже не жив, но ещё не снят:
        /// isAlive (0x10045e7d4) требует и живого объекта, и счётчика меньше единицы.
        /// </summary>
        public int DelayedDeathCounter;

        /// <summary>Поле +1854.</summary>
        public bool InStasis;

        public CharacterData CharacterData;

        public LogicPlayer Player;

        /// <summary>
        /// Поле +1656. Статус-эффекты лежат прямо в персонаже, поэтому в
        /// декомпиляции все обращения к ним видны как this + 0x678.
        /// </summary>
        public readonly LogicStatusEffectController StatusEffects = new LogicStatusEffectController();

        public LogicCharacterServer(CharacterData data) : base(data)
        {
            CharacterData = data;
        }

        public override LogicGameObjectType GetGameObjectType()
        {
            return LogicGameObjectType.Character;
        }

        /// <summary>
        /// 0x10045e7d4. Персонаж жив.
        ///
        /// Складывается из трёх условий: объект не снят, здоровье больше нуля и
        /// счётчик отложенной смерти не взведён.
        ///
        /// Про здоровье в самой isAlive не написано — она спрашивает виртуальный
        /// слот +96. Но именно здоровье там и проверяется: мёртвая ветка кадра
        /// персонажа (0x10044efb8) досчитывает отложенную смерть, causeDamage
        /// ловит переход здоровья через ноль как момент гибели, а самоподрыв
        /// обходит через тот же слот живых врагов. Другой модели, согласованной
        /// со всеми тремя местами, нет.
        /// </summary>
        public override bool IsAlive()
        {
            return base.IsAlive() && Health > 0 && DelayedDeathCounter < 1;
        }

        /// <summary>
        /// Умения персонажа. Индексы из applyCooldown (0x100482b20):
        /// 0 — основная атака, 1 — второе умение, 3 — ульта.
        /// </summary>
        public readonly LogicSkillServer[] Skills = new LogicSkillServer[4];

        /// <summary>
        /// Сколько умений у бойца на самом деле собрано. В кадр уходит ровно
        /// столько записей: клиент читает их по числу построенных, а не по
        /// размеру массива.
        /// </summary>
        public int SkillCount;

        /// <summary>0x10044eee0.</summary>
        public LogicSkillServer GetSkillByIndex(int index)
        {
            if (index < 0 || index >= Skills.Length) return null;
            return Skills[index];
        }

        /// <summary>0x10044c4d8. Выставляет сразу три поля здоровья.</summary>
        public void SetMaxHealth(int value)
        {
            Health = value;
            MaxHealth = value;
            BaseHealth = value;
        }

        /// <summary>
        /// 0x1004570a0. Лечение упирается в максимум. Оригинал до этого ещё режет
        /// сумму порогом из вариации режима (поле +700) и при трейте 161 усиливает
        /// её на процент из +388 — это придёт вместе с трейтами.
        /// </summary>
        public void Heal(int amount)
        {
            if (amount < 1) return;

            int healed = Health + amount;
            Health = healed >= MaxHealth ? MaxHealth : healed;
        }

        /// <summary>
        /// 0x10044efb8. Кадр персонажа. Порядок взят из либы буквально.
        ///
        /// Живой персонаж идёт по одной из двух веток. В стазисе это только
        /// tickStasis, хук режима и эффект пета. В обычном состоянии — таймеры,
        /// хук режима, умения, клетка под ногами, лечение, движение с атакой,
        /// отложенная смерть, эффект пета, самоподрыв и особые свойства.
        ///
        /// Хук режима стоит ВТОРЫМ в обеих ветках, то есть до умений и до движения.
        /// </summary>
        public override void Tick()
        {
            if (!IsAlive())
            {
                // Мёртвый персонаж только досчитывает отложенную смерть и чистит
                // три поля состояния: +1200, +1204 и +2532.
                DelayedDeathCounter--;
                ClearStateOnDeath();
                return;
            }

            if (InStasis)
            {
                TickStasis();
                TickGameModeHook();
                TickPetEffect();
                return;
            }

            TickTimers();
            TickGameModeHook();
            TickSkills();
            TickTile();
            TickHeals();
            HandleMoveAndAttack();
            HandleDelayedDeath();
            TickPetEffect();
            TickSelfDestruct();
            TickUniqueProperties();
        }

        /// <summary>Слот +104 объекта режима игры, поле +1344 режима боя.</summary>
        protected virtual void TickGameModeHook()
        {
        }

        /// <summary>0x100453b2c.</summary>
        protected virtual void TickPetEffect()
        {
        }

        /// <summary>Чистка полей +1200, +1204 и +2532 из мёртвой ветки tick.</summary>
        protected virtual void ClearStateOnDeath()
        {
        }
    }
}
