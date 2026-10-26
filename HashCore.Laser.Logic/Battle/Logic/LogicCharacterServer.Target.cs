namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;

    /// <summary>
    /// Выбор и проверка цели: 0x100458bac, 0x100465aec, 0x100465f5c, 0x1004632e4.
    /// </summary>
    public partial class LogicCharacterServer
    {
        /// <summary>Сколько логических единиц в одной единице дальности из таблицы.</summary>
        public const int RangeUnitSize = 100;

        /// <summary>За сколько тиков засветка от действия ещё считается свежей.</summary>
        public const int VisibleByActionTicks = 20;

        /// <summary>Особое свойство, при котором боец не умеет быстро перемещаться.</summary>
        public const int NoFastTravelProperty = 26;

        /// <summary>Особое свойство, при котором боец виден всегда.</summary>
        public const int AlwaysVisibleProperty = 68;

        /// <summary>Особое свойство, при котором боец прячется, даже когда всё против этого.</summary>
        public const int StubbornStealthProperty = 20;

        /// <summary>Поле +1204. Боец прячется.</summary>
        public bool Hidden;

        /// <summary>Поле +1205. Прятаться сейчас нельзя.</summary>
        public bool StealthBroken;

        /// <summary>Поле +1844. Причина, по которой боец невидим.</summary>
        public int StealthKind;

        /// <summary>Поле +1848. Невидимость снята насильно.</summary>
        public bool StealthDisabled;

        /// <summary>Поле +1560. Объект, который держит бойца на виду.</summary>
        public LogicGameObjectServer VisibilityHolder;

        /// <summary>Поле +1532. Пока больше нуля, боец не может спрятаться.</summary>
        public int StealthBlockTimer;

        /// <summary>
        /// Поле +688. Боец копит заряд умения. Опознано по условию окончания зоны
        /// targetNotCharging (0x1003e1eb4): зона живёт, пока это поле взведено.
        /// </summary>
        public bool Charging;

        /// <summary>
        /// Поле +689. Взведено, когда боец выдал себя действием: по нему
        /// wasVisibleByActionWithin отвечает сразу, не глядя на тики.
        /// </summary>
        public bool RevealedByAction;

        /// <summary>
        /// Поля +820, +416 и +824. Тики, на которых боец себя выдал. Средний из
        /// них ставит сама атака (0x10045b660), два других — прочие действия,
        /// которые снимают укрытие.
        /// </summary>
        public int LastRevealTickA;
        public int LastAttackTick;
        public int LastRevealTickB;

        /// <summary>Поле +828. Пока больше нуля, боец светится в любом случае.</summary>
        public int ForcedVisibleTimer;

        /// <summary>Поле +1862. Проверять ли бурю при расчёте видимости.</summary>
        public bool CheckVisibilityStorm;

        /// <summary>Поле +2136. Через сколько тиков искать новую цель.</summary>
        public int NextTargetSearchDelay;

        /// <summary>Поле +580. Кого боец держит на прицеле между поисками.</summary>
        public int PreferredTargetGlobalId = -1;

        /// <summary>
        /// 0x100458bac. Годится ли цель для удара.
        ///
        /// Проверяется по очереди: цель вообще можно бить, она с нужной стороны,
        /// она нужного вида, до неё достаёт оружие и её видно. Последнее — самое
        /// дорогое, поэтому и стоит последним.
        ///
        /// Дальность задана в сотнях логических единиц и сравнивается в квадратах,
        /// без корня.
        /// </summary>
        public virtual bool IsInRange(
            int range,
            LogicCharacterServer target,
            bool heroesOnly,
            bool ignoreStealth,
            bool ignoreLineOfSight,
            bool sameTeam)
        {
            if (target == null || !target.CanBeTargeted()) return false;

            if ((target.GetCurrentTeamIndex() == GetCurrentTeamIndex()) != sameTeam) return false;

            if (heroesOnly && !target.IsHero()) return false;

            int reach = RangeUnitSize * range;

            int dx = target.X - X;
            int dy = target.Y - Y;

            if (dx * dx + dy * dy > reach * reach) return false;

            if (ignoreStealth) return true;

            bool visible = (!target.Hidden && target.StealthKind < 1)
                || target.StealthBroken
                || target.VisibilityHolder != null
                || sameTeam;

            return visible && IsTargetVisibleToAttack(target, ignoreLineOfSight);
        }

        /// <summary>Можно ли по объекту бить вообще. Слот +96 в оригинале.</summary>
        public virtual bool CanBeTargeted()
        {
            return IsAlive();
        }

        /// <summary>
        /// 0x100465aec. Видит ли бьющий эту цель.
        ///
        /// Того, кто и так на виду, видно сразу. Спрятавшегося ищут глазами всей
        /// команды, и если стена мешает — засветка от недавнего выстрела всё равно
        /// выдаёт его, но только по прямой линии.
        /// </summary>
        public virtual bool IsTargetVisibleToAttack(LogicCharacterServer target, bool ignoreLineOfSight)
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null || target == null) return false;

            LogicTileMap map = battle.TileMap;
            bool mirrored = battle.IsPlayFieldMirroredForPlayer(GetCurrentTeamIndex());

            bool visibleByAction;

            if (target.IsForcedVisible())
            {
                if (ignoreLineOfSight) return true;
                visibleByAction = true;
            }
            else if (IsHero())
            {
                visibleByAction = target.WasVisibleByActionWithin(VisibleByActionTicks);
                if (ignoreLineOfSight && visibleByAction) return true;

                if (ignoreLineOfSight) return VisibleToTeamNoLineOfSight(battle, map, target, mirrored);
            }
            else
            {
                if (ignoreLineOfSight) return VisibleToTeamNoLineOfSight(battle, map, target, mirrored);
                visibleByAction = false;
            }

            bool visible = Manager.IsPositionVisibleToPlayerTeamAndLineOfSightNotBlocked(
                map, X, Y,
                battle.GetTeamMemberPositions(GetCurrentTeamIndex()),
                target.X, target.Y,
                mirrored,
                GetCurrentTeamIndex(),
                target.DimensionIndex,
                CheckVisibilityStorm);

            if (visible) return true;

            // Засветку от действия стена всё-таки держит: выстрел выдаёт стрелка
            // только тому, кто его видит по прямой.
            if (!visibleByAction) return false;

            return map != null && map.IsPlayerLineOfSightClear(X, Y, target.X, target.Y);
        }

        private bool VisibleToTeamNoLineOfSight(
            LogicBattleModeServer battle, LogicTileMap map, LogicCharacterServer target, bool mirrored)
        {
            return Manager.IsPositionVisibleToPlayerTeam(
                map, X, Y,
                battle.GetTeamMemberPositions(GetCurrentTeamIndex()),
                target.X, target.Y,
                mirrored,
                GetCurrentTeamIndex(),
                target.DimensionIndex,
                CheckVisibilityStorm);
        }

        /// <summary>
        /// 0x100465f5c. Виден ли боец в любом случае, без всяких кустов.
        ///
        /// Прятаться умеют не все: у целого ряда видов боец на виду по своей сути,
        /// и никакие кусты этого не меняют. Обычный же боец прячется, только когда
        /// он жив, стоит в кустах и ничем себя не выдал.
        /// </summary>
        public virtual bool IsForcedVisible()
        {
            if (IsAlwaysVisibleKind()) return true;
            if (IsMegaBoss()) return true;
            if (GetUniqueProperty() == AlwaysVisibleProperty) return true;

            if (StealthDisabled || VisibilityHolder != null) return true;

            bool canHide = CanBeTargeted()
                && StealthBlockTimer <= 0
                && (!Charging || StealthKind == 2)
                && ForcedVisibleTimer <= 0;

            if (!canHide) return true;

            return GetUniqueProperty() == StubbornStealthProperty;
        }

        /// <summary>
        /// Виды бойцов, которые не прячутся никогда. Маска 0x604061 из
        /// isForcedVisible отсчитывается от шестого вида, и единицы в ней
        /// приходятся на базу, второго искателя врагов, робовойны, прислужника
        /// вторжения и двоих арены.
        /// </summary>
        protected virtual bool IsAlwaysVisibleKind()
        {
            int kind = GetCharacterKind() - 6;
            return (uint)kind < 23 && ((0x604061u >> kind) & 1) != 0;
        }

        /// <summary>Поле данных +548. Вид бойца.</summary>
        protected virtual int GetCharacterKind()
        {
            return -1;
        }

        /// <summary>
        /// 0x1004632e4. Выдал ли боец себя за последние столько-то тиков.
        ///
        /// Выйдя из кустов, боец виден сразу. Оставшись в них, он всё равно
        /// светится, если недавно стрелял, двигался или шумел.
        /// </summary>
        public virtual bool WasVisibleByActionWithin(int ticks)
        {
            if (RevealedByAction && ((!Hidden && StealthKind < 1) || StealthBroken)) return true;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            int since = (battle != null ? battle.Tick : 0) - ticks;

            return LastRevealTickA >= since
                || LastAttackTick >= since
                || LastRevealTickB >= since
                || ForcedVisibleTimer > 0;
        }

        /// <summary>
        /// 0x1005099b8. Умеет ли боец быстро перемещаться.
        ///
        /// Это про героев и питомцев: остальные ходят как ходят. Одно особое
        /// свойство отбирает это умение и у них.
        /// </summary>
        public virtual bool CanUseFastTravel()
        {
            if (!IsHero() && !IsPet()) return false;

            return GetUniqueProperty() != NoFastTravelProperty;
        }

        /// <summary>
        /// Угол движения. Это тот же угол поворота: направление и взгляд у бойца
        /// разъезжаются только когда его развернули насильно.
        /// </summary>
        public virtual int GetMoveAngle()
        {
            return FacingAngle;
        }

        /// <summary>
        /// Применяет ли боец прямо сейчас то умение, из которого вышла эта зона.
        /// Сравнение идёт по данным зоны: у умения их два, основное и запасное.
        /// </summary>
        public virtual bool IsCastingAreaEffect(AreaEffectData data)
        {
            LogicSkillServer skill = GetCurrentActiveOrCastingSkill();
            if (skill == null || skill.Data == null || data == null) return false;

            return skill.Data.AreaEffectObject == data.Name
                || skill.Data.AreaEffectObject2 == data.Name;
        }

        /// <summary>Боец является питомцем.</summary>
        protected virtual bool IsPet()
        {
            return false;
        }

        /// <summary>
        /// 0x10045a474. Куда идти, пока цели нет.
        ///
        /// Поиск не идёт каждый кадр: между заходами двадцать тиков, иначе боец
        /// дёргался бы на месте. Когда цель уже выбрана, он идёт не прямо в неё, а
        /// в случайную точку вокруг: у кустов разброс вчетверо меньше, чтобы не
        /// вылезти из укрытия. Цели нет — боец просто топчется рядом с собой.
        /// </summary>
        public virtual void PathFindToNextTarget()
        {
            if (--NextTargetSearchDelay > 0) return;

            if (!WandersToTarget()) return;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null || Manager == null) return;

            LogicTileMap map = battle.TileMap;
            if (map == null) return;

            LogicGameObjectServer target = Manager.GetGameObjectById(PreferredTargetGlobalId);

            int x;
            int y;

            if (target == null)
            {
                x = ClampToMap(X + battle.Random.Rand(601) - 300, map.LogicWidth);
                y = ClampToMap(Y + battle.Random.Rand(601) - 300, map.LogicHeight);

                MoveTo(x, y, false, 0, false, CanUseFastTravel());
                NextTargetSearchDelay = 20;
                return;
            }

            if (GetCharacterKind() != 2)
            {
                NextTargetSearchDelay = 20;
                return;
            }

            LogicTile tile = map.GetTile(
                LogicTileMap.LogicToPathFinderTile(target.X) / 3,
                LogicTileMap.LogicToPathFinderTile(target.Y) / 3);

            bool inBush = tile != null && tile.HidesHero();
            int spread = inBush ? 150 : 450;

            x = ClampToMap(target.X - spread + battle.Random.Rand((2 * spread) | 1), map.LogicWidth);
            y = ClampToMap(target.Y - spread + battle.Random.Rand((2 * spread) | 1), map.LogicHeight);

            if (inBush) AiMoveToPreferForest(x, y);
            else MoveTo(x, y, false, 0, false, CanUseFastTravel());

            NextTargetSearchDelay = 20;
        }

        /// <summary>
        /// Бродит ли боец к цели сам. Из pathFindToNextTarget: целый список видов
        /// стоит на месте, и боссы с двойниками тоже.
        /// </summary>
        protected virtual bool WandersToTarget()
        {
            int kind = GetCharacterKind();

            if (kind == 3 || kind == 11 || kind == 8 || kind == 12
                || kind == 27 || kind == 20 || kind == 24)
            {
                return false;
            }

            if (IsMegaBoss()) return false;

            return IsTwinMinion() && !IsDecoy();
        }

        /// <summary>Боец является одним из парных прислужников.</summary>
        protected virtual bool IsTwinMinion()
        {
            return false;
        }

        /// <summary>0x10045a6a8. Идти к точке, держась кустов.</summary>
        protected virtual void AiMoveToPreferForest(int x, int y)
        {
            MoveTo(x, y, false, 0, false, false);
        }
    }
}
