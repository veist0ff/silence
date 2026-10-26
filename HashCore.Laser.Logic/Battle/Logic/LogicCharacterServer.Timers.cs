namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Таймеры персонажа, 0x10044f254.
    ///
    /// Оригинал держит полтора десятка обратных счётчиков прямо полями и каждый тик
    /// уменьшает их по одному шаблону: значение уменьшается только если оно было не
    /// меньше единицы. Часть счётчиков при обнулении что-то делает, остальные просто
    /// доходят до нуля и этим открывают какое-то действие в других местах.
    ///
    /// Счётчики здесь названы по их смещениям в оригинале, а не выдуманными именами:
    /// назначение большинства из них видно только по месту, где их взводят, и
    /// придумывать им смысл значило бы врать в комментарии.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Смещения счётчиков, которые tickTimers просто уменьшает.</summary>
        public static readonly int[] PlainTimerOffsets =
        {
            828, 2164, 708, 2216, 2404, 2632, 2640,
        };

        /// <summary>
        /// Счётчик +2180. По исчерпании сбрасывает пару полей +2172.
        /// </summary>
        public int Timer2180;

        /// <summary>
        /// Счётчик +2220. Когда он доходит до нуля, персонаж получает 99999 урона —
        /// это гарантированная смерть по таймеру.
        /// </summary>
        public int LethalTimer;

        /// <summary>Счётчик +2300. По исчерпании поле +2292 становится равным -1.</summary>
        public int Timer2300;

        /// <summary>Поле +2172, которое сбрасывает счётчик +2180.</summary>
        public long LinkedValue2172;

        /// <summary>Поле +2292, которое счётчик +2300 переводит в -1.</summary>
        public long LinkedValue2292;

        /// <summary>Остальные счётчики, ключ — смещение поля в оригинале.</summary>
        public readonly Dictionary<int, int> Timers = new Dictionary<int, int>();

        /// <summary>
        /// Поле +592. Разброс, который персонаж получает один раз за бой:
        /// случайное число от 100 до 120. Пока он нулевой, значение ещё не выдано.
        /// </summary>
        public int RandomJitter;

        /// <summary>Поле +640.</summary>
        public int Field640;

        /// <summary>Поле +2196. Тик, на котором персонаж последний раз обновлялся.</summary>
        public int LastUpdateTick;

        /// <summary>Поле +776. Пока этот тик в будущем, регенерация выключена.</summary>
        public int RegenerationBlockUntil;

        /// <summary>Поле +704.</summary>
        public int Counter704;

        /// <summary>Поле +2240. Текущий заряд ульты.</summary>
        public int UltiCharge;

        /// <summary>Поле +2260. Заряд, при котором ульта готова.</summary>
        public int UltiChargeMax;

        /// <summary>Поле +2256. Тип накопления заряда.</summary>
        public int UltiChargeType;

        /// <summary>
        /// Маска типов заряда, которые копятся сами собой. В оригинале это константа
        /// 0x1844, то есть типы 2, 6, 11 и 12.
        /// </summary>
        public const int SelfChargingUltiTypes = 0x1844;

        /// <summary>Сколько заряда прибавляется за тик у самозаряжающихся типов.</summary>
        public const int UltiChargePerTick = 50;

        /// <summary>Урон, которым добивает истёкший счётчик +2220.</summary>
        public const int LethalTimerDamage = 99999;

        /// <summary>
        /// Сколько единиц времени умения проходит за один тик боя.
        ///
        /// Из tickSkills (0x10044f70c): умения тикают с шагом пятьдесят, а полный
        /// патрон это тысяча единиц. Значит секунда боя — двадцать тиков — даёт
        /// ровно тысячу единиц, и время перезарядки из таблицы, заданное в
        /// миллисекундах, ложится один в один.
        /// </summary>
        public const int SkillTimeUnitsPerTick = 50;

        /// <summary>Поле +780. Откат накопления ульты, уменьшается на шаг умений.</summary>
        public int UltiChargeCooldown;

        /// <summary>
        /// 0x10044f70c. Тик умений персонажа.
        ///
        /// Сначала стачивается откат накопления ульты, потом тикают все умения.
        /// Шаг у всех один и тот же.
        /// </summary>
        protected virtual void TickSkills()
        {
            UltiChargeCooldown = UltiChargeCooldown - SkillTimeUnitsPerTick;
            if (UltiChargeCooldown < 0) UltiChargeCooldown = 0;

            for (int i = 0; i < Skills.Length; i++)
            {
                LogicSkillServer skill = Skills[i];
                if (skill != null) skill.Tick(this, SkillTimeUnitsPerTick);
            }
        }

        /// <summary>0x10044f254.</summary>
        protected virtual void TickTimers()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int now = battle != null ? battle.Tick : 0;

            Timer2180--;
            if (Timer2180 <= 0) LinkedValue2172 = 0;

            if (LethalTimer >= 1)
            {
                LethalTimer--;
                if (LethalTimer == 0) CauseDamage(LethalTimerDamage, DamageSource.HealthDrain);
            }

            if (Timer2300 >= 1)
            {
                Timer2300--;
                if (Timer2300 == 0) LinkedValue2292 = -1;
            }

            for (int i = 0; i < PlainTimerOffsets.Length; i++)
            {
                int offset = PlainTimerOffsets[i];

                int value;
                if (!Timers.TryGetValue(offset, out value)) continue;
                if (value >= 1) Timers[offset] = value - 1;
            }

            // Разброс выдаётся один раз и потом не меняется: он делает поведение
            // одинаковых персонажей чуть-чуть разным.
            if (RandomJitter == 0 && battle != null)
                RandomJitter = battle.GetRandomInt(-10, 10) + 110;

            if (LastUpdateTick != now) Field640 = 63;

            RegenerationBlocked = RegenerationBlockUntil > 0 && RegenerationBlockUntil > now;

            Counter704--;

            TickUltiCharge();
        }

        /// <summary>
        /// Накопление заряда ульты из середины tickTimers.
        ///
        /// Типы 2, 6, 11 и 12 копят заряд сами. Тип 1 копит, только пока умение
        /// полностью заряжено. Тип 10 копит лишь в определённых состояниях, а в
        /// остальных обнуляет заряд. Тип 7 берёт заряд прямо у умения.
        /// </summary>
        private void TickUltiCharge()
        {
            bool chargeUp = false;

            if (State != StateNoSelfDamage)
            {
                if (UltiChargeType <= 12 && ((1 << UltiChargeType) & SelfChargingUltiTypes) != 0)
                    chargeUp = true;
                else if (UltiChargeType == 1 && HasFullSkillCharge())
                    chargeUp = true;
            }

            if (!chargeUp)
            {
                if (UltiChargeType == 10)
                {
                    // Состояния 1, 3 и 4 сбрасывают заряд, нулевое — копит.
                    if (State - 3 < 2 && State - 3 >= 0 || State == 1) UltiCharge = 0;
                    else if (State == 0) chargeUp = true;
                }
                else if (UltiChargeType == 7 && State != StateNoSelfDamage)
                {
                    int fromSkill = GetUltiChargeFromSkill();
                    UltiCharge = fromSkill >= 1 ? fromSkill : 0;
                }
            }

            if (!chargeUp) return;

            int charged = UltiCharge + UltiChargePerTick;
            UltiCharge = charged >= UltiChargeMax ? UltiChargeMax : charged;
        }

        protected virtual bool HasFullSkillCharge()
        {
            return false;
        }

        /// <summary>
        /// Заряд, взятый прямо у умения. Работает только когда у ульты тип
        /// накопления 15, иначе заряд обнуляется.
        /// </summary>
        protected virtual int GetUltiChargeFromSkill()
        {
            return 0;
        }
    }
}
