namespace HashCore.Laser.Logic.Battle.Logic
{
    using System;

    /// <summary>Какой стороны прямоугольника коснулся отрезок.</summary>
    public enum RectangleSide
    {
        None = 0,

        /// <summary>Верхняя сторона.</summary>
        Top = 1,

        /// <summary>Правая сторона.</summary>
        Right = 2,

        /// <summary>Левая сторона.</summary>
        Left = 3,

        /// <summary>Нижняя сторона.</summary>
        Bottom = 4,
    }

    /// <summary>
    /// Геометрия боя, 1:1 с LogicGamePlayUtil.
    /// </summary>
    public static partial class LogicGamePlayUtil
    {
        /// <summary>0x10050746c.</summary>
        public static int GetDistanceBetween(int x1, int y1, int x2, int y2)
        {
            return LogicMath.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        /// <summary>
        /// Квадрат расстояния. Там, где сравниваются два расстояния, корень не
        /// нужен, а без него ещё и не теряется точность округления.
        /// </summary>
        public static int GetDistanceSquaredBetween(int x1, int y1, int x2, int y2)
        {
            return (x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1);
        }

        /// <summary>
        /// 0x100506034. Пересечение двух отрезков.
        ///
        /// Параметры пересечения считаются в тысячных, а проверка попадания в
        /// отрезок сделана беззнаковым сравнением с 1001 — так одним сравнением
        /// отсекается и отрицательное значение, и вылет за конец.
        /// </summary>
        public static bool LineSegmentIntersectsLineSegment(
            int x1, int y1, int x2, int y2,
            int x3, int y3, int x4, int y4,
            out int hitX, out int hitY)
        {
            hitX = 0;
            hitY = 0;

            int deltaX = x2 - x1;
            int deltaY = y2 - y1;

            int denominator = (y4 - y3) * deltaX - (x4 - x3) * deltaY;
            if (denominator == 0) return false;

            int alongThis = (int)(1000L * ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator);
            int alongOther = (int)(1000L * ((y1 - y3) * deltaX - (x1 - x3) * deltaY) / denominator);

            if ((uint)alongOther >= 1001 || (uint)alongThis >= 1001) return false;

            hitX = deltaX * alongThis / 1000 + x1;
            hitY = deltaY * alongThis / 1000 + y1;
            return true;
        }

        /// <summary>
        /// 0x1005060f0. Пересечение отрезка с окружностью.
        ///
        /// ВНИМАНИЕ: это единственное место движка, где счёт идёт не в целых
        /// числах, а в float. Координаты и радиус делятся на сто, чтобы влезть
        /// в точность float, потом решается обычное квадратное уравнение.
        /// Порядок операций и порядок слагаемых повторены дословно: у float
        /// сложение не ассоциативно, и перестановка слагаемых меняет последний
        /// бит результата, а значит и попадание на границе.
        ///
        /// Корень берётся в double, как и в оригинале (там sqrt от float
        /// расширяется до double), и только потом сужается обратно.
        /// </summary>
        public static bool LineSegmentIntersectsCircle(
            int x1, int y1, int x2, int y2,
            int centerX, int centerY, int radius,
            out int hitX, out int hitY)
        {
            hitX = 0;
            hitY = 0;

            float deltaX = (x2 - x1) * 0.01f;
            float deltaY = (y2 - y1) * 0.01f;
            float offsetX = (x1 - centerX) * 0.01f;
            float offsetY = (y1 - centerY) * 0.01f;

            float a = deltaY * deltaY + deltaX * deltaX;
            float b = (deltaY * offsetY + offsetX * deltaX) + (deltaY * offsetY + offsetX * deltaX);

            float scaledRadius = radius * 0.01f;

            float discriminant = a * -4.0f * (offsetY * offsetY + offsetX * offsetX - scaledRadius * scaledRadius)
                               + b * b;

            if (discriminant < 0.0f) return false;

            float root = (float)Math.Sqrt(discriminant);
            float negativeB = -b;
            float denominator = a + a;

            float t = (negativeB - root) / denominator;

            if (t < 0.0f || t > 1.0f)
            {
                t = (root + negativeB) / denominator;
                if (t < 0.0f || t > 1.0f) return false;
            }

            hitX = (int)(t * deltaX * 100.0f) + x1;
            hitY = (int)(t * deltaY * 100.0f) + y1;
            return true;
        }

        /// <summary>
        /// 0x100505e48. Пересечение отрезка с прямоугольником.
        ///
        /// Проверяются все четыре стороны, и выбирается та, чья точка пересечения
        /// ближе к началу отрезка. Именно поэтому снаряд, летящий в угол, отскакивает
        /// от той стены, до которой долетел первой, а не от произвольной.
        /// </summary>
        public static RectangleSide LineSegmentIntersectsRectangle(
            int x1, int y1, int x2, int y2,
            int left, int top, int right, int bottom,
            out int hitX, out int hitY)
        {
            RectangleSide side = RectangleSide.None;
            int bestDistance = 999999999;

            hitX = -1;
            hitY = -1;

            int x;
            int y;

            if (LineSegmentIntersectsLineSegment(x1, y1, x2, y2, left, top, right, top, out x, out y))
            {
                int distance = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (distance < bestDistance)
                {
                    side = RectangleSide.Top;
                    bestDistance = distance;
                    hitX = x;
                    hitY = y;
                }
            }

            if (LineSegmentIntersectsLineSegment(x1, y1, x2, y2, left, top, left, bottom, out x, out y))
            {
                int distance = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (distance < bestDistance)
                {
                    side = RectangleSide.Left;
                    bestDistance = distance;
                    hitX = x;
                    hitY = y;
                }
            }

            if (LineSegmentIntersectsLineSegment(x1, y1, x2, y2, right, bottom, left, bottom, out x, out y))
            {
                int distance = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (distance < bestDistance)
                {
                    side = RectangleSide.Bottom;
                    bestDistance = distance;
                    hitX = x;
                    hitY = y;
                }
            }

            if (LineSegmentIntersectsLineSegment(x1, y1, x2, y2, right, bottom, right, top, out x, out y))
            {
                int distance = (x - x1) * (x - x1) + (y - y1) * (y - y1);
                if (distance < bestDistance)
                {
                    side = RectangleSide.Right;
                    hitX = x;
                    hitY = y;
                }
            }

            return side;
        }
    }
}
