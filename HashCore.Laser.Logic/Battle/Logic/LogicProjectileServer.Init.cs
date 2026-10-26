namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Запуск снаряда, 0x100477118.
    /// </summary>
    public partial class LogicProjectileServer
    {
        /// <summary>
        /// Числитель времени полёта у снарядов с постоянным временем. Отсюда
        /// и берётся «постоянство»: время не зависит от расстояния, только от
        /// скорости.
        /// </summary>
        public const int ConstantFlyTimeNumerator = 18000;

        /// <summary>Во сколько раз расстояние переводится в тики при делении на скорость.</summary>
        public const int FlightTimeScale = 20;

        /// <summary>Поле +300. Скорость, с которой снаряд стартовал.</summary>
        public int LaunchSpeed;

        /// <summary>Поле +1276. Сколько было до цели в момент выстрела.</summary>
        public int ShotDistance;

        /// <summary>Поле +1320. Процент усиления урона, унесённый от хозяина.</summary>
        public int DamageBuffPercent;

        /// <summary>Урон, который снаряд несёт.</summary>
        public int Damage;

        /// <summary>Поле +1212. Время полёта, заданное снаружи; ноль — считать самому.</summary>
        public int FlightTicksOverride;

        /// <summary>Поле +188. Урон в процентах от здоровья цели.</summary>
        public int PercentDamage;

        /// <summary>Поле +920. Какой по счёту выстрел в очереди выпустил этот снаряд.</summary>
        public int ShotIndex;

        /// <summary>Поля +928 и +932. Куда целились, до разброса.</summary>
        public int AimX;
        public int AimY;

        /// <summary>Поле +868. Каким видом атаки снаряд выпущен.</summary>
        public int AttackKind;

        /// <summary>Поле +1308. Заряд стрелка в момент выстрела.</summary>
        public int ChargedShotValue;

        /// <summary>Поле +944. Особые параметры выстрела.</summary>
        public readonly LogicAttackSpecialParams SpecialParams = new LogicAttackSpecialParams();

        /// <summary>Поле +1100. Особые параметры уже применены.</summary>
        public bool SpecialParamsApplied = true;

        /// <summary>Поле +860. Сколько тиков снаряд уже прожил.</summary>
        public int LivedTicks;

        /// <summary>Поле +864. Снаряд уже сделал хотя бы один шаг.</summary>
        public bool HasTicked;

        /// <summary>Колонка EarlyTicks. Сколько шагов снаряд делает сразу при выстреле.</summary>
        public int EarlyTicks;

        /// <summary>
        /// 0x10047e4b0. Прогоняет снаряду первые шаги прямо в момент выстрела.
        ///
        /// Нужно это тем снарядам, которые должны появиться уже в полёте, а не у
        /// самого дула: без этого выстрел в упор давал бы попадание на кадр позже,
        /// чем ему положено.
        /// </summary>
        public void RunEarlyTicks()
        {
            for (int i = 0; i < EarlyTicks; i++)
            {
                LivedTicks++;
                Tick();
                HasTicked = true;
            }
        }

        /// <summary>
        /// 0x100477118. Готовит снаряд к полёту.
        ///
        /// Время полёта считается как двадцать расстояний, делённых на скорость, —
        /// ровно та же формула, по которой ходит персонаж. У снарядов с постоянным
        /// временем полёта вместо расстояния берётся фиксированное число, поэтому
        /// они летят одинаково долго и вблизи, и вдали.
        ///
        /// Деление округляется к ближайшему, а не вниз: оригинал считает его через
        /// умножение на тысячу с прибавкой пятисот.
        /// </summary>
        public void Init(int fromX, int fromY, int toX, int toY, int startTick, int speedPenalty)
        {
            // Поле +300. Скорость это скорость из таблицы за вычетом штрафа; штраф
            // бывает и отрицательным, и тогда снаряд летит быстрее обычного.
            int speed = (ProjectileData != null ? ProjectileData.Speed : 0) - speedPenalty;

            LaunchSpeed = speed;
            BaseSpeed = speed;
            LastSpeed = speed;

            StartTick = startTick;

            int flightTicks = FlightTicksOverride;

            if (flightTicks <= 0)
            {
                int dx = toX - fromX;
                int dy = toY - fromY;

                int distance = LogicMath.Sqrt(dx * dx + dy * dy);
                ShotDistance = distance;

                int divisor = speed <= 1 ? 1 : speed;

                bool constantFlyTime = ProjectileData != null && ProjectileData.ConstantFlyTime;
                int numerator = constantFlyTime ? ConstantFlyTimeNumerator : FlightTimeScale * distance;

                flightTicks = RoundedDivision(numerator, divisor);
                if (flightTicks <= 1) flightTicks = 1;
            }

            EndTick = flightTicks + startTick;

            if (ProjectileData != null && ProjectileData.TravelType == TravelTypeTimeShifted)
            {
                int variable = TravelTypeVariableOverride != 0
                    ? TravelTypeVariableOverride
                    : ProjectileData.TravelTypeVariable;

                EndTick += variable;
            }

            SpeedProgressPerTick = MaxSpeedProgress / (EndTick - StartTick);

            EndX = toX;
            EndY = toY;

            SetPosition(fromX, fromY);

            PrevTickX = TickX;
            PrevTickY = TickY;
            TickX = fromX;
            TickY = fromY;

            StartX = X;
            StartY = Y;

            Angle = LogicMath.GetAngle(EndX - StartX, EndY - StartY);
        }

        /// <summary>
        /// 0x100509dd4. Деление с округлением к ближайшему.
        ///
        /// Оригинал делает это в 64 битах: умножает на тысячу, прибавляет пятьсот
        /// и делит на тысячу. Обычное целое деление дало бы на тик меньше.
        /// </summary>
        public static int RoundedDivision(int value, int divisor)
        {
            if (divisor == 0) return 0;
            return (int)((1000L * value / divisor + 500) / 1000);
        }
    }
}
