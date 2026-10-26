namespace HashCore.Laser.Logic.Battle
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server;
    using HashCore.Laser.Titan.Math;

    /// <summary>
    /// Нокаут (game_mode_variations.csv: Knockout — RespawnSeconds −1, NumberOfRounds 3,
    /// MaxScore 2). Раунд длится, пока жива хотя бы одна команда целиком не выбита; без
    /// возрождения. Кто выбил всех — берёт раунд; по таймеру раунд берёт команда с большим
    /// числом живых. После паузы все возрождаются на своих точках с полным здоровьем.
    /// Блок раундов в кадре — LogicGameObjectManagerClient::decode, ветка isKnockout.
    /// </summary>
    public partial class BattleMode
    {
        public const int KnockoutRoundTicks = 20 * 60;
        public const int KnockoutPauseTicks = 20 * 3;

        private int[] m_koWinners;
        private int m_koRound;
        private int m_koRoundStartTick;
        private int m_koRoundEndTick = -1;

        public bool IsKnockout => GameModeUtil.GetVariationData(m_gameModeVariation)?.GetName()?.StartsWith("Knockout") == true;

        private int KnockoutRounds => System.Math.Max(1, GameModeUtil.GetVariationData(m_gameModeVariation)?.NumberOfRounds ?? 3);
        private int KnockoutWinsNeeded => System.Math.Max(1, GameModeUtil.GetVariationData(m_gameModeVariation)?.MaxScore ?? 2);

        private void InitKnockout()
        {
            m_koWinners = new int[KnockoutRounds];
            for (int i = 0; i < m_koWinners.Length; i++) m_koWinners[i] = -1;
            m_koRound = 0;
            m_koRoundStartTick = 0;
            m_koRoundEndTick = -1;
        }

        private int KnockoutWins(int team)
        {
            int n = 0;
            foreach (int w in m_koWinners) if (w == team) n++;
            return n;
        }

        private void TickKnockout()
        {
            if (m_koWinners == null) InitKnockout();
            int tick = GetTicksGone();

            if (m_koRoundEndTick >= 0)
            {
                if (tick >= m_koRoundEndTick + KnockoutPauseTicks && m_koRound + 1 < m_koWinners.Length
                    && KnockoutWins(0) < KnockoutWinsNeeded && KnockoutWins(1) < KnockoutWinsNeeded)
                    StartNextKnockoutRound();
                return;
            }

            int[] alive = new int[2], present = new int[2];
            foreach (BattlePlayer p in m_players)
            {
                if (p == null || p.TeamIndex < 0 || p.TeamIndex > 1) continue;
                present[p.TeamIndex]++;
                if (p.IsAlive) alive[p.TeamIndex]++;
            }

            int winner = int.MinValue;
            if (present[0] > 0 && present[1] > 0)
            {
                if (alive[0] == 0 && alive[1] > 0) winner = 1;
                else if (alive[1] == 0 && alive[0] > 0) winner = 0;
                else if (alive[0] == 0 && alive[1] == 0) winner = -1;
            }
            // Одиночный бой без соперника — раунд тоже кончается смертью.
            else if (present[0] + present[1] > 0 && alive[0] + alive[1] == 0) winner = -1;

            if (winner == int.MinValue && tick - m_koRoundStartTick >= KnockoutRoundTicks)
                winner = alive[0] > alive[1] ? 0 : alive[1] > alive[0] ? 1 : -1;

            if (winner == int.MinValue) return;
            m_koWinners[m_koRound] = winner;
            m_koRoundEndTick = tick;
            Logger.Print($"[Knockout] бой {Id}: раунд {m_koRound + 1} — {(winner < 0 ? "ничья" : "команда " + winner)}, счёт {KnockoutWins(0)}:{KnockoutWins(1)}");
        }

        private void StartNextKnockoutRound()
        {
            m_koRound++;
            m_koRoundStartTick = GetTicksGone();
            m_koRoundEndTick = -1;

            // Дым, облака и снаряды прошлого раунда не переносятся.
            foreach (GameObject go in m_gameObjectManager.GetGameObjects())
                if (go is AreaEffect || go is Projectile) m_gameObjectManager.RemoveGameObject(go);

            foreach (BattlePlayer p in m_players)
            {
                if (p == null) continue;
                LogicVector2 sp = p.GetSpawnPoint();
                Character c = m_gameObjectManager.GetGameObjectByID(p.OwnObjectId) as Character;
                if (c != null && c.IsAlive())
                {
                    c.SetPosition(sp.X, sp.Y, 0);
                    c.m_hitpoints = c.m_maxHitpoints;
                    continue;
                }
                // Как TickSpawnHeroes: новый герой на точке появления.
                Character hero = SpawnHero(p.CharacterData, p.HeroPowerLevel, p.TeamIndex * 16 + p.PlayerIndex, 0, false);
                hero.SetPosition(sp.X, sp.Y, 0);
                hero.SetBot(p.IsBot());
                hero.SpawnTick = GetTicksGone();
                hero.IsInvincible = true;
                p.OwnObjectId = hero.GetGlobalID();
                p.IsAlive = true;
            }
            Logger.Print($"[Knockout] бой {Id}: раунд {m_koRound + 1}");
        }

        /// <summary>Конец нокаута: у кого-то MaxScore раундов или раунды кончились. null — бой идёт.</summary>
        private bool? KnockoutGameOver()
        {
            if (m_koWinners == null) return null;
            int w0 = KnockoutWins(0), w1 = KnockoutWins(1);
            bool decided = w0 >= KnockoutWinsNeeded || w1 >= KnockoutWinsNeeded
                || (m_koRound + 1 >= m_koWinners.Length && m_koRoundEndTick >= 0);
            if (!decided || GetTicksGone() < m_koRoundEndTick + KnockoutPauseTicks) return null;
            m_winnerTeam = w0 > w1 ? 0 : w1 > w0 ? 1 : -1;
            return true;
        }

        /// <summary>
        /// Блок раундов в начале кадра, порядок как у клиента: Max8191 (+400), bool (+332),
        /// bool (+405), Max7 (+396) — номер раунда, bool (+404), и по IntMax127 на раунд —
        /// победитель (−1 — не сыгран или ничья).
        /// </summary>
        public void EncodeKnockout(BitStream s)
        {
            if (m_koWinners == null) InitKnockout();
            s.WritePositiveIntMax8191(System.Math.Min(m_koRoundStartTick, 8191));
            s.WriteBoolean(m_koRoundEndTick >= 0);
            s.WriteBoolean(true); // +405: без него CombatHUD::updateGameModes не обновляет иконки живых/выбитых
            s.WritePositiveIntMax7(System.Math.Min(m_koRound, 7));
            s.WriteBoolean(false);
            foreach (int w in m_koWinners) s.WriteIntMax127(w);
        }
    }
}
