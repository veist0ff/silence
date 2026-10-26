using System;
using HashCore.Laser.Logic.Util
;
using HashCore.Laser.Logic.Battle.Component;
using System.Collections.Generic;
using HashCore.Laser.Logic.Battle.Level;
using HashCore.Laser.Logic.Battle.Structures;
using HashCore.Laser.Logic.Data;
using HashCore.Laser.Titan.Math;

namespace HashCore.Laser.Logic.Battle.Objects
{
    /// <summary>
    /// Бот по образцу оригинального LogicCharacterServer::tickBot (клиент v64, 0x100467604)
    /// и aiUseOffensiveSkills (0x100466ff8).
    ///
    /// Раз в «такт» (20 тиков у слабого бота, 10 у среднего, 6 у сильного) бот считает
    /// очки вариантов и делает тот, у кого очков больше:
    ///   0 — бой: подойти к врагу на линию огня и стрелять;
    ///   1 — отход: если «желание драться» ≤ 40 — отойти на 1200 от врагов в укрытие;
    ///   3 — предмет: ближайший куб/предмет, очки растут с запасом здоровья.
    /// Желание драться = своё здоровье % + 30% здоровья союзников рядом − 30% здоровья
    /// врагов рядом − 10·свои кубы + 5·кубы врага (+разница бонусов урона в шд)
    /// − 60 без патронов + случайные ±10.
    /// Без целей: уклониться от снаряда, иначе идти к союзнику, иначе бродить у центра
    /// карты (у каждого бота своя полоса) и ломать ближайший ящик.
    /// Стрельба: ульта первой, затем оружие; прицел — позиция врага ± 150 без упреждения;
    /// слабый бот стреляет в 30% тактов, средний в 50%, сильный всегда.
    /// </summary>
    public partial class Character
    {
        private const int TILE = 300;
        public static bool BotBrainEnabled = true;

        private int m_botNextThink = -1;
        private bool m_botHadUlti;
        private int m_botAction = -1;
        private LogicVector2 m_botGoal;
        private Character m_botEnemy;
        private int m_botEagerness;
        private bool m_botLos;
        private List<LogicVector2> m_botPath;
        private int m_botPathIndex;
        private int m_botPathTick = -100;
        private LogicVector2 m_botPathGoal;

        /// <summary>Уровень бота как в оригинале (getBotSkill): 1 — обычный, 3 — сложный.</summary>
        private int BotSkill => m_isBot == 2 ? 3 : 1;

        private int Rnd(int max) => GetBattle().GetRandomInt(max);
        private int Rnd(int min, int max) => min + GetBattle().GetRandomInt(max - min + 1);

        private void TickBrain()
        {
            if (IsControlled || !IsAlive() || IsPlayerControlRemoved()) return;
            // Изнанка: пока идёт выбор карточек, боты стоят — бой начинается после окна выбора.
            if (GameObjectManager.GetBattle().GetGameModeVariation() == 67 && GameObjectManager.GetBattle().GetTicksGone() < BattleMode.RogueliteCardPickTicks) return;

            // Броубол: с мячом — всегда к чужим воротам и бить; свободный мяч — за ним.
            if (TickBallBot()) return;

            // Между тактами бот только идёт к выбранной цели.
            if (TicksGone < m_botNextThink)
            {
                FollowPath();
                return;
            }
            int skill = BotSkill;
            m_botNextThink = TicksGone + (skill < 3 ? 20 : skill == 3 ? 10 : 6);

            // Оригинал: если ульта только что зарядилась — один такт «замечает» это и ждёт.
            bool hasUlti = GetPlayer()?.HasUlti() ?? false;
            if (hasUlti && !m_botHadUlti) { m_botHadUlti = true; return; }
            m_botHadUlti = hasUlti;

            Think();
        }

