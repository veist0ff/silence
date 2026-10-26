namespace HashCore.Laser.Logic.Battle
{
    using HashCore.Laser.Logic.Battle.Input;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server;

    /// <summary>
    /// Броубол (режимы с IsGoalBasedMode и ModeCarryable = LaserBall). Мяч — персонаж
    /// LaserBall типа Carryable. Подбирается касанием, пока его несут — бросок атакой
    /// или ультой (AllowAttackWhileCarrying = FALSE), катится с затуханием и отскакивает
    /// от стен (carryables.csv: ShouldRoll, Bouncing). Гол — мяч доехал до крайних рядов
    /// поля: ворота на карте — это проходы в стенах. После гола раунд сбрасывается
    /// (RoundResetsWhenObjectiveIsMissing), победа — MaxScore голов или счёт в конце.
    /// </summary>
    public partial class BattleMode
    {
        private const int BallPickupRange = 180;          // касание, логических единиц
        private const int BallPickupCooldownTicks = 10;   // бросивший не ловит мяч сразу
        private const int BallThrowSpeed = 270;           // ~9 клеток с затуханием 0.9
        private const int BallUltiSpeed = 450;            // ~15 клеток
        private const int BallFrictionPermille = 900;
        private const int GoalRows = 3;                   // ряды ворот у края карты

        private Character m_ball;
        private Character m_ballCarrier;
        private BattlePlayer m_ballLastToucher;
        private int m_ballVX, m_ballVY;                   // скорость, единиц за тик
        private int m_ballPickupBlockedUntil;
        private Character m_ballPickupBlockedFor;
        private readonly int[] m_goals = new int[2];

        public bool IsBrawlBall => GameModeUtil.GetVariationData(m_gameModeVariation)?.ModeCarryable == "LaserBall";

        private int BallCenterX => m_tileMap.LogicWidth / 2;
        private int BallCenterY => m_tileMap.LogicHeight / 2;

        /// <summary>Мяч, который несёт этот персонаж (для кадра героя), иначе null.</summary>
        public Character GetCarriedBall(Character c) => m_ball != null && c != null && m_ballCarrier == c ? m_ball : null;

        /// <summary>Свободный мяч (никто не несёт) — для ботов.</summary>
        public Character GetLooseBall() => m_ball != null && m_ballCarrier == null && m_ball.IsAlive() ? m_ball : null;

        /// <summary>Центр ворот соперника для персонажа: середина крайнего ряда, куда забивает его команда.</summary>
        public HashCore.Laser.Titan.Math.LogicVector2 GetEnemyGoalFor(Character c)
        {
            int team = c.GetIndex() / 16;
            bool top = TopGoalScorer() == team;
            int y = top ? TileMap.TileToLogic(GoalRows - 1) : m_tileMap.LogicHeight - TileMap.TileToLogic(GoalRows - 1);
            return new HashCore.Laser.Titan.Math.LogicVector2(BallCenterX, y);
        }

        /// <summary>Удар бота: то же, что ввод атаки у несущего мяч, но сразу в точку.</summary>
        public bool BotKickBall(Character c, int tx, int ty, bool ulti)
        {
            if (m_ball == null || c == null || m_ballCarrier != c) return false;
            int dx = tx - c.GetX(), dy = ty - c.GetY();
            double len = System.Math.Sqrt((double)dx * dx + (double)dy * dy);
            if (len < 1) return false;
            int speed = ulti ? BallUltiSpeed : BallThrowSpeed;
            m_ballVX = (int)(dx * speed / len);
            m_ballVY = (int)(dy * speed / len);
            m_ballCarrier = null;
            m_ballPickupBlockedFor = c;
            m_ballPickupBlockedUntil = GetTicksGone() + BallPickupCooldownTicks;
            return true;
        }

        /// <summary>Поле мяча Max3 в кадре: 0 — никто не несёт, 1 — команда 0, 2 — команда 1.</summary>
        public int GetBallCarrierCode() => m_ballCarrier == null ? 0 : (m_ballCarrier.GetIndex() / 16 == 0 ? 1 : 2);

        private void SpawnBrawlBall()
        {
            // Отладка вылета: /root/v64/ball_mode.txt
            //   0 — как было; 1 — не трогать State мяча; 2 — вместо мяча манекен;
            //   3 — мяч без хозяина-команды (-17, команда 0).
            m_ballMode = 0;
            try { m_ballMode = int.Parse(System.IO.File.ReadAllText("/root/v64/ball_mode.txt").Trim()); } catch { }
            if (m_ballMode == 4) { Logger.Print($"[BrawlBall] бой {Id}: режим 4 — мяч не создаётся"); return; }
            string ballName = m_ballMode == 2 ? "TrainingDummySmall" : "LaserBall";
            CharacterData data = DataTables.Get(DataType.Character).GetData<CharacterData>(ballName);
            if (data == null) { Logger.Error("[BrawlBall] нет персонажа LaserBall"); return; }
            m_ball = new Character(data);
            m_ball.SetPosition(BallCenterX, BallCenterY, 0);
            m_ball.SetIndex(m_ballMode == 3 ? -17 : -16); // как в оригинале: игрок −1 и команда −1 (tickSpawnEventStuffDelayed)
            if (m_ballMode != 1) m_ball.State = 0; // конструктор ставит 4 («появляется»), объекты из него сами не выходят
            m_gameObjectManager.AddGameObject(m_ball);
            Logger.Print($"[BrawlBall] бой {Id}: мяч в центре {BallCenterX},{BallCenterY}, режим отладки {m_ballMode} ({ballName}), навыков {(m_ball.GetWeaponSkill()!=null?1:0)+(m_ball.GetUltimateSkill()!=null?1:0)}");
        }

        /// <summary>Атака или ульта того, кто несёт мяч, — это бросок. true = ввод съеден.</summary>
        private bool TryBrawlBallInput(BattlePlayer player, Character character, ClientInput input)
        {
            if (m_ball == null || character == null || m_ballCarrier != character) return false;
            if (input.Type != 0 && input.Type != 1) return false;

            // Ввод атаки/ульты — смещение от героя (как у обычного выстрела); при автоприцеле
            // целимся в цель (или ближайшего видимого врага) по абсолютным координатам.
            // У удара мячом (DummySkillForLaserBall) нет снаряда, и клиент шлёт ТОЧКУ на карте,
            // а не смещение: со смещением мяч летел «от угла карты», то есть не туда.
            int dx = input.X - character.GetX(), dy = input.Y - character.GetY();
            Logger.Print($"[BrawlBall] удар: ввод {input.X},{input.Y}, герой {character.GetX()},{character.GetY()}");
            if (input.AutoAttack)
            {
                Character target = m_gameObjectManager.GetGameObjectByID(input.AutoAttackTarget) as Character
                                   ?? character.GetClosestVisibleEnemy();
                dx = target != null ? target.GetX() - character.GetX() : 0;
                dy = target != null ? target.GetY() - character.GetY() : 0;
            }
            double len = System.Math.Sqrt((double)dx * dx + (double)dy * dy);
            if (len < 1) { dx = 0; dy = player.TeamIndex == 0 ? -1 : 1; len = 1; }
            int speed = input.Type == 1 ? BallUltiSpeed : BallThrowSpeed;
            m_ballVX = (int)(dx * speed / len);
            m_ballVY = (int)(dy * speed / len);
            m_ballCarrier = null;
            m_ballPickupBlockedFor = character;
            m_ballPickupBlockedUntil = GetTicksGone() + BallPickupCooldownTicks;
            return true;
        }

        /// <summary>
        /// Оригинал создаёт мяч не сразу, а в LogicBattleModeServer::tickSpawnEventStuffDelayed —
        /// уже после старта боя. Мяч в первом кадре, пока идёт загрузка, клиент не переживает.
        /// </summary>
        public const int BallSpawnDelayTicks = 20 * 3;   // после первого кадра клиенту
        private bool m_ballPending;
        private bool m_ballDeathLogged;
        private int m_ballMode;
        private int m_firstFrameTick = -1;

        private void TickBrawlBall()
        {
            if (m_ballPending && m_firstFrameTick >= 0 && GetTicksGone() >= m_firstFrameTick + BallSpawnDelayTicks) { m_ballPending = false; SpawnBrawlBall(); }
            if (m_ball == null) return;
            if (m_ballMode != 1) m_ball.State = 0;
            if (!m_ball.IsAlive() && !m_ballDeathLogged) { m_ballDeathLogged = true; Logger.Print($"[BrawlBall] бой {Id}: мяч умер (HP 0)"); }

            // Несущий погиб — мяч падает на месте.
            if (m_ballCarrier != null && !m_ballCarrier.IsAlive()) m_ballCarrier = null;

            if (m_ballCarrier != null)
            {
                m_ball.SetPosition(m_ballCarrier.GetX(), m_ballCarrier.GetY(), 0);
            }
            else
            {
                MoveBall();
                TryPickupBall();
            }

            CheckGoal();
        }

        private void MoveBall()
        {
            if (m_ballVX == 0 && m_ballVY == 0) return;
            int x = m_ball.GetX(), y = m_ball.GetY();
            int nx = x + m_ballVX, ny = y + m_ballVY;

            // Отскок: блокирующая клетка по оси — разворачиваем эту составляющую.
            if (!Free(nx, y)) { m_ballVX = -m_ballVX; nx = x; }
            if (!Free(nx, ny)) { m_ballVY = -m_ballVY; ny = y; }

            m_ball.SetPosition(nx, ny, 0);
            m_ballVX = m_ballVX * BallFrictionPermille / 1000;
            m_ballVY = m_ballVY * BallFrictionPermille / 1000;
            if (System.Math.Abs(m_ballVX) < 8 && System.Math.Abs(m_ballVY) < 8) { m_ballVX = 0; m_ballVY = 0; }
        }

        private bool Free(int x, int y)
        {
            if (x < 0 || y < 0 || x >= m_tileMap.LogicWidth || y >= m_tileMap.LogicHeight) return false;
            return m_tileMap.IsPassable(0, TileMap.LogicToTile(x), TileMap.LogicToTile(y), 0, 0);
        }

        private void TryPickupBall()
        {
            foreach (BattlePlayer p in m_players)
            {
                if (p == null) continue;
                Character c = m_gameObjectManager.GetGameObjectByID(p.OwnObjectId) as Character;
                if (c == null || !c.IsAlive()) continue;
                if (c == m_ballPickupBlockedFor && GetTicksGone() < m_ballPickupBlockedUntil) continue;
                long dx = c.GetX() - m_ball.GetX(), dy = c.GetY() - m_ball.GetY();
                if (dx * dx + dy * dy > (long)BallPickupRange * BallPickupRange) continue;
                m_ballCarrier = c;
                m_ballLastToucher = p;
                Logger.Print($"[BrawlBall] бой {Id}: мяч подобран, тик {GetTicksGone()}, игрок {p.PlayerIndex} команда {p.TeamIndex}, навыков у героя {(c.GetWeaponSkill()!=null?1:0)+(c.GetUltimateSkill()!=null?1:0)}");
                m_ballVX = m_ballVY = 0;
                return;
            }
        }

        /// <summary>
        /// Гол: мяч в крайних рядах карты. Ворота команды — со стороны её точки появления,
        /// забивает та команда, что атакует с другой стороны.
        /// </summary>
        private void CheckGoal()
        {
            int tileY = TileMap.LogicToTile(m_ball.GetY());
            int team;
            if (tileY < GoalRows) team = TopGoalScorer();
            else if (tileY >= m_tileMap.Height - GoalRows) team = 1 - TopGoalScorer();
            else return;

            m_goals[team]++;
            BattlePlayer scorer = m_ballLastToucher != null && m_ballLastToucher.TeamIndex == team
                ? m_ballLastToucher
                : System.Array.Find(m_players.ToArray(), p => p != null && p.TeamIndex == team);
            scorer?.AddScore(1);
            Logger.Print($"[BrawlBall] бой {Id}: гол команды {team}, счёт {m_goals[0]}:{m_goals[1]}");
            ResetBrawlBallRound();
        }

        /// <summary>Какая команда забивает в верхние ворота: та, что появляется внизу.</summary>
        private int TopGoalScorer()
        {
            int y0 = 0, n0 = 0;
            foreach (Tile t in m_tileMap.SpawnPointsTeam1) { y0 += t.Y; n0++; }
            return n0 > 0 && y0 / n0 > m_tileMap.LogicHeight / 2 ? 0 : 1;
        }

        private void ResetBrawlBallRound()
        {
            m_ballCarrier = null;
            m_ballLastToucher = null;
            m_ballVX = m_ballVY = 0;
            m_ball.SetPosition(BallCenterX, BallCenterY, 0);
            foreach (BattlePlayer p in m_players)
            {
                if (p == null) continue;
                Character c = m_gameObjectManager.GetGameObjectByID(p.OwnObjectId) as Character;
                if (c == null) continue;
                var sp = p.GetSpawnPoint();
                c.SetPosition(sp.X, sp.Y, 0);
            }
        }

        /// <summary>Конец броубола: MaxScore голов у кого-то или вышло время. null — бой идёт.</summary>
        private bool? BrawlBallGameOver()
        {
            int max = GameModeUtil.GetVariationData(m_gameModeVariation)?.MaxScore ?? 2;
            if (max < 1) max = 2;
            if (m_goals[0] >= max) { m_winnerTeam = 0; return true; }
            if (m_goals[1] >= max) { m_winnerTeam = 1; return true; }
            if (GetTicksGone() >= GameModeUtil.GetBattleTicks(m_gameModeVariation) - 1)
            {
                m_winnerTeam = m_goals[0] > m_goals[1] ? 0 : m_goals[1] > m_goals[0] ? 1 : -1;
                return true;
            }
            return null;
        }
    }
}
