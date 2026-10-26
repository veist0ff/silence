namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Один трейт, 1:1 со строкой traits.csv.
    ///
    /// Смещения оригинала, сверенные по LogicTraitData::createReferences и
    /// resetStatics:
    ///   +84  Type, переведённый в число (см. LogicTraitType)
    ///   +92  Value
    ///   +97  Stackable
    ///   +99  OverchargeActive
    ///   +160 ссылка на StatusEffect
    /// </summary>
    public class LogicTrait
    {
        /// <summary>Поле +84. Колонка Type, переведённая в число.</summary>
        public LogicTraitType Type;

        /// <summary>Поле +92. Колонка Value.</summary>
        public int Value;

        /// <summary>
        /// Поле +97. Колонка Stackable. Складывающиеся трейты суммируются,
        /// нескладывающиеся перебивают всё остальное.
        /// </summary>
        public bool Stackable;

        /// <summary>
        /// Поле +99. Колонка OverchargeActive. Такой трейт работает только пока
        /// у персонажа включена перезарядка сверх нормы.
        /// </summary>
        public bool OverchargeActive;

        public LogicTrait()
        {
        }

        public LogicTrait(LogicTraitType type, int value, bool stackable = false, bool overchargeActive = false)
        {
            Type = type;
            Value = value;
            Stackable = stackable;
            OverchargeActive = overchargeActive;
        }
    }

    /// <summary>
    /// Набор трейтов объекта, 1:1 с LogicTraitController.
    ///
    /// Смещения оригинала:
    ///   +0  список трейтов, счётчик +12
    ///   +16 список вспомогательных значений
    ///   +32 признак включённой перезарядки сверх нормы
    /// </summary>
    public class LogicTraitController
    {
        /// <summary>Поле +0.</summary>
        public readonly List<LogicTrait> Traits = new List<LogicTrait>();

        /// <summary>
        /// Поле +32. Пока он не взведён, трейты с OverchargeActive не учитываются
        /// ни в getTrait, ни в getValue.
        /// </summary>
        public bool OverchargeActive;

        /// <summary>
        /// 0x10048b240. Добавляет трейт, если такого типа ещё нет.
        ///
        /// Повтор разрешён только когда оба трейта складывающиеся: иначе второй
        /// трейт того же типа отбрасывается, и работает первый добавленный.
        /// </summary>
        public bool TryAddTrait(LogicTrait trait)
        {
            if (trait == null) return false;

            for (int i = 0; i < Traits.Count; i++)
            {
                LogicTrait existing = Traits[i];
                if (existing.Type != trait.Type) continue;

                bool bothStackable = existing.Stackable && trait.Stackable;
                if (!bothStackable) return false;
            }

            Traits.Add(trait);
            return true;
        }

        /// <summary>0x10048b2e0. Переносит трейты с другого объекта.</summary>
        public void CopyFrom(LogicTraitController other, bool clearFirst)
        {
            if (other == null) return;

            if (clearFirst) Traits.Clear();

            OverchargeActive = other.OverchargeActive;

            for (int i = 0; i < other.Traits.Count; i++) TryAddTrait(other.Traits[i]);
        }

        /// <summary>0x10048b414. Первый подходящий трейт заданного типа.</summary>
        public LogicTrait GetTrait(LogicTraitType type)
        {
            for (int i = 0; i < Traits.Count; i++)
            {
                LogicTrait trait = Traits[i];
                if (trait.Type != type) continue;
                if (trait.OverchargeActive && !OverchargeActive) continue;
                return trait;
            }

            return null;
        }

        /// <summary>
        /// 0x10048b460. Итоговое значение трейта.
        ///
        /// Складывающиеся трейты суммируются, но первый же нескладывающийся
        /// прерывает перебор и отдаёт своё значение как есть — он перебивает
        /// всё, что успело накопиться. Ложь означает, что трейта нет вовсе;
        /// это не то же самое, что значение ноль.
        /// </summary>
        public bool TryGetValue(LogicTraitType type, out int value)
        {
            int sum = 0;
            bool found = false;

            for (int i = 0; i < Traits.Count; i++)
            {
                LogicTrait trait = Traits[i];
                if (trait.Type != type) continue;
                if (trait.OverchargeActive && !OverchargeActive) continue;

                if (!trait.Stackable)
                {
                    value = trait.Value;
                    return true;
                }

                sum += trait.Value;
                found = true;
            }

            value = found ? sum : 0;
            return found;
        }

        /// <summary>Удобная обёртка: значение трейта или ноль, если его нет.</summary>
        public int GetValue(LogicTraitType type)
        {
            int value;
            return TryGetValue(type, out value) ? value : 0;
        }

        public void Clear()
        {
            Traits.Clear();
        }
    }
}