        private void Think()
        {
            TileMap map = GameObjectManager.GetBattle().GetTileMap();
            LogicVector2 me = GetPosition();
            int myHp = HpPct(this);
            int[] score = new int[6];
            var target = new LogicVector2[6];

            // ---- 3: предмет (куб силы и т. п.)
            Item item = ClosestItem();
            if (item != null)
            {
                int dist = me.GetDistance(item.GetPosition());
                int s = Math.Max(1, myHp - dist / 20 + 10) + Rnd(20);
                if (item.ItemData?.Name == "BattleRoyaleBuff") s = Math.Max(1, myHp / 5 + 5) + Rnd(20) + 110 - Math.Min(109, dist / 35);
                if (AllyCloserTo(item.GetPosition(), dist)) s /= 4;
                score[3] = s;
                target[3] = item.GetPosition();
            }

            // ---- враг
            Character enemy = EnemyForAI(map, out bool los);
            m_botEnemy = enemy;
            m_botLos = los;
            m_botEagerness = 0;
            if (enemy != null)
            {
                LogicVector2 ep = enemy.GetPosition();
                int alliesHp = 0, enemiesHp = 0, n = 1;
                long cx = ep.X, cy = ep.Y;
                foreach (Character c in GameObjectManager.GetCharacters())
                {
                    if (c == null || c == this || !c.IsAlive() || !c.CharacterData.IsHero()) continue;
                    bool near = me.GetDistance(c.GetPosition()) < 1200 || ep.GetDistance(c.GetPosition()) < 1200;
                    if (!near) continue;
                    if (SameTeam(c)) alliesHp += HpPct(c);
                    else if (c != enemy) { enemiesHp += HpPct(c); cx += c.GetX(); cy += c.GetY(); n++; }
                }
                bool manyTeams = GameModeUtil.GetTeamCount(GetBattle().GetGameModeVariation()) > 2;
                int eager = 30 * alliesHp / 100 + myHp + (manyTeams ? 0 : -30 * enemiesHp / 100)
                            - 10 * BattleRoyaleBuffs + 5 * enemy.BattleRoyaleBuffs;
                Skill w = GetWeaponSkill();
                if (w != null && !w.HasEnoughCharge()) eager -= 60;
                if (GameModeUtil.IsBattleRoyale(GetBattle().GetGameModeVariation()))
                    eager += 10 * BattleRoyaleBuffs - 10 * enemy.BattleRoyaleBuffs;
                eager += Rnd(-10, 10);
                m_botEagerness = eager;

                if (eager <= 40 && !NearBorder(map, me))
                {
                    // ---- 1: отход от центра группы врагов в место, которое они не видят
                    LogicVector2 away = Dir(me.X - (int)(cx / n), me.Y - (int)(cy / n), 1200);
                    LogicVector2 p = null;
                    for (int i = 0; i < 10 && p == null; i++)
                    {
                        LogicVector2 v = Rotate(away, Rnd(-45, 45));
                        LogicVector2 c = Clamp(map, me.X + v.X, me.Y + v.Y);
                        if (Walkable(map, c.X, c.Y) && !LineOfSight(map, c, new LogicVector2((int)(cx / n), (int)(cy / n)))) p = c;
                    }
                    p ??= Clamp(map, me.X + away.X + Rnd(-150, 150), me.Y + away.Y + Rnd(-150, 150));
                    score[1] = 15 * BattleRoyaleBuffs - myHp + Rnd(20) + 100;
                    target[1] = p;
                }
                else
                {
                    // ---- 0: бой — точка в 1200 в сторону врага, откуда его видно
                    LogicVector2 toward = Dir(ep.X - me.X, ep.Y - me.Y, 1200);
                    LogicVector2 p = null;
                    for (int i = 0; i < 10 && p == null; i++)
                    {
                        LogicVector2 v = Rotate(toward, Rnd(-60, 60));
                        LogicVector2 c = Clamp(map, me.X + v.X, me.Y + v.Y);
                        if (Walkable(map, c.X, c.Y) && LineOfSight(map, c, ep)) p = c;
                    }
                    p ??= ep;
                    score[0] = -HpPct(enemy) + 5 * enemy.BattleRoyaleBuffs + Rnd(20) + 105;
                    target[0] = Clamp(map, p.X, p.Y);
                }
            }

            int best = -1, bestScore = 0;
            for (int i = 0; i < 6; i++) if (score[i] > bestScore) { bestScore = score[i]; best = i; }
            m_botAction = best;

            if (best >= 0)
            {
                SetGoal(target[best]);
                // Оригинал: стреляет и на ходу, кроме случая «слабый и в отступлении».
                if (enemy != null && !(best == 1 && m_botEagerness < 0 && myHp < 70)) UseOffensiveSkills(enemy, los);
                FollowPath();
                return;
            }

            // ---- без целей: уклон от снаряда
            Projectile pr = ProjectileFlyingAtMe(600);
            if (pr != null)
            {
                LogicVector2 v = Dir(pr.GetX() - me.X, pr.GetY() - me.Y, 600);
                SetGoal(Clamp(map, me.X - v.X, me.Y - v.Y));
                FollowPath();
                return;
            }

            // ---- к ближайшему союзнику, иначе бродить у центра своей полосы
            Character ally = NearestAlly();
            LogicVector2 g;
            if (ally != null) g = new LogicVector2(ally.GetX() + Rnd(-150, 150), ally.GetY() + Rnd(-150, 150));
            else
            {
                int x = map.LogicWidth / 2 + Rnd(-600, 600), y = map.LogicHeight / 2 + Rnd(-600, 600);
                int lane = Math.Abs(GetGlobalID()) % 3;
                if (lane == 1) x = Rnd(300, 900);
                else if (lane == 2) x = map.LogicWidth - 900 + Rnd(0, 600);
                g = new LogicVector2(x + Rnd(-300, 300), y + Rnd(-300, 300));
            }
            SetGoal(Clamp(map, g.X, g.Y));

            // ---- ломать ближайший ящик (до 1500)
            Character box = NearestLootBox(1500);
            if (box != null && (GetWeaponSkill()?.HasEnoughCharge() ?? false))
                ActivateSkill(0, box.GetX(), box.GetY());

            FollowPath();
        }

