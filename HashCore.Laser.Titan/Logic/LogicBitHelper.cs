namespace HashCore.Laser.Logic.Util
{
  
    using HashCore.Laser.Titan.Math;
    using System.Numerics;

    public static class LogicBitHelper
    {
        public static bool Get(BigInteger value, int index)
        {
            return ((value >> index) & BigInteger.One) == BigInteger.One;
        }

        public static BigInteger Set(BigInteger value, int index, bool data)
        {
            value &= ~(BigInteger.One << index);

            if (data)
                value |= (BigInteger.One << index);

            return value;
        }
    }
}
