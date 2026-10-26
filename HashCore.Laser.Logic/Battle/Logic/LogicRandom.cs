namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Генератор случайных чисел боя, 1:1 с LogicRandom (0x100a18318).
    ///
    /// Это xorshift на 32 битах. Три детали здесь обязательны к точному повторению,
    /// иначе сервер и клиент разойдутся уже на первом же случайном числе:
    ///
    ///   1. нулевое зерно подменяется на -1, а не на единицу;
    ///   2. средний сдвиг арифметический, со знаком (в оригинале это ASR);
    ///   3. остаток берётся беззнаковый, поэтому int.MinValue после смены знака
    ///      остаётся отрицательным и делится как большое положительное число.
    /// </summary>
    public class LogicRandom
    {
        /// <summary>Текущее зерно. Его же пишет и читает encode/decode.</summary>
        public int Seed;

        public LogicRandom()
        {
        }

        public LogicRandom(int seed)
        {
            Seed = seed;
        }

        public void SetSeed(int seed)
        {
            Seed = seed;
        }

        /// <summary>0x100a18318. Случайное число от нуля до max, не включая max.</summary>
        public int Rand(int max)
        {
            if (max < 1) return 0;

            int state = Seed;
            if (state == 0) state = -1;

            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
            }

            Seed = state;

            if (state < 0) state = -state;

            return (int)((uint)state % (uint)max);
        }

        /// <summary>Случайное число в диапазоне, нижняя граница включается.</summary>
        public int Rand(int min, int max)
        {
            return Rand(max - min) + min;
        }
    }
}
