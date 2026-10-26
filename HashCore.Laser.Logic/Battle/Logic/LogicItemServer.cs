namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Предмет на карте, 1:1 с LogicItemServer.
    ///
    /// Смещения оригинала:
    ///   +116 предмет подобран или уничтожен
    ///   +128 сколько тиков он ещё держится
    ///   +216 тиков до следующего состояния
    ///   +264 следующий предмет в связке
    /// </summary>
    public partial class LogicItemServer : LogicGameObjectServer
    {
        /// <summary>Поле +116.</summary>
        public bool Destroyed;

        /// <summary>Поле +128. Пока больше нуля, снятый предмет ещё не убирается.</summary>
        public int TicksLeft;

        /// <summary>Поле +216.</summary>
        public int TicksToNextState;

        /// <summary>Поле +264. Предметы могут стоять цепочкой.</summary>
        public LogicItemServer NextInChain;

        /// <summary>Состояние предмета. setState, 0x1004763fc.</summary>
        public int State;

        public ItemData ItemData;

        public LogicItemServer(ItemData data) : base(data)
        {
            ItemData = data;
        }

        public override LogicGameObjectType GetGameObjectType()
        {
            return LogicGameObjectType.Item;
        }

        /// <summary>
        /// 0x1004764a8. Предмет снимается, только когда он помечен И его время
        /// вышло. Оригинал дополнительно исключает предметы с типом 29 в данных —
        /// такие остаются на карте до конца боя.
        /// </summary>
        public override bool ShouldDestruct()
        {
            return Destroyed && TicksLeft <= 0 && !IsPermanent();
        }

        protected virtual bool IsPermanent()
        {
            return false;
        }

        /// <summary>0x1004e7cc4.</summary>
        public override void Tick()
        {
            if (TicksLeft > 0) TicksLeft--;

            if (TicksToNextState > 0)
            {
                TicksToNextState--;
                if (TicksToNextState == 0) AdvanceState();
            }
        }

        /// <summary>
        /// 0x100476610. Оригинал проставляет значение по всей цепочке, пока не
        /// упрётся в предмет, у которого оно уже стоит.
        /// </summary>
        public void SetTicksToNextState(int ticks)
        {
            LogicItemServer item = this;

            while (item != null)
            {
                item.TicksToNextState = ticks;
                item = item.NextInChain;
                if (item != null && item.TicksToNextState == ticks) break;
            }
        }

        protected virtual void AdvanceState()
        {
            State++;
        }
    }
}
