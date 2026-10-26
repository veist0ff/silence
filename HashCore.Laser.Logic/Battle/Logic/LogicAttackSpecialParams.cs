namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Особые параметры выстрела, 0x100446cc8.
    ///
    /// Это набор значений, который атака передаёт снаряду поверх обычных: чем
    /// именно снаряд должен отличаться от такого же, выпущенного просто так.
    /// Хранится он в самом снаряде (поле +944) и переносится копированием.
    ///
    /// Поля добавляются по мере разбора assignValues (0x100446cf0); те, что уже
    /// разобраны, названы, остальные лежат в общем наборе по смещению — так
    /// значение не теряется, даже пока неизвестно, что оно значит.
    /// </summary>
    public class LogicAttackSpecialParams
    {
        /// <summary>Поле +296. Выстрел идёт от вторичного умения.</summary>
        public bool FromSecondarySkill;

        /// <summary>Разобранные позже поля, по смещению в оригинале.</summary>
        public readonly Dictionary<int, int> Values = new Dictionary<int, int>();

        /// <summary>0x100446cf0. Переносит значения из другого набора.</summary>
        public void CopyFrom(LogicAttackSpecialParams other)
        {
            if (other == null) return;

            FromSecondarySkill = other.FromSecondarySkill;

            Values.Clear();
            foreach (KeyValuePair<int, int> pair in other.Values) Values[pair.Key] = pair.Value;
        }

        /// <summary>0x100446cec. Возвращает набор в исходное состояние.</summary>
        public void Reset()
        {
            FromSecondarySkill = false;
            Values.Clear();
        }
    }
}