        // ================= броубол =================

        /// <summary>true — ход занят мячом, обычная логика в этот тик не нужна.</summary>
        private bool TickBallBot()
        {
            BattleMode battle = GameObjectManager.GetBattle();
            if (!battle.IsBrawlBall) return false;
            TileMap map = battle.GetTileMap();
            LogicVector2 me = GetPosition();

            if (battle.GetCarriedBall(this) != null)
            {
                LogicVector2 goal = battle.GetEnemyGoalFor(this);
                int dist = me.GetDistance(goal);
                // Удар: ворота видно (мяч долетит без стен) и они в пределах удара (~9 клеток),
                // ультой — дальше (~15 клеток).
                bool clear = LineOfSight(map, me, goal);
                if (clear && dist <= 2700) { battle.BotKickBall(this, goal.X + Rnd(-300, 300), goal.Y, false); return true; }
                if (clear && dist <= 4500 && (GetPlayer()?.HasUlti() ?? false)) { battle.BotKickBall(this, goal.X + Rnd(-200, 200), goal.Y, true); return true; }
                SetGoal(goal);
                FollowPath();
                return true;
            }

            Character ball = battle.GetLooseBall();
            if (ball != null)
            {
                // За мячом бежит ближайший к нему из своей команды.
                int my = me.GetDistance(ball.GetPosition());
                bool nearest = true;
                foreach (Character c in GameObjectManager.GetCharacters())
                {
                    if (c == null || c == this || !c.IsAlive() || !c.CharacterData.IsHero() || !SameTeam(c)) continue;
                    if (c.GetPosition().GetDistance(ball.GetPosition()) < my) { nearest = false; break; }
                }
                if (nearest)
                {
                    SetGoal(ball.GetPosition());
                    FollowPath();
                    // По пути отстреливается, если враг рядом.
                    if (TicksGone >= m_botNextThink)
                    {
                        m_botNextThink = TicksGone + (BotSkill < 3 ? 20 : 10);
                        Character enemy = EnemyForAI(map, out bool los);
                        if (enemy != null) UseOffensiveSkills(enemy, los);
                    }
                    return true;
                }
            }
            return false;
        }

        // ================= стрельба (aiUseOffensiveSkills) =================

        private void UseOffensiveSkills(Character enemy, bool los)
        {
            int skill = BotSkill;
            int chance = skill == 0 ? 30 : skill == 1 ? 50 : 100;
            if (Rnd(100) >= chance) return;

            int tx = enemy.GetX() + Rnd(-150, 150), ty = enemy.GetY() + Rnd(-150, 150);
            int dist = GetPosition().GetDistance(enemy.GetPosition());

            Skill ulti = GetUltimateSkill();
            if (ulti != null && (GetPlayer()?.HasUlti() ?? false) && los
                && dist <= Math.Max(600, ulti.SkillData.CastingRange * 50))
            {
                ActivateSkill(1, tx, ty);
                return;
            }

            Skill w = GetWeaponSkill();
            if (w == null || !w.HasEnoughCharge() || !los) return;
            if (dist > Math.Max(600, w.SkillData.CastingRange * 50) + 200) return;
            ActivateSkill(0, tx, ty);
        }

