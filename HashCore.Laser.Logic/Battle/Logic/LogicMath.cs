namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Целочисленная математика боя, 1:1 с LogicMath.
    ///
    /// Вся математика движка целая — иначе сервер и клиент разойдутся в расчётах.
    /// Синус берётся из таблицы с масштабом 1024, угол — из таблицы арктангенса
    /// на 128 отсчётов, корень — из таблицы на 256 значений с доводкой Ньютоном.
    /// Таблицы выгружены из бинаря (0x100C31C46, 0x100C31BC4, 0x100C31CFC) и
    /// сверены с эталонными значениями — расхождений нет.
    /// </summary>
    public static class LogicMath
    {
        /// <summary>Масштаб синуса: Sin(90) равен 1024, а не единице.</summary>
        public const int SinScale = 1024;

        /// <summary>0x100C31C46. Синус в градусах от 0 до 90, умноженный на 1024.</summary>
        private static readonly short[] SinTable =
        {
            0, 18, 36, 54, 71, 89, 107, 125, 143, 160, 178, 195, 213,
            230, 248, 265, 282, 299, 316, 333, 350, 367, 384, 400, 416, 433,
            449, 465, 481, 496, 512, 527, 543, 558, 573, 587, 602, 616, 630,
            644, 658, 672, 685, 698, 711, 724, 737, 749, 761, 773, 784, 796,
            807, 818, 828, 839, 849, 859, 868, 878, 887, 896, 904, 912, 920,
            928, 935, 943, 949, 956, 962, 968, 974, 979, 984, 989, 994, 998,
            1002, 1005, 1008, 1011, 1014, 1016, 1018, 1020, 1022, 1023, 1023, 1024, 1024,
        };

        /// <summary>
        /// 0x100C31BC4. Арктангенс: индекс это (меньшая сторона &lt;&lt; 7) / большая,
        /// значение — градусы от 0 до 45.
        /// </summary>
        private static readonly byte[] AtanTable =
        {
            0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7,
            7, 8, 8, 8, 9, 9, 10, 10, 11, 11, 11, 12, 12, 13, 13, 14,
            14, 14, 15, 15, 16, 16, 17, 17, 17, 18, 18, 19, 19, 19, 20, 20,
            21, 21, 21, 22, 22, 22, 23, 23, 24, 24, 24, 25, 25, 25, 26, 26,
            27, 27, 27, 28, 28, 28, 29, 29, 29, 30, 30, 30, 31, 31, 31, 32,
            32, 32, 33, 33, 33, 34, 34, 34, 35, 35, 35, 35, 36, 36, 36, 37,
            37, 37, 37, 38, 38, 38, 39, 39, 39, 39, 40, 40, 40, 40, 41, 41,
            41, 41, 42, 42, 42, 42, 43, 43, 43, 43, 44, 44, 44, 44, 45, 45,
            45,
        };

        /// <summary>0x100C31CFC. Корень: значение равно round(sqrt(index) * 16).</summary>
        private static readonly byte[] SqrtTable =
        {
            0, 16, 22, 27, 32, 35, 39, 42, 45, 48, 50, 53, 55, 57, 59, 61,
            64, 65, 67, 69, 71, 73, 75, 76, 78, 80, 81, 83, 84, 86, 87, 89,
            90, 91, 93, 94, 96, 97, 98, 99, 101, 102, 103, 104, 106, 107, 108, 109,
            110, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126,
            128, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142,
            143, 144, 144, 145, 146, 147, 148, 149, 150, 150, 151, 152, 153, 154, 155, 155,
            156, 157, 158, 159, 160, 160, 161, 162, 163, 163, 164, 165, 166, 167, 167, 168,
            169, 170, 170, 171, 172, 173, 173, 174, 175, 176, 176, 177, 178, 178, 179, 180,
            181, 181, 182, 183, 183, 184, 185, 185, 186, 187, 187, 188, 189, 189, 190, 191,
            192, 192, 193, 193, 194, 195, 195, 196, 197, 197, 198, 199, 199, 200, 201, 201,
            202, 203, 203, 204, 204, 205, 206, 206, 207, 208, 208, 209, 209, 210, 211, 211,
            212, 212, 213, 214, 214, 215, 215, 216, 217, 217, 218, 218, 219, 219, 220, 221,
            221, 222, 222, 223, 224, 224, 225, 225, 226, 226, 227, 227, 228, 229, 229, 230,
            230, 231, 231, 232, 232, 233, 234, 234, 235, 235, 236, 236, 237, 237, 238, 238,
            239, 240, 240, 241, 241, 242, 242, 243, 243, 244, 244, 245, 245, 246, 246, 247,
            247, 248, 248, 249, 249, 250, 250, 251, 251, 252, 252, 253, 253, 254, 254, 255,
        };

        /// <summary>0x100a17bb4. Угол в диапазон от 0 до 359.</summary>
        public static int NormalizeAngle360(int angle)
        {
            int result = angle % 360;
            return result >= 0 ? result : result + 360;
        }

        /// <summary>0x100a17be8. Угол в диапазон от -180 до 180.</summary>
        public static int NormalizeAngle180(int angle)
        {
            int result = angle % 360;
            if (result < 0) result += 360;
            return result <= 179 ? result : result - 360;
        }

        /// <summary>
        /// 0x100a17c28. Синус угла в градусах, умноженный на 1024.
        ///
        /// Таблица хранит только первую четверть. Вторая половина круга даёт тот же
        /// модуль со знаком минус, а углы больше 90 зеркалятся через 180.
        /// </summary>
        public static int Sin(int angle)
        {
            int normalized = angle % 360;
            if (normalized < 0) normalized += 360;

            bool negative = normalized > 179;
            int index = negative ? normalized - 180 : normalized;
            if (index > 90) index = 180 - index;

            int value = SinTable[index];
            return negative ? -value : value;
        }

        /// <summary>Косинус через синус со сдвигом на 90 градусов.</summary>
        public static int Cos(int angle)
        {
            return Sin(angle + 90);
        }

        /// <summary>
        /// 0x100a17e9c. Угол вектора в градусах от 0 до 359.
        ///
        /// Отсчёт против часовой стрелки от оси X, то есть это atan2(y, x).
        /// Каждая четверть считается своей веткой, чтобы обойтись целыми числами:
        /// меньшая сторона делится на большую со сдвигом на семь бит, и частное
        /// ищется в таблице арктангенса.
        ///
        /// В таблице 129 значений, а не 128: когда стороны равны, частное выходит
        /// ровно 128, и этот индекс обязан существовать. На 128 там записано 45.
        /// </summary>
        public static int GetAngle(int x, int y)
        {
            if (x == 0 && y == 0) return 0;

            if (x >= 1 && y >= 0)
                return y >= x ? 90 - AtanTable[(x << 7) / y] : AtanTable[(y << 7) / x];

            int absX = x >= 0 ? x : -x;

            if (x <= 0 && y >= 1)
                return absX >= y ? 180 - AtanTable[(y << 7) / absX] : AtanTable[(absX << 7) / y] + 90;

            int absY = y >= 0 ? y : -y;

            if (x < 0 && y <= 0)
            {
                if (absY < absX) return AtanTable[(absY << 7) / absX] + 180;
                if (y != 0) return 270 - AtanTable[(absX << 7) / absY];
                return 0;
            }

            if (absX < absY) return AtanTable[(absX << 7) / absY] + 270;

            if (x != 0)
            {
                int degrees = 360 - AtanTable[(absY << 7) / absX];
                if (degrees >= 360) degrees -= 360;
                return degrees >= 0 ? degrees : degrees + 360;
            }

            return x;
        }

        /// <summary>
        /// 0x100a17c8c. Целый квадратный корень.
        ///
        /// Числа меньше 256 берутся прямо из таблицы. Дальше таблица даёт начальное
        /// приближение по старшим битам, а доводит его шаг Ньютона; для чисел от
        /// 0x1000000 шагов два. В конце оригинал снимает единицу, если квадрат
        /// перелетел исходное число — там это сделано заёмом в 64 битах.
        /// </summary>
        public static int Sqrt(int value)
        {
            if (value < 0) return -1;

            if (value < 0x10000)
            {
                if (value < 256) return SqrtTable[value] >> 4;

                int guess;

                if (value < 0x1000)
                    guess = value < 0x400
                        ? (SqrtTable[value >> 2] >> 3) + 1
                        : (SqrtTable[value >> 4] >> 2) + 1;
                else
                    guess = value < 0x4000
                        ? (SqrtTable[value >> 6] >> 1) + 1
                        : SqrtTable[value >> 8] + 1;

                return Correct(value, guess);
            }

            int approximation;

            if ((value >> 24) != 0)
            {
                int shift;
                int scale;

                if ((value >> 28) != 0)
                {
                    if ((value >> 30) != 0)
                    {
                        // Особенность оригинала: для int.MaxValue он отдаёт 65535,
                        // хотя настоящий корень равен 46340. Оставлено как есть —
                        // сервер обязан считать ровно то же, что и клиент.
                        if (value == 0x7FFFFFFF) return 0xFFFF;
                        scale = 8;
                        shift = 24;
                    }
                    else
                    {
                        scale = 7;
                        shift = 22;
                    }
                }
                else
                {
                    shift = (value >> 26) != 0 ? 20 : 18;
                    scale = (value >> 26) != 0 ? 6 : 5;
                }

                int first = SqrtTable[value >> shift] << scale;
                approximation = (value / first + first + 1) >> 1;
            }
            else
            {
                int shift = (value >> 18) != 0 ? 12 : 10;
                int scale = (value >> 18) != 0 ? 2 : 1;

                if (value >= 0x100000)
                {
                    shift = (value >> 22) != 0 ? 16 : 14;
                    scale = (value >> 22) != 0 ? 4 : 3;
                }

                approximation = SqrtTable[value >> shift] << scale;
            }

            return Correct(value, (value / approximation + approximation + 1) >> 1);
        }

        /// <summary>
        /// Доводка корня: если квадрат перелетел, ответ на единицу меньше.
        ///
        /// Сравнение обязано быть беззнаковым. Квадрат приближения доходит до
        /// 46341 * 46341, а это уже больше int.MaxValue: в знаковом сравнении
        /// произведение уходит в минус, проверка не срабатывает, и корень
        /// получается на единицу больше нужного. В оригинале то же самое сделано
        /// вычитанием в 64 битах с заёмом.
        /// </summary>
        private static int Correct(int value, int guess)
        {
            return (uint)value < (uint)(guess * guess) ? guess - 1 : guess;
        }

        /// <summary>
        /// 0x100a1807c. Оценка длины вектора без корня: большая сторона плюс
        /// 53/128 меньшей. Оригинал берёт её там, где важнее скорость.
        /// </summary>
        public static int SqrtApproximate(int x, int y)
        {
            int absX = x >= 0 ? x : -x;
            int absY = y >= 0 ? y : -y;

            int max = absX <= absY ? absY : absX;
            int min = absX >= absY ? absY : absX;

            return max + ((53 * min) >> 7);
        }

        /// <summary>0x100a18044. Возведение в степень удвоением.</summary>
        public static int Pow(int value, int power)
        {
            if (power == 0) return 1;

            int result = 1;

            while (true)
            {
                if ((power & 1) != 0) result *= value;
                value *= value;

                bool more = (uint)power > 1;
                power >>= 1;
                if (!more) break;
            }

            return result;
        }

        /// <summary>0x100a18008. Число единичных битов.</summary>
        public static int GetBitsInInteger(int value)
        {
            uint bits = (uint)value;
            int count = 0;

            while (bits != 0)
            {
                count += (int)(bits & 1);
                bits >>= 1;
            }

            return count;
        }

        /// <summary>
        /// 0x100a180a8. Квадрат расстояния от точки до отрезка.
        ///
        /// Проекция ищется скалярным произведением: если она вышла за начало или
        /// за конец отрезка, считается расстояние до соответствующего конца.
        /// </summary>
        public static int GetDistanceSquareToLine(
            int lineStartX, int lineStartY,
            int lineEndX, int lineEndY,
            int pointX, int pointY)
        {
            int lineX = lineEndX - lineStartX;
            int lineY = lineEndY - lineStartY;
            int pointRelativeX = pointX - lineStartX;
            int pointRelativeY = pointY - lineStartY;

            int lineLengthSquare = lineX * lineX + lineY * lineY;

            if (lineLengthSquare == 0)
                return pointRelativeX * pointRelativeX + pointRelativeY * pointRelativeY;

            int projection = pointRelativeX * lineX + pointRelativeY * lineY;

            if (projection <= 0)
                return pointRelativeX * pointRelativeX + pointRelativeY * pointRelativeY;

            if (projection >= lineLengthSquare)
                return (pointX - lineEndX) * (pointX - lineEndX)
                     + (pointY - lineEndY) * (pointY - lineEndY);

            return pointRelativeX * pointRelativeX + pointRelativeY * pointRelativeY
                 - (int)((long)(uint)projection * (uint)projection / (uint)lineLengthSquare);
        }

        /// <summary>Квадрат расстояния между двумя точками.</summary>
        public static int GetDistanceSquared(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            return dx * dx + dy * dy;
        }

        /// <summary>Расстояние между двумя точками.</summary>
        public static int GetDistance(int x1, int y1, int x2, int y2)
        {
            return Sqrt(GetDistanceSquared(x1, y1, x2, y2));
        }

        /// <summary>
        /// Смещение по X на заданное расстояние вдоль угла прицела.
        ///
        /// ВНИМАНИЕ: в движке живут две конвенции угла, и путать их нельзя.
        /// GetAngle возвращает atan2(y, x) — ноль смотрит вдоль оси X. А угол
        /// прицела персонажа (поле +360) хранится со сдвигом на 90 градусов:
        /// направление из него берётся как (Sin(a), Sin(a - 90)), то есть ноль
        /// смотрит вверх, против оси Y. Это видно в tickSkills по 0x10044f9b8,
        /// где X считается через Sin(угол), а Y через Sin(угол - 90); там же
        /// строкой ниже видно, что сам угол получен как MoveAngle плюс девяносто.
        /// </summary>
        public static int GetAimOffsetX(int aimAngle, int length)
        {
            return Sin(aimAngle) * length / SinScale;
        }

        /// <summary>Смещение по Y вдоль угла прицела.</summary>
        public static int GetAimOffsetY(int aimAngle, int length)
        {
            return Sin(aimAngle - 90) * length / SinScale;
        }

        /// <summary>Угол прицела из вектора: это GetAngle со сдвигом на девяносто.</summary>
        public static int GetAimAngle(int x, int y)
        {
            return NormalizeAngle360(GetAngle(x, y) + 90);
        }

        /// <summary>
        /// 0x1004579f8. Поворот вектора на угол. Делится на 1024, потому что
        /// синус в таблице уже умножен на 1024.
        /// </summary>
        public static int GetRotatedX(int x, int y, int angle)
        {
            return (Cos(angle) * x - Sin(angle) * y) / SinScale;
        }

        public static int GetRotatedY(int x, int y, int angle)
        {
            return (Sin(angle) * x + Cos(angle) * y) / SinScale;
        }
    }
}
