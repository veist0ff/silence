using System;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000CB RID: 203
	internal sealed class InflateBlocks
	{
		// Token: 0x06000602 RID: 1538 RVA: 0x0000FBE8 File Offset: 0x0000DDE8
		internal InflateBlocks(ZLibCodec codec, object checkfn, int w)
		{
			this.m_codec = codec;
			this.hufts = new int[4320];
			this.window = new byte[w];
			this.end = w;
			this.checkfn = checkfn;
			this.mode = InflateBlocks.InflateBlockMode.TYPE;
			this.Reset();
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x0000FC68 File Offset: 0x0000DE68
		internal uint Reset()
		{
			uint result = this.check;
			this.mode = InflateBlocks.InflateBlockMode.TYPE;
			this.bitk = 0;
			this.bitb = 0;
			this.writeAt = 0;
			this.readAt = 0;
			if (this.checkfn != null)
			{
				this.m_codec.m_Adler32 = (this.check = Adler.Adler32(0U, null, 0, 0));
			}
			return result;
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x0000FCC4 File Offset: 0x0000DEC4
		internal int Process(int r)
		{
			int num = this.m_codec.NextIn;
			int num2 = this.m_codec.AvailableBytesIn;
			int num3 = this.bitb;
			int i = this.bitk;
			int num4 = this.writeAt;
			int num5 = (num4 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4);
			int num6;
			for (;;)
			{
				switch (this.mode)
				{
				case InflateBlocks.InflateBlockMode.TYPE:
					while (i < 3)
					{
						if (num2 == 0)
						{
							goto IL_904;
						}
						r = 0;
						num2--;
						num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
						i += 8;
					}
					num6 = (num3 & 7);
					this.last = (num6 & 1);
					switch ((uint)num6 >> 1)
					{
					case 0U:
						num3 >>= 3;
						i -= 3;
						num6 = (i & 7);
						num3 >>= num6;
						i -= num6;
						this.mode = InflateBlocks.InflateBlockMode.LENS;
						continue;
					case 1U:
					{
						int[] array = new int[1];
						int[] array2 = new int[1];
						int[][] array3 = new int[1][];
						int[][] array4 = new int[1][];
						InfTree.inflate_trees_fixed(array, array2, array3, array4, this.m_codec);
						this.codes.Init(array[0], array2[0], array3[0], 0, array4[0], 0);
						num3 >>= 3;
						i -= 3;
						this.mode = InflateBlocks.InflateBlockMode.CODES;
						continue;
					}
					case 2U:
						num3 >>= 3;
						i -= 3;
						this.mode = InflateBlocks.InflateBlockMode.TABLE;
						continue;
					case 3U:
						goto IL_888;
					default:
						continue;
					}
					break;
				case InflateBlocks.InflateBlockMode.LENS:
				{
					while (i < 32)
					{
						if (num2 == 0)
						{
							goto IL_9CF;
						}
						r = 0;
						num2--;
						num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
						i += 8;
					}
					if ((~num3 >> 16 & 65535) != (num3 & 65535))
					{
						goto Block_45;
					}
					this.left = (num3 & 65535);
					int num7 = 0;
					i = 0;
					num3 = num7;
					this.mode = ((this.left != 0) ? InflateBlocks.InflateBlockMode.STORED : ((this.last != 0) ? InflateBlocks.InflateBlockMode.DRY : InflateBlocks.InflateBlockMode.TYPE));
					continue;
				}
				case InflateBlocks.InflateBlockMode.STORED:
					if (num2 == 0)
					{
						goto IL_A26;
					}
					if (num5 == 0)
					{
						if (num4 == this.end && this.readAt != 0)
						{
							num4 = 0;
							num5 = ((0 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4));
						}
						if (num5 == 0)
						{
							this.writeAt = num4;
							r = this.Flush(r);
							num4 = this.writeAt;
							num5 = ((num4 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4));
							if (num4 == this.end && this.readAt != 0)
							{
								num4 = 0;
								num5 = ((0 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4));
							}
							if (num5 == 0)
							{
								goto IL_A7D;
							}
						}
					}
					r = 0;
					num6 = this.left;
					if (num6 > num2)
					{
						num6 = num2;
					}
					if (num6 > num5)
					{
						num6 = num5;
					}
					Array.Copy(this.m_codec.InputBuffer, num, this.window, num4, num6);
					num += num6;
					num2 -= num6;
					num4 += num6;
					num5 -= num6;
					if ((this.left -= num6) == 0)
					{
						this.mode = ((this.last != 0) ? InflateBlocks.InflateBlockMode.DRY : InflateBlocks.InflateBlockMode.TYPE);
						continue;
					}
					continue;
				case InflateBlocks.InflateBlockMode.TABLE:
					while (i < 14)
					{
						if (num2 == 0)
						{
							goto IL_B48;
						}
						r = 0;
						num2--;
						num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
						i += 8;
					}
					num6 = (this.table = (num3 & 16383));
					if ((num6 & 31) <= 29 && (num6 >> 5 & 31) <= 29)
					{
						num6 = 258 + (num6 & 31) + (num6 >> 5 & 31);
						if (this.blens != null && this.blens.Length >= num6)
						{
							Array.Clear(this.blens, 0, num6);
						}
						else
						{
							this.blens = new int[num6];
						}
						num3 >>= 14;
						i -= 14;
						this.index = 0;
						this.mode = InflateBlocks.InflateBlockMode.BTREE;
						goto IL_3F9;
					}
					goto IL_AD4;
				case InflateBlocks.InflateBlockMode.BTREE:
					goto IL_3F9;
				case InflateBlocks.InflateBlockMode.DTREE:
					goto IL_2C9;
				case InflateBlocks.InflateBlockMode.CODES:
					goto IL_56;
				case InflateBlocks.InflateBlockMode.DRY:
					goto IL_E0A;
				case InflateBlocks.InflateBlockMode.DONE:
					goto IL_EB1;
				case InflateBlocks.InflateBlockMode.BAD:
					goto IL_F0B;
				}
				goto Block_51;
            IL_2C9:
                for (;;)
				{
					num6 = this.table;
					if (this.index >= 258 + (num6 & 31) + (num6 >> 5 & 31))
					{
						break;
					}
					num6 = this.bb[0];
					while (i < num6)
					{
						if (num2 == 0)
						{
							goto IL_D36;
						}
						r = 0;
						num2--;
						num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
						i += 8;
					}
					num6 = this.hufts[(this.tb[0] + (num3 & InternalInflateConstants.InflateMask[num6])) * 3 + 1];
					int num8 = this.hufts[(this.tb[0] + (num3 & InternalInflateConstants.InflateMask[num6])) * 3 + 2];
					if (num8 < 16)
					{
						num3 >>= num6;
						i -= num6;
						int[] array5 = this.blens;
						int num9 = this.index;
						this.index = num9 + 1;
						array5[num9] = num8;
					}
					else
					{
						int num10 = (num8 == 18) ? 7 : (num8 - 14);
						int num11 = (num8 == 18) ? 11 : 3;
						while (i < num6 + num10)
						{
							if (num2 == 0)
							{
								goto IL_CDF;
							}
							r = 0;
							num2--;
							num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
							i += 8;
						}
						num3 >>= num6;
						i -= num6;
						num11 += (num3 & InternalInflateConstants.InflateMask[num10]);
						num3 >>= num10;
						i -= num10;
						num10 = this.index;
						num6 = this.table;
						if (num10 + num11 > 258 + (num6 & 31) + (num6 >> 5 & 31) || (num8 == 16 && num10 < 1))
						{
							goto IL_C64;
						}
						num8 = ((num8 == 16) ? this.blens[num10 - 1] : 0);
						do
						{
							this.blens[num10++] = num8;
						}
						while (--num11 != 0);
						this.index = num10;
					}
				}
				this.tb[0] = -1;
				int[] array6 = new int[]
				{
					9
				};
				int[] array7 = new int[]
				{
					6
				};
				int[] array8 = new int[1];
				int[] array9 = new int[1];
				num6 = this.table;
				num6 = this.inftree.inflate_trees_dynamic(257 + (num6 & 31), 1 + (num6 >> 5 & 31), this.blens, array6, array7, array8, array9, this.hufts, this.m_codec);
				if (num6 == 0)
				{
					this.codes.Init(array6[0], array7[0], this.hufts, array8[0], this.hufts, array9[0]);
					this.mode = InflateBlocks.InflateBlockMode.CODES;
					goto IL_56;
				}
				goto IL_D8D;
				IL_3F9:
				while (this.index < 4 + (this.table >> 10))
				{
					while (i < 3)
					{
						if (num2 == 0)
						{
							goto IL_C0D;
						}
						r = 0;
						num2--;
						num3 |= (int)(this.m_codec.InputBuffer[num++] & byte.MaxValue) << i;
						i += 8;
					}
					int[] array10 = this.blens;
					int[] array11 = InflateBlocks.border;
					int num9 = this.index;
					this.index = num9 + 1;
					array10[array11[num9]] = (num3 & 7);
					num3 >>= 3;
					i -= 3;
				}
				while (this.index < 19)
				{
					int[] array12 = this.blens;
					int[] array13 = InflateBlocks.border;
					int num9 = this.index;
					this.index = num9 + 1;
					array12[array13[num9]] = 0;
				}
				this.bb[0] = 7;
				num6 = this.inftree.inflate_trees_bits(this.blens, this.bb, this.tb, this.hufts, this.m_codec);
				if (num6 == 0)
				{
					this.index = 0;
					this.mode = InflateBlocks.InflateBlockMode.DTREE;
					goto IL_2C9;
				}
				goto IL_B9F;
				IL_56:
				this.bitb = num3;
				this.bitk = i;
				this.m_codec.AvailableBytesIn = num2;
				this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
				this.m_codec.NextIn = num;
				this.writeAt = num4;
				r = this.codes.Process(this, r);
				if (r != 1)
				{
					break;
				}
				r = 0;
				num = this.m_codec.NextIn;
				num2 = this.m_codec.AvailableBytesIn;
				num3 = this.bitb;
				i = this.bitk;
				num4 = this.writeAt;
				num5 = ((num4 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4));
				if (this.last != 0)
				{
					goto IL_E03;
				}
				this.mode = InflateBlocks.InflateBlockMode.TYPE;
			}
			return this.Flush(r);
			Block_45:
			this.mode = InflateBlocks.InflateBlockMode.BAD;
			this.m_codec.Message = "invalid stored block lengths";
			r = -3;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-3);
			Block_51:
			r = -2;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-2);
			IL_888:
			num3 >>= 3;
			i -= 3;
			this.mode = InflateBlocks.InflateBlockMode.BAD;
			this.m_codec.Message = "invalid block type";
			r = -3;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-3);
			IL_904:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_9CF:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_A26:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_A7D:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_AD4:
			this.mode = InflateBlocks.InflateBlockMode.BAD;
			this.m_codec.Message = "too many length or distance symbols";
			r = -3;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-3);
			IL_B48:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_B9F:
			r = num6;
			if (r == -3)
			{
				this.blens = null;
				this.mode = InflateBlocks.InflateBlockMode.BAD;
			}
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_C0D:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_C64:
			this.blens = null;
			this.mode = InflateBlocks.InflateBlockMode.BAD;
			this.m_codec.Message = "invalid bit length repeat";
			r = -3;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-3);
			IL_CDF:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_D36:
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_D8D:
			if (num6 == -3)
			{
				this.blens = null;
				this.mode = InflateBlocks.InflateBlockMode.BAD;
			}
			r = num6;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(r);
			IL_E03:
			this.mode = InflateBlocks.InflateBlockMode.DRY;
			IL_E0A:
			this.writeAt = num4;
			r = this.Flush(r);
			num4 = this.writeAt;
			int num12 = (num4 < this.readAt) ? (this.readAt - num4 - 1) : (this.end - num4);
			if (this.readAt != this.writeAt)
			{
				this.bitb = num3;
				this.bitk = i;
				this.m_codec.AvailableBytesIn = num2;
				this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
				this.m_codec.NextIn = num;
				this.writeAt = num4;
				return this.Flush(r);
			}
			this.mode = InflateBlocks.InflateBlockMode.DONE;
			IL_EB1:
			r = 1;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(1);
			IL_F0B:
			r = -3;
			this.bitb = num3;
			this.bitk = i;
			this.m_codec.AvailableBytesIn = num2;
			this.m_codec.TotalBytesIn += (long)(num - this.m_codec.NextIn);
			this.m_codec.NextIn = num;
			this.writeAt = num4;
			return this.Flush(-3);
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x00008354 File Offset: 0x00006554
		internal void Free()
		{
			this.Reset();
			this.window = null;
			this.hufts = null;
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00010C38 File Offset: 0x0000EE38
		internal void SetDictionary(byte[] d, int start, int n)
		{
			Array.Copy(d, start, this.window, 0, n);
			this.writeAt = n;
			this.readAt = n;
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x0000836B File Offset: 0x0000656B
		internal int SyncPoint()
		{
			if (this.mode != InflateBlocks.InflateBlockMode.LENS)
			{
				return 0;
			}
			return 1;
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x00010C64 File Offset: 0x0000EE64
		internal int Flush(int r)
		{
			for (int i = 0; i < 2; i++)
			{
				int num;
				if (i == 0)
				{
					num = ((this.readAt <= this.writeAt) ? this.writeAt : this.end) - this.readAt;
				}
				else
				{
					num = this.writeAt - this.readAt;
				}
				if (num == 0)
				{
					if (r == -5)
					{
						r = 0;
					}
					return r;
				}
				if (num > this.m_codec.AvailableBytesOut)
				{
					num = this.m_codec.AvailableBytesOut;
				}
				if (num != 0 && r == -5)
				{
					r = 0;
				}
				this.m_codec.AvailableBytesOut -= num;
				this.m_codec.TotalBytesOut += (long)num;
				if (this.checkfn != null)
				{
					this.m_codec.m_Adler32 = (this.check = Adler.Adler32(this.check, this.window, this.readAt, num));
				}
				Array.Copy(this.window, this.readAt, this.m_codec.OutputBuffer, this.m_codec.NextOut, num);
				this.m_codec.NextOut += num;
				this.readAt += num;
				if (this.readAt == this.end && i == 0)
				{
					this.readAt = 0;
					if (this.writeAt == this.end)
					{
						this.writeAt = 0;
					}
				}
				else
				{
					i++;
				}
			}
			return r;
		}

		// Token: 0x04000299 RID: 665
		private const int MANY = 1440;

		// Token: 0x0400029A RID: 666
		internal static readonly int[] border = new int[]
		{
			16,
			17,
			18,
			0,
			8,
			7,
			9,
			6,
			10,
			5,
			11,
			4,
			12,
			3,
			13,
			2,
			14,
			1,
			15
		};

		// Token: 0x0400029B RID: 667
		private InflateBlocks.InflateBlockMode mode;

		// Token: 0x0400029C RID: 668
		internal int left;

		// Token: 0x0400029D RID: 669
		internal int table;

		// Token: 0x0400029E RID: 670
		internal int index;

		// Token: 0x0400029F RID: 671
		internal int[] blens;

		// Token: 0x040002A0 RID: 672
		internal int[] bb = new int[1];

		// Token: 0x040002A1 RID: 673
		internal int[] tb = new int[1];

		// Token: 0x040002A2 RID: 674
		internal InflateCodes codes = new InflateCodes();

		// Token: 0x040002A3 RID: 675
		internal int last;

		// Token: 0x040002A4 RID: 676
		internal ZLibCodec m_codec;

		// Token: 0x040002A5 RID: 677
		internal int bitk;

		// Token: 0x040002A6 RID: 678
		internal int bitb;

		// Token: 0x040002A7 RID: 679
		internal int[] hufts;

		// Token: 0x040002A8 RID: 680
		internal byte[] window;

		// Token: 0x040002A9 RID: 681
		internal int end;

		// Token: 0x040002AA RID: 682
		internal int readAt;

		// Token: 0x040002AB RID: 683
		internal int writeAt;

		// Token: 0x040002AC RID: 684
		internal object checkfn;

		// Token: 0x040002AD RID: 685
		internal uint check;

		// Token: 0x040002AE RID: 686
		internal InfTree inftree = new InfTree();

		// Token: 0x020000CC RID: 204
		private enum InflateBlockMode
		{
			// Token: 0x040002B0 RID: 688
			TYPE,
			// Token: 0x040002B1 RID: 689
			LENS,
			// Token: 0x040002B2 RID: 690
			STORED,
			// Token: 0x040002B3 RID: 691
			TABLE,
			// Token: 0x040002B4 RID: 692
			BTREE,
			// Token: 0x040002B5 RID: 693
			DTREE,
			// Token: 0x040002B6 RID: 694
			CODES,
			// Token: 0x040002B7 RID: 695
			DRY,
			// Token: 0x040002B8 RID: 696
			DONE,
			// Token: 0x040002B9 RID: 697
			BAD
		}
	}
}
