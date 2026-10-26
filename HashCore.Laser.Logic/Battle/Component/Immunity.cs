namespace HashCore.Laser.Logic.Battle.Component
{
    public class Immunity
    {
        public int TargetIndex;
        public int Ticks;
        public LogicData TargetData;

        public Immunity(int TargetIndex,LogicData TargetData,int Ticks)
        {
            this.TargetIndex = TargetIndex;
            this.TargetData = TargetData;
            this.Ticks = Ticks;
        }

        public bool Tick(int a2)
        {
            Ticks -= a2;
            return Ticks < 1;
        }

    }
}
