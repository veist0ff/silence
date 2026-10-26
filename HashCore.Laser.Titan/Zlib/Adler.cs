using System;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000DE RID: 222
	public sealed class Adler
	{
		// Token: 0x06000659 RID: 1625 RVA: 0x00014040 File Offset: 0x00012240
		public static uint Adler32(uint adler, byte[] buf, int index, int len)
		{
			if (buf == null)
			{
				return 1U;
			}
			uint num = adler & 65535U;
			uint num2 = adler >> 16 & 65535U;
			while (len > 0)
			{
				int i = (len < Adler.NMAX) ? len : Adler.NMAX;
				len -= i;
				while (i >= 16)
				{
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					num += (uint)buf[index++];
					num2 += num;
					i -= 16;
				}
				if (i != 0)
				{
					do
					{
						num += (uint)buf[index++];
						num2 += num;
					}
					while (--i != 0);
				}
				num %= Adler.BASE;
				num2 %= Adler.BASE;
			}
			return num2 << 16 | num;
		}

		// Token: 0x0400037B RID: 891
		private static readonly uint BASE = 65521U;

		// Token: 0x0400037C RID: 892
		private static readonly int NMAX = 5552;
	}
}
