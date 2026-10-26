namespace HashCore.Laser.Logic.Battle.Arena
{
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;

    /// <summary>Боец минимального боя: только то, что нужно, чтобы стоять и ходить.</summary>
    public class ArenaHero
    {
        public int Instance;            // номер объекта в кадре (GlobalID = 1*1000000 + Instance)
        public BattlePlayer Player;
        public CharacterData Data;
        public SkillData Weapon;
        public SkillData Ulti;

        public int X, Y;
        public int TargetX, TargetY;
        public bool Moving;
        public int Angle;               // 0..359
        public int Hitpoints, MaxHitpoints;

        public int Team => Player.TeamIndex;
        public int PlayerIndex => Player.PlayerIndex;
        public bool Alive => Hitpoints > 0;

        /// <summary>GlobalID, с которым клиент сравнивает «свой» объект (класс 1 — персонаж).</summary>
        public int ClientGlobalId => 1000000 + Instance;

        /// <summary>Сдвиг к цели за один тик: скорость из таблицы — в единицах за секунду, 20 тиков.</summary>
        public void Tick()
        {
            if (!Moving || !Alive) return;
            int dx = TargetX - X, dy = TargetY - Y;
            long dist2 = (long)dx * dx + (long)dy * dy;
            int step = System.Math.Max(1, Data.Speed / 20);
            if (dist2 <= (long)step * step)
            {
                X = TargetX; Y = TargetY; Moving = false;
                return;
            }
            double dist = System.Math.Sqrt(dist2);
            X += (int)(dx * step / dist);
            Y += (int)(dy * step / dist);
            Angle = ((int)(System.Math.Atan2(dy, dx) * 180 / System.Math.PI) + 360) % 360;
        }
    }
}
