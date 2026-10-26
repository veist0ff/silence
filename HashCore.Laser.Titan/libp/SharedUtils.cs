using System;
using System.IO;
using System.Text;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000DB RID: 219
	internal class SharedUtils
	{
		// Token: 0x06000651 RID: 1617 RVA: 0x000085D8 File Offset: 0x000067D8
		public static int URShift(int number, int bits)
		{
			return (int)((uint)number >> bits);
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x00013EFC File Offset: 0x000120FC
		public static int ReadInput(TextReader sourceTextReader, byte[] target, int start, int count)
		{
			if (target.Length == 0)
			{
				return 0;
			}
			char[] array = new char[target.Length];
			int num = sourceTextReader.Read(array, start, count);
			if (num == 0)
			{
				return -1;
			}
			for (int i = start; i < start + num; i++)
			{
				target[i] = (byte)array[i];
			}
			return num;
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x000085E0 File Offset: 0x000067E0
		internal static byte[] ToByteArray(string sourceString)
		{
			return Encoding.UTF8.GetBytes(sourceString);
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x000085ED File Offset: 0x000067ED
		internal static char[] ToCharArray(byte[] byteArray)
		{
			return Encoding.UTF8.GetChars(byteArray);
		}
	}
}