        // ================= поиск целей =================

        /// <summary>getEnemyForAI: ближайший видимый враг-герой (или босс), сначала в линии огня.</summary>
        private Character EnemyForAI(TileMap map, out bool los)
        {
            los = false;
            LogicVector2 me = GetPosition();
            Character best = null, bestLos = null;
            int bd = int.MaxValue, bdLos = int.MaxValue;
            foreach (GameObject go in GameObjectManager.GetVisibleGameObjects(GetIndex(), GetGlobalID()))
            {
                if (go.GetObjectType() != 0) continue;
                Character c = (Character)go;
                if (c == this || !c.IsAlive() || c.IsInvisible || SameTeam(c)) continue;
                if (!c.CharacterData.IsHero() && !c.CharacterData.IsBoss()) continue;
                int d = me.GetDistance(c.GetPosition());
                if (d < bd) { bd = d; best = c; }
                if (d < bdLos && LineOfSight(map, me, c.GetPosition())) { bdLos = d; bestLos = c; }
            }
            if (bestLos != null) { los = true; return bestLos; }
            return best;
        }

        private Item ClosestItem()
        {
            Item best = null; int bd = int.MaxValue;
            foreach (GameObject go in GameObjectManager.GetGameObjects())
            {
                if (go is not Item it || it.ItemData == null || !it.ItemData.CanBePickedUp) continue;
                int d = GetPosition().GetDistance(it.GetPosition());
                if (d < bd && d < 5000) { bd = d; best = it; }
            }
            return best;
        }

        private bool AllyCloserTo(LogicVector2 p, int myDist)
        {
            foreach (Character c in GameObjectManager.GetCharacters())
                if (c != null && c != this && c.IsAlive() && SameTeam(c) && c.CharacterData.IsHero()
                    && c.GetPosition().GetDistance(p) < myDist) return true;
            return false;
        }

