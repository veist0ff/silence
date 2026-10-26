namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Что зона делает. Колонка Type таблицы зон, разобранная в createReferences
    /// (0x1003e15e0) в номер.
    ///
    /// Номер в таблице не записан — он получается из имени, и по нему вся логика
    /// боя и различает зоны. Поэтому имена здесь переписаны из бинаря буква в
    /// букву: ошибка в одном имени молча превратит одну зону в другую.
    /// </summary>
    public static class LogicAreaEffectType
    {
        public const int Damage = 0;
        public const int SmokeScreen = 1;
        public const int Dot = 2;
        public const int Heal = 3;
        public const int Hot = 4;
        public const int BulletExplosion = 5;
        public const int Effect = 6;
        public const int Pushback = 7;
        public const int DelayedDamage = 8;
        public const int HeroSpawn = 9;
        public const int DamageBoost = 10;
        public const int SpeedBuff = 11;
        public const int ReloadSpeedBuff = 12;
        public const int ShieldBuff = 13;
        public const int PercentDamage = 14;
        public const int WallSpawn = 15;
        public const int Stun = 16;
        public const int HealRegen = 17;
        public const int ChargeSuper = 18;
        public const int Slow = 19;
        public const int PetrolDistribution = 20;
        public const int Slippery = 21;
        public const int HealAndDamage = 22;
        public const int IncreaseHealth = 23;
        public const int Cleanse = 24;
        public const int Silence = 25;
        public const int Rebuild = 26;
        public const int EnemyChargeSuper = 27;
        public const int Vision = 28;
        public const int VisionSlow = 29;
        public const int InvasionIndicator = 30;
        public const int AllyChargeSuper = 31;
        public const int DamageAndStatusEffect = 32;
        public const int ExtraSuperCharge = 33;
        public const int EnragerOverchargeSuper = 34;
        public const int SwitchSkill = 35;
        public const int DamageDrain = 36;
        public const int HotAndDot = 37;
        public const int StatusEffect = 38;
        public const int CrossSlashGround = 39;
        public const int HealOnlyAllies = 40;
        public const int SpeedDebuff = 41;
        public const int BulletSpin = 42;
        public const int BulletSpin2 = 43;
        public const int MeepleWallSpawn = 44;
        public const int HpBasedStun = 45;
        public const int MeepleCheatArea = 46;
        public const int HotAndOverheals = 47;
        public const int WeakSpotAttack = 48;
        public const int LoseAmmo = 49;
        public const int BlockProjectiles = 50;
        public const int ChargeSuperFromProjectiles = 51;
        public const int HealAbsolute = 52;

        private static readonly Dictionary<string, int> ByName = new Dictionary<string, int>
        {
            { "Damage", Damage },
            { "SmokeScreen", SmokeScreen },
            { "Dot", Dot },
            { "Heal", Heal },
            { "Hot", Hot },
            { "BulletExplosion", BulletExplosion },
            { "Effect", Effect },
            { "Pushback", Pushback },
            { "DelayedDamage", DelayedDamage },
            { "HeroSpawn", HeroSpawn },
            { "DamageBoost", DamageBoost },
            { "SpeedBuff", SpeedBuff },
            { "ReloadSpeedBuff", ReloadSpeedBuff },
            { "ShieldBuff", ShieldBuff },
            { "PercentDamage", PercentDamage },
            { "WallSpawn", WallSpawn },
            { "Stun", Stun },
            { "HealRegen", HealRegen },
            { "ChargeSuper", ChargeSuper },
            { "Slow", Slow },
            { "PetrolDistribution", PetrolDistribution },
            { "Slippery", Slippery },
            { "HealAndDamage", HealAndDamage },
            { "IncreaseHealth", IncreaseHealth },
            { "Cleanse", Cleanse },
            { "Silence", Silence },
            { "Rebuild", Rebuild },
            { "EnemyChargeSuper", EnemyChargeSuper },
            { "Vision", Vision },
            { "VisionSlow", VisionSlow },
            { "InvasionIndicator", InvasionIndicator },
            { "AllyChargeSuper", AllyChargeSuper },
            { "DamageAndStatusEffect", DamageAndStatusEffect },
            { "ExtraSuperCharge", ExtraSuperCharge },
            { "EnragerOverchargeSuper", EnragerOverchargeSuper },
            { "SwitchSkill", SwitchSkill },
            { "DamageDrain", DamageDrain },
            { "HotAndDot", HotAndDot },
            { "StatusEffect", StatusEffect },
            { "CrossSlashGround", CrossSlashGround },
            { "HealOnlyAllies", HealOnlyAllies },
            { "SpeedDebuff", SpeedDebuff },
            { "BulletSpin", BulletSpin },
            { "BulletSpin2", BulletSpin2 },
            { "MeepleWallSpawn", MeepleWallSpawn },
            { "HpBasedStun", HpBasedStun },
            { "MeepleCheatArea", MeepleCheatArea },
            { "HotAndOverheals", HotAndOverheals },
            { "WeakSpotAttack", WeakSpotAttack },
            { "LoseAmmo", LoseAmmo },
            { "BlockProjectiles", BlockProjectiles },
            { "ChargeSuperFromProjectiles", ChargeSuperFromProjectiles },
            { "HealAbsolute", HealAbsolute },
        };

        /// <summary>
        /// Номер по имени из таблицы. Незнакомое имя даёт обычный урон — так же,
        /// как пустая колонка.
        /// </summary>
        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return Damage;

            int value;
            return ByName.TryGetValue(name, out value) ? value : Damage;
        }
    }
}
