namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Персонаж, собранный из строки characters.csv.
    ///
    /// Это первая конкретная реализация поверх LogicCharacterServer: она отвечает
    /// на все вопросы, которые базовый класс задаёт данным, — жив ли герой, какой
    /// у него радиус, скорость, срок жизни и так далее.
    /// </summary>
    public class LogicHeroCharacter : LogicCharacterServer
    {
        public LogicHeroCharacter(CharacterData data) : base(data)
        {
            if (data == null) return;

            SetHeroLevel(DefaultHeroLevel);

            // Ссылки на другие таблицы оригинал разрешает один раз, при загрузке.
            // Здесь то же самое, только по имени из строки.
            AutoAttackProjectile = FindProjectile(data.AutoAttackProjectile);
        }

        /// <summary>
        /// Собирает оружие бойца из таблиц: умение по имени из characters.csv,
        /// снаряд — по имени из строки умения.
        ///
        /// Возвращает ложь, если чего-то из этой цепочки в таблицах нет: боец
        /// тогда остаётся безоружным, но живым.
        /// </summary>
        public bool EquipWeaponFromData()
        {
            if (CharacterData == null || string.IsNullOrEmpty(CharacterData.WeaponSkill)) return false;

            SkillData skillData = FindSkill(CharacterData.WeaponSkill);
            if (skillData == null) return false;

            LogicWeaponSkill weapon = new LogicWeaponSkill(skillData, this);

            // Снарядов у умения список: в attack (0x10045aab0) нужный берётся по
            // номеру варианта выстрела, и нулевой — обычный.
            List<string> names = LogicDataAccess.GetStringList(skillData, "Projectiles");

            weapon.Projectiles.Clear();

            for (int i = 0; i < names.Count; i++)
            {
                ProjectileData projectile = FindProjectile(names[i]);
                if (projectile != null) weapon.Projectiles.Add(projectile);
            }

            weapon.Projectile = weapon.Projectiles.Count > 0 ? weapon.Projectiles[0] : null;

            Skills[0] = weapon;
            SkillCount = 1;

            EquipUltimateFromData();

            return weapon.Projectile != null;
        }

        /// <summary>
        /// Собирает ульту из строки бойца. Её может не быть вовсе — тогда у бойца
        /// остаётся одно умение, и в кадр уходит ровно одна запись.
        /// </summary>
        public bool EquipUltimateFromData()
        {
            if (CharacterData == null || string.IsNullOrEmpty(CharacterData.UltimateSkill)) return false;

            SkillData skillData = FindSkill(CharacterData.UltimateSkill);
            if (skillData == null) return false;

            LogicWeaponSkill ultimate = new LogicWeaponSkill(skillData, this);

            List<string> names = LogicDataAccess.GetStringList(skillData, "Projectiles");

            ultimate.Projectiles.Clear();

            for (int i = 0; i < names.Count; i++)
            {
                ProjectileData projectile = FindProjectile(names[i]);
                if (projectile != null) ultimate.Projectiles.Add(projectile);
            }

            ultimate.Projectile = ultimate.Projectiles.Count > 0 ? ultimate.Projectiles[0] : null;

            Skills[1] = ultimate;
            SkillCount = 2;

            return true;
        }

        /// <summary>Уровень бойца по умолчанию. Тот же, с которым игрок заходит в бой.</summary>
        public const int DefaultHeroLevel = 11;

        /// <summary>На сколько процентов от исходного растёт здоровье за уровень.</summary>
        public const int PerLevelPercent = 5;

        /// <summary>Уровень бойца в этом бою.</summary>
        public int HeroLevel = DefaultHeroLevel;

        /// <summary>Прибавка к урону от уровня, в процентах.</summary>
        public int DamageMultiplier;

        /// <summary>
        /// Задаёт уровень и пересчитывает от него здоровье.
        ///
        /// Прибавка за уровень считается от исходного здоровья и округляется вниз
        /// один раз, а не на каждом уровне: иначе у высоких уровней набегала бы
        /// заметная разница.
        /// </summary>
        public void SetHeroLevel(int level)
        {
            HeroLevel = level;

            int baseHealth = CharacterData != null ? CharacterData.Hitpoints : 0;
            int perLevel = (int)((float)PerLevelPercent / 100 * baseHealth);

            SetMaxHealth(baseHealth + perLevel * (level - 1));

            DamageMultiplier = (level - 1) * PerLevelPercent;
        }

        /// <summary>
        /// Урон основного оружия с учётом уровня. Множитель уровня входит в
        /// показатель дважды — сам уровень и накопленная от него прибавка.
        /// </summary>
        public int GetNormalWeaponDamage()
        {
            LogicWeaponSkill weapon = GetWeapon();
            if (weapon == null || weapon.Data == null) return 0;

            int damage = weapon.Data.Damage;
            int perLevel = (int)((float)PerLevelPercent / 100 * damage);

            return damage + perLevel * (HeroLevel + DamageMultiplier);
        }

        /// <summary>Основное оружие бойца, если оно собрано.</summary>
        public LogicWeaponSkill GetWeapon()
        {
            return Skills[0] as LogicWeaponSkill;
        }

        private static SkillData FindSkill(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_skillsByName == null)
            {
                _skillsByName = new Dictionary<string, SkillData>();

                DataTable table = DataTables.Get(DataType.Skill);
                if (table != null)
                {
                    foreach (LogicData data in table.GetDatas())
                    {
                        SkillData skill = data as SkillData;
                        if (skill != null && !string.IsNullOrEmpty(skill.Name))
                            _skillsByName[skill.Name] = skill;
                    }
                }
            }

            SkillData found;
            return _skillsByName.TryGetValue(name, out found) ? found : null;
        }

        /// <summary>
        /// Снаряды и умения ищутся по имени, и каждый боец спрашивает одни и те же
        /// строки. Поэтому имена разбираются один раз и дальше берутся из памяти:
        /// перебор всей таблицы на каждое появление бойца обходился бы дорого.
        /// </summary>
        private static Dictionary<string, ProjectileData> _projectilesByName;

        private static Dictionary<string, SkillData> _skillsByName;

        private static ProjectileData FindProjectile(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (_projectilesByName == null)
            {
                _projectilesByName = new Dictionary<string, ProjectileData>();

                DataTable table = DataTables.Get(DataType.Projectile);
                if (table != null)
                {
                    foreach (LogicData data in table.GetDatas())
                    {
                        ProjectileData projectile = data as ProjectileData;
                        if (projectile != null && !string.IsNullOrEmpty(projectile.Name))
                            _projectilesByName[projectile.Name] = projectile;
                    }
                }
            }

            ProjectileData found;
            return _projectilesByName.TryGetValue(name, out found) ? found : null;
        }

        /// <summary>
        /// Декорации и базы боевого блока не пишут: у них нет ни автоатаки, ни
        /// скорости. Обучающая мишень исключение — она считается дерущейся.
        /// </summary>
        protected override bool IsCombatant()
        {
            if (CharacterData == null) return false;

            return CharacterData.Speed > 0
                || CharacterData.IsHero()
                || CharacterData.IsTrainingDummy();
        }

        protected override bool IsHero()
        {
            return CharacterData != null && CharacterData.IsHero();
        }

        protected override int GetCharacterKind()
        {
            return LogicCharacterType.Parse(CharacterData != null ? CharacterData.Type : null);
        }

        protected override int GetUniqueProperty()
        {
            return CharacterData != null ? CharacterData.UniqueProperty : 0;
        }

        protected override bool IsPet()
        {
            return CharacterData != null && CharacterData.IsPet();
        }

        protected override bool IsTwinMinion()
        {
            return CharacterData != null && CharacterData.IsTwin();
        }

        protected override int GetAutoAttackModeFromData()
        {
            return LogicAutoAttackModeNames.Parse(CharacterData != null ? CharacterData.AutoAttackMode : null);
        }

        protected override int GetBurstDelayTicks()
        {
            return CharacterData != null ? CharacterData.AutoAttackTicksBetweenBurstShots : 0;
        }

        protected override int GetAutoAttackBulletCount()
        {
            if (CharacterData == null) return 1;

            return CharacterData.AutoAttackBulletsPerShot > 1 ? CharacterData.AutoAttackBulletsPerShot : 1;
        }

        protected override int GetAutoAttackSpread()
        {
            return CharacterData != null ? CharacterData.AutoAttackProjectileSpread : 0;
        }

        protected override int GetAutoAttackRange()
        {
            return CharacterData != null ? CharacterData.AutoAttackRange : 0;
        }

        protected override int GetAutoAttackDamageFromData()
        {
            return CharacterData != null ? CharacterData.AutoAttackDamage : 0;
        }

        /// <summary>
        /// Поле данных +436. Скорость автоатаки задана в миллисекундах, а тик идёт
        /// каждые пятьдесят: деление и переводит одно в другое.
        /// </summary>
        public override int GetCurrentAttackSpeedTicks()
        {
            if (CharacterData == null) return 0;

            return CharacterData.AutoAttackSpeedMs / SkillTimeUnitsPerTick;
        }

        /// <summary>
        /// Достаёт из таблицы снаряд автоатаки. У бойцов без автоатаки его там
        /// нет, и они остаются с пустым — это не ошибка.
        /// </summary>
        public bool LoadAutoAttackProjectile()
        {
            if (CharacterData == null) return false;

            AutoAttackProjectile = FindProjectile(CharacterData.AutoAttackProjectile);
            return AutoAttackProjectile != null;
        }

        public override bool IsBase()
        {
            return CharacterData != null && CharacterData.IsBase();
        }

        protected override bool IsDecoy()
        {
            return CharacterData != null && CharacterData.IsDecoy();
        }

        protected override bool IsCarryable()
        {
            return CharacterData != null && CharacterData.IsCarryable();
        }

        protected override bool IsPayload()
        {
            return CharacterData != null && CharacterData.IsPayload();
        }

        protected override bool IsTrain()
        {
            return CharacterData != null && CharacterData.IsTrain();
        }

        protected override bool IsMegaBoss()
        {
            return CharacterData != null && CharacterData.IsBoss();
        }

        public override int GetCollisionRadius()
        {
            return CharacterData != null ? CharacterData.CollisionRadius : 0;
        }

        public override int GetRadius()
        {
            return GetCollisionRadius();
        }

        protected override int GetBaseSpeed()
        {
            return CharacterData != null ? CharacterData.Speed : 0;
        }

        protected override bool AllowMovementControl()
        {
            return IsAlive() && !Stunned && !InStasis;
        }

        /// <summary>
        /// Персонаж без пути стоит на месте: движение идёт только по найденному
        /// маршруту, и пока его нет, тики движения ничего не делают.
        /// </summary>
        protected override void SetStraightPath(int x, int y)
        {
            PathX.Clear();
            PathY.Clear();

            // Путь хранится задом наперёд: нулевой элемент это цель.
            PathX.Add(x);
            PathY.Add(y);
            PathX.Add(X);
            PathY.Add(Y);
        }

        /// <summary>
        /// 0x10045f40c. Строит маршрут через поиск пути.
        ///
        /// Если до цели дороги нет, персонаж всё равно идёт — но до ближайшего
        /// узла, куда поиск сумел добраться. Иначе он просто вставал бы на месте
        /// перед любой стеной.
        /// </summary>
        protected override void FindPathTo(int x, int y, bool allowPartial, bool flag)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            LogicPathFinder finder = battle != null ? battle.GetPathFinder() : null;

            if (finder == null)
            {
                SetStraightPath(x, y);
                return;
            }

            int layer = GetSizeSubTilesForPathfinding();

            int startNode = finder.GetNode(
                LogicTileMap.LogicToPathFinderTile(X), LogicTileMap.LogicToPathFinderTile(Y));
            int goalNode = finder.GetNode(
                LogicTileMap.LogicToPathFinderTile(x), LogicTileMap.LogicToPathFinderTile(y));

            bool found = finder.FindPath(layer, startNode, goalNode, CanMoveThroughWalls(), CanWalkOnWater(), false);

            if (!found)
            {
                if (!allowPartial || finder.ReachedNode == startNode)
                {
                    SetStraightPath(x, y);
                    return;
                }

                int reachedX = LogicTileMap.PathFinderTileToLogic(finder.ReachedNode % finder.Width, true);
                int reachedY = LogicTileMap.PathFinderTileToLogic(finder.ReachedNode / finder.Width, true);

                SetStraightPath(reachedX, reachedY);
                return;
            }

            PathX.Clear();
            PathY.Clear();

            // Поиск отдаёт узлы от цели к старту — ровно в том порядке, в каком
            // путь и хранится у персонажа.
            for (int i = 0; i < finder.Path.Count; i++)
            {
                int node = finder.Path[i];

                PathX.Add(LogicTileMap.PathFinderTileToLogic(node % finder.Width, true));
                PathY.Add(LogicTileMap.PathFinderTileToLogic(node / finder.Width, true));
            }
        }

        /// <summary>
        /// 0x100458454. Размер персонажа в подклетках: крупные и боссы занимают две.
        /// </summary>
        public virtual int GetSizeSubTilesForPathfinding()
        {
            if (CharacterData == null) return 1;

            if (CharacterData.IsCarryable())
                return CharacterData.CollisionRadius > 199 ? 2 : 0;

            return CharacterData.CollisionRadius > 200 || CharacterData.IsBoss() ? 2 : 1;
        }

        protected override int GetPathLength()
        {
            if (PathX.Count < 2) return 0;

            int length = 0;

            for (int i = PathX.Count - 1; i > 0; i--)
            {
                int dx = PathX[i - 1] - PathX[i];
                int dy = PathY[i - 1] - PathY[i];
                length += LogicMath.Sqrt(dx * dx + dy * dy);
            }

            return length;
        }

        protected override int GetIntroTicks()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            return battle != null ? battle.GetIntroTicks() : 0;
        }

        protected override bool HasEventModifier(int modifier)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            return battle != null && battle.HasEventModifier(modifier);
        }

        protected override bool IsIntroOver()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return true;

            return battle.Tick >= battle.GetIntroTicks();
        }

        protected override bool IsMovementFrozen()
        {
            return true;
        }
    }
}
