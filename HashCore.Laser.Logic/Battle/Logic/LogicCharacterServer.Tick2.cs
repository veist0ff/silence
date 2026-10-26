namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Подтики персонажа: клетка под ногами, самоподрыв и особые свойства.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Сколько тиков в секунде. Все проверки «раз в секунду» идут по нему.</summary>
        public const int TicksPerSecond = 20;

        /// <summary>
        /// Минимальный шаг нарастания урона от ядовитой клетки. Из tickTile
        /// (0x10044fe24): урон считается как 300 * n - 500, где n не меньше пяти,
        /// то есть первый удар всегда 1000, потом 1300, 1600 и так далее.
        /// </summary>
        public const int TileDamageMinStep = 5;

        /// <summary>Тип эффекта клетки, который бьёт через такт. Константа 118 из tickTile.</summary>
        public const int PulsingTileEffectType = 118;

        /// <summary>Длина такта пульсирующей клетки в тиках.</summary>
        public const int PulsingTilePeriod = 60;

        /// <summary>Урон пульсирующей клетки.</summary>
        public const int PulsingTileDamage = 500;

        /// <summary>Поле +2636. Сколько тиков персонаж стоит на бьющей клетке.</summary>
        public int TileDamageTicks;

        /// <summary>Поле +2632. Пока больше нуля, пульсирующая клетка не бьёт.</summary>
        public int PulsingTileImmunityTicks;

        /// <summary>Поле +772. От него отсчитывается посекундная потеря здоровья.</summary>
        public int HealthDrainStartTick;

        /// <summary>Поле +412. Состояние персонажа; четвёрка выключает и подрыв, и утечку.</summary>
        /// <summary>
        /// Состояние появления. Боец рождается именно в нём и выходит из него,
        /// когда отыграет появление: по этому полю клиент и понимает, что боец
        /// только что встал на поле.
        /// </summary>
        public const int StateSpawning = 4;

        public int State = StateSpawning;

        /// <summary>Состояние, при котором самоподрыв и утечка здоровья не работают.</summary>
        public const int StateNoSelfDamage = 4;

        /// <summary>
        /// 0x10044fe24. Клетка под ногами.
        ///
        /// Урон от ядовитой клетки нарастает: счётчик тиков на клетке делится на
        /// двадцать, и каждую полную секунду наносится 300 * n - 500. Нижняя граница
        /// n равна пяти, поэтому персонаж, только шагнувший в яд, сразу получает
        /// тысячу, а не символические сто.
        /// </summary>
        protected virtual void TickTile()
        {
            LogicTileMap map = Manager != null && Manager.BattleMode != null
                ? Manager.BattleMode.TileMap
                : null;

            if (map == null) return;

            int tileX = LogicTileMap.LogicToTile(X);
            int tileY = LogicTileMap.LogicToTile(Y);

            LogicTile tile = map.GetTile(tileX, tileY);
            if (tile == null) return;

            if (tile.DamageLevel >= 1)
            {
                TileDamageTicks++;

                if (TileDamageTicks % TicksPerSecond == 0)
                {
                    int steps = TileDamageTicks / TicksPerSecond;
                    if (steps <= TileDamageMinStep) steps = TileDamageMinStep;

                    CauseDamage(300 * steps - 500, DamageSource.Tile);
                }
            }

            if (tile.EffectType == PulsingTileEffectType)
            {
                LogicBattleModeServer battle = Manager.BattleMode;
                int now = battle != null ? battle.Tick : 0;

                // Клетка бьёт через такт: шестьдесят тиков бьёт, шестьдесят молчит.
                if ((now / PulsingTilePeriod) % 2 == 0 && PulsingTileImmunityTicks <= 0)
                    CauseDamage(PulsingTileDamage, DamageSource.Tile);
            }
        }

        /// <summary>
        /// 0x100453cfc. Самоподрыв и посекундная потеря здоровья.
        ///
        /// Подрыв срабатывает, когда рядом оказался живой враг: радиус берётся у
        /// зоны смерти, уменьшается на одну клетку и увеличивается на радиус самого
        /// врага. Проверка сначала грубая, по осям, и только потом по квадрату
        /// расстояния — так в оригинале, и так же дешевле.
        /// </summary>
        protected virtual void TickSelfDestruct()
        {
            if (HasSelfDestruct() && State != StateNoSelfDamage && !IsSelfDestructSuppressed())
            {
                int radius = GetSelfDestructRadius();

                if (radius > 0 && Manager != null)
                {
                    for (int i = 0; i < Manager.Characters.Count; i++)
                    {
                        LogicCharacterServer other = Manager.Characters[i] as LogicCharacterServer;
                        if (other == null || other == this) continue;

                        if (other.DimensionIndex != DimensionIndex) continue;
                        if (!other.IsAlive()) continue;
                        if (other.TeamIndex == TeamIndex) continue;
                        if (other.IsImmuneAndBulletsGoThrough()) continue;
                        if (other.IsPayload()) continue;

                        int reach = radius - LogicTile.TileSize + other.GetRadius();

                        int dx = other.X - X;
                        int dy = other.Y - Y;

                        int absX = dx >= 0 ? dx : -dx;
                        int absY = dy >= 0 ? dy : -dy;

                        if (absX > reach || absY > reach) continue;
                        if (dx * dx + dy * dy > reach * reach) continue;

                        // Подрыв — это урон самому себе на всё оставшееся здоровье.
                        CauseDamage(Health, DamageSource.SelfDestruct);
                        break;
                    }
                }
            }

            int promille = GetHealthPromilleLostPerSecond();
            if (promille < 1 || State == StateNoSelfDamage) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int tick = battle != null ? battle.Tick : 0;

            // В оригинале проверка делимости на двадцать сделана умножением на
            // 0xCCCCCCCD с ротацией — это ровно «раз в секунду».
            if ((tick - HealthDrainStartTick) % TicksPerSecond != 0) return;

            promille = GetHealthDrainPromilleOverride(promille);

            CauseDamage(MaxHealth * promille / 1000, DamageSource.HealthDrain);
        }

        /// <summary>
        /// 0x10045402c. Особые свойства персонажа.
        ///
        /// Свойство 18 каждый тик ищет цель в радиусе применения умения и запоминает
        /// её. Свойства 28 и 32 просто дожимают свои счётчики; у 32 по исчерпании
        /// персонаж пытается применить ульту, а если не вышло — превращается в того,
        /// кого эта ульта призывает.
        /// </summary>
        protected virtual void TickUniqueProperties()
        {
            int property = GetUniqueProperty();

            if (property == UniquePropertyAutoTarget && !IsAutoTargetDisabled())
            {
                if (IsAlive())
                {
                    AutoTarget = FindAutoTarget();
                    HasAutoTarget = AutoTarget != null;
                }
                else
                {
                    AutoTarget = null;
                    HasAutoTarget = false;
                }
            }

            if (property == UniquePropertyCountdown && IsAlive())
            {
                if (CountdownTicks - 1 >= 0)
                {
                    CountdownTicks--;
                    if (CountdownTicks == 0) CountdownValue = 0;
                }
            }

            if (property == UniquePropertyTransform && IsAlive())
            {
                if (TransformTicks - 1 >= 0)
                {
                    TransformTicks--;

                    if (TransformTicks == 0)
                    {
                        TransformValue = 0;

                        // Сначала пробуем применить ульту на месте. Если она не
                        // применилась, персонаж превращается в того, кого она призывает.
                        if (!ActivateUltimateInPlace()) TransformIntoSummonedCharacter();
                    }
                }
            }
        }

        /// <summary>Особое свойство: постоянно держать цель в прицеле.</summary>
        public const int UniquePropertyAutoTarget = 18;

        /// <summary>Особое свойство со счётчиком.</summary>
        public const int UniquePropertyCountdown = 28;

        /// <summary>Особое свойство с превращением по истечении счётчика.</summary>
        public const int UniquePropertyTransform = 32;

        /// <summary>Поле +2664.</summary>
        public LogicCharacterServer AutoTarget;

        /// <summary>Поле +1640.</summary>
        public bool HasAutoTarget;

        /// <summary>Поля +608 и +612.</summary>
        public int CountdownTicks;
        public int CountdownValue;

        /// <summary>Поля +2788 и +2792.</summary>
        public int TransformTicks;
        public int TransformValue;

        /// <summary>Откуда пришёл урон. Нужен, чтобы отличать источники в causeDamage.</summary>
        public enum DamageSource
        {
            Tile,
            SelfDestruct,
            HealthDrain,
        }

        /// <summary>
        /// Урон из внутренних источников: клетка, подрыв, утечка здоровья.
        /// В оригинале это тот же causeDamage с атакующим -1, поэтому и здесь
        /// он идёт через общий путь, а не мимо него.
        /// </summary>
        protected virtual void CauseDamage(int damage, DamageSource source)
        {
            if (damage < 1) return;

            CauseDamage(damage, -1, null, null, 0, true);
        }

        protected virtual int GetUniqueProperty()
        {
            return 0;
        }

        protected virtual bool IsAutoTargetDisabled()
        {
            return false;
        }

        protected virtual LogicCharacterServer FindAutoTarget()
        {
            return null;
        }

        protected virtual bool ActivateUltimateInPlace()
        {
            return false;
        }

        protected virtual void TransformIntoSummonedCharacter()
        {
        }

        protected virtual int GetSelfDestructRadius()
        {
            return 0;
        }

        protected virtual bool IsSelfDestructSuppressed()
        {
            return false;
        }

        protected virtual int GetHealthPromilleLostPerSecond()
        {
            return 0;
        }

        /// <summary>
        /// Свойство 20 подменяет скорость утечки здоровья, когда персонаж привязан
        /// к союзнику: тогда берётся первое значение особого свойства.
        /// </summary>
        protected virtual int GetHealthDrainPromilleOverride(int promille)
        {
            return promille;
        }

        /// <summary>0x10045e818 в оригинале носит чужое имя, это радиус персонажа.</summary>
        public override int GetRadius()
        {
            return 0;
        }

        protected virtual bool IsImmuneAndBulletsGoThrough()
        {
            return false;
        }

        /// <summary>0x10044ef20. Персонаж запелёнат и не может ни двигаться, ни перезаряжаться.</summary>
        public virtual bool IsCocooned()
        {
            return false;
        }

        protected virtual bool IsPayload()
        {
            return false;
        }

        /// <summary>Прибавка снаряжения по идентификатору.</summary>
        public virtual int GetGearBonus(int bonusId)
        {
            return 0;
        }

        /// <summary>Значение трейта. Ложь означает, что трейта на персонаже нет.</summary>
        public virtual bool TryGetTrait(int traitId, out int value)
        {
            return Traits.TryGetValue((LogicTraitType)traitId, out value);
        }

        /// <summary>
        /// Поле +680. Процент, на который персонажу срезано время перезарядки.
        /// </summary>
        public int AttackSpeedReductionPercent;

        /// <summary>
        /// Скорость атаки в процентах поверх базовой. Ниже -99 не опускается,
        /// иначе деление в getAmmoRechargeTime ушло бы в ноль.
        /// </summary>
        public virtual int GetAttackSpeedPercent()
        {
            return 0;
        }

        /// <summary>
        /// Особое свойство 28 со взведённым счётчиком даёт бесплатные выстрелы:
        /// consumeAmmo (0x100482b94) для такого персонажа ничего не списывает.
        /// </summary>
        public virtual bool HasFreeShots()
        {
            return GetUniqueProperty() == UniquePropertyCountdown && CountdownTicks != 0;
        }
    }
}
