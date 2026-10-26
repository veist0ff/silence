namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Тик умения, 0x10047f044.
    ///
    /// Здесь крутятся все счётчики умения и здесь же решается момент выстрела:
    /// умение срабатывает не по команде, а когда счётчик подготовки дошёл до нуля.
    /// </summary>
    public partial class LogicSkillServer
    {
        /// <summary>Тип умения, у которого отдельно тикает область скорострельной стрельбы.</summary>
        public const int RapidFireAreaSkillType = 24;

        /// <summary>Тип умения, которое мгновенно восполняет боезапас.</summary>
        public const int InstantRefillChargeType = 1;

        /// <summary>Задержка, которая ставится после отката у умений с признаком +428.</summary>
        public const int PostCooldownLockTicks = 500;

        /// <summary>Во сколько раз ускоряется перезарядка под соответствующим эффектом.</summary>
        public const int FastReloadMultiplier = 16;

        /// <summary>Поле +12. Счётчик, который тикает вместе с остальными.</summary>
        public int TimerC;

        /// <summary>Поле +20.</summary>
        public int TimerD;

        /// <summary>Поле +24.</summary>
        public int TimerE;

        /// <summary>
        /// Поле +32. Пока он больше нуля, боезапас не восполняется. Ставится на
        /// PostCooldownLockTicks в момент, когда откат дошёл до нуля, если у
        /// умения взведён соответствующий признак в данных.
        /// </summary>
        public int ReloadLock;

        /// <summary>Поле +52. Умение сейчас готовится к применению.</summary>
        public bool Casting;

        /// <summary>Поле +54. На ближайшем тике надо выпустить снаряд.</summary>
        public bool PendingProjectile;

        /// <summary>Поля +44 и +48. Точка, в которую целится умение.</summary>
        public int TargetX;
        public int TargetY;

        /// <summary>Перезарядка идёт в шестнадцать раз быстрее.</summary>
        public bool FastReload;

        /// <summary>
        /// 0x10047f044. Шаг умения на заданное число тиков.
        ///
        /// Счётчики уменьшаются на delta и не уходят ниже нуля. Боезапас копится
        /// в тысячных: за тик прибавляется 1000 / время_перезарядки, поэтому шкала
        /// растёт плавно, а не скачком на целый патрон.
        ///
        /// Выстрел происходит в момент, когда счётчик подготовки был взведён и на
        /// этом тике дошёл до нуля. Проверка идёт по значению ДО уменьшения —
        /// иначе умение с нулевой подготовкой не выстрелило бы никогда.
        /// </summary>
        public virtual void Tick(LogicCharacterServer character, int delta)
        {
            if (PendingProjectile)
            {
                ShootPendingProjectile(character);
                PendingProjectile = false;
            }

            if (GetSkillType() == RapidFireAreaSkillType) TickRapidFireArea(character, delta);

            if (!IsTicking(character)) return;

            int chargeBefore = Charge;
            Charge = Reduce(Charge, delta);

            if (ReloadLock >= 1) ReloadLock = Reduce(ReloadLock, delta);

            if (Cooldown >= 1)
            {
                Cooldown -= delta;

                if (Cooldown <= 0)
                {
                    Cooldown = 0;
                    if (LocksReloadAfterCooldown()) ReloadLock = PostCooldownLockTicks;
                }
            }

            if (TimerD >= 1) TimerD = Reduce(TimerD, delta);
            if (TimerE >= 1) TimerE = Reduce(TimerE, delta);

            TickAmmo(character, delta);

            // Выстрел ровно на переходе через ноль, а не пока счётчик нулевой.
            if (chargeBefore >= 1 && Charge <= 0) OnChargeFinished(character);
        }

        /// <summary>
        /// Восполнение боезапаса.
        ///
        /// Пока идёт откат или время перезарядки не задано, боезапас сам не растёт;
        /// у умений с мгновенным восполнением он в этот момент разом ставится в
        /// полный патрон. В остальных случаях он копится по чуть-чуть каждый тик.
        /// </summary>
        private void TickAmmo(LogicCharacterServer character, int delta)
        {
            int rechargeTime = GetAmmoRechargeTime(character);

            if (Cooldown > 0 || rechargeTime < 1)
            {
                if (GetChargeType() == InstantRefillChargeType
                    && rechargeTime == 0
                    && Cooldown <= 0
                    && ReloadLock <= 0
                    && Ammo <= AmmoPerShot - 1
                    && !IsEmptyCheckBlocking(character))
                {
                    Ammo = AmmoPerShot;
                }

                return;
            }

            if (ReloadLock > 0) return;
            if (character != null && character.IsCocooned()) return;

            int max = AmmoPerShot * GetMaxAmmo(character);
            if (max <= AmmoPerShot) max = AmmoPerShot;

            int step = AmmoPerShot * delta / rechargeTime;
            Ammo += step;

            // Ускоренная перезарядка добавляет ещё пятнадцать таких же шагов:
            // в оригинале это записано как «убрать шаг и положить шестнадцать».
            if (FastReload || IsFastReloadActive(character))
                Ammo = Ammo - step + FastReloadMultiplier * step;

            if (Ammo > max) Ammo = max;
        }

        /// <summary>Уменьшение счётчика без ухода в минус.</summary>
        private static int Reduce(int value, int delta)
        {
            int left = value - delta;
            return left < 0 ? 0 : left;
        }

        /// <summary>Счётчики умения идут только когда бой не на паузе для него.</summary>
        protected virtual bool IsTicking(LogicCharacterServer character)
        {
            return true;
        }

        /// <summary>Поле данных +408. Вид умения.</summary>
        protected virtual int GetSkillType()
        {
            return 0;
        }

        /// <summary>Поле данных +412. Тип накопления.</summary>
        protected virtual int GetChargeType()
        {
            return 0;
        }

        /// <summary>Поле данных +428. Ставить ли задержку восполнения после отката.</summary>
        protected virtual bool LocksReloadAfterCooldown()
        {
            return false;
        }

        /// <summary>0x10048391c. Особая проверка пустоты для одного бойца.</summary>
        protected virtual bool IsEmptyCheckBlocking(LogicCharacterServer character)
        {
            return false;
        }

        /// <summary>Действует ли на персонажа ускоренная перезарядка.</summary>
        protected virtual bool IsFastReloadActive(LogicCharacterServer character)
        {
            return false;
        }

        /// <summary>0x1004836e0.</summary>
        protected virtual void TickRapidFireArea(LogicCharacterServer character, int delta)
        {
        }

        /// <summary>Подготовка закончилась — умение применяется.</summary>
        protected virtual void OnChargeFinished(LogicCharacterServer character)
        {
        }

        /// <summary>Выпускает отложенный снаряд в начале тика.</summary>
        protected virtual void ShootPendingProjectile(LogicCharacterServer character)
        {
        }
    }
}
