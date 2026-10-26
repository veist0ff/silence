namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Путь персонажа и его позиция на произвольном тике.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>
        /// Точки пути, поля +432 и +448 оригинала, количество в +444.
        ///
        /// ВНИМАНИЕ: путь хранится задом наперёд. Нулевой элемент это конечная
        /// точка, последний — место, откуда персонаж вышел. Обход в getPosAtTick
        /// идёт от последнего элемента к нулевому, поэтому и здесь порядок такой же.
        /// </summary>
        public readonly List<int> PathX = new List<int>();
        public readonly List<int> PathY = new List<int>();

        /// <summary>Поле +2072. Доля пройденного пути в тысячных.</summary>
        public int MoveProgress;

        /// <summary>Поле +2856. Потолок высоты прыжка, ноль означает «без потолка».</summary>
        public int JumpHeightLimit;

        /// <summary>Поле +688. Персонаж в рывке или прыжке.</summary>
        public bool InDash;

        /// <summary>Поле +800. Состояние рывка.</summary>
        public int DashState;

        /// <summary>
        /// Состояния рывка, которые удлиняют переход на один тик. Из getPosAtTick
        /// (0x100459da8): состояния с 23 по 26 и особые свойства 55, 66 и 71.
        /// </summary>
        private bool NeedsExtraMoveTick()
        {
            if (!InDash) return false;

            if (DashState >= 23 && DashState < 27) return true;

            int property = GetUniqueProperty();
            return property == 55 || property == 66 || property == 71;
        }

        /// <summary>
        /// 0x100459da8. Позиция персонажа на заданном тике и точка, куда он смотрит.
        ///
        /// Доля пройденного считается от тиков перехода, а сама позиция ищется
        /// проходом по пути: накапливается длина отрезков, пока не наберётся нужное
        /// расстояние, и внутри последнего отрезка точка берётся пропорционально.
        ///
        /// Обратите внимание на «плюс один» в подсчёте прошедших тиков: на тике
        /// старта персонаж уже считается сдвинувшимся на один шаг, а не стоящим
        /// на месте.
        /// </summary>
        public int GetPosAtTick(int tick, out int posX, out int posY, out int lookX, out int lookY)
        {
            posX = X;
            posY = Y;
            lookX = X;
            lookY = Y;

            if (MoveEndTick == MoveStartTick) return 0;

            int total = MoveEndTick - MoveStartTick + (NeedsExtraMoveTick() ? 1 : 0);

            int elapsed = tick - MoveStartTick + 1;
            if (elapsed > total) elapsed = total;

            int progress = 1000 * elapsed / total;
            MoveProgress = progress;

            int distance;

            if (UsesEaseOutMovement())
            {
                // Замедление к концу: доля прогоняется через четвёртую степень
                // обратного хода, отчего последние тики дают всё меньший шаг.
                int left = 1000 - progress;
                int square = left * left / 1000;

                int eased = 1000 - square * square / 1000;

                MoveProgress = eased;
                distance = PathLength * eased;
            }
            else
            {
                distance = PathLength * progress;
            }

            distance /= 1000;

            return WalkPath(distance, out posX, out posY, out lookX, out lookY);
        }

        /// <summary>
        /// Проход по пути до заданного расстояния.
        ///
        /// Возвращает высоту над землёй; для обычной ходьбы она нулевая.
        /// </summary>
        private int WalkPath(int distance, out int posX, out int posY, out int lookX, out int lookY)
        {
            posX = X;
            posY = Y;
            lookX = X;
            lookY = Y;

            int count = PathX.Count;
            if (count != PathY.Count) return 0;

            int index = count - 1;

            int accumulated = 0;
            int accumulatedBefore = 0;

            int pointX = 0;
            int pointY = 0;
            int stepX = 0;
            int stepY = 0;
            int segment = 0;

            do
            {
                if (count < 2) return 0;

                accumulatedBefore = accumulated;

                pointX = PathX[index];
                pointY = PathY[index];

                stepX = PathX[index - 1] - pointX;
                stepY = PathY[index - 1] - pointY;

                segment = LogicMath.Sqrt(stepX * stepX + stepY * stepY);
                accumulated = accumulatedBefore + segment;

                count--;
                index--;
            }
            while (accumulated < distance);

            if (segment >= 1)
            {
                stepX = stepX * (distance - accumulatedBefore) / segment;
                stepY = stepY * (distance - accumulatedBefore) / segment;
            }

            posX = pointX + stepX;
            posY = pointY + stepY;

            // Смотрит персонаж в начало последнего пройденного отрезка.
            lookX = pointX;
            lookY = pointY;

            return GetJumpHeight(distance);
        }

        /// <summary>
        /// Данные персонажа просят замедление к концу перехода. Поле +424 данных.
        /// </summary>
        protected virtual bool UsesEaseOutMovement()
        {
            return false;
        }

        /// <summary>
        /// Высота над землёй во время прыжка или рывка.
        ///
        /// В оригинале она считается по параболе, коэффициент которой зависит от
        /// состояния рывка, и сверху ограничена тремя тысячами, а также полем
        /// +2856, если оно задано. Для обычной ходьбы высота нулевая.
        /// </summary>
        protected virtual int GetJumpHeight(int distance)
        {
            return 0;
        }
    }
}
