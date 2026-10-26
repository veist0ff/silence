namespace HashCore.Laser.Logic.Battle.Logic
{
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Клетка карты, 1:1 с LogicTile.
    ///
    /// Смещения оригинала:
    ///   +0   текущие данные клетки
    ///   +8   исходные данные клетки, к ним возвращает setData при восстановлении
    ///   +16  маска намертво закрытых подклеток
    ///   +24  маска, которую можно игнорировать первым флагом
    ///   +32  маска, которую можно игнорировать вторым флагом
    ///   +40  временная маска, обнуляется в конце каждого тика
    ///   +48  обратный отсчёт до восстановления клетки
    ///   +56  X клетки
    ///   +60  Y клетки
    ///   +72  счётчик времени жизни
    ///   +100 признак «наводить зону избегания вокруг клетки»
    ///   +121 клетка динамическая
    /// </summary>
    public class LogicTile
    {
        /// <summary>Сторона клетки в логических единицах.</summary>
        public const int TileSize = 300;

        /// <summary>Сторона подклетки пасфайндера. Три подклетки на клетку.</summary>
        public const int SubTileSize = 100;

        /// <summary>Сколько подклеток в клетке по стороне.</summary>
        public const int SubTilesPerTile = 3;

        public TileData Data;
        public TileData OriginalData;

        public int X;
        public int Y;

        /// <summary>Поле +16. Подклетки, закрытые в любом случае.</summary>
        public long BlockedMask;

        /// <summary>Поле +24. Снимается первым флагом isPassablePathFinder.</summary>
        public long ConditionalMaskA;

        /// <summary>Поле +32. Снимается вторым флагом.</summary>
        public long ConditionalMaskB;

        /// <summary>
        /// Поле +40. Ставится на один тик и обнуляется в самом конце LogicTile::tick,
        /// поэтому её нельзя чистить раньше — иначе временная непроходимость
        /// не доживёт до шага снарядов.
        /// </summary>
        public long TemporaryMask;

        /// <summary>Поле +48. Обратный отсчёт до восстановления разрушенной клетки.</summary>
        public int RespawnCountdown;

        /// <summary>
        /// Поле +72. Оригинал уменьшает его каждый тик, пока у данных клетки
        /// поле +88 не меньше единицы — в нашей таблице это Lifetime.
        /// </summary>
        public int LifetimeCounter;

        /// <summary>
        /// Поле +100. Уровень урона от клетки. Ненулевое значение значит, что клетка
        /// жжёт стоящего на ней (LogicCharacterServer::tickTile) и одновременно
        /// помечается как опасная для ботов (LogicTile::tick зовёт для неё
        /// addTilesToAvoidInCircle). Это ядовитое облако и подобное.
        /// </summary>
        public int DamageLevel;

        /// <summary>Поле +108. Клетка замедляет стоящего на ней.</summary>
        public int SlowLevel;

        /// <summary>
        /// Тип эффекта клетки. Значение 118 в tickTile обрабатывается особо:
        /// такая клетка бьёт на 500 через такт в шестьдесят тиков.
        /// </summary>
        public int EffectType;

        /// <summary>Поле +121.</summary>
        public bool IsDynamic;

        /// <summary>Клетка разрушена и ждёт восстановления. Байты +68 и +69 оригинала.</summary>
        public bool Destroyed;
        public bool Respawns;

        /// <summary>
        /// 0x1003b070c. Бит подклетки внутри маски: 9 бит на слой, внутри слоя
        /// строка за строкой.
        /// </summary>
        private static long GetSubTileBit(int layer, int subX, int subY)
        {
            return 1L << (9 * layer + subX + 3 * subY);
        }

        /// <summary>
        /// 0x1003b070c. Порядок проверок повторён буквально: сначала намертво
        /// закрытая маска, потом временная, потом две условных — причём вторая
        /// условная проверяется, только если предыдущие не закрыли подклетку.
        /// </summary>
        public bool IsPassablePathFinder(int layer, int subX, int subY, bool ignoreA, bool ignoreB)
        {
            if (subX < 0 || subX > 2 || subY < 0 || subY > 2) return false;

            long bit = GetSubTileBit(layer, subX, subY);

            if ((BlockedMask & bit) != 0) return false;

            bool blocked = (TemporaryMask & bit) != 0;

            if (!blocked && !ignoreA) blocked = (ConditionalMaskA & bit) != 0;
            if (!blocked && !ignoreB) blocked = (ConditionalMaskB & bit) != 0;

            return !blocked;
        }

        /// <summary>Какую из четырёх масок клетки правит разметка.</summary>
        public enum MaskKind
        {
            /// <summary>Поле +16. Закрыто в любом случае.</summary>
            Blocked,

            /// <summary>Поле +24.</summary>
            ConditionalA,

            /// <summary>Поле +32.</summary>
            ConditionalB,

            /// <summary>Поле +40. Живёт один тик.</summary>
            Temporary,
        }

        /// <summary>
        /// Наибольший размер персонажа в подклетках. Из getSizeSubTilesForPathfinding
        /// (0x100458454): обычный боец занимает одну подклетку, крупный и босс — две.
        /// </summary>
        public const int MaxSizeLayer = 2;

        /// <summary>
        /// 0x1003b1d90. Закрывает подклетку для персонажей заданного размера и всех,
        /// кто крупнее.
        ///
        /// Слой маски это и есть размер персонажа: пометка для размера один
        /// автоматически закрывает проход и для размера два, потому что цикл идёт
        /// от заданного слоя до наибольшего. Обратное неверно — то, что закрыто
        /// только для крупных, мелкий боец пройдёт.
        /// </summary>
        public void MarkBlockable(int sizeLayer, int subX, int subY, MaskKind kind)
        {
            if (subX < 0 || subX > 2 || subY < 0 || subY > 2) return;

            int lastLayer = sizeLayer <= MaxSizeLayer ? MaxSizeLayer : sizeLayer;

            long mask = GetMask(kind);

            for (int layer = sizeLayer; layer <= lastLayer; layer++)
                mask |= GetSubTileBit(layer, subX, subY);

            SetMask(kind, mask);
        }

        /// <summary>Закрывает всю клетку целиком для заданного размера и крупнее.</summary>
        public void MarkWholeTileBlockable(int sizeLayer, MaskKind kind)
        {
            for (int subY = 0; subY < SubTilesPerTile; subY++)
                for (int subX = 0; subX < SubTilesPerTile; subX++)
                    MarkBlockable(sizeLayer, subX, subY, kind);
        }

        private long GetMask(MaskKind kind)
        {
            switch (kind)
            {
                case MaskKind.ConditionalA: return ConditionalMaskA;
                case MaskKind.ConditionalB: return ConditionalMaskB;
                case MaskKind.Temporary: return TemporaryMask;
                default: return BlockedMask;
            }
        }

        private void SetMask(MaskKind kind, long mask)
        {
            switch (kind)
            {
                case MaskKind.ConditionalA: ConditionalMaskA = mask; break;
                case MaskKind.ConditionalB: ConditionalMaskB = mask; break;
                case MaskKind.Temporary: TemporaryMask = mask; break;
                default: BlockedMask = mask; break;
            }
        }

        /// <summary>0x1003b0784. Непроходимая клетка стоит максимум int.</summary>
        public int GetPathFinderCost(int layer, int subX, int subY, bool ignoreA, bool ignoreB)
        {
            return IsPassablePathFinder(layer, subX, subY, ignoreA, ignoreB) ? 0 : int.MaxValue;
        }

        /// <summary>0x1003b08f4. Зрение перекрывает то же, что и снаряды.</summary>
        public bool BlocksVision()
        {
            return Data != null && Data.BlocksProjectiles;
        }

        public bool BlocksMovement()
        {
            return Data != null && Data.BlocksMovement;
        }

        /// <summary>
        /// 0x10003f7e0. Клетка, через которую ходить нельзя, а стрелять можно, —
        /// вода или яма. Отдельной колонки под это нет, признак выводится из трёх
        /// других: движение держит, снаряды нет, и пробивным оружием не ломается.
        /// </summary>
        public bool IsWater()
        {
            if (Data == null || !Data.BlocksMovement) return false;
            if (Data.BlocksProjectiles) return false;

            return !Data.IsDestructibleNormalWeapon;
        }

        /// <summary>Поле +120. По этой воде всё же можно пройти.</summary>
        public bool WaterWalkable;

        /// <summary>
        /// Поле +64. Клетка прячет того, кто в ней стоит. Это кусты: боец в них
        /// виден только вблизи, и вся механика невидимости держится на этом
        /// признаке.
        /// </summary>
        public bool HidesHero()
        {
            if (Data == null || Destroyed) return false;

            // ВНИМАНИЕ: свойство TileData.HidesHero привязано к несуществующей
            // колонке и всегда ложно. Настоящая колонка называется IsForest.
            return LogicDataAccess.GetBool(Data, "IsForest");
        }

        /// <summary>0x1003b061c. Возврат клетки к заданным данным.</summary>
        public void SetData(TileData data)
        {
            Data = data;
            Destroyed = false;
            RespawnCountdown = 0;
            Respawns = data != null && data.RespawnSeconds > 0;
            UpdateIsDynamic();
        }

        /// <summary>
        /// 0x1003b0814. Динамической клетка считается, если она восстанавливается
        /// либо является неразрушимой стеной. Отдельного признака стены в таблице
        /// нет, он выводится из того, что клетка держит снаряды и неразрушима.
        /// </summary>
        public void UpdateIsDynamic()
        {
            IsDynamic = Respawns || IsIndestructibleWall();
        }

        public bool IsIndestructibleWall()
        {
            return Data != null && Data.BlocksProjectiles && !Data.IsDestructible;
        }

        /// <summary>
        /// 0x1003b0468. Порядок: сточить прочность, при необходимости навести зону
        /// избегания, отсчитать восстановление и в самом конце обнулить временную маску.
        /// </summary>
        public void Tick(LogicGameObjectManagerServer manager)
        {
            if (Data != null && Data.Lifetime >= 1) LifetimeCounter--;

            if (Destroyed && Respawns)
            {
                RespawnCountdown--;

                // Оригинал перед восстановлением проверяет, что рядом нет живого
                // персонажа: клетка не должна вырасти внутри игрока. Радиус там
                // зашит как 0x15F91 — это 300 логических единиц в квадрате,
                // то есть ровно одна клетка.
                if (RespawnCountdown <= 0 && !IsBlockedByCharacter(manager))
                    SetData(OriginalData);
            }

            TemporaryMask = 0;
        }

        /// <summary>Радиус проверки из LogicTile::tick, 0x15F91 в квадрате.</summary>
        private const int RespawnBlockRadiusSquared = 0x15F91;

        private bool IsBlockedByCharacter(LogicGameObjectManagerServer manager)
        {
            if (manager == null || !BlocksMovement()) return false;

            int centerX = X * TileSize + TileSize / 2;
            int centerY = Y * TileSize + TileSize / 2;

            for (int i = 0; i < manager.Characters.Count; i++)
            {
                LogicGameObjectServer character = manager.Characters[i];
                if (character == null || !character.IsAlive()) continue;

                int dx = character.X - centerX;
                int dy = character.Y - centerY;

                if (dx * dx + dy * dy < RespawnBlockRadiusSquared) return true;
            }

            return false;
        }
    }
}
