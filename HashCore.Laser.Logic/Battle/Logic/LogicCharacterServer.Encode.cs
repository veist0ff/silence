namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Кодирование персонажа, 0x10044cfa8.
    ///
    /// Поля названы по смещениям оригинала. Порядок записи неприкосновенен:
    /// поток битовый, и лишний бит ломает всё, что идёт дальше.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Поле +356. Угол атаки.</summary>
        public int AttackAngle;

        /// <summary>Поле +2476. Кто именно управляет персонажем, когда тот под контролем.</summary>
        public int MindControlOwner;

        /// <summary>Поле +2308. Тик, до которого угол принудительно зафиксирован.</summary>
        public int ForcedAngleTick;

        /// <summary>Поле +668. Пока больше нуля, персонажа отбрасывает.</summary>
        public int KnockBackTicks;

        /// <summary>Поле +640. Тики проигрываемой анимации атаки.</summary>
        public int AttackAnimationTicks;

        /// <summary>Поле +689. Персонаж развёрнут спиной.</summary>
        public bool Reversed;

        /// <summary>Поля +692 и +693. Оглушение и его слабая разновидность.</summary>
        public bool Stunned;
        public bool WeaklyStunned;

        /// <summary>Поле +700. Пока больше нуля, персонажа трясёт.</summary>
        public int ShakeTicks;

        /// <summary>Поле +704. Указатель звёздной силы.</summary>
        public int StarPowerIndicatorTicks;

        /// <summary>Поле +2141.</summary>
        public bool ExtraStateFlag;

        /// <summary>Поле +1824. Временная прибавка к урону.</summary>
        public int TemporaryDamageBuff;

        /// <summary>Поле +1540. Счётчик, который вместе с признаком данных даёт свечение.</summary>
        public int GlowTicks;

        /// <summary>Поле +2220. Сколько персонажу осталось жить, если у него есть срок.</summary>
        public int LifeTimeLeft;

        /// <summary>Поля +2120 и +2124. Чем персонажа последний раз задело.</summary>
        public int HitEffectProjectile;
        public int HitEffectProjectileExtra;

        /// <summary>Поле +2128. Скин, от которого проигрывается эффект попадания.</summary>
        public int HitEffectSkin;

        /// <summary>Поля +2132 и +2133.</summary>
        public bool HitEffectStateA;
        public bool HitEffectStateB;

        /// <summary>Поле +1855.</summary>
        public bool StateFlag1855;

        /// <summary>Поле +2384. Уходит клиенту делённым на десять.</summary>
        public int ScaledStateValue;

        /// <summary>Поле +2796. Счётчик для одиночной доставки груза.</summary>
        public int PayloadTicks;

        /// <summary>Поле +2064. Кто добил персонажа; пишется только когда он мёртв.</summary>
        public int KillerIndex = -1;

        /// <summary>Поле +2400. Состояние приманки.</summary>
        public bool DecoyState;

        /// <summary>Режим, в котором груз считается активным всегда.</summary>
        public const int PayloadAlwaysActiveGameMode = 26;

        /// <summary>Режим, в котором свечение включается по списку игроков.</summary>
        public const int GlowByPlayerListGameMode = 19;

        /// <summary>Режим, в котором здоровье берётся у прицепленного объекта.</summary>
        public const int HealthFromAttachedGameMode = 61;

        /// <summary>
        /// 0x10044cfa8. Состояние персонажа.
        ///
        /// Первым делом идёт общая часть объекта, затем блок боевого состояния, и
        /// он разный у тех, кто умеет драться, и у декораций: у вторых пишется
        /// только состояние и, может быть, углы.
        /// </summary>
        /// <summary>Отметка места в потоке. Нужна только для сверки с рабочим энкодером.</summary>
        private static void Mark(BitStream stream, string name)
        {
            if (!LogicGameObjectManagerServer.DebugSections) return;
            if (LogicGameObjectManagerServer.SectionOffsets.Count < 60)
                LogicGameObjectManagerServer.SectionOffsets.Add("    " + name + " " + stream.GetWriteOffset());
        }

        public override void Encode(BitStream stream, bool isOwn, int ownObjectIndex, int teamIndex)
        {
            base.Encode(stream, isOwn, ownObjectIndex, teamIndex);

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int gameMode = battle != null ? battle.GameMode : 0;

            if (IsCombatant()) EncodeCombatState(stream, isOwn, gameMode);
            else EncodeDecorationState(stream);

            if (GetLifeTimeTicks() >= 1) stream.WritePositiveIntMax511(LifeTimeLeft);

            stream.WritePositiveVIntMax65535OftenZero(HitEffectProjectile);
            if (HitEffectProjectile >= 1) stream.WritePositiveIntMax15(HitEffectProjectileExtra);

            stream.WritePositiveVIntMax65535OftenZero(HitEffectSkin);
            Mark(stream, "hiteffects");

            stream.WriteBoolean(HitEffectStateA);
            stream.WriteBoolean(HitEffectStateB);

            // Скользкий пол.
            stream.WriteBoolean(false);

            stream.WriteBoolean(HasBuff(BuffFast));
            stream.WriteBoolean(HasBuff(BuffSlow));

            // Немота.
            stream.WriteBoolean(false);

            // Этот бит рабочий энкодер держит взведённым всегда.
            stream.WriteBoolean(true);

            stream.WriteBoolean(HasBuff(BuffShield));
            stream.WriteBoolean(HasBuff(BuffReflect));

            if (stream.WriteBoolean(false)) stream.WritePositiveIntMax7(0);

            stream.WriteBoolean(false);

            if (stream.WriteBoolean(ReloadBuffTicks > 0))
            {
                stream.WriteIntMax15(0);
                stream.WriteBoolean(false);
            }

            if (stream.WriteBoolean(false)) stream.WriteIntMax127(0);

            stream.WritePositiveVIntMax255OftenZero(0);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            stream.WritePositiveVIntMax255OftenZero(CountPoisonBuffs());

            EncodeHealth(stream, gameMode);
            Mark(stream, "health " + Health + "/" + MaxHealth);

            if (Health <= 0) stream.WriteIntMax15(KillerIndex);

            if (IsDecoy()) stream.WriteBoolean(DecoyState);

            if (stream.WriteBoolean(false)) stream.WritePositiveIntMax511(511);
            Mark(stream, "predecoy");

            bool heroLike = IsHero() || IsDecoy();

            if (heroLike)
            {
                stream.WritePositiveVIntMax255OftenZero(CarriedItemCount);

                if ((gameMode == 11 || gameMode == 27 || gameMode == 41) && CarriedItemCount >= 1)
                    stream.WritePositiveVIntMax255(0);

                if (gameMode == 35) stream.WritePositiveVIntMax65535OftenZero(BattleRoyaleBuffs);
                else stream.WritePositiveVIntMax255OftenZero(BattleRoyaleBuffs);

                stream.WriteBoolean(false);

                if (!IsAlive()) stream.WriteBoolean(TransformAnimation);

                if (stream.WriteBoolean(ConsumableShield > 0))
                {
                    stream.WritePositiveIntMax16383(ConsumableShield);
                    stream.WritePositiveIntMax16383(ConsumableShieldMax);
                }
            }

            Mark(stream, "heroitems");

            if (GetUniqueProperty() == 24 || IsTwinMinion()) stream.WriteBoolean(false);

            if (IsWideStateGameMode(gameMode)) stream.WriteIntMax7(0);
            else stream.WriteBoolean(false);

            Mark(stream, "widestate hero=" + IsHero());

            if (IsHero()) EncodeHeroBlock(stream, isOwn, gameMode);
            else EncodeNonHeroBlock(stream);

            Mark(stream, "heroblock");

            EncodeTail(stream, isOwn, ownObjectIndex, gameMode);
            Mark(stream, "tail");
        }

        /// <summary>Виды усилений, состояние которых уходит в кадр.</summary>
        public const int BuffFast = 4;
        public const int BuffSlow = 3;
        public const int BuffShield = 14;
        public const int BuffReflect = 15;

        /// <summary>Действует ли на персонажа усиление такого вида.</summary>
        protected virtual bool HasBuff(int kind)
        {
            return false;
        }

        /// <summary>
        /// Блок тех, кто умеет драться: управление, углы, состояние и оглушение.
        ///
        /// Углы для своего персонажа пишутся НЕ всегда. Клиент и так знает, куда
        /// целится игрок, поэтому сервер досылает углы только когда персонаж
        /// потерял управление, его развернуло принудительно, им управляют извне
        /// или его отбрасывает. Для чужих персонажей углы пишутся всегда.
        /// </summary>
        private void EncodeCombatState(BitStream stream, bool isOwn, int gameMode)
        {
            bool mindControlled = IsMindControlled();
            stream.WriteBoolean(mindControlled);
            if (mindControlled) stream.WriteIntMax15(MindControlOwner);

            bool writeAngles = true;

            if (isOwn)
            {
                LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
                int now = battle != null ? battle.Tick : 0;

                bool controlRemoved = IsPlayerControlRemoved();
                bool forcedAngle = ForcedAngleTick + 1 >= now;
                bool knockedBack = KnockBackTicks > 0;

                stream.WriteBoolean(controlRemoved);
                stream.WriteBoolean(forcedAngle);
                stream.WriteBoolean(knockedBack);

                writeAngles = controlRemoved || forcedAngle || mindControlled || knockedBack;
            }

            if (writeAngles)
            {
                stream.WritePositiveIntMax511(AttackAngle);
                stream.WritePositiveIntMax511(FacingAngle);
            }

            stream.WritePositiveIntMax7(State);
            Mark(stream, "state");

            stream.WriteBoolean(HasVisibleDamageBuff(gameMode));
            Mark(stream, "dmgbuff");
            stream.WriteIntMax63(AttackAnimationTicks);
            Mark(stream, "attackticks");
            stream.WriteBoolean(Reversed);

            if (stream.WriteBoolean(Stunned)) stream.WriteBoolean(WeaklyStunned);

            stream.WriteBoolean(ShakeTicks > 0);
            stream.WriteBoolean(StarPowerIndicatorTicks > 0);
            stream.WriteBoolean(ExtraStateFlag);
            Mark(stream, "combatend");
        }

        /// <summary>
        /// Блок декораций и прочих недерущихся. Транспорт и объекты с ручным
        /// поворотом отдают оба угла, носители зоны — только угол атаки,
        /// остальные не отдают ничего.
        /// </summary>
        private void EncodeDecorationState(BitStream stream)
        {
            stream.WritePositiveIntMax7(State);

            if (IsVehicle() || HasManualRotations())
            {
                stream.WritePositiveIntMax511(AttackAngle);
                stream.WritePositiveIntMax511(FacingAngle);
            }
            else if (HasAreaEffect())
            {
                stream.WritePositiveIntMax511(AttackAngle);
            }
        }

        /// <summary>
        /// Здоровье. В режиме 61 оно берётся у прицепленного объекта, а если тот
        /// уже мёртв, персонаж показывается с нулём.
        /// </summary>
        private void EncodeHealth(BitStream stream, int gameMode)
        {
            int health = Health;
            int maxHealth = MaxHealth;

            if (gameMode == HealthFromAttachedGameMode && HealthSource != null)
            {
                if (HealthSource.IsAlive())
                {
                    health = HealthSource.Health;
                    maxHealth = HealthSource.MaxHealth;
                }
                else
                {
                    Health = 0;
                    health = 0;
                }
            }

            // В оригинале это writePositiveVIntMax2147483647, но длина у него та же
            // пятибитная, что и у варианта на 16777215, поэтому поток совпадает.
            stream.WritePositiveVIntMax16777215(health);
            stream.WritePositiveVIntMax16777215(maxHealth);
        }

        /// <summary>Поле +2704. Объект, у которого персонаж берёт здоровье.</summary>
        public LogicCharacterServer HealthSource;

        /// <summary>
        /// Привязка к другому персонажу. Пишется, только когда хозяин в той же
        /// команде, что и смотрящий, и сам является персонажем-героем.
        /// </summary>
        private void EncodeAttachment(BitStream stream, int ownObjectIndex)
        {
            if (AttachedTo == null || AttachedTo.PlayerIndex != ownObjectIndex)
            {
                stream.WriteBoolean(false);
                return;
            }

            bool hostIsHero = AttachedTo.IsHero();
            stream.WriteBoolean(hostIsHero);

            if (hostIsHero) stream.WritePositiveIntMax511(AttachAngle);
        }

        /// <summary>
        /// Признак заметного усиления урона. Складываются баффы типов 1, 2 и 5
        /// плюс временная прибавка; отдельно засчитывается свечение по данным и
        /// список игроков в режиме 19.
        /// </summary>
        private bool HasVisibleDamageBuff(int gameMode)
        {
            int sum = TemporaryDamageBuff + SumDamageBuffs();

            if (sum > 0) return true;
            if (HasGlowFlagInData() && GlowTicks > 0) return true;

            return gameMode == GlowByPlayerListGameMode && IsInGlowPlayerList();
        }

        /// <summary>Груз считается активным по-разному в разных режимах.</summary>
        private bool IsPayloadActive(LogicBattleModeServer battle, int gameMode)
        {
            if (!IsPayload()) return false;
            if (battle == null) return false;
            if (battle.Tick <= GetIntroTicks()) return false;

            if (gameMode == PayloadAlwaysActiveGameMode) return true;

            return IsPayloadSingleMode(gameMode) && PayloadTicks > 0;
        }

        /// <summary>Персонаж вообще умеет драться или это декорация.</summary>
        protected virtual bool IsCombatant()
        {
            return true;
        }

        protected virtual bool IsMindControlled()
        {
            return false;
        }

        protected virtual bool IsPlayerControlRemoved()
        {
            return false;
        }

        protected virtual bool IsVehicle()
        {
            return false;
        }

        protected virtual bool HasManualRotations()
        {
            return false;
        }

        protected virtual bool HasAreaEffect()
        {
            return false;
        }

        protected virtual int GetLifeTimeTicks()
        {
            return 0;
        }

        protected virtual bool HasGlowFlagInData()
        {
            return false;
        }

        protected virtual bool IsInGlowPlayerList()
        {
            return false;
        }

        protected virtual bool IsPayloadSingleMode(int gameMode)
        {
            return false;
        }

        /// <summary>Сумма баффов урона типов 1, 2 и 5. Маска 0x26 из оригинала.</summary>
        protected virtual int SumDamageBuffs()
        {
            return 0;
        }

        /// <summary>Сколько на персонаже баффов типа 9. Больше 254 не считается.</summary>
        protected virtual int CountPoisonBuffs()
        {
            return 0;
        }

        /// <summary>LogicStatusEffectControllerServer::encode для поля +1656.</summary>
        protected virtual void EncodeStatusEffects(BitStream stream)
        {
            stream.WritePositiveVIntMax255OftenZero(0);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
        }

        /// <summary>LogicK2BattleHookServer::uponEncodeOwnerPlayer для поля +2000.</summary>
        protected virtual void EncodeOwnerPlayerHook(BitStream stream, bool isOwn)
        {
        }
    }
}
