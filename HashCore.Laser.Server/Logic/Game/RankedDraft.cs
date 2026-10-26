namespace HashCore.Laser.Server.Logic.Game
{
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Message.Battle;
    using HashCore.Laser.Server.Networking;

    /// <summary>
    /// Драфт ранговых на сервере: 22150 старт → 22151 баны (12152 от клиента, 22152/22164,
    /// 22153 конец) → пики по ходам 1-2-2-1 (22154 чей ход, 12155 от клиента, 22156 выбран)
    /// → обычный старт боя выбранным бойцом. Остальные пятеро — боты: банят и пикают сами.
    /// Раскладка сообщений — по клиенту (RankedTeamSelectionManager::onMessage).
    /// </summary>
    public class RankedDraft
    {
        public const int BanSeconds = 15;
        public const int PickSeconds = 20;
        private const int BotThinkMs = 1500;

        private static readonly Dictionary<Connection, RankedDraft> s_active = new Dictionary<Connection, RankedDraft>();

        private readonly MatchmakingSlot m_slot;
        private readonly List<MatchmakingEntry> m_entries;
        private readonly Connection m_conn;
        private readonly RankedMatchStartedMessage m_match;
        private readonly RankedMatchPlayer m_me;
        private readonly Random m_rnd = new Random();
        private readonly Dictionary<long, int> m_bans = new Dictionary<long, int>();
        private readonly List<int> m_order = new List<int>();
        private readonly object m_lock = new object();
        private Timer m_timer;
        private int m_phase;          // 1 — баны, 2 — пики, 3 — готово
        private int m_step;
        private DateTime m_deadline;
        private DateTime m_botPickAt;

        public static RankedDraft Get(Connection c) { lock (s_active) return s_active.TryGetValue(c, out RankedDraft d) ? d : null; }

        public RankedDraft(MatchmakingSlot slot, List<MatchmakingEntry> entries, RankedMatchStartedMessage match)
        {
            m_slot = slot; m_entries = entries; m_match = match;
            m_conn = entries[0].Connection;
            m_me = match.Players[0];

            // Порядок пиков 1-2-2-1: первая команда, затем двое второй, двое первой, последний второй.
            List<int> first = match.Players.Select((p, i) => (p, i)).Where(x => x.p.Team == match.FirstPickTeam).OrderBy(x => x.p.TeamIndex).Select(x => x.i).ToList();
            List<int> second = match.Players.Select((p, i) => (p, i)).Where(x => x.p.Team != match.FirstPickTeam).OrderBy(x => x.p.TeamIndex).Select(x => x.i).ToList();
            m_order.AddRange(new[] { first[0], second[0], second[1], first[1], first[2], second[2] });
        }

        public void Start()
        {
            lock (s_active) s_active[m_conn] = this;
            m_conn.Send(m_match);
            m_phase = 1;
            m_deadline = DateTime.UtcNow.AddSeconds(BanSeconds);
            m_conn.Send(new RankedMatchBanStartedMessage { Seconds = BanSeconds });
            m_timer = new Timer(_ => Tick(), null, 500, 500);
            Logger.Print($"[Ranked] драфт начат: баны {BanSeconds} с");
        }

        public void OnBan(int heroGlobalId)
        {
            lock (m_lock)
            {
                if (m_phase != 1) return;
                m_bans[m_me.AccountId] = heroGlobalId;
                m_conn.Send(new RankedMatchBanHeroResponseMessage { AccountId = m_me.AccountId, HeroGlobalId = heroGlobalId });
                var status = new RankedMatchBanStatusUpdateMessage();
                status.PreBans.Add(new KeyValuePair<long, int>(m_me.AccountId, heroGlobalId));
                foreach (RankedMatchPlayer p in m_match.Players) status.Done.Add(new KeyValuePair<long, bool>(p.AccountId, p == m_me));
                m_conn.Send(status);
                Logger.Print($"[Ranked] бан игрока: {heroGlobalId}");
                EndBans();
            }
        }

        public void OnPick(int heroGlobalId)
        {
            lock (m_lock)
            {
                if (m_phase != 2 || Current != m_me || IsTaken(heroGlobalId)) return;
                Pick(m_me, heroGlobalId);
            }
        }

        private RankedMatchPlayer Current => m_step < m_order.Count ? m_match.Players[m_order[m_step]] : null;

        private void Tick()
        {
            lock (m_lock)
            {
                try
                {
                    if (m_phase == 1 && DateTime.UtcNow >= m_deadline) EndBans();
                    else if (m_phase == 2 && Current != null)
                    {
                        if (Current != m_me && DateTime.UtcNow >= m_botPickAt) Pick(Current, RandomHero());
                        else if (Current == m_me && DateTime.UtcNow >= m_deadline) Pick(m_me, MyDefaultHero());
                    }
                }
                catch (Exception e) { Logger.Error($"[Ranked] драфт: {e}"); Finish(); }
            }
        }

        private void EndBans()
        {
            foreach (RankedMatchPlayer p in m_match.Players)
                if (p != m_me && !m_bans.ContainsKey(p.AccountId)) m_bans[p.AccountId] = RandomHero();

            var ended = new RankedMatchBanEndedMessage();
            foreach (RankedMatchPlayer p in m_match.Players)
                ended.Bans.Add(new KeyValuePair<long, int>(p.AccountId, m_bans.TryGetValue(p.AccountId, out int h) ? h : 0));
            m_conn.Send(ended);
            m_phase = 2;
            m_step = 0;
            StartTurn();
        }

        private void StartTurn()
        {
            if (Current == null) { Finish(); return; }
            RankedMatchPlayer next = m_step + 1 < m_order.Count ? m_match.Players[m_order[m_step + 1]] : null;
            m_conn.Send(new RankedMatchPickStartedMessage { Seconds = PickSeconds, Current = Current, NextAccountId = next?.AccountId ?? 0 });
            m_deadline = DateTime.UtcNow.AddSeconds(PickSeconds);
            m_botPickAt = DateTime.UtcNow.AddMilliseconds(BotThinkMs);
        }

        private void Pick(RankedMatchPlayer p, int hero)
        {
            p.HeroGlobalId = hero;
            m_conn.Send(new RankedMatchHeroPickedMessage { Player = p });
            Logger.Print($"[Ranked] пик {p.Name}: {hero}");
            m_step++;
            StartTurn();
        }

        private bool IsTaken(int hero) => m_bans.ContainsValue(hero) || m_match.Players.Any(p => p.HeroGlobalId == hero);

        private int MyDefaultHero()
        {
            int own = m_conn.Home.CharacterIds[0];
            return own != 0 && !IsTaken(own) ? own : RandomHero();
        }

        private int RandomHero()
        {
            var heroes = DataTables.Get(DataType.Character).GetDatas().OfType<CharacterData>()
                .Where(c => c.IsHero() && !c.Disabled && !IsTaken(c.GetGlobalId()))
                .Select(c => c.GetGlobalId()).ToList();
            return heroes.Count > 0 ? heroes[m_rnd.Next(heroes.Count)] : 16000000;
        }

        private void Finish()
        {
            if (m_phase == 3) return;
            m_phase = 3;
            m_timer?.Dispose();
            lock (s_active) s_active.Remove(m_conn);
            if (m_me.HeroGlobalId != 0) m_conn.Home.CharacterIds[0] = m_me.HeroGlobalId;
            Logger.Print($"[Ranked] драфт окончен, боец игрока {m_me.HeroGlobalId}");
            m_slot.StartGame(m_entries, skipDraft: true);
        }
    }
}
