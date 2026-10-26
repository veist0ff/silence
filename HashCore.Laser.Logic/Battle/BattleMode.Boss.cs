namespace HashCore.Laser.Logic.Battle
{
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Util;
    using HashCore.Laser.Server;

    /// <summary>
    /// Бой с боссом (game_mode_variations: IsTypeOfBossFight — BossFight 10, TownCrush 18, MegaBoss 61).
    /// Кооператив: одна команда (TeamCount 1) против босса. Босс — нейтральный персонаж
    /// (индекс −16, без игрока-хозяина), управляется мозгом ботов. Клиент в хвосте кадра
    /// читает процент здоровья босса (LogicGameObjectManagerClient::decode, ветка isTypeOfBossFight).
    /// Победа — босс убит; поражение — вышло время (BattleTicks секунд).
    /// </summary>
    public partial class BattleMode
    {
        /// <summary>Босс из события (EventData.BossCharacterId); 0 — из game_mode_variations.</summary>
        public int BossCharacterId;
        private int m_bossObjectId = -1;
        private int m_bossMaxSeen;

        public bool IsBossFightMode => GameModeUtil.GetVariationData(m_gameModeVariation)?.IsTypeOfBossFight == true;

        private Character GetBoss() =>
            m_bossObjectId < 0 ? null : m_gameObjectManager.GetGameObjectByID(m_bossObjectId) as Character;

        private CharacterData ResolveBossData()
        {
            CharacterData data = null;
            if (BossCharacterId != 0) data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(BossCharacterId);
            if (data == null)
            {
                string name = GameModeUtil.GetVariationData(m_gameModeVariation)?.BossCharacterData;
                if (!string.IsNullOrEmpty(name)) data = DataTables.Get(16).GetData<CharacterData>(name);
            }
            return data;
        }

        /// <summary>Персонаж босса для StartLoading (первая из трёх ссылок в конце сообщения).</summary>
        public int BossCharacterGlobalId => IsBossFightMode ? (ResolveBossData()?.GetGlobalId() ?? 0) : 0;

        /// <summary>
        /// Босс — НЕ игрок (LogicBattleModeServer::addBots оригинала его не добавляет):
        /// отдельный объект без хозяина (игрок и команда −1, как в tickSpawnEventStuffDelayed),
        /// на одной из клеток «2» карты. Управляется мозгом ботов в режиме босса (m_isBot = 2).
        /// </summary>
        public void AddBossPlayer() { }

        private void SpawnBoss()
        {
            CharacterData data = ResolveBossData();
            if (data == null) { Logger.Warning($"[Boss] бой {Id}: босс не найден ({BossCharacterId})"); return; }
            int x = m_tileMap.LogicWidth / 2, y = m_tileMap.LogicHeight / 2;
            if (m_tileMap.SpawnPointsTeam2.Count > 0)
            {
                var t = m_tileMap.SpawnPointsTeam2[new System.Random().Next(m_tileMap.SpawnPointsTeam2.Count)];
                x = t.X + 150; y = t.Y + 150;
            }
            Character boss = SpawnHero(data, 11, -16, 0, false);
            boss.SetPosition(x, y, 0);
            boss.SetBot(2);
            m_bossObjectId = boss.GetGlobalID();
            foreach (BattlePlayer p in m_players)
                Logger.Print($"[Boss] бой {Id}: игрок {p.PlayerIndex} команда {p.TeamIndex} бот {p.IsBot()} персонаж {p.CharacterId}");
            Logger.Print($"[Boss] бой {Id}: босс {data.Name} ({data.GetGlobalId()}) на точке босса");
        }

        /// <summary>Процент здоровья босса для хвоста кадра (Max127).</summary>
        public int BossHitpointPercent()
        {
            Character boss = GetBoss();
            if (boss == null || !boss.IsAlive()) return 0;
            int p = boss.GetHitpointPercentage();
            return p < 1 ? 1 : p > 100 ? 100 : p; // как у сервера оригинала: у живого босса минимум 1
        }

        private bool? BossGameOver()
        {
            if (m_bossObjectId < 0) return null;
            Character boss = GetBoss();
            if (boss == null || !boss.IsAlive())
            {
                m_winnerTeam = 0;
                Logger.Print($"[Boss] бой {Id}: босс побеждён");
                return true;
            }
            int seconds = GameModeUtil.GetVariationData(m_gameModeVariation)?.BattleTicks ?? 300;
            if (seconds <= 0) seconds = 300;
            if (GetTicksGone() >= seconds * 20)
            {
                m_winnerTeam = 1;
                Logger.Print($"[Boss] бой {Id}: время вышло, босс жив ({BossHitpointPercent()}%)");
                return true;
            }
            return null;
        }
    }
}
