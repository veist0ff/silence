using HashCore.Laser.Titan.Library;

namespace HashCore.Laser.Server.Networking.Security
{
    public static class PepperKey
    {
        public const int VERSION = 14;
        
        public static readonly byte[] CLIENT_SK = { 0x84, 0x82, 0x46, 0x93, 0xAB, 0x9A, 0xA7, 0xAA, 0xC9, 0x53, 0x16, 0x1D, 0x34, 0x26, 0xE8, 0x72, 0xE2, 0xD4, 0xF6, 0xB3, 0x28, 0xCD, 0x27, 0xCA, 0xE3, 0x77, 0x33, 0x1B, 0xA1, 0xBC, 0x23, 0x5C };
        public static readonly byte[] CLIENT_PK = TweetNaCl.CryptoScalarmultBase(CLIENT_SK);

        public static readonly byte[] SERVER_SK = { 158, 217, 110, 5, 87, 249, 222, 234, 204, 121, 177, 228, 59, 79, 93, 217, 25, 33, 113, 185, 119, 171, 205, 246, 11, 185, 185, 22, 140, 152, 107, 20};
        public static readonly byte[] SERVER_PK = TweetNaCl.CryptoScalarmultBase(SERVER_SK);

    }
}
