namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Умение персонажа, 1:1 с LogicSkillServer.
    ///
    /// Смещения оригинала:
    ///   +0  данные умения
    ///   +8  заряд
    ///   +16 откат
    ///   +28 боезапас в тысячных долях выстрела
    ///   +68 сколько тысячных стоит один выстрел
    ///   +76 умение заблокировано
    ///   +80 значение трейта времени перезарядки
    /// </summary>
    public partial class LogicSkillServer
    {
        /// <summary>
        /// Боезапас хранится в тысячных: полный патрон это тысяча. Так сделано,
        /// чтобы перезарядка шла плавно и её можно было показывать шкалой.
        /// </summary>
        public const int AmmoPerShot = 1000;

        /// <summary>Поле +0.</summary>
        public SkillData Data;

        /// <summary>Поле +8. Пока заряд больше нуля, умение применить нельзя.</summary>
        public int Charge;

        /// <summary>Поле +16. Откат в тиках.</summary>
        public int Cooldown;

        /// <summary>Поле +28. Боезапас в тысячных долях выстрела.</summary>
        public int Ammo;

        /// <summary>Поле +68. Во сколько выстрелов обходится одно применение.</summary>
        public int AmmoCost = 1;

        /// <summary>Поле +76.</summary>
        public bool Blocked;

        /// <summary>Поле +80. Время перезарядки, навязанное трейтом 25.</summary>
        public int RechargeTimeFromTrait;

        public LogicSkillServer(SkillData data)
        {
            Data = data;
        }

        /// <summary>0x10048b5f8.</summary>
        public SkillData GetData()
        {
            return Data;
        }

        /// <summary>
        /// 0x10047ef30. Умение можно применить, когда сошлись четыре условия:
        /// откат вышел, заряд израсходован, боезапаса хватает хотя бы на выстрел,
        /// и умение не заблокировано.
        /// </summary>
        public bool CanActivate(LogicCharacterServer character)
        {
            if (character == null) return false;
            if (Data == null) return false;

            if (Cooldown > 0) return false;
            if (Charge > 0) return false;
            if (Ammo < AmmoPerShot) return false;

            return !Blocked;
        }

        /// <summary>
        /// 0x10047eef0. Полный боезапас: из данных плюс прибавка снаряжения.
        /// Отрицательная прибавка отбрасывается.
        /// </summary>
        public virtual int GetMaxAmmo(LogicCharacterServer character)
        {
            // В skills.csv это колонка MaxCharge: патроны у бойцов и есть заряды.
            int fromData = Data != null ? Data.MaxCharge : 0;

            int fromGear = character != null ? character.GetGearBonus(GearBonusMaxAmmo) : 0;
            if (fromGear < 0) fromGear = 0;

            return fromData + fromGear;
        }

        /// <summary>Идентификатор прибавки снаряжения к боезапасу.</summary>
        public const int GearBonusMaxAmmo = 48;

        /// <summary>Идентификатор прибавки снаряжения к скорости перезарядки.</summary>
        public const int GearBonusRechargeTime = 58;

        /// <summary>Трейт OverrideBasicAttackRechargeTime: задаёт время перезарядки напрямую.</summary>
        public const LogicTraitType RechargeTimeTrait = LogicTraitType.OverrideBasicAttackRechargeTime;

        /// <summary>
        /// 0x1004840fc. Прибавка боезапаса. Счёт идёт в процентах от выстрела:
        /// сотня это ровно один патрон.
        /// </summary>
        public void AddAmmo(LogicCharacterServer character, int percent)
        {
            Ammo += 10 * percent * AmmoCost;

            int max = AmmoPerShot * GetMaxAmmo(character);
            if (Ammo > max) Ammo = max;
        }

        /// <summary>
        /// 0x100482b94. Списание боезапаса за применение.
        ///
        /// Особое свойство 28 со взведённым счётчиком выстрел не тратит — это
        /// бесплатные выстрелы, которые такому персонажу положены.
        /// </summary>
        public void ConsumeAmmo(LogicCharacterServer character)
        {
            if (character != null && character.HasFreeShots()) return;

            Ammo -= AmmoPerShot * AmmoCost;
        }

        /// <summary>
        /// 0x10048381c. Сколько тиков восстанавливается один патрон.
        ///
        /// Порядок такой: базовое время из данных, трейт может заменить его целиком,
        /// снаряжение вычитает своё, потом снимается процент от скорости атаки,
        /// результат не опускается ниже единицы и делится на скорость атаки
        /// персонажа. Босс в режиме большой игры перезаряжается по своей формуле.
        /// </summary>
        public virtual int GetAmmoRechargeTime(LogicCharacterServer character)
        {
            // Колонка RechargeTime, соседняя с MaxCharge.
            int time = Data != null ? Data.RechargeTime : 0;
            if (time == 0) return 0;

            int traitValue;
            if (character != null && character.Traits.TryGetValue(RechargeTimeTrait, out traitValue))
                time = traitValue;

            int gear = character != null ? character.GetGearBonus(GearBonusRechargeTime) : 0;
            if (gear < 0) gear = 0;

            time -= gear;

            int reductionPercent = character != null ? character.AttackSpeedReductionPercent : 0;
            time = time - reductionPercent / 100 * time / 100;

            if (time <= 1) time = 1;

            int attackSpeed = character != null ? character.GetAttackSpeedPercent() : 0;
            if (attackSpeed <= -99) attackSpeed = -99;

            return 100 * time / (attackSpeed + 100);
        }

        /// <summary>0x1004839dc.</summary>
        public void Interrupt()
        {
            Charge = 0;
        }
    }
}
