namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Разброс выстрела: 0x10047e638 и 0x10047df78.
    /// </summary>
    public partial class LogicProjectileServer
    {
        /// <summary>На сколько единиц дальности приходится одна единица разброса.</summary>
        public const int SpreadDistanceScale = 300;

        /// <summary>Во сколько раз разброс больше у снарядов с постоянным временем полёта.</summary>
        public const int ConstantFlyTimeSpreadScale = 3;

        /// <summary>Ниже этого разброс не считается вовсе.</summary>
        public const int MinSpreadToApply = 3;

        /// <summary>
        /// 0x10047e638. Наибольшее отклонение выстрела в единицах поля.
        ///
        /// Разброс задан в промилле угла, и чем дальше цель, тем шире расходятся
        /// выстрелы. У снарядов с постоянным временем полёта расстояние не при чём
        /// — они всегда летят одинаково, поэтому разброс у них просто утроенный.
        ///
        /// Непрямой снаряд не может стрелять слишком кучно вблизи: если до цели
        /// ближе, чем задано в таблице, разброс всё равно считается по этому
        /// минимальному расстоянию.
        /// </summary>
        public static int GetSpreadMax(ProjectileData data, int spread, int distance)
        {
            int value = spread >= 0 ? spread : -spread;

            if (data != null && data.ConstantFlyTime) return ConstantFlyTimeSpreadScale * value;

            int scaled = distance;

            if (data != null && data.Indirect && data.MinDistanceForSpread > distance)
                scaled = data.MinDistanceForSpread;

            return scaled * value / SpreadDistanceScale;
        }

        /// <summary>
        /// 0x10047df78. Куда на самом деле уходит выстрел с учётом разброса.
        ///
        /// Разброс бывает двух видов. Заданный сдвигает точку прицела поперёк
        /// линии выстрела ровно на величину разброса — по ней и раскладывается
        /// веер дробовика. Случайный дёргает точку по обеим осям независимо, и
        /// тогда две одинаковые очереди уже не совпадут.
        ///
        /// После сдвига точка возвращается на прежнее расстояние от стрелка: иначе
        /// выстрел вбок оказывался бы ещё и длиннее. Исключение — снаряды, которым
        /// можно за границу карты: их просто зажимают в её пределах.
        /// </summary>
        public static void GetSpreadEndLocation(
            ProjectileData data,
            LogicTileMap map,
            LogicRandom random,
            int startX, int startY,
            int targetX, int targetY,
            int spread,
            bool fixedSpread,
            out int endX, out int endY)
        {
            endX = targetX;
            endY = targetY;

            int dx = targetX - startX;
            int dy = targetY - startY;

            int distance = LogicMath.Sqrt(dx * dx + dy * dy);
            int max = GetSpreadMax(data, spread, distance);

            if (max < MinSpreadToApply) return;

            if (fixedSpread)
            {
                // Поворот направления на прямой угол: синус девяноста это ровно
                // тысяча двадцать четыре, а синус ста восьмидесяти — ноль, так что
                // от всей формулы остаётся смена осей местами.
                int offsetX = (LogicMath.Sin(180) * dx - LogicMath.Sin(90) * dy) / 1024;
                int offsetY = (LogicMath.Sin(90) * dx + LogicMath.Sin(180) * dy) / 1024;

                int length = LogicMath.Sqrt(offsetX * offsetX + offsetY * offsetY);

                if (length != 0)
                {
                    offsetX = offsetX * max / length;
                    offsetY = offsetY * max / length;
                }

                if (spread >= 0)
                {
                    endX += offsetX;
                    endY += offsetY;
                }
                else
                {
                    endX -= offsetX;
                    endY -= offsetY;
                }
            }
            else if (random != null)
            {
                endX += 1 - max + 2 * random.Rand(max);
                endY += 1 - max + 2 * random.Rand(max);
            }

            if (data != null && data.Indirect)
            {
                if (map != null)
                {
                    endX = ClampToBorder(endX, map.LogicWidth);
                    endY = ClampToBorder(endY, map.LogicHeight);
                }

                return;
            }

            int newX = endX - startX;
            int newY = endY - startY;

            int newLength = LogicMath.Sqrt(newY * newY + newX * newX);

            if (newLength != 0)
            {
                newX = newX * distance / newLength;
                newY = newY * distance / newLength;
            }

            endX = newX + startX;
            endY = newY + startY;
        }

        private static int ClampToBorder(int value, int size)
        {
            int high = size - 2;
            int clamped = value < high ? value : high;
            return value <= 1 ? 1 : clamped;
        }
    }
}
