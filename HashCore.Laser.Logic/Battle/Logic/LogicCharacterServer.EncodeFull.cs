namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Хвост кадра персонажа: всё, что идёт после здоровья.
    ///
    /// Порядок полей здесь сверен байт в байт с энкодером, который клиент
    /// принимает в живом бою. Поток битовый: лишний или пропущенный бит уводит
    /// весь остаток кадра, поэтому поля, значения которых ещё не разобраны,
    /// пишутся теми же постоянными, что и в рабочем энкодере, — место в потоке
    /// они занимают правильное.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Поле +2388. Запас щита и его потолок.</summary>
        public int ConsumableShield;
        public int ConsumableShieldMax;

        /// <summary>Пока больше нуля, вокруг персонажа золотой щит.</summary>
        public int ShieldTicks;

        /// <summary>Поле +1204 в паре с видимостью. Два бита состояния видимости.</summary>
        public int VisibilityState;

        /// <summary>Персонаж невидим.</summary>
        public bool Invisible;

        /// <summary>Персонажа кто-то видит и ему об этом показывают.</summary>
        public bool SeenByEnemy;

        /// <summary>Поля обзора, подменённого умением.</summary>
        public int VisionOverrideTicks;
        public int VisionOverrideX;
        public int VisionOverrideY;

        /// <summary>Сколько всего боец накопил усилений выживания.</summary>
        public int BattleRoyaleBuffs;

        /// <summary>Персонаж копит заряд.</summary>
        public bool IsCharging;

        /// <summary>Вид накопления, который видно со стороны.</summary>
        public int VisualChargeType;

        /// <summary>Персонаж неуязвим.</summary>
        public bool Invincible;

        /// <summary>Ульта применяется прямо сейчас.</summary>
        public bool UsingUlti;

        /// <summary>Ульта в действии.</summary>
        public bool Ultiing;

        /// <summary>Кадр анимации накопления и его вид.</summary>
        public int ChargeAnimation;
        public int ActiveChargeType;

        /// <summary>Накопленное и его вид.</summary>
        public int ChargeUp;
        public int ChargeUpType;

        /// <summary>Сколько зарядов у заряжаемого выстрела.</summary>
        public int ChargedShotCount;

        /// <summary>Сколько тиков умение держат нажатым.</summary>
        public int SkillHoldTicks;

        /// <summary>Персонаж переносится.</summary>
        public bool Teleporting;

        /// <summary>Анимация превращения после смерти.</summary>
        public bool TransformAnimation;

        /// <summary>Пока больше нуля, перезарядка ускорена.</summary>
        public int ReloadBuffTicks;

        /// <summary>Числа урона, которые надо показать в этом кадре.</summary>
        public readonly List<LogicDamageNumber> DamageNumbers = new List<LogicDamageNumber>();

        /// <summary>Режимы, в которых состояние выживания пишется тремя битами.</summary>
        private static bool IsWideStateGameMode(int gameMode)
        {
            return gameMode == 35 || gameMode == 48 || gameMode == 58 || gameMode == 59;
        }

        /// <summary>
        /// Блок героя. Он самый длинный, и почти всё в нём — состояние ульты,
        /// накопления и щита.
        /// </summary>
        private void EncodeHeroBlock(BitStream stream, bool isOwn, int gameMode)
        {
            stream.WritePositiveVIntMax255OftenZero(0);

            bool anyState = IsCharging || VisualChargeType > 0 || Invincible || UsingUlti || Ultiing;

            if (stream.WriteBoolean(anyState))
            {
                stream.WriteBoolean(IsCharging || VisualChargeType > 0);
                stream.WriteBoolean(Invincible);
                stream.WriteBoolean(UsingUlti);
                stream.WriteBoolean(Ultiing);
                stream.WriteBoolean(false);
            }

            LogicPlayer player = GetPlayer();

            if (player != null && player.HasOverCharge)
            {
                stream.WriteBoolean(player.OverChargeReady);
                stream.WriteBoolean(player.OverCharging);
                stream.WriteBoolean(player.OverChargeStarted);
                stream.WriteBoolean(player.OverChargeEnded);
            }

            if (isOwn)
            {
                if (stream.WriteBoolean(VisionOverrideTicks >= 1))
                {
                    stream.WriteIntMax65535(VisionOverrideX);
                    stream.WriteIntMax65535(VisionOverrideY);
                }
            }

            Mark(stream, "hb_vision");
            EncodeWeaponSkillExtras(stream, isOwn);
            Mark(stream, "hb_weapon");

            if (UltimateChargeType() == 15) stream.WriteBoolean(false);

            if (ShouldEncodePetStatus() || (GetUniqueProperty() == 13 && IsHero())) stream.WriteBoolean(false);

            int powerLevelsType = GetPowerLevelsType();

            if (powerLevelsType == 1) stream.WritePositiveIntMax7(GetPowerLevel());
            else if (GetUniqueProperty() == 1) stream.WritePositiveIntMax3(GetPowerLevel());
            else if (powerLevelsType == 2)
            {
                stream.WritePositiveIntMax31(0);
                stream.WriteBoolean(false);
            }

            if (stream.WriteBoolean(false))
            {
                stream.WritePositiveIntMax15(0);
                stream.WritePositiveIntMax7(0);
            }

            if (IsCharging || VisualChargeType > 0)
            {
                stream.WritePositiveIntMax255(ChargeAnimation);
                stream.WritePositiveIntMax31(ActiveChargeType);
                stream.WritePositiveIntMax31(0);

                if (ActiveChargeType == 17) stream.WriteBoolean(false);
            }

            Mark(stream, "hb_power");
            EncodeChargeUp(stream);
            Mark(stream, "hb_charge");

            if (UltimateProjectileUniqueProperty() == 5) stream.WriteBoolean(false);

            if (GetPetAutoSpawnDelay() > 0) stream.WritePositiveIntMax1023(0);
        }

        /// <summary>Накопленное пишется по-разному в зависимости от вида накопления.</summary>
        private void EncodeChargeUp(BitStream stream)
        {
            switch (ChargeUpType)
            {
                case 0:
                    break;

                case 1:
                case 7:
                    stream.WritePositiveVIntMax255OftenZero(ChargeUp / SkillTimeUnitsPerTick);
                    break;

                case 5:
                    break;

                case 8:
                    stream.WritePositiveIntMax1023(Clamp(ChargeUp, 0, 1023));
                    break;

                case 9:
                    stream.WritePositiveIntMax7(ChargedShotCount);
                    break;

                default:
                    stream.WritePositiveIntMax1023(Clamp(ChargeUp / SkillTimeUnitsPerTick, 0, 1023));
                    break;
            }
        }

        /// <summary>Довески, которые зависят от рисунка основного оружия.</summary>
        private void EncodeWeaponSkillExtras(BitStream stream, bool isOwn)
        {
            LogicSkillServer weapon = GetSkillByIndex(0);
            if (weapon == null || weapon.Data == null) return;

            if (weapon.Data.ChargedShotCount >= 1) stream.WriteIntMax3(ChargedShotCount);

            if (isOwn && weapon.Data.AttackPattern == 13)
                stream.WritePositiveVIntMax255OftenZero(Clamp(SkillHoldTicks, 0, 255));

            if (weapon.Data.AttackPattern == 15 || weapon.Data.AttackPattern == 17)
            {
                stream.WritePositiveVIntMax255OftenZero(100);
                stream.WritePositiveIntMax511(GetMoveAngle());
            }
        }

        /// <summary>Блок тех, кто не герой: только состояние разгона, если оно есть.</summary>
        private void EncodeNonHeroBlock(BitStream stream)
        {
            if (HasSecondaryPet())
            {
                stream.WritePositiveIntMax7(0);
                return;
            }

            if (!HasChargeBehaviourSkill()) return;

            if (stream.WriteBoolean(IsCharging)) stream.WritePositiveIntMax31(ActiveChargeType);
        }

        /// <summary>Общий хвост: видимость, числа урона, умения и остаток полей.</summary>
        private void EncodeTail(BitStream stream, bool isOwn, int ownObjectIndex, int gameMode)
        {
            Mark(stream, "tail_start");
            if (HasPet()) stream.WriteBoolean(true);

            if (GetUniqueProperty() == 9) stream.WriteBoolean(false);
            if (GetUniqueProperty() == 20) stream.WriteBoolean(false);

            if (CanUseFastTravel())
            {
                stream.WriteBoolean(Teleporting);
                stream.WriteBoolean(false);
            }

            if (GetUniqueProperty() == 22) stream.WriteBoolean(false);

            stream.WriteBoolean(false);

            if (stream.WriteBoolean(false)) stream.WriteIntMax15(0);

            if (GetCharacterKind() == LogicCharacterType.MinionLastStand) stream.WriteBoolean(false);

            if (GetUniqueProperty() == 11 && isOwn)
            {
                stream.WriteIntMax255(165);
                stream.WritePositiveIntMax7(0);
            }

            if (IsMegaBossOrBoss()) stream.WritePositiveIntMax7(0);

            Mark(stream, "tail_pre");
            stream.WriteBoolean(ShieldTicks > 0);
            stream.WritePositiveInt(VisibilityState, 2);
            stream.WriteBoolean(Invisible);
            stream.WritePositiveInt(0, 9);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            if (stream.WriteBoolean(false)) stream.WriteIntMax15(15);

            stream.WriteBoolean(false);

            if (DimensionIndex != 0)
            {
                stream.WritePositiveIntMax255(0);
                stream.WritePositiveIntMax255(0);
            }

            if (isOwn)
            {
                if (stream.WriteBoolean(GetBuffedSpeed() != 0)) stream.WriteIntMax1023(GetBuffedSpeed());

                stream.WriteBoolean(SeenByEnemy);
            }

            Mark(stream, "tail_vis");
            EncodeDamageNumbers(stream, ownObjectIndex);
            Mark(stream, "tail_dmg");

            for (int i = 0; i < SkillCount && i < Skills.Length; i++)
            {
                LogicSkillServer skill = Skills[i];

                if (stream.WriteBoolean(skill != null)) skill.Encode(stream, isOwn, this);
            }

            stream.WritePositiveIntMax3(0);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            if (gameMode == 48)
            {
                if (IsHero())
                {
                    stream.WritePositiveIntMax131071(0);
                    stream.WriteBoolean(false);
                }
                else if (GetUniqueProperty() == 44)
                {
                    stream.WriteBoolean(false);
                }
            }

            stream.WritePositiveIntMax1023(0);
            stream.WriteBoolean(false);
            stream.WritePositiveVIntMax65535OftenZero(0);
            stream.WritePositiveVIntOftenZero(0, 5);
            stream.WritePositiveIntMax3(0);
        }

        /// <summary>
        /// Числа урона. Показываются только те, что уже отсчитали задержку, и
        /// только своему да тому, кто их нанёс.
        /// </summary>
        private void EncodeDamageNumbers(BitStream stream, int ownObjectIndex)
        {
            int shown = 0;
            int ownIndex = TeamIndex * 16 + PlayerIndex;

            for (int i = 0; i < DamageNumbers.Count; i++)
            {
                LogicDamageNumber number = DamageNumbers[i];
                if (number.Delay != 0) continue;
                if (number.Dealer != ownObjectIndex && ownIndex != ownObjectIndex) continue;

                shown++;
            }

            stream.WritePositiveIntMax31(shown);

            if (shown == 0) return;

            for (int i = 0; i < DamageNumbers.Count; i++)
            {
                LogicDamageNumber number = DamageNumbers[i];
                if (number.Delay != 0) continue;
                if (number.Dealer != ownObjectIndex && ownIndex != ownObjectIndex) continue;

                stream.WriteIntMax32767(Clamp(number.Value, -32767, 32767));
                stream.WriteBoolean(false);
            }
        }

        /// <summary>Скорость сверх обычной, которую надо показать своему клиенту.</summary>
        protected virtual int GetBuffedSpeed()
        {
            return 0;
        }

        /// <summary>Уровень силы бойца.</summary>
        protected virtual int GetPowerLevel()
        {
            return 0;
        }

        /// <summary>Поле данных: как у бойца устроены уровни силы.</summary>
        protected virtual int GetPowerLevelsType()
        {
            return 0;
        }

        /// <summary>Вид накопления ульты из её строки.</summary>
        protected virtual int UltimateChargeType()
        {
            return 0;
        }

        /// <summary>Особое свойство снаряда ульты.</summary>
        protected virtual int UltimateProjectileUniqueProperty()
        {
            return 0;
        }

        /// <summary>Поле данных: надо ли слать состояние питомца.</summary>
        protected virtual bool ShouldEncodePetStatus()
        {
            return false;
        }

        /// <summary>У бойца есть питомец.</summary>
        protected virtual bool HasPet()
        {
            return false;
        }

        /// <summary>Боец сам является вторичным питомцем.</summary>
        protected virtual bool HasSecondaryPet()
        {
            return false;
        }

        /// <summary>Задержка появления питомца.</summary>
        protected virtual int GetPetAutoSpawnDelay()
        {
            return 0;
        }

        /// <summary>Есть ли у бойца умение с разгоном.</summary>
        protected virtual bool HasChargeBehaviourSkill()
        {
            return false;
        }

        /// <summary>Боец является боссом любого вида.</summary>
        protected virtual bool IsMegaBossOrBoss()
        {
            return false;
        }

        /// <summary>Особое свойство бойца, открытое энкодеру умения.</summary>
        public int GetUniquePropertyForEncoding()
        {
            return GetUniqueProperty();
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }

    /// <summary>Одно всплывающее число урона.</summary>
    public struct LogicDamageNumber
    {
        /// <summary>Сколько урона показать; отрицательное это лечение.</summary>
        public int Value;

        /// <summary>Кто его нанёс, в виде «команда и номер».</summary>
        public int Dealer;

        /// <summary>Сколько тиков число ещё не показывать.</summary>
        public int Delay;
    }
}
