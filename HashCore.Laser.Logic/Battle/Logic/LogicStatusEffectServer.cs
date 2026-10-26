namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Наложенный статус-эффект, 1:1 с LogicStatusEffectServer.
    ///
    /// Смещения оригинала, снятые с encode (0x10048a6bc):
    ///   +0   идентификатор наложения
    ///   +8   данные эффекта
    ///   +20  индекс, который уходит клиенту как есть
    ///   +24  второй индекс, уходит с прибавкой единицы
    ///   +32  объект-источник
    ///   +88  полная длительность
    ///   +92  сколько тиков осталось
    ///   +100 дополнительный флаг
    /// </summary>
    public class LogicStatusEffectServer
    {
        /// <summary>Поле +0.</summary>
        public int Id;

        /// <summary>Поле +8.</summary>
        public StatusEffectData Data;

        /// <summary>Поле +20. Отрицательное значение означает «не задан».</summary>
        public int IndexA = -1;

        /// <summary>Поле +24. Отрицательное значение означает «не задан».</summary>
        public int IndexB = -1;

        /// <summary>Поле +32. Кто наложил эффект.</summary>
        public LogicGameObjectServer Source;

        /// <summary>Поле +88.</summary>
        public int TotalTicks;

        /// <summary>Поле +92.</summary>
        public int TicksLeft;

        /// <summary>Поле +100.</summary>
        public bool Flag;

        public LogicStatusEffectServer(StatusEffectData data)
        {
            Data = data;
            TotalTicks = data != null ? data.DurationTicks : 0;
            TicksLeft = TotalTicks;
        }

        /// <summary>
        /// 0x10048a890. Эффект действует.
        ///
        /// Мигающий эффект (заданы оба поля Pulsating) то включается, то гаснет:
        /// от прошедшего времени берётся остаток по сумме обоих отрезков, и эффект
        /// активен, пока этот остаток меньше отрезка включения. У обычного эффекта
        /// проверка одна — остались ли тики.
        /// </summary>
        public bool IsActive()
        {
            if (TicksLeft < 1) return false;
            if (Data == null) return true;

            int on = Data.PulsatingActiveTicksOn;
            int off = Data.PulsatingActiveTicksOff;

            if (on < 1 || off < 1) return true;

            int elapsed = TotalTicks - TicksLeft;
            return elapsed % (off + on) < on;
        }

        /// <summary>
        /// Доля действия от нуля до ста, как её считает encode.
        /// </summary>
        public int GetProgressPercent()
        {
            if (TotalTicks < 1) return 0;

            int percent = 100 * TicksLeft / TotalTicks;
            if (percent >= 100) percent = 100;
            return percent < 0 ? 0 : percent;
        }

        /// <summary>
        /// Сила эффекта с учётом постепенного затухания.
        ///
        /// Пока до конца больше, чем GradualWearOffAfter, эффект держит полную силу.
        /// После этого порога сила падает линейно до нуля.
        /// </summary>
        public int GetStrengthPercent()
        {
            if (Data == null) return 100;

            int wearOff = Data.GradualWearOffAfter;
            if (wearOff < 1) return 100;

            int threshold = Data.DurationTicks - wearOff;
            if (threshold < 1) return 100;

            return TicksLeft < threshold ? 100 * TicksLeft / threshold : 100;
        }

        /// <summary>
        /// 0x10048a2b4. Сколько раз эффект сработает за всё время.
        /// Нулевая частота означает, что срабатывание бывает только первым тиком.
        /// </summary>
        public int GetTotalTriggerCount()
        {
            if (Data == null) return 0;

            int firstTick = Data.TriggerOnFirstTick ? 1 : 0;
            if (Data.TriggerRate < 1) return firstTick;

            int count = Data.DurationTicks / Data.TriggerRate + firstTick;
            return count < 0 ? 0 : count;
        }

        /// <summary>0x10048a280.</summary>
        public int GetTotalDamage()
        {
            return Data != null ? GetTotalTriggerCount() * Data.Damage : 0;
        }

        /// <summary>0x10048a2e0.</summary>
        public int GetTotalHealing()
        {
            return Data != null ? GetTotalTriggerCount() * Data.Healing : 0;
        }
    }

    /// <summary>
    /// Набор статус-эффектов объекта. В оригинале он лежит прямо в персонаже по
    /// смещению +1656, поэтому в декомпиляции виден как this + 0x678.
    /// </summary>
    public class LogicStatusEffectController
    {
        public readonly List<LogicStatusEffectServer> Effects = new List<LogicStatusEffectServer>();

        private int _nextId = 1;

        /// <summary>
        /// Наложение эффекта.
        ///
        /// Правила берутся из колонок таблицы: Stacking разрешает держать несколько
        /// копий одного эффекта, Refreshable позволяет повторному наложению
        /// перезапустить уже висящий. Если эффект не складывается и не обновляется,
        /// повторное наложение не делает ничего.
        /// </summary>
        public LogicStatusEffectServer TryApply(StatusEffectData data, LogicGameObjectServer source)
        {
            if (data == null) return null;

            if (!data.Stacking)
            {
                LogicStatusEffectServer existing = Find(data);

                if (existing != null)
                {
                    if (!data.Refreshable) return null;

                    existing.TotalTicks = data.DurationTicks;
                    existing.TicksLeft = data.DurationTicks;
                    existing.Source = source;
                    return existing;
                }
            }

            LogicStatusEffectServer effect = new LogicStatusEffectServer(data);
            effect.Id = _nextId++;
            effect.Source = source;

            Effects.Add(effect);
            return effect;
        }

        public LogicStatusEffectServer Find(StatusEffectData data)
        {
            for (int i = 0; i < Effects.Count; i++)
                if (Effects[i].Data == data) return Effects[i];

            return null;
        }

        /// <summary>Есть ли действующий эффект с таким набором данных.</summary>
        public bool IsActive(StatusEffectData data)
        {
            LogicStatusEffectServer effect = Find(data);
            return effect != null && effect.IsActive();
        }

        /// <summary>Тик всех эффектов. Истёкшие снимаются.</summary>
        public void Tick()
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                LogicStatusEffectServer effect = Effects[i];
                effect.TicksLeft--;

                if (effect.TicksLeft < 1) Effects.RemoveAt(i);
            }
        }

        /// <summary>Снимает эффекты по признаку, например при получении урона.</summary>
        public void CancelOnDamage()
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
                if (Effects[i].Data != null && Effects[i].Data.CancelOnDamage) Effects.RemoveAt(i);
        }

        /// <summary>Снимает эффекты, привязанные к жизни источника.</summary>
        public void CancelOnSourceDeath(LogicGameObjectServer source)
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                LogicStatusEffectServer effect = Effects[i];
                if (effect.Source != source) continue;
                if (effect.Data != null && effect.Data.CancelOnSourceDeath) Effects.RemoveAt(i);
            }
        }

        public void Clear()
        {
            Effects.Clear();
        }
    }
}
