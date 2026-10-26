namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Главный кадр движения и атаки персонажа, 0x100450408.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>
        /// Модификатор боя, который может целиком отменить кадр движения.
        /// Проверяется самым первым, до всего остального.
        /// </summary>
        public const int FreezeMovementModifier = 47;

        /// <summary>
        /// Сколько прошлых позиций персонаж помнит. Из handleMoveAndAttack: список
        /// добивается до четырёх элементов, каждый тик сдвигается, и в нулевой
        /// пишется текущая позиция.
        /// </summary>
        public const int PositionHistoryLength = 4;

        /// <summary>
        /// Поле +2360. История позиций, нулевой элемент — позиция этого тика.
        /// По ней считаются следы и проверки «где персонаж был недавно».
        /// </summary>
        public readonly List<LogicVector2> PositionHistory = new List<LogicVector2>();

        /// <summary>Поле +2320. Объект, к которому персонаж прицеплен.</summary>
        public LogicCharacterServer AttachedTo;

        /// <summary>Поле +2328. Угол, под которым персонаж висит на хозяине.</summary>
        public int AttachAngle;

        /// <summary>Поле +2376.</summary>
        public int LastZ;

        /// <summary>
        /// Сдвигает историю позиций и записывает текущую.
        ///
        /// Список сначала добивается до четырёх элементов текущей позицией — так
        /// делает и оригинал, поэтому только что созданный персонаж не имеет
        /// «пустой» истории, а имеет четыре одинаковых точки.
        /// </summary>
        public void PushPositionHistory()
        {
            while (PositionHistory.Count < PositionHistoryLength)
                PositionHistory.Add(new LogicVector2(X, Y));

            for (int i = PositionHistoryLength - 1; i > 0; i--)
                PositionHistory[i] = PositionHistory[i - 1];

            PositionHistory[0] = new LogicVector2(X, Y);
        }

        /// <summary>
        /// Ставит прицепленного персонажа вокруг хозяина.
        ///
        /// Расстояние равно сумме радиусов столкновения обоих, а угол складывается
        /// из собственного смещения и того, куда смотрит хозяин. Сдвиг вправо на
        /// десять — это деление на 1024, масштаб таблицы синуса.
        ///
        /// Если хозяин умер, привязка снимается.
        /// </summary>
        public void UpdateAttachedPosition()
        {
            if (AttachedTo == null) return;

            if (!AttachedTo.IsAlive())
            {
                AttachedTo = null;
                return;
            }

            int angle = LogicMath.NormalizeAngle360(AttachAngle + AttachedTo.FacingAngle);
            int distance = GetCollisionRadius() + AttachedTo.GetCollisionRadius();

            int x = AttachedTo.X + LogicMath.Sin(angle + 90) * distance / LogicMath.SinScale;
            int y = AttachedTo.Y + LogicMath.Sin(angle) * distance / LogicMath.SinScale;

            SetCharacterPositionSafe(x, y);
        }

        /// <summary>
        /// 0x100450408. Кадр движения и атаки.
        ///
        /// Начало кадра восстановлено полностью: модификатор 47 может отменить весь
        /// шаг, потом обновляется история позиций, потом ставится прицепленный
        /// персонаж. Дальше идут прицеливание, приказы движения и умения — эта
        /// часть переносится по мере разбора.
        /// </summary>
        protected virtual void HandleMoveAndAttack()
        {
            if (HasEventModifier(FreezeMovementModifier) && IsMovementFrozen()) return;

            LastZ = Z;

            PushPositionHistory();

            UpdateAttachedPosition();

            if (!IsIntroOver()) return;

            TickMovementAlongPath();
            TickItemPickup();
            TickAutoAttack();
        }

        /// <summary>Поле высоты персонажа.</summary>
        public int Z;

        /// <summary>
        /// Продвигает персонажа по найденному пути на текущий тик.
        /// </summary>
        protected virtual void TickMovementAlongPath()
        {
            if (MoveEndTick == MoveStartTick) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return;

            int posX;
            int posY;
            int lookX;
            int lookY;

            Z = GetPosAtTick(battle.Tick, out posX, out posY, out lookX, out lookY);

            UpdateMoveDirection(posX, posY, lookX, lookY, X, Y);
            SetCharacterPositionSafe(posX, posY);
        }

        /// <summary>Радиус столкновения персонажа из его данных.</summary>
        public virtual int GetCollisionRadius()
        {
            return 0;
        }

        /// <summary>Модификатор 47 замораживает движение не всегда, а по своему условию.</summary>
        protected virtual bool IsMovementFrozen()
        {
            return true;
        }

        /// <summary>Вступление закончилось и персонажу разрешено двигаться.</summary>
        protected virtual bool IsIntroOver()
        {
            return true;
        }
    }
}
