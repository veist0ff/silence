namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>Запись в списке невосприимчивости: от кого и от чего урон не проходит.</summary>
    public readonly struct LogicDamageImmunity
    {
        public readonly int AttackerIndex;
        public readonly LogicData Source;

        public LogicDamageImmunity(int attackerIndex, LogicData source)
        {
            AttackerIndex = attackerIndex;
            Source = source;
        }
    }

    /// <summary>
    /// Нанесение урона персонажу, 0x100454278.
    ///
    /// В оригинале эта функция носит чужое имя UpgradeItem::getDesc — символы в
    /// бинаре ненадёжны, опознана она по телу: именно здесь списывается здоровье
    /// по смещению +488.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Модификатор боя, при котором переносимые объекты неуязвимы.</summary>
        public const int CarryableImmunityModifier = 26;

        /// <summary>Режим игры, в котором процент усиления урона срезается до тридцати.</summary>
        public const int ReducedDamageBuffGameMode = 61;

        /// <summary>Режим игры, в котором добивание оставляет полное здоровье.</summary>
        public const int FullHealthOnLethalGameMode = 63;

        /// <summary>Вид урона, который не трогает здоровье напрямую.</summary>
        public const int DamageTypeNoHealthLoss = 11;

        /// <summary>Вид урона, не запускающий очистку эффектов при смерти.</summary>
        public const int DamageTypeKeepEffects = 12;

        /// <summary>Вид урона, не усиливаемый пассивкой атакующего.</summary>
        public const int DamageTypeNoAttackerBonus = 18;

        /// <summary>Обычный выстрел. Только он кровоточит от модификатора 30.</summary>
        public const int DamageTypeBasicAttack = 1;

        /// <summary>Модификатор боя, добавляющий кровотечение обычным выстрелам.</summary>
        public const int BleedOnBasicAttackModifier = 30;

        /// <summary>Поле +1836. Процент, на который меняется входящий урон.</summary>
        public int DamageReceivedPercent;

        /// <summary>
        /// Поле +1840. Разовая прибавка к входящему урону: применяется один раз
        /// и тут же обнуляется.
        /// </summary>
        public int DamageReceivedFlatBonus;

        /// <summary>Поле +2557. Щит на одно очко здоровья уже израсходован.</summary>
        public bool OneHpShieldUsed;

        /// <summary>
        /// Поле +1760. Пары «атакующий и источник», от которых урон не проходит.
        /// Это защита от повторного попадания одним и тем же снарядом.
        /// </summary>
        public readonly List<LogicDamageImmunity> DamageImmunities = new List<LogicDamageImmunity>();

        /// <summary>
        /// 0x100454278. Наносит урон и возвращает, сколько его прошло на самом деле.
        ///
        /// Порядок в точности как в оригинале, и он важен:
        ///   1. неуязвимость переносимых объектов при модификаторе 26;
        ///   2. список невосприимчивости по паре «атакующий, источник»;
        ///   3. пересчёт урона: процент, разовая прибавка, двойной урон приманке;
        ///   4. усиление от пассивки атакующего;
        ///   5. щит на одно очко здоровья;
        ///   6. списание здоровья;
        ///   7. смерть и всё, что с ней связано.
        ///
        /// Приманка получает ровно вдвое больше: в оригинале это сдвиг влево на
        /// признак isDecoy, то есть умножение на два без всякого процента.
        /// </summary>
        public virtual int CauseDamage(
            int rawDamage,
            int attackerIndex,
            LogicCharacterServer attacker,
            LogicData source,
            int damageType,
            bool skipChecks)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;

            if (!skipChecks && IsCarryable() && HasEventModifier(CarryableImmunityModifier))
                return 0;

            if (IsImmuneTo(attackerIndex, source)) return 0;

            int damage = ScaleIncomingDamage(rawDamage, attackerIndex, damageType, battle);
            if (damage < 1) damage = 1;

            if (damageType == DamageTypeNoHealthLoss) return 0;

            int healthBefore = Health;

            // Щит на одно очко: пока трейт есть и не израсходован, добивающий удар
            // оставляет ровно единицу здоровья.
            bool oneHpShield = !OneHpShieldUsed && Traits.GetTrait(LogicTraitType.AddOneHPShield) != null;

            int floor = 0;

            if (oneHpShield && damage >= Health)
            {
                floor = 1;
            }
            else if (battle != null && battle.GameMode == FullHealthOnLethalGameMode
                     && damage >= Health && KeepsFullHealthOnLethal())
            {
                floor = MaxHealth;
            }

            int left = Health - damage;
            if (floor <= left) floor = left;

            Health = floor;

            if (Health == 1 && Traits.GetTrait(LogicTraitType.AddOneHPShield) != null)
                OneHpShieldUsed = true;

            int dealt = healthBefore - Health;

            if (dealt >= 1)
            {
                Damaged = true;

                // Базе команды урон регенерацию не сбивает: она чинится по своим
                // правилам, и обстрел не должен останавливать починку навсегда.
                if (GetCharacterKind() != LogicCharacterType.PvpBase)
                    RegenerationBlockedUntilTick = battle != null ? battle.Tick : 0;
                StatusEffects.CancelOnDamage();

                ShowDamageNumber(dealt, attacker);
                ApplyBleedOnBasicAttack(dealt, damageType, attacker);
            }

            if (Health <= 0 && healthBefore >= 1) OnKilled(attacker, damageType);

            return dealt;
        }

        /// <summary>
        /// Пересчёт входящего урона из середины causeDamage.
        ///
        /// Разовая прибавка снимается сразу, поэтому второй удар её уже не получит.
        /// В режиме 61 процент усиления срезается до тридцати от исходного.
        /// </summary>
        private int ScaleIncomingDamage(
            int rawDamage, int attackerIndex, int damageType, LogicBattleModeServer battle)
        {
            int flatBonus = DamageReceivedFlatBonus;
            if (flatBonus >= 1) DamageReceivedFlatBonus = 0;

            int percent = DamageReceivedPercent;
            if (battle != null && battle.GameMode == ReducedDamageBuffGameMode)
                percent = 30 * percent / 100;

            int doubled = IsDecoy() ? rawDamage << 1 : rawDamage;

            int damage = (percent + 100) * doubled / 100 + flatBonus;
            if (damage <= 1) damage = 1;

            // Пассивка атакующего усиливает урон только по не-героям и только
            // если вид урона это допускает.
            if (damageType != DamageTypeNoAttackerBonus && !IsHero() && battle != null)
            {
                LogicPlayer attackerPlayer = battle.GetPlayer(attackerIndex);

                if (attackerPlayer != null)
                {
                    int bonus = GetAttackerDamagePassive(attackerPlayer);
                    if (bonus >= 1) damage += bonus * damage / 100;
                }
            }

            return damage;
        }

        /// <summary>
        /// Модификатор 30 добавляет обычным выстрелам кровотечение. Урон эффекта
        /// делится на число его срабатываний, чтобы суммарно вышло двадцатая часть
        /// нанесённого урона.
        /// </summary>
        private void ApplyBleedOnBasicAttack(int dealt, int damageType, LogicCharacterServer attacker)
        {
            if (damageType != DamageTypeBasicAttack) return;
            if (!HasEventModifier(BleedOnBasicAttackModifier)) return;

            StatusEffectData bleed = GetBleedStatusEffect();
            if (bleed == null) return;

            LogicStatusEffectServer effect = StatusEffects.TryApply(bleed, attacker);
            if (effect == null) return;

            int triggers = effect.GetTotalTriggerCount();
            if (triggers < 1) return;

            effect.IndexA = dealt / 20 / triggers;
        }

        /// <summary>Есть ли запись невосприимчивости к этой паре «атакующий, источник».</summary>
        public bool IsImmuneTo(int attackerIndex, LogicData source)
        {
            for (int i = 0; i < DamageImmunities.Count; i++)
            {
                LogicDamageImmunity immunity = DamageImmunities[i];
                if (immunity.AttackerIndex == attackerIndex && immunity.Source == source) return true;
            }

            return false;
        }

        public void AddDamageImmunity(int attackerIndex, LogicData source)
        {
            DamageImmunities.Add(new LogicDamageImmunity(attackerIndex, source));
        }

        /// <summary>Персонаж погиб: чистка эффектов и запись убийства.</summary>
        protected virtual void OnKilled(LogicCharacterServer attacker, int damageType)
        {
            if (damageType != DamageTypeKeepEffects) StatusEffects.Clear();

            HandleKillCredit(attacker, damageType);
            DropCarriedItems(GetDroppedItemData());
        }

        /// <summary>0x10045ff10.</summary>
        protected virtual void ShowDamageNumber(int damage, LogicCharacterServer attacker)
        {
        }

        protected virtual bool IsCarryable()
        {
            return false;
        }

        protected virtual bool IsDecoy()
        {
            return false;
        }

        protected virtual bool IsHero()
        {
            return false;
        }

        /// <summary>Персонаж является базой команды. Для хвоста кадра в режимах с базами.</summary>
        public virtual bool IsBase()
        {
            return false;
        }

        /// <summary>
        /// В режиме 63 добивающий удар по подходящему персонажу восстанавливает
        /// ему здоровье до максимума вместо смерти.
        /// </summary>
        protected virtual bool KeepsFullHealthOnLethal()
        {
            return false;
        }

        /// <summary>
        /// Пассивка карты атакующего, усиливающая урон. Нумерация пассивок карт
        /// своя и с номерами трейтов не совпадает.
        /// </summary>
        protected virtual int GetAttackerDamagePassive(LogicPlayer attackerPlayer)
        {
            return 0;
        }

        /// <summary>
        /// Поле +1992. Заранее посчитанное усиление урона; ноль означает, что его
        /// надо считать на месте.
        /// </summary>
        public int PrecomputedDamageBuff;

        /// <summary>
        /// Усиление урона, которое снаряд забирает при выстреле. Складывается из
        /// заранее посчитанного либо общего значения и прибавки от снаряжения.
        /// </summary>
        public virtual int GetDamageBuffForProjectile()
        {
            int buff = PrecomputedDamageBuff;
            if (buff == 0) buff = GetTotalDamageBuffPercent();

            return buff + GetGearBonus(GearBonusDamage);
        }

        /// <summary>Идентификатор прибавки снаряжения к урону.</summary>
        public const int GearBonusDamage = 9;

        /// <summary>
        /// Что рассыпается при смерти. В режимах без носимого здесь пусто, и
        /// рассыпаться нечему.
        /// </summary>
        protected virtual ItemData GetDroppedItemData()
        {
            LogicGemGrabBattle gemGrab = Manager != null ? Manager.BattleMode as LogicGemGrabBattle : null;
            return gemGrab != null ? gemGrab.PointData : null;
        }

        /// <summary>Эффект кровотечения SharpenedAmmoBleed из таблицы статус-эффектов.</summary>
        protected virtual StatusEffectData GetBleedStatusEffect()
        {
            return null;
        }
    }
}
