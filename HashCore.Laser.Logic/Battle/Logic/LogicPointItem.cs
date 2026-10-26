namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Кристалл захвата: предмет, который валяется на земле и подбирается на ходу.
    ///
    /// Жизнь у него из двух частей. Сначала он отлетает от того места, где
    /// появился, к точке падения — по дуге, чтобы было видно, откуда он взялся.
    /// Потом лежит, пока кто-нибудь не подойдёт, и уже к нему летит сам.
    ///
    /// Очко команде засчитывается не в момент прикосновения, а когда кристалл
    /// долетел: перехватить его в этот промежуток нельзя, но и убитый по дороге
    /// боец его не получит.
    /// </summary>
    public class LogicPointItem : LogicItemServer
    {
        /// <summary>Сколько времени кристалл летит к подобравшему. Поле +?, из Item::PickUp.</summary>
        public const int PickUpDuration = 250;

        /// <summary>На сколько время убывает за тик. Тик боя это пятьдесят единиц.</summary>
        public const int TimeUnitsPerTick = 50;

        /// <summary>За сколько единиц от бойца кристалл к нему тянется.</summary>
        public const int PickUpRange = 350;

        /// <summary>Сколько времени кристалл летит к месту падения.</summary>
        public const int MoveDuration = 250;

        /// <summary>Потолок доли пройденного пути, в тысячных.</summary>
        public const int MaxProgress = 1000;

        /// <summary>Кто кристалл подобрал; пока он не подобран, здесь пусто.</summary>
        public LogicCharacterServer Picker;

        /// <summary>Сколько времени осталось до того, как кристалл долетит.</summary>
        public int PickUpTimer;

        /// <summary>Откуда кристалл летит.</summary>
        public int StartX;
        public int StartY;

        /// <summary>Куда кристалл падает после появления.</summary>
        public int TargetX;
        public int TargetY;

        /// <summary>Сколько времени осталось до падения.</summary>
        public int MoveTimer;

        public LogicPointItem(ItemData data) : base(data)
        {
        }

        protected override int GetItemType()
        {
            return LogicItemType.Point;
        }

        /// <summary>Кристалл ещё летит к месту падения и подобрать его нельзя.</summary>
        public bool IsFlying
        {
            get { return MoveTimer > 0; }
        }

        /// <summary>Кристалл уже чей-то.</summary>
        public bool IsPickedUp
        {
            get { return Picker != null; }
        }

        /// <summary>Отправляет кристалл из точки появления в точку падения.</summary>
        public void Throw(int fromX, int fromY, int toX, int toY)
        {
            StartX = fromX;
            StartY = fromY;
            TargetX = toX;
            TargetY = toY;

            MoveTimer = MoveDuration;

            SetPosition(fromX, fromY);
        }

        /// <summary>
        /// 0x1004761b4. Кристалл начинает лететь к тому, кто его подобрал.
        ///
        /// Точка вылета запоминается здесь же: боец двигается, и тянуть кристалл
        /// надо от того места, где он лежал, а не от того, где боец стоит сейчас.
        /// </summary>
        public void PickUp(LogicCharacterServer character)
        {
            Picker = character;
            PickUpTimer = PickUpDuration;

            StartX = X;
            StartY = Y;
        }

        /// <summary>Подобранный и долетевший кристалл снимается с поля.</summary>
        public override bool ShouldDestruct()
        {
            return IsPickedUp && PickUpTimer <= 0;
        }

        public override void Tick()
        {
            base.Tick();

            if (IsPickedUp)
            {
                TickPickUp();
                return;
            }

            TickFlight();
        }

        /// <summary>
        /// Полёт к подобравшему. Если тот умер по дороге, кристалл остаётся там,
        /// где его застали, и снова становится ничьим.
        /// </summary>
        private void TickPickUp()
        {
            if (Picker != null && !Picker.IsAlive())
            {
                Picker = null;
                PickUpTimer = 0;
                return;
            }

            PickUpTimer -= TimeUnitsPerTick;

            int progress = MaxProgress - MaxProgress * PickUpTimer / PickUpDuration;
            if (progress > MaxProgress) progress = MaxProgress;
            if (progress < 0) progress = 0;

            SetPosition(
                (Picker.X * progress + StartX * (MaxProgress - progress)) / MaxProgress,
                (Picker.Y * progress + StartY * (MaxProgress - progress)) / MaxProgress);

            if (PickUpTimer > 0) return;

            if (Picker.IsAlive()) Picker.ApplyItem(this);
        }

        /// <summary>Полёт к месту падения.</summary>
        private void TickFlight()
        {
            if (MoveTimer <= 0) return;

            MoveTimer -= TimeUnitsPerTick;
            if (MoveTimer < 0) MoveTimer = 0;

            int progress = MaxProgress - MaxProgress * MoveTimer / MoveDuration;
            if (progress > MaxProgress) progress = MaxProgress;
            if (progress < 0) progress = 0;

            SetPosition(
                (TargetX * progress + StartX * (MaxProgress - progress)) / MaxProgress,
                (TargetY * progress + StartY * (MaxProgress - progress)) / MaxProgress);
        }

        /// <summary>
        /// Может ли этот боец его подобрать. Летящий и уже подобранный кристалл
        /// не трогают, остальное решают данные.
        /// </summary>
        public virtual bool CanBePickedUpBy(LogicCharacterServer character)
        {
            if (IsPickedUp || IsFlying) return false;
            if (character == null || !character.IsAlive()) return false;

            return ItemData == null || ItemData.CanBePickedUp;
        }
    }

    /// <summary>
    /// Источник кристаллов: стоит посреди карты и раз в несколько секунд
    /// выбрасывает новый кристалл в случайную сторону.
    ///
    /// Первый кристалл появляется раньше остальных — иначе начало боя проходило
    /// бы в пустую, пока обе команды бегут к середине.
    /// </summary>
    public class LogicOrbSpawnerItem : LogicItemServer
    {
        /// <summary>На каком тике боя появится первый кристалл.</summary>
        public const int FirstSpawnTick = 40;

        /// <summary>Сколько тиков между кристаллами.</summary>
        public const int SpawnIntervalTicks = 140;

        /// <summary>Как далеко от источника кристалл падает.</summary>
        public const int ThrowDistance = 600;

        /// <summary>Данные кристалла, которые источник выбрасывает.</summary>
        public ItemData PointData;

        public LogicOrbSpawnerItem(ItemData data) : base(data)
        {
            SpawningTick = FirstSpawnTick;
        }

        protected override int GetItemType()
        {
            return LogicItemType.OrbSpawner;
        }

        /// <summary>Источник стоит до конца боя.</summary>
        protected override bool IsPermanent()
        {
            return true;
        }

        public override void Tick()
        {
            base.Tick();

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null || PointData == null) return;

            // Единица означает, что время следующего появления ещё не назначено:
            // так его выставляет сам оригинал на первом же тике.
            if (SpawningTick == 1) SpawningTick = FirstSpawnTick;

            if (battle.Tick < SpawningTick) return;

            SpawnPoint(battle);

            SpawnedTick = battle.Tick;
            SpawningTick = battle.Tick + SpawnIntervalTicks;
        }

        /// <summary>Выбрасывает один кристалл в случайную сторону.</summary>
        protected virtual void SpawnPoint(LogicBattleModeServer battle)
        {
            LogicPointItem point = new LogicPointItem(PointData);

            int angle = battle.Random.Rand(360);

            int toX = X + LogicMath.GetRotatedX(ThrowDistance, 0, angle);
            int toY = Y + LogicMath.GetRotatedY(ThrowDistance, 0, angle);

            battle.GameObjectManager.AddGameObject(point);
            point.Throw(X, Y, toX, toY);
        }
    }
}
