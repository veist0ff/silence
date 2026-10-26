namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Кадр умения.
    ///
    /// Времена уходят клиенту в тиках, а не в единицах времени умения: делитель
    /// тот же самый, на который они каждый кадр убывают.
    /// </summary>
    public partial class LogicSkillServer
    {
        /// <summary>Делитель заряда у умений, которые держат нажатыми.</summary>
        public const int HoldChargeDivisor = 20;

        /// <summary>Поле +8. Сколько умение уже работает.</summary>
        public int TicksActive;

        /// <summary>Сколько всего оно проработает.</summary>
        public int MaxTicksActive;

        /// <summary>Умение только что включили.</summary>
        public bool OnActivate;

        /// <summary>Накопленный заряд.</summary>
        public int ChargeValue;

        public void Encode(BitStream stream, bool isOwn, LogicCharacterServer owner)
        {
            stream.WritePositiveVIntMax255OftenZero(TicksActive / LogicCharacterServer.SkillTimeUnitsPerTick);
            stream.WritePositiveVIntMax255OftenZero(MaxTicksActive / LogicCharacterServer.SkillTimeUnitsPerTick);

            stream.WriteBoolean(OnActivate);
            stream.WriteBoolean(false);

            stream.WritePositiveVIntMax255OftenZero(Cooldown / LogicCharacterServer.SkillTimeUnitsPerTick);

            // Отдельный счётчик и признак состояния идут перед зарядом.
            stream.WritePositiveIntMax131071(0);
            stream.WriteBoolean(false);

            if (Data != null && Data.MaxCharge >= 1)
            {
                int divisor = Data.HoldToShoot ? HoldChargeDivisor : 1;
                stream.WritePositiveIntMax4095(ChargeValue / divisor);
            }

            if (Data != null && Data.SkillChangeType != 0)
                stream.WritePositiveIntMax511(Data.GetInstanceId());

            if (owner != null && owner.GetUniquePropertyForEncoding() == 18) stream.WriteBoolean(false);

            stream.WriteBoolean(false);
        }
    }
}