        private Character NearestAlly()
        {
            Character best = null; int bd = int.MaxValue;
            foreach (Character c in GameObjectManager.GetCharacters())
            {
                if (c == null || c == this || !c.IsAlive() || !SameTeam(c) || !c.CharacterData.IsHero()) continue;
                int d = GetPosition().GetDistance(c.GetPosition());
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        private Character NearestLootBox(int maxDist)
        {
            Character best = null; int bd = maxDist;
            foreach (Character c in GameObjectManager.GetCharacters())
            {
                if (c == null || !c.IsAlive() || !c.CharacterData.IsLootBox()) continue;
                int d = GetPosition().GetDistance(c.GetPosition());
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        private Projectile ProjectileFlyingAtMe(int radius)
        {
            LogicVector2 me = GetPosition();
            foreach (GameObject go in GameObjectManager.GetGameObjects())
            {
                if (go is not Projectile p || p.State == 4) continue;
                if (p.GetIndex() / 16 == GetIndex() / 16) continue;
                int dx = me.X - p.GetX(), dy = me.Y - p.GetY();
                int d = LogicMath.Sqrt(dx * dx + dy * dy);
                if (d > radius * 3) continue;
                int diff = Math.Abs(LogicMath.NormalizeAngle180(p.Angle - LogicMath.GetAngle(dx, dy)));
                if (diff < 20) return p;
            }
            return null;
        }

        private bool SameTeam(Character c) => c.GetIndex() / 16 == GetIndex() / 16;
        private static int HpPct(Character c) => c.m_maxHitpoints > 0 ? 100 * c.m_hitpoints / c.m_maxHitpoints : 100;

        // ================= движение =================

        private void SetGoal(LogicVector2 g)
        {
            TileMap gm = GameObjectManager.GetBattle().GetTileMap();
            if (!Walkable(gm, g.X, g.Y))
                for (int r = 1; r < 5; r++)
                {
                    bool done = false;
                    for (int i = -r; i <= r && !done; i++)
                        for (int j = -r; j <= r && !done; j++)
                            if (Walkable(gm, g.X + i * TILE, g.Y + j * TILE)) { g = new LogicVector2(g.X + i * TILE, g.Y + j * TILE); done = true; }
                    if (done) break;
                }
            if (m_botGoal == null || m_botGoal.GetDistance(g) > 150) { m_botGoal = g; m_botPath = null; }
        }

        private LogicVector2 m_botLastPos;
        private int m_botStuckTicks;

        private void FollowPath()
        {
            if (m_botGoal == null) return;
            TileMap map = GameObjectManager.GetBattle().GetTileMap();
            LogicVector2 me = GetPosition();
            if (me.GetDistance(m_botGoal) < 150) return;

            // Застрял (почти не сдвинулся 10 тиков) — заново строим путь и делаем шаг в сторону.
            if (m_botLastPos != null && m_botLastPos.GetDistance(me) < 20) m_botStuckTicks++;
            else m_botStuckTicks = 0;
            m_botLastPos = new LogicVector2(me.X, me.Y);
            if (m_botStuckTicks >= 10)
            {
                m_botStuckTicks = 0;
                m_botPath = null;
                for (int i = 0; i < 8; i++)
                {
                    LogicVector2 v = Rotate(new LogicVector2(450, 0), Rnd(0, 359));
                    if (Walkable(map, me.X + v.X, me.Y + v.Y)) { MoveTo(0, me.X + v.X, me.Y + v.Y, 0, 0, 0, 0); return; }
                }
            }

            // Напрямую — только если проходит полоса шириной с бойца, а не линия выстрела.
            if (WalkClear(map, me, m_botGoal)) { MoveTo(0, m_botGoal.X, m_botGoal.Y, 0, 0, 0, 0); return; }

            if (m_botPath == null || TicksGone - m_botPathTick > 20 || m_botPathGoal == null || m_botPathGoal.GetDistance(m_botGoal) > 300)
            {
                m_botPathTick = TicksGone;
                m_botPathGoal = m_botGoal;
                m_botPath = FindPath(map, me, m_botGoal);
                m_botPathIndex = 0;
            }
            if (m_botPath == null || m_botPath.Count == 0)
            {
                // Пути нет — шаг к цели в ближайшую свободную сторону.
                LogicVector2 step = Dir(m_botGoal.X - me.X, m_botGoal.Y - me.Y, 300);
                for (int a = 0; a <= 90; a += 30)
                    foreach (int sgn in new[] { 1, -1 })
                    {
                        LogicVector2 v = Rotate(step, a * sgn);
                        if (Walkable(map, me.X + v.X, me.Y + v.Y)) { MoveTo(0, me.X + v.X, me.Y + v.Y, 0, 0, 0, 0); return; }
                    }
                return;
            }
            while (m_botPathIndex < m_botPath.Count - 1 && me.GetDistance(m_botPath[m_botPathIndex]) < 200) m_botPathIndex++;
            int pick = m_botPathIndex;
            for (int i = m_botPathIndex; i < Math.Min(m_botPath.Count, m_botPathIndex + 6); i++)
                if (WalkClear(map, me, m_botPath[i])) pick = i;
            MoveTo(0, m_botPath[pick].X, m_botPath[pick].Y, 0, 0, 0, 0);
        }

        /// <summary>Проходима ли полоса шириной с бойца (три параллельные линии, шаг 100).</summary>
        private bool WalkClear(TileMap map, LogicVector2 a, LogicVector2 b)
        {
            int d = a.GetDistance(b);
            if (d == 0) return true;
            int r = Math.Max(60, Math.Min(140, CharacterData.CollisionRadius));
            int ox = -(b.Y - a.Y) * r / d, oy = (b.X - a.X) * r / d;
            int n = Math.Max(1, d / 100);
            for (int k = -1; k <= 1; k++)
                for (int i = 1; i <= n; i++)
                {
                    int x = a.X + (b.X - a.X) * i / n + ox * k, y = a.Y + (b.Y - a.Y) * i / n + oy * k;
                    if (x < 0 || y < 0 || !TileFree(map, x / TILE, y / TILE)) return false;
                }
            return true;
        }

        private List<LogicVector2> FindPath(TileMap map, LogicVector2 from, LogicVector2 to)
        {
            int w = map.Width, h = map.Height;
            int sx = LogicMath.Clamp(from.X / TILE, 0, w - 1), sy = LogicMath.Clamp(from.Y / TILE, 0, h - 1);
            int gx = LogicMath.Clamp(to.X / TILE, 0, w - 1), gy = LogicMath.Clamp(to.Y / TILE, 0, h - 1);
            if (!TileFree(map, gx, gy))
            {
                bool found = false;
                for (int r = 1; r < 6 && !found; r++)
                    for (int i = -r; i <= r && !found; i++)
                        for (int j = -r; j <= r && !found; j++)
                            if (TileFree(map, gx + i, gy + j)) { gx += i; gy += j; found = true; }
                if (!found) return null;
            }

            var g = new int[w, h]; var came = new int[w, h]; var closed = new bool[w, h];
            for (int i = 0; i < w; i++) for (int j = 0; j < h; j++) { g[i, j] = int.MaxValue; came[i, j] = -1; }
            var open = new PriorityQueue<(int x, int y), int>();
            g[sx, sy] = 0; open.Enqueue((sx, sy), 0);
            int[] dxs = { 1, -1, 0, 0, 1, 1, -1, -1 }, dys = { 0, 0, 1, -1, 1, -1, 1, -1 };
            int steps = 0;
            while (open.Count > 0 && steps++ < 4000)
            {
                var c = open.Dequeue();
                if (closed[c.x, c.y]) continue;
                closed[c.x, c.y] = true;
                if (c.x == gx && c.y == gy) break;
                for (int k = 0; k < 8; k++)
                {
                    int nx = c.x + dxs[k], ny = c.y + dys[k];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h || closed[nx, ny] || !TileFree(map, nx, ny)) continue;
                    if (k >= 4 && (!TileFree(map, c.x + dxs[k], c.y) || !TileFree(map, c.x, c.y + dys[k]))) continue;
                    int cost = g[c.x, c.y] + (k < 4 ? 10 : 14);
                    if (cost >= g[nx, ny]) continue;
                    g[nx, ny] = cost; came[nx, ny] = c.x * h + c.y;
                    open.Enqueue((nx, ny), cost + 10 * (Math.Abs(nx - gx) + Math.Abs(ny - gy)));
                }
            }
            if (!closed[gx, gy]) return null;
            var path = new List<LogicVector2>();
            int px = gx, py = gy;
            while (!(px == sx && py == sy) && came[px, py] >= 0)
            {
                path.Add(new LogicVector2(px * TILE + TILE / 2, py * TILE + TILE / 2));
                int p = came[px, py]; px = p / h; py = p % h;
            }
            path.Reverse();
            return path;
        }

        private bool TileFree(TileMap map, int tx, int ty)
        {
            if (tx < 0 || ty < 0 || tx >= map.Width || ty >= map.Height) return false;
            Tile t = map.GetTile(tx, ty, true);
            return t == null || t.Data == null || !t.Data.BlocksMovement || t.IsDestructed();
        }

        private bool Walkable(TileMap map, int x, int y)
        {
            if (x < 150 || y < 150 || x > map.LogicWidth - 150 || y > map.LogicHeight - 150) return false;
            return TileFree(map, x / TILE, y / TILE);
        }

        private bool LineOfSight(TileMap map, LogicVector2 a, LogicVector2 b)
        {
            int n = Math.Max(1, a.GetDistance(b) / 150);
            for (int i = 1; i < n; i++)
            {
                int x = a.X + (b.X - a.X) * i / n, y = a.Y + (b.Y - a.Y) * i / n;
                Tile t = map.GetTile(x / TILE, y / TILE, true);
                if (t != null && t.Data != null && t.Data.BlocksProjectiles && !t.IsDestructed()) return false;
            }
            return true;
        }

        private static bool NearBorder(TileMap map, LogicVector2 p) =>
            p.X < 600 || p.X > map.LogicWidth - 600 || p.Y < 600 || p.Y > map.LogicHeight - 600;

        private static LogicVector2 Clamp(TileMap map, int x, int y) =>
            new LogicVector2(LogicMath.Clamp(x, 1, map.LogicWidth - 2), LogicMath.Clamp(y, 1, map.LogicHeight - 2));

        private static LogicVector2 Dir(int dx, int dy, int len)
        {
            int d = LogicMath.Sqrt(dx * dx + dy * dy);
            if (d == 0) return new LogicVector2(len, 0);
            return new LogicVector2(dx * len / d, dy * len / d);
        }

        private static LogicVector2 Rotate(LogicVector2 v, int deg)
        {
            int c = LogicMath.Cos(deg), s = LogicMath.Sin(deg);
            return new LogicVector2((v.X * c - v.Y * s) >> 10, (v.X * s + v.Y * c) >> 10);
        }
    }
}
