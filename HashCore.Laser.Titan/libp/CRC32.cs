using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000C2 RID: 194
	[Guid("ebc25cf6-9120-4283-b972-0e5520d0000C")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class CRC32
	{
		public long TotalBytesRead { get; private set; }

		public int Crc32Result
		{
			get
			{
				return (int)(~(int)this.m_register);
			}
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x00007D5E File Offset: 0x00005F5E
		public int GetCrc32(Stream input)
		{
			return this.GetCrc32AndCopy(input, null);
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x0000CAE0 File Offset: 0x0000ACE0
		public int GetCrc32AndCopy(Stream input, Stream output)
		{
			if (input == null)
			{
				throw new Exception("The input stream must not be null.");
			}
			byte[] array = new byte[8192];
			int count = 8192;
			this.TotalBytesRead = 0L;
			int i = input.Read(array, 0, 8192);
			if (output != null)
			{
				output.Write(array, 0, i);
			}
			this.TotalBytesRead += (long)i;
			while (i > 0)
			{
				this.SlurpBlock(array, 0, i);
				i = input.Read(array, 0, count);
				if (output != null)
				{
					output.Write(array, 0, i);
				}
				this.TotalBytesRead += (long)i;
			}
			return (int)(~(int)this.m_register);
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x00007D68 File Offset: 0x00005F68
		public int ComputeCrc32(int W, byte B)
		{
			return this.m_InternalComputeCrc32((uint)W, B);
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x00007D72 File Offset: 0x00005F72
		internal int m_InternalComputeCrc32(uint W, byte B)
		{
			return (int)(this.crc32Table[(int)((W ^ (uint)B) & 255U)] ^ W >> 8);
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x0000CB80 File Offset: 0x0000AD80
		public void SlurpBlock(byte[] block, int offset, int count)
		{
			if (block == null)
			{
				throw new Exception("The data buffer must not be null.");
			}
			for (int i = 0; i < count; i++)
			{
				int num = offset + i;
				byte b = block[num];
				if (this.reverseBits)
				{
					uint num2 = this.m_register >> 24 ^ (uint)b;
					this.m_register = (this.m_register << 8 ^ this.crc32Table[(int)num2]);
				}
				else
				{
					uint num3 = (this.m_register & 255U) ^ (uint)b;
					this.m_register = (this.m_register >> 8 ^ this.crc32Table[(int)num3]);
				}
			}
			this.TotalBytesRead += (long)count;
		}

		// Token: 0x06000570 RID: 1392 RVA: 0x0000CC14 File Offset: 0x0000AE14
		public void UpdateCRC(byte b)
		{
			if (this.reverseBits)
			{
				uint num = this.m_register >> 24 ^ (uint)b;
				this.m_register = (this.m_register << 8 ^ this.crc32Table[(int)num]);
				return;
			}
			uint num2 = (this.m_register & 255U) ^ (uint)b;
			this.m_register = (this.m_register >> 8 ^ this.crc32Table[(int)num2]);
		}

		// Token: 0x06000571 RID: 1393 RVA: 0x0000CC74 File Offset: 0x0000AE74
		public void UpdateCRC(byte b, int n)
		{
			while (n-- > 0)
			{
				if (this.reverseBits)
				{
					uint num = this.m_register >> 24 ^ (uint)b;
					this.m_register = (this.m_register << 8 ^ this.crc32Table[(int)num]);
				}
				else
				{
					uint num2 = (this.m_register & 255U) ^ (uint)b;
					this.m_register = (this.m_register >> 8 ^ this.crc32Table[(int)num2]);
				}
			}
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x0000CCE0 File Offset: 0x0000AEE0
		private static uint ReverseBits(uint data)
		{
			uint num = (data & 1431655765U) << 1 | (data >> 1 & 1431655765U);
			num = ((num & 858993459U) << 2 | (num >> 2 & 858993459U));
			num = ((num & 252645135U) << 4 | (num >> 4 & 252645135U));
			return num << 24 | (num & 65280U) << 8 | (num >> 8 & 65280U) | num >> 24;
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x0000CD4C File Offset: 0x0000AF4C
		private static byte ReverseBits(byte data)
		{
			int num = (int)data * 131586;
			uint num2 = (uint)(num & 17055760);
			uint num3 = (uint)(num << 2 & 34111520);
			return (byte)(16781313U * (num2 + num3) >> 24);
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x0000CD80 File Offset: 0x0000AF80
		private void GenerateLookupTable()
		{
			this.crc32Table = new uint[256];
			byte b = 0;
			do
			{
				uint num = (uint)b;
				for (byte b2 = 8; b2 > 0; b2 -= 1)
				{
					if ((num & 1U) == 1U)
					{
						num = (num >> 1 ^ this.dwPolynomial);
					}
					else
					{
						num >>= 1;
					}
				}
				if (this.reverseBits)
				{
					this.crc32Table[(int)CRC32.ReverseBits(b)] = CRC32.ReverseBits(num);
				}
				else
				{
					this.crc32Table[(int)b] = num;
				}
				b += 1;
			}
			while (b != 0);
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x0000CDF8 File Offset: 0x0000AFF8
		private uint gf2_matrix_times(uint[] matrix, uint vec)
		{
			uint num = 0U;
			int num2 = 0;
			while (vec != 0U)
			{
				if ((vec & 1U) == 1U)
				{
					num ^= matrix[num2];
				}
				vec >>= 1;
				num2++;
			}
			return num;
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x0000CE24 File Offset: 0x0000B024
		private void gf2_matrix_square(uint[] square, uint[] mat)
		{
			for (int i = 0; i < 32; i++)
			{
				square[i] = this.gf2_matrix_times(mat, mat[i]);
			}
		}

		// Token: 0x06000577 RID: 1399 RVA: 0x0000CE4C File Offset: 0x0000B04C
		public void Combine(int crc, int length)
		{
			uint[] array = new uint[32];
			uint[] array2 = new uint[32];
			if (length == 0)
			{
				return;
			}
			uint num = ~this.m_register;
			array2[0] = this.dwPolynomial;
			uint num2 = 1U;
			for (int i = 1; i < 32; i++)
			{
				array2[i] = num2;
				num2 <<= 1;
			}
			this.gf2_matrix_square(array, array2);
			this.gf2_matrix_square(array2, array);
			uint num3 = (uint)length;
			do
			{
				this.gf2_matrix_square(array, array2);
				if ((num3 & 1U) == 1U)
				{
					num = this.gf2_matrix_times(array, num);
				}
				num3 >>= 1;
				if (num3 == 0U)
				{
					break;
				}
				this.gf2_matrix_square(array2, array);
				if ((num3 & 1U) == 1U)
				{
					num = this.gf2_matrix_times(array2, num);
				}
				num3 >>= 1;
			}
			while (num3 != 0U);
			num ^= (uint)crc;
			this.m_register = ~num;
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x00007D88 File Offset: 0x00005F88
		public CRC32() : this(false)
		{
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x00007D91 File Offset: 0x00005F91
		public CRC32(bool reverseBits) : this(-306674912, reverseBits)
		{
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x00007D9F File Offset: 0x00005F9F
		public CRC32(int polynomial, bool reverseBits)
		{
			this.reverseBits = reverseBits;
			this.dwPolynomial = (uint)polynomial;
			this.GenerateLookupTable();
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x00007DC2 File Offset: 0x00005FC2
		public void Reset()
		{
			this.m_register = uint.MaxValue;
		}

		// Token: 0x04000229 RID: 553
		private readonly uint dwPolynomial;

		// Token: 0x0400022A RID: 554
		private readonly bool reverseBits;

		// Token: 0x0400022B RID: 555
		private uint[] crc32Table;

		// Token: 0x0400022C RID: 556
		private const int BUFFER_SIZE = 8192;

		// Token: 0x0400022D RID: 557
		private uint m_register = uint.MaxValue;
	}
}
