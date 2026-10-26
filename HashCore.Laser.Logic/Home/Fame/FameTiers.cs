namespace HashCore.Laser.Logic.Home.Fame
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Уровни славы (csv_client/fame_tiers.csv) и их награды.
    ///
    /// Повторяет клиент: LogicDataTables::getFameTierByFame (0x10040d7c4) идёт по
    /// уровням, копит FameToNext и берёт первый, где сумма превысила славу; если
    /// славы больше всей таблицы — последний. Награда уровня — Reward + RewardType:
    /// 63 рамка (класс 85), 25 иконка (28), 19 эмоция (52).
    /// </summary>
    public static class FameTiers
    {
        private struct Tier
        {
            public int FameToNext;
            public string Reward;
            public int RewardType;
        }

        private static List<Tier> m_tiers;

        private static List<string[]> ReadRows(string path)
        {
            var rows = new List<string[]>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 2; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;
                string[] cells = lines[i].Split(',');
                for (int j = 0; j < cells.Length; j++) cells[j] = cells[j].Trim('"');
                rows.Add(cells);
            }
            return rows;
        }

        private static List<Tier> Tiers
        {
            get
            {
                if (m_tiers != null) return m_tiers;
                var tiers = new List<Tier>();
                foreach (string[] c in ReadRows("Assets/csv_client/fame_tiers.csv"))
                {
                    if (c[0].Length == 0) continue;
                    tiers.Add(new Tier
                    {
                        FameToNext = int.Parse(c[1]),
                        Reward = c[8],
                        RewardType = int.Parse(c[9]),
                    });
                }
                m_tiers = tiers;
                return tiers;
            }
        }

        /// <summary>Глобальный id по имени: номер строки с непустым Name в таблице.</summary>
        private static int FindGlobalId(string table, int classId, string name)
        {
            int index = 0;
            foreach (string[] c in ReadRows("Assets/csv_logic/" + table))
            {
                if (c[0].Length == 0) continue;
                if (c[0] == name) return classId * 1000000 + index;
                index++;
            }
            return 0;
        }

        /// <summary>Награда уровня, в который попадает эта слава. 0 — награды нет.</summary>
        public static int GetRewardGlobalId(int fame)
        {
            List<Tier> tiers = Tiers;
            if (tiers.Count == 0) return 0;

            Tier tier = tiers[tiers.Count - 1];
            int sum = 0;
            foreach (Tier t in tiers)
            {
                if (t.FameToNext + sum > fame) { tier = t; break; }
                sum += t.FameToNext;
            }

            switch (tier.RewardType)
            {
                case 63: return FindGlobalId("player_frames.csv", 85, tier.Reward);
                case 25: return FindGlobalId("player_thumbnails.csv", 28, tier.Reward);
                case 19: return FindGlobalId("emotes.csv", 52, tier.Reward);
                default: return 0;
            }
        }
    }
}
