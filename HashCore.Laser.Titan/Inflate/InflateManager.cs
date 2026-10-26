using System;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000CF RID: 207
	internal sealed class InflateManager
	{
		internal bool HandleRfc1950HeaderBytes { get; set; } = true;

		// Token: 0x06000611 RID: 1553 RVA: 0x000083FB File Offset: 0x000065FB
		public InflateManager()
		{
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x0000840A File Offset: 0x0000660A
		public InflateManager(bool expectRfc1950HeaderBytes)
		{
			this.HandleRfc1950HeaderBytes = expectRfc1950HeaderBytes;
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x00011EEC File Offset: 0x000100EC
		internal int Reset()
		{
			ZLibCodec codec = this.m_codec;
			this.m_codec.TotalBytesOut = 0L;
			codec.TotalBytesIn = 0L;
			this.m_codec.Message = null;
			this.mode = (this.HandleRfc1950HeaderBytes ? InflateManager.InflateManagerMode.METHOD : InflateManager.InflateManagerMode.BLOCKS);
			this.blocks.Reset();
			return 0;
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x00008420 File Offset: 0x00006620
		internal int End()
		{
			if (this.blocks != null)
			{
				this.blocks.Free();
			}
			this.blocks = null;
			return 0;
		}

		// Token: 0x06000615 RID: 1557 RVA: 0x00011F4C File Offset: 0x0001014C
		internal int Initialize(ZLibCodec codec, int w)
		{
			this.m_codec = codec;
			this.m_codec.Message = null;
			this.blocks = null;
			if (w >= 8 && w <= 15)
			{
				this.wbits = w;
				this.blocks = new InflateBlocks(codec, this.HandleRfc1950HeaderBytes ? this : null, 1 << w);
				this.Reset();
				return 0;
			}
			this.End();
			throw new ZLibException("Bad window size.");
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x00011FC0 File Offset: 0x000101C0
		internal int Inflate(FlushType flush)
		{
			if (this.m_codec.InputBuffer == null)
			{
				throw new ZLibException("InputBuffer is null. ");
			}
			int num = 0;
			int num2 = -5;
			int nextIn;
			for (;;)
			{
				switch (this.mode)
				{
				case InflateManager.InflateManagerMode.METHOD:
				{
					if (this.m_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					this.m_codec.AvailableBytesIn--;
					this.m_codec.TotalBytesIn += 1L;
					byte[] inputBuffer = this.m_codec.InputBuffer;
					ZLibCodec codec = this.m_codec;
					nextIn = codec.NextIn;
					codec.NextIn = nextIn + 1;
					if (((this.method = inputBuffer[nextIn]) & 15) != 8)
					{
						this.mode = InflateManager.InflateManagerMode.BAD;
						this.m_codec.Message = string.Format("unknown compression method (0x{0:X2})", this.method);
						this.marker = 5;
						continue;
					}
					if ((this.method >> 4) + 8 > this.wbits)
					{
						this.mode = InflateManager.InflateManagerMode.BAD;
						this.m_codec.Message = string.Format("invalid window size ({0})", (this.method >> 4) + 8);
						this.marker = 5;
						continue;
					}
					this.mode = InflateManager.InflateManagerMode.FLAG;
					continue;
				}
				case InflateManager.InflateManagerMode.FLAG:
				{
					if (this.m_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					this.m_codec.AvailableBytesIn--;
					this.m_codec.TotalBytesIn += 1L;
					byte[] inputBuffer2 = this.m_codec.InputBuffer;
					ZLibCodec codec2 = this.m_codec;
					nextIn = codec2.NextIn;
					codec2.NextIn = nextIn + 1;
					int num3 = inputBuffer2[nextIn] & 255;
					if (((this.method << 8) + num3) % 31 != 0)
					{
						this.mode = InflateManager.InflateManagerMode.BAD;
						this.m_codec.Message = "incorrect header check";
						this.marker = 5;
						continue;
					}
					this.mode = (((num3 & 32) == 0) ? InflateManager.InflateManagerMode.BLOCKS : InflateManager.InflateManagerMode.DICT4);
					continue;
				}
				case InflateManager.InflateManagerMode.DICT4:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						byte[] inputBuffer3 = this.m_codec.InputBuffer;
						ZLibCodec codec3 = this.m_codec;
						nextIn = codec3.NextIn;
						codec3.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(inputBuffer3[nextIn] << 24 & 4278190080L);
						this.mode = InflateManager.InflateManagerMode.DICT3;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.DICT3:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						uint num4 = this.expectedCheck;
						byte[] inputBuffer4 = this.m_codec.InputBuffer;
						ZLibCodec codec4 = this.m_codec;
						nextIn = codec4.NextIn;
						codec4.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(num4 + (inputBuffer4[nextIn] << 16 & 16711680U));
						this.mode = InflateManager.InflateManagerMode.DICT2;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.DICT2:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						uint num5 = this.expectedCheck;
						byte[] inputBuffer5 = this.m_codec.InputBuffer;
						ZLibCodec codec5 = this.m_codec;
						nextIn = codec5.NextIn;
						codec5.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(num5 + (inputBuffer5[nextIn] << 8 & 65280U));
						this.mode = InflateManager.InflateManagerMode.DICT1;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.DICT1:
					goto IL_659;
				case InflateManager.InflateManagerMode.DICT0:
					goto IL_6EA;
				case InflateManager.InflateManagerMode.BLOCKS:
					num2 = this.blocks.Process(num2);
					if (num2 == -3)
					{
						this.mode = InflateManager.InflateManagerMode.BAD;
						this.marker = 0;
						continue;
					}
					if (num2 == 0)
					{
						num2 = num;
					}
					if (num2 != 1)
					{
						return num2;
					}
					num2 = num;
					this.computedCheck = this.blocks.Reset();
					if (this.HandleRfc1950HeaderBytes)
					{
						this.mode = InflateManager.InflateManagerMode.CHECK4;
						continue;
					}
					goto IL_70E;
				case InflateManager.InflateManagerMode.CHECK4:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						byte[] inputBuffer6 = this.m_codec.InputBuffer;
						ZLibCodec codec6 = this.m_codec;
						nextIn = codec6.NextIn;
						codec6.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(inputBuffer6[nextIn] << 24 & 4278190080L);
						this.mode = InflateManager.InflateManagerMode.CHECK3;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.CHECK3:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						uint num6 = this.expectedCheck;
						byte[] inputBuffer7 = this.m_codec.InputBuffer;
						ZLibCodec codec7 = this.m_codec;
						nextIn = codec7.NextIn;
						codec7.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(num6 + (inputBuffer7[nextIn] << 16 & 16711680U));
						this.mode = InflateManager.InflateManagerMode.CHECK2;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.CHECK2:
					if (this.m_codec.AvailableBytesIn != 0)
					{
						num2 = num;
						this.m_codec.AvailableBytesIn--;
						this.m_codec.TotalBytesIn += 1L;
						uint num7 = this.expectedCheck;
						byte[] inputBuffer8 = this.m_codec.InputBuffer;
						ZLibCodec codec8 = this.m_codec;
						nextIn = codec8.NextIn;
						codec8.NextIn = nextIn + 1;
						this.expectedCheck = (uint)(num7 + (inputBuffer8[nextIn] << 8 & 65280U));
						this.mode = InflateManager.InflateManagerMode.CHECK1;
						continue;
					}
					return num2;
				case InflateManager.InflateManagerMode.CHECK1:
				{
					if (this.m_codec.AvailableBytesIn == 0)
					{
						return num2;
					}
					num2 = num;
					this.m_codec.AvailableBytesIn--;
					this.m_codec.TotalBytesIn += 1L;
					uint num8 = this.expectedCheck;
					byte[] inputBuffer9 = this.m_codec.InputBuffer;
					ZLibCodec codec9 = this.m_codec;
					nextIn = codec9.NextIn;
					codec9.NextIn = nextIn + 1;
					this.expectedCheck = num8 + (inputBuffer9[nextIn] & 255U);
					if (this.computedCheck != this.expectedCheck)
					{
						this.mode = InflateManager.InflateManagerMode.BAD;
						this.m_codec.Message = "incorrect data check";
						this.marker = 5;
						continue;
					}
					goto IL_720;
				}
				case InflateManager.InflateManagerMode.DONE:
					return 1;
				case InflateManager.InflateManagerMode.BAD:
					goto IL_72A;
				}
				goto Block_20;
			}
			return num2;
			Block_20:
			throw new ZLibException("Stream error.");
			IL_659:
			if (this.m_codec.AvailableBytesIn == 0)
			{
				return num2;
			}
			this.m_codec.AvailableBytesIn--;
			this.m_codec.TotalBytesIn += 1L;
			uint num9 = this.expectedCheck;
			byte[] inputBuffer10 = this.m_codec.InputBuffer;
			ZLibCodec codec10 = this.m_codec;
			nextIn = codec10.NextIn;
			codec10.NextIn = nextIn + 1;
			this.expectedCheck = num9 + (inputBuffer10[nextIn] & 255U);
			this.m_codec.m_Adler32 = this.expectedCheck;
			this.mode = InflateManager.InflateManagerMode.DICT0;
			return 2;
			IL_6EA:
			this.mode = InflateManager.InflateManagerMode.BAD;
			this.m_codec.Message = "need dictionary";
			this.marker = 0;
			return -2;
			IL_70E:
			this.mode = InflateManager.InflateManagerMode.DONE;
			return 1;
			IL_720:
			this.mode = InflateManager.InflateManagerMode.DONE;
			return 1;
			IL_72A:
			throw new ZLibException(string.Format("Bad state ({0})", this.m_codec.Message));
		}

		// Token: 0x06000617 RID: 1559 RVA: 0x00012714 File Offset: 0x00010914
		internal int SetDictionary(byte[] dictionary)
		{
			int start = 0;
			int num = dictionary.Length;
			if (this.mode != InflateManager.InflateManagerMode.DICT0)
			{
				throw new ZLibException("Stream error.");
			}
			if (Adler.Adler32(1U, dictionary, 0, dictionary.Length) != this.m_codec.m_Adler32)
			{
				return -3;
			}
			this.m_codec.m_Adler32 = Adler.Adler32(0U, null, 0, 0);
			if (num >= 1 << this.wbits)
			{
				num = (1 << this.wbits) - 1;
				start = dictionary.Length - num;
			}
			this.blocks.SetDictionary(dictionary, start, num);
			this.mode = InflateManager.InflateManagerMode.BLOCKS;
			return 0;
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x000127A4 File Offset: 0x000109A4
		internal int Sync()
		{
			if (this.mode != InflateManager.InflateManagerMode.BAD)
			{
				this.mode = InflateManager.InflateManagerMode.BAD;
				this.marker = 0;
			}
			int num;
			if ((num = this.m_codec.AvailableBytesIn) == 0)
			{
				return -5;
			}
			int num2 = this.m_codec.NextIn;
			int num3 = this.marker;
			while (num != 0 && num3 < 4)
			{
				if (this.m_codec.InputBuffer[num2] == InflateManager.mark[num3])
				{
					num3++;
				}
				else if (this.m_codec.InputBuffer[num2] != 0)
				{
					num3 = 0;
				}
				else
				{
					num3 = 4 - num3;
				}
				num2++;
				num--;
			}
			this.m_codec.TotalBytesIn += (long)(num2 - this.m_codec.NextIn);
			this.m_codec.NextIn = num2;
			this.m_codec.AvailableBytesIn = num;
			this.marker = num3;
			if (num3 != 4)
			{
				return -3;
			}
			long totalBytesIn = this.m_codec.TotalBytesIn;
			long totalBytesOut = this.m_codec.TotalBytesOut;
			this.Reset();
			this.m_codec.TotalBytesIn = totalBytesIn;
			this.m_codec.TotalBytesOut = totalBytesOut;
			this.mode = InflateManager.InflateManagerMode.BLOCKS;
			return 0;
		}

		// Token: 0x06000619 RID: 1561 RVA: 0x0000843D File Offset: 0x0000663D
		internal int SyncPoint(ZLibCodec z)
		{
			return this.blocks.SyncPoint();
		}

		// Token: 0x040002D3 RID: 723
		private const int PRESET_DICT = 32;

		// Token: 0x040002D4 RID: 724
		private const int Z_DEFLATED = 8;

		// Token: 0x040002D5 RID: 725
		private InflateManager.InflateManagerMode mode;

		// Token: 0x040002D6 RID: 726
		internal ZLibCodec m_codec;

		// Token: 0x040002D7 RID: 727
		internal int method;

		// Token: 0x040002D8 RID: 728
		internal uint computedCheck;

		// Token: 0x040002D9 RID: 729
		internal uint expectedCheck;

		// Token: 0x040002DA RID: 730
		internal int marker;

		// Token: 0x040002DC RID: 732
		internal int wbits;

		// Token: 0x040002DD RID: 733
		internal InflateBlocks blocks;

		// Token: 0x040002DE RID: 734
		private static readonly byte[] mark = new byte[]
		{
			0,
			0,
			byte.MaxValue,
			byte.MaxValue
		};

		// Token: 0x020000D0 RID: 208
		private enum InflateManagerMode
		{
			// Token: 0x040002E0 RID: 736
			METHOD,
			// Token: 0x040002E1 RID: 737
			FLAG,
			// Token: 0x040002E2 RID: 738
			DICT4,
			// Token: 0x040002E3 RID: 739
			DICT3,
			// Token: 0x040002E4 RID: 740
			DICT2,
			// Token: 0x040002E5 RID: 741
			DICT1,
			// Token: 0x040002E6 RID: 742
			DICT0,
			// Token: 0x040002E7 RID: 743
			BLOCKS,
			// Token: 0x040002E8 RID: 744
			CHECK4,
			// Token: 0x040002E9 RID: 745
			CHECK3,
			// Token: 0x040002EA RID: 746
			CHECK2,
			// Token: 0x040002EB RID: 747
			CHECK1,
			// Token: 0x040002EC RID: 748
			DONE,
			// Token: 0x040002ED RID: 749
			BAD
		}
	}
}
