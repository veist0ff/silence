namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Целочисленный вектор боя, 1:1 с LogicVector2.
    ///
    /// Во всех расчётах длины стоит защита от переполнения: координата больше
    /// 46340 по модулю уже даёт квадрат за пределами int, поэтому такой случай
    /// сразу отдаёт int.MaxValue, а не переполняется в отрицательное число.
    /// Проверка в оригинале записана как беззнаковое сравнение (v - 46341) с
    /// 0xFFFE95F7, что означает ровно «модуль не больше 46340».
    /// </summary>
    public struct LogicVector2
    {
        /// <summary>
        /// Наибольшая координата, квадрат которой ещё влезает в int вместе с
        /// такой же второй. Дальше расчёт насыщается.
        /// </summary>
        public const int MaxSafeComponent = 46340;

        public int X;
        public int Y;

        public LogicVector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Квадрат длины с насыщением, общий знаменатель всех расчётов.</summary>
        public static int GetLengthSquared(int x, int y)
        {
            if (x > MaxSafeComponent || x < -MaxSafeComponent) return int.MaxValue;
            if (y > MaxSafeComponent || y < -MaxSafeComponent) return int.MaxValue;

            int squareX = x * x;
            int squareY = y * y;

            // Оригинал сравнивает с (squareX ^ int.MaxValue); для неотрицательного
            // squareX это ровно int.MaxValue - squareX.
            if (squareY >= int.MaxValue - squareX) return int.MaxValue;

            return squareX + squareY;
        }

        /// <summary>0x100a183a0.</summary>
        public int GetLength()
        {
            return LogicMath.Sqrt(GetLengthSquared(X, Y));
        }

        /// <summary>0x100a18490. Квадрат расстояния между двумя векторами.</summary>
        public static int GetDistanceSquared(LogicVector2 from, LogicVector2 to)
        {
            return GetLengthSquared(to.X - from.X, to.Y - from.Y);
        }

        /// <summary>0x100a184f4. То же, но вторая точка передана числами.</summary>
        public static int GetDistanceSquared(LogicVector2 from, int toX, int toY)
        {
            return GetLengthSquared(toX - from.X, toY - from.Y);
        }

        /// <summary>
        /// 0x100a183f4. Приводит вектор к заданной длине и возвращает прежнюю.
        /// Нулевой вектор остаётся нулевым.
        /// </summary>
        public int Normalize(int length)
        {
            int currentLength = GetLength();
            if (currentLength == 0) return 0;

            int newY = Y * length / currentLength;
            X = X * length / currentLength;
            Y = newY;

            return currentLength;
        }

        public void Set(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
