using System;
using HashCore.Laser.Titan.ZLib;
using HashCore.Laser.Server;

namespace HashCore.Laser.Titan.Util
{
    // Token: 0x020000E6 RID: 230
    public static class ZLibHelper
    {
        // Token: 0x060006A8 RID: 1704 RVA: 0x00015310 File Offset: 0x00013510
        public static int DecompressInMySQLFormat(byte[] input, out byte[] output)
        {
            int num = (int)input[0] | (int)input[1] << 8 | (int)input[2] << 16 | (int)input[3] << 24;
            int num2 = input.Length - 4;
            byte[] array = new byte[num2];
            Array.Copy(input, 4, array, 0, num2);
            output = ZLibStream.UncompressBuffer(array);
            if (num != output.Length)
            {
                Logger.Error("ZLibHelper.decompressInMySQLFormat decompressed byte array is corrupted");
                return -1;
            }
            return num;
        }

        // Token: 0x060006A9 RID: 1705 RVA: 0x00015368 File Offset: 0x00013568
        public static int CompressInZLibFormat(byte[] input, out byte[] output)
        {
            byte[] array = ZLibStream.CompressBuffer(input, CompressionLevel.BestSpeed);
            int num = array.Length;
            int num2 = input.Length;
            output = new byte[num + 4];
            output[0] = (byte)num2;
            output[1] = (byte)(num2 >> 8);
            output[2] = (byte)(num2 >> 16);
            output[3] = (byte)(num2 >> 24);
            Array.Copy(array, 0, output, 4, num);
            return output.Length;
        }
    }
}
