namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Зона эффекта, 1:1 с LogicAreaEffectServer.
    ///
    /// Смещения оригинала:
    ///   +116 тик создания
    ///   +124 тик, на котором зона гаснет
    ///   +164 счётчик затухания
    ///   +184 источник, к которому зона привязана
    ///   +588 угол, снятый с движения источника
    /// </summary>
    public partial class LogicAreaEffectServer : LogicGameObjectServer
    {
        /// <summary>
        /// За сколько тиков до конца начинается затухание. Порог из
        /// calculateIsExpired (0x100445094): ровно десять.
        /// </summary>
        public const int FadeoutTicks = 10;

        /// <summary>Поле +116.</summary>
        public int StartTick;

        /// <summary>Поле +124.</summary>
        public int ExpireTick;

        /// <summary>
        /// Поле +164. Сколько тиков осталось до полного затухания.
        ///
        /// Начальное значение намеренно велико: пока до конца срока далеко,
        /// calculateIsExpired это поле не трогает, и при кодировании оно не должно
        /// занижать счётчик проявления. Иначе зона всю жизнь уходит клиенту
        /// уже погасшей.
        /// </summary>
        public int FadeoutCounter = int.MaxValue;

        /// <summary>Поле +184. Зона едет за источником.</summary>
        public LogicGameObjectServer Source;

        /// <summary>Поле +588.</summary>
        public int SourceMoveAngle;

        public AreaEffectData AreaEffectData;

        /// <summary>
        /// Поле данных +224. Особое свойство зоны. Два его значения делают зону
        /// глазами команды: всё внутри неё команда видит, даже без бойцов рядом.
        /// </summary>
        public virtual int GetVisionKind()
        {
            return GetAreaEffectType();
        }

        /// <summary>Поле данных +224. Что зона делает.</summary>
        public virtual int GetAreaEffectType()
        {
            return LogicAreaEffectType.Parse(AreaEffectData != null ? AreaEffectData.Type : null);
        }

        public LogicAreaEffectServer(AreaEffectData data) : base(data)
        {
            AreaEffectData = data;
        }

        public override LogicGameObjectType GetGameObjectType()
        {
            return LogicGameObjectType.AreaEffect;
        }

        protected virtual bool IsIgnoreFadeout()
        {
            return false;
        }

        /// <summary>0x10044658c.</summary>
        protected virtual void OnExpire()
        {
        }

        protected int GetCurrentTick()
        {
            return Manager != null && Manager.BattleMode != null ? Manager.BattleMode.Tick : 0;
        }
    }
}
