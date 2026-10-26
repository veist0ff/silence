namespace HashCore.Laser.Server.Logic.Game
{
    using HashCore.Laser.Logic.Battle;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public static class Battles
    {
        private static long m_battleIdCounter;
        private static ConcurrentDictionary<long, BattleMode> m_battles;

        public static void Init()
        {
            m_battles = new ConcurrentDictionary<long, BattleMode>();
            m_battleIdCounter = 0;

            new Thread(Update).Start();
        }

        public static void Update()
        {
            while (true)
            {
                try
                {
                    foreach (BattleMode battle in m_battles.Values.ToArray())
                    {
                        if (battle.IsGameOver)
                        {
                            m_battles.Remove(battle.Id, out _);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error($"[Battles::Update] {exception}");
                }
                Thread.Sleep(1000);
            }
        }

        public static long Add(BattleMode battle)
        {
            long id = ++m_battleIdCounter;
            m_battles[id] = battle;
            return id;
        }

        /// <summary>Идущие бои (для Brawl TV).</summary>
        public static List<BattleMode> GetActive()
        {
            var list = new List<BattleMode>();
            foreach (BattleMode b in m_battles.Values)
                if (!b.IsGameOver) list.Add(b);
            return list;
        }

        public static BattleMode Get(long id)
        {
            if (!m_battles.ContainsKey(id)) return null;
            return m_battles[id];
        }
    }
}
