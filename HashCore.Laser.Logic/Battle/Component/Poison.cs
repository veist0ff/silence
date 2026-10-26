namespace HashCore.Laser.Logic.Battle.Component
{
    using System.Diagnostics.SymbolStore;
    using HashCore.Laser.Logic.Battle.Objects;

    public class Poison
    {
        public int TickTimer;
        public int SecondTimer;
        public int Damage;
        public int NormalDMG;
        public Character Source;
        public int SourceIndex;
        public int Type;
        public bool IsUlti;
        
        public Poison(int Damage,int NormalDMG,bool IsUlti,Character Source,int SourceIndex,int Type)
        {
            int v7;
            if (Type == 4) v7 = 2;
            else v7 = 4;
            this.Damage = Damage/v7;
            this.NormalDMG = NormalDMG/v7;
            this.IsUlti = IsUlti;
            this.Source = Source;
            this.SourceIndex = SourceIndex;
            this.Type = Type;
            this.TickTimer = 20;
            this.SecondTimer = v7;
        }
        public void RefreshPosion(int Damage, int NormalDMG, bool IsUlti, int Type)
        {
            int v7;
            if (Type == 4) v7 = 2;
            else v7 = 4;
            this.Damage = Damage / v7;
            this.NormalDMG = NormalDMG / v7;
            this.IsUlti = IsUlti;
            this.Type = Type;
            this.TickTimer = 20;
            this.SecondTimer = v7;
        }
        public bool AllowStacking()
        {
            return Type == 4;
        }
        public bool Tick(Character character)
        {
            TickTimer--;
            if (TickTimer > 0) return false;
            character.CauseDamage(Source, Damage, NormalDMG, IsUlti, null, true);
            SecondTimer--;
            TickTimer = 20;
            if (SecondTimer >= 1) return false;
            return true;
        }
    }
}
