namespace HashCore.Laser.Logic.Battle.Arena
{
    using HashCore.Laser.Logic.Battle.Input;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Message.Battle;
    using HashCore.Laser.Server;

    /// <summary>
    /// Минимальный бой, написанный заново. От старого BattleMode взят только
    /// транспорт (игроки, сессии, ввод, таймер, конец боя); мир, тик и кадр —
    /// свои. Бойцы появляются на точках карты, ходят по вводу; кадр — ArenaFrame.
    /// Атаки, ульты, боты и предметы — следующими шагами.
    /// </summary>
    public class ArenaBattle : BattleMode
    {
        public const int DurationTicks = 20 * 150;

        public readonly List<ArenaHero> Heroes = new List<ArenaHero>();
        private readonly Dictionary<BattlePlayer, ArenaHero> _byPlayer = new Dictionary<BattlePlayer, ArenaHero>();
        private readonly HashSet<long> _logged = new HashSet<long>();
        private int _tick;

        public ArenaBattle(int locationId) : base(locationId) { }

        public ArenaHero HeroOf(BattlePlayer p) => p != null && _byPlayer.TryGetValue(p, out ArenaHero h) ? h : null;

        public override void AddGameObjects()
        {
            TileMap map = GetTileMap();
            var points = new List<Tile>();
            points.AddRange(map.SpawnPointsTeam1);
            points.AddRange(map.SpawnPointsTeam2);

            int i = 0;
            foreach (BattlePlayer p in GetPlayers())
            {
                CharacterData data = p.CharacterData;
                if (data == null) continue;
                Tile spawn = points.Count > 0 ? points[i % points.Count] : null;
                var h = new ArenaHero
                {
                    Instance = Heroes.Count,
                    Player = p,
                    Data = data,
                    Weapon = data.WeaponSkill != null ? DataTables.Get(DataType.Skill).GetData<SkillData>(data.WeaponSkill) : null,
                    Ulti = data.UltimateSkill != null ? DataTables.Get(DataType.Skill).GetData<SkillData>(data.UltimateSkill) : null,
                    X = spawn != null ? spawn.X + 150 : 1500,
                    Y = spawn != null ? spawn.Y + 150 : 1500,
                    Hitpoints = data.Hitpoints,
                    MaxHitpoints = data.Hitpoints,
                };
                h.TargetX = h.X; h.TargetY = h.Y;
                Heroes.Add(h);
                _byPlayer[p] = h;
                p.IsAlive = true;
                p.OwnObjectId = h.ClientGlobalId;
                p.SetSpawnPoint(h.X, h.Y);
                i++;
            }
            Logger.Print($"[Arena] бой {Id}: {Heroes.Count} бойцов, карта {map.Width}x{map.Height}, режим {GetGameModeVariation()}");
        }

        public override void ExecuteOneTick()
        {
            try
            {
                while (m_inputQueue.Count > 0) HandleInput(m_inputQueue.Dequeue());

                if (_tick >= DurationTicks)
                {
                    m_updateTimer?.Dispose();
                    m_winnerTeam = -1;
                    GameOver();
                    IsGameOver = true;
                    return;
                }

                foreach (ArenaHero h in Heroes) h.Tick();
                SendFrames();
                _tick++;
                m_time.IncreaseTick();
            }
            catch (Exception e)
            {
                Logger.Error($"[Arena] бой {Id} тик {_tick}: {e}");
                m_updateTimer?.Dispose();
                IsGameOver = true;
            }
        }

        private void HandleInput(ClientInput input)
        {
            BattlePlayer p = GetPlayerBySessionId(input.OwnerSessionId);
            if (p == null || p.LastHandledInput >= input.Index) return;
            p.LastHandledInput = input.Index;
            ArenaHero h = HeroOf(p);
            if (h == null) return;

            if (input.Type == 2)                          // движение к точке
            {
                h.TargetX = input.X; h.TargetY = input.Y; h.Moving = true;
            }
            // Тип 4 — НЕ выход: клиент шлёт его ещё на загрузке. Игрок убирается
            // из боя только когда бой закончен (LogicBattleModeServer::handleClientInput).
        }

        private void SendFrames()
        {
            foreach (BattlePlayer p in GetPlayers())
            {
                if (p.GameListener == null) continue;
                BitStream s = new BitStream(64);
                ArenaFrame.Encode(s, this, p, HeroOf(p));

                if (_logged.Add(p.SessionId))
                    System.Console.WriteLine("[Vision::step] battle " + Id + " tick " + _tick + " p" + p.PlayerIndex
                        + " objects " + Heroes.Count + " score 0 len " + s.GetWriteOffset() + " "
                        + System.BitConverter.ToString(s.GetByteArray(), 0, s.GetWriteOffset()));

                p.GameListener.SendMessage(new VisionUpdateMessage
                {
                    Tick = _tick,
                    HandledInputs = p.LastHandledInput,
                    Viewers = 0,
                    VisionBitStream = s
                });
            }
        }
    }
}
