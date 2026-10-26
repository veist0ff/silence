using System;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000C6 RID: 198
	internal sealed class DeflateManager
	{
		// Token: 0x06000594 RID: 1428 RVA: 0x0000CF7C File Offset: 0x0000B17C
		internal DeflateManager()
		{
			this.dyn_ltree = new short[DeflateManager.HEAP_SIZE * 2];
			this.dyn_dtree = new short[(2 * InternalConstants.D_CODES + 1) * 2];
			this.bl_tree = new short[(2 * InternalConstants.BL_CODES + 1) * 2];
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x0000D030 File Offset: 0x0000B230
		private void m_InitializeLazyMatch()
		{
			this.window_size = 2 * this.w_size;
			Array.Clear(this.head, 0, this.hash_size);
			this.config = DeflateManager.Config.Lookup(this.compressionLevel);
			this.SetDeflater();
			this.strstart = 0;
			this.block_start = 0;
			this.lookahead = 0;
			this.match_length = (this.prev_length = DeflateManager.MIN_MATCH - 1);
			this.match_available = 0;
			this.ins_h = 0;
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x0000D0B0 File Offset: 0x0000B2B0
		private void m_InitializeTreeData()
		{
			this.treeLiterals.dyn_tree = this.dyn_ltree;
			this.treeLiterals.staticTree = StaticTree.Literals;
			this.treeDistances.dyn_tree = this.dyn_dtree;
			this.treeDistances.staticTree = StaticTree.Distances;
			this.treeBitLengths.dyn_tree = this.bl_tree;
			this.treeBitLengths.staticTree = StaticTree.BitLengths;
			this.bi_buf = 0;
			this.bi_valid = 0;
			this.last_eob_len = 8;
			this.m_InitializeBlocks();
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x0000D13C File Offset: 0x0000B33C
		internal void m_InitializeBlocks()
		{
			for (int i = 0; i < InternalConstants.L_CODES; i++)
			{
				this.dyn_ltree[i * 2] = 0;
			}
			for (int j = 0; j < InternalConstants.D_CODES; j++)
			{
				this.dyn_dtree[j * 2] = 0;
			}
			for (int k = 0; k < InternalConstants.BL_CODES; k++)
			{
				this.bl_tree[k * 2] = 0;
			}
			this.dyn_ltree[DeflateManager.END_BLOCK * 2] = 1;
			this.static_len = 0;
			this.opt_len = 0;
			this.matches = 0;
			this.last_lit = 0;
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x0000D1C8 File Offset: 0x0000B3C8
		internal void pqdownheap(short[] tree, int k)
		{
			int num = this.heap[k];
			for (int i = k << 1; i <= this.heap_len; i <<= 1)
			{
				if (i < this.heap_len && DeflateManager.m_IsSmaller(tree, this.heap[i + 1], this.heap[i], this.depth))
				{
					i++;
				}
				if (DeflateManager.m_IsSmaller(tree, num, this.heap[i], this.depth))
				{
					break;
				}
				this.heap[k] = this.heap[i];
				k = i;
			}
			this.heap[k] = num;
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x0000D254 File Offset: 0x0000B454
		internal static bool m_IsSmaller(short[] tree, int n, int m, sbyte[] depth)
		{
			short num = tree[n * 2];
			short num2 = tree[m * 2];
			return num < num2 || (num == num2 && depth[n] <= depth[m]);
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x0000D284 File Offset: 0x0000B484
		internal void scan_tree(short[] tree, int max_code)
		{
			int num = -1;
			int num2 = (int)tree[1];
			int num3 = 0;
			int num4 = 7;
			int num5 = 4;
			if (num2 == 0)
			{
				num4 = 138;
				num5 = 3;
			}
			tree[(max_code + 1) * 2 + 1] = short.MaxValue;
			for (int i = 0; i <= max_code; i++)
			{
				int num6 = num2;
				num2 = (int)tree[(i + 1) * 2 + 1];
				if (++num3 >= num4 || num6 != num2)
				{
					if (num3 < num5)
					{
						this.bl_tree[num6 * 2] = (short)((int)this.bl_tree[num6 * 2] + num3);
					}
					else if (num6 != 0)
					{
						if (num6 != num)
						{
							short[] array = this.bl_tree;
							int num7 = num6 * 2;
							array[num7] += 1;
						}
						short[] array2 = this.bl_tree;
						int num8 = InternalConstants.REP_3_6 * 2;
						array2[num8] += 1;
					}
					else if (num3 <= 10)
					{
						short[] array3 = this.bl_tree;
						int num9 = InternalConstants.REPZ_3_10 * 2;
						array3[num9] += 1;
					}
					else
					{
						short[] array4 = this.bl_tree;
						int num10 = InternalConstants.REPZ_11_138 * 2;
						array4[num10] += 1;
					}
					num3 = 0;
					num = num6;
					if (num2 == 0)
					{
						num4 = 138;
						num5 = 3;
					}
					else if (num6 == num2)
					{
						num4 = 6;
						num5 = 3;
					}
					else
					{
						num4 = 7;
						num5 = 4;
					}
				}
			}
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x0000D3A0 File Offset: 0x0000B5A0
		internal int build_bl_tree()
		{
			this.scan_tree(this.dyn_ltree, this.treeLiterals.max_code);
			this.scan_tree(this.dyn_dtree, this.treeDistances.max_code);
			this.treeBitLengths.build_tree(this);
			int num = InternalConstants.BL_CODES - 1;
			while (num >= 3 && this.bl_tree[(int)(Tree.bl_order[num] * 2 + 1)] == 0)
			{
				num--;
			}
			this.opt_len += 3 * (num + 1) + 5 + 5 + 4;
			return num;
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x0000D428 File Offset: 0x0000B628
		internal void send_all_trees(int lcodes, int dcodes, int blcodes)
		{
			this.send_bits(lcodes - 257, 5);
			this.send_bits(dcodes - 1, 5);
			this.send_bits(blcodes - 4, 4);
			for (int i = 0; i < blcodes; i++)
			{
				this.send_bits((int)this.bl_tree[(int)(Tree.bl_order[i] * 2 + 1)], 3);
			}
			this.send_tree(this.dyn_ltree, lcodes - 1);
			this.send_tree(this.dyn_dtree, dcodes - 1);
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x0000D49C File Offset: 0x0000B69C
		internal void send_tree(short[] tree, int max_code)
		{
			int num = -1;
			int num2 = (int)tree[1];
			int num3 = 0;
			int num4 = 7;
			int num5 = 4;
			if (num2 == 0)
			{
				num4 = 138;
				num5 = 3;
			}
			for (int i = 0; i <= max_code; i++)
			{
				int num6 = num2;
				num2 = (int)tree[(i + 1) * 2 + 1];
				if (++num3 >= num4 || num6 != num2)
				{
					if (num3 < num5)
					{
						do
						{
							this.send_code(num6, this.bl_tree);
						}
						while (--num3 != 0);
					}
					else if (num6 != 0)
					{
						if (num6 != num)
						{
							this.send_code(num6, this.bl_tree);
							num3--;
						}
						this.send_code(InternalConstants.REP_3_6, this.bl_tree);
						this.send_bits(num3 - 3, 2);
					}
					else if (num3 <= 10)
					{
						this.send_code(InternalConstants.REPZ_3_10, this.bl_tree);
						this.send_bits(num3 - 3, 3);
					}
					else
					{
						this.send_code(InternalConstants.REPZ_11_138, this.bl_tree);
						this.send_bits(num3 - 11, 7);
					}
					num3 = 0;
					num = num6;
					if (num2 == 0)
					{
						num4 = 138;
						num5 = 3;
					}
					else if (num6 == num2)
					{
						num4 = 6;
						num5 = 3;
					}
					else
					{
						num4 = 7;
						num5 = 4;
					}
				}
			}
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00007F61 File Offset: 0x00006161
		private void put_bytes(byte[] p, int start, int len)
		{
			Array.Copy(p, start, this.pending, this.pendingCount, len);
			this.pendingCount += len;
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x0000D5B4 File Offset: 0x0000B7B4
		internal void send_code(int c, short[] tree)
		{
			int num = c * 2;
			this.send_bits((int)tree[num] & 65535, (int)tree[num + 1] & 65535);
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x0000D5E0 File Offset: 0x0000B7E0
		internal void send_bits(int value, int length)
		{
			if (this.bi_valid > DeflateManager.Buf_size - length)
			{
				this.bi_buf |= (short)(value << this.bi_valid & 65535);
				byte[] array = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array[num] = (byte)this.bi_buf;
				byte[] array2 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array2[num] = (byte)(this.bi_buf >> 8);
				this.bi_buf = (short)((uint)value >> DeflateManager.Buf_size - this.bi_valid);
				this.bi_valid += length - DeflateManager.Buf_size;
				return;
			}
			this.bi_buf |= (short)(value << this.bi_valid & 65535);
			this.bi_valid += length;
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x0000D6BC File Offset: 0x0000B8BC
		internal void m_tr_align()
		{
			this.send_bits(DeflateManager.STATIC_TREES << 1, 3);
			this.send_code(DeflateManager.END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
			this.bi_flush();
			if (1 + this.last_eob_len + 10 - this.bi_valid < 9)
			{
				this.send_bits(DeflateManager.STATIC_TREES << 1, 3);
				this.send_code(DeflateManager.END_BLOCK, StaticTree.lengthAndLiteralsTreeCodes);
				this.bi_flush();
			}
			this.last_eob_len = 7;
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x0000D730 File Offset: 0x0000B930
		internal bool m_tr_tally(int dist, int lc)
		{
			this.pending[this.m_distanceOffset + this.last_lit * 2] = (byte)((uint)dist >> 8);
			this.pending[this.m_distanceOffset + this.last_lit * 2 + 1] = (byte)dist;
			this.pending[this.m_lengthOffset + this.last_lit] = (byte)lc;
			this.last_lit++;
			if (dist == 0)
			{
				short[] array = this.dyn_ltree;
				int num = lc * 2;
				array[num] += 1;
			}
			else
			{
				this.matches++;
				dist--;
				short[] array2 = this.dyn_ltree;
				int num2 = ((int)Tree.LengthCode[lc] + InternalConstants.LITERALS + 1) * 2;
				array2[num2] += 1;
				short[] array3 = this.dyn_dtree;
				int num3 = Tree.DistanceCode(dist) * 2;
				array3[num3] += 1;
			}
			if ((this.last_lit & 8191) == 0 && this.compressionLevel > CompressionLevel.Level2)
			{
				int num4 = this.last_lit << 3;
				int num5 = this.strstart - this.block_start;
				for (int i = 0; i < InternalConstants.D_CODES; i++)
				{
					num4 = (int)((long)num4 + (long)this.dyn_dtree[i * 2] * (5L + (long)Tree.ExtraDistanceBits[i]));
				}
				num4 >>= 3;
				if (this.matches < this.last_lit / 2 && num4 < num5 / 2)
				{
					return true;
				}
			}
			return this.last_lit == this.lit_bufsize - 1 || this.last_lit == this.lit_bufsize;
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x0000D89C File Offset: 0x0000BA9C
		internal void send_compressed_block(short[] ltree, short[] dtree)
		{
			int num = 0;
			if (this.last_lit != 0)
			{
				do
				{
					int num2 = this.m_distanceOffset + num * 2;
					int num3 = ((int)this.pending[num2] << 8 & 65280) | (int)(this.pending[num2 + 1] & byte.MaxValue);
					int num4 = (int)(this.pending[this.m_lengthOffset + num] & byte.MaxValue);
					num++;
					if (num3 == 0)
					{
						this.send_code(num4, ltree);
					}
					else
					{
						int num5 = (int)Tree.LengthCode[num4];
						this.send_code(num5 + InternalConstants.LITERALS + 1, ltree);
						int num6 = Tree.ExtraLengthBits[num5];
						if (num6 != 0)
						{
							num4 -= Tree.LengthBase[num5];
							this.send_bits(num4, num6);
						}
						num3--;
						num5 = Tree.DistanceCode(num3);
						this.send_code(num5, dtree);
						num6 = Tree.ExtraDistanceBits[num5];
						if (num6 != 0)
						{
							num3 -= Tree.DistanceBase[num5];
							this.send_bits(num3, num6);
						}
					}
				}
				while (num < this.last_lit);
			}
			this.send_code(DeflateManager.END_BLOCK, ltree);
			this.last_eob_len = (int)ltree[DeflateManager.END_BLOCK * 2 + 1];
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x0000D9AC File Offset: 0x0000BBAC
		internal void set_data_type()
		{
			int i = 0;
			int num = 0;
			int num2 = 0;
			while (i < 7)
			{
				num2 += (int)this.dyn_ltree[i * 2];
				i++;
			}
			while (i < 128)
			{
				num += (int)this.dyn_ltree[i * 2];
				i++;
			}
			while (i < InternalConstants.LITERALS)
			{
				num2 += (int)this.dyn_ltree[i * 2];
				i++;
			}
			this.data_type = (sbyte)((num2 > num >> 2) ? DeflateManager.Z_BINARY : DeflateManager.Z_ASCII);
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x0000DA28 File Offset: 0x0000BC28
		internal void bi_flush()
		{
			if (this.bi_valid == 16)
			{
				byte[] array = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array[num] = (byte)this.bi_buf;
				byte[] array2 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array2[num] = (byte)(this.bi_buf >> 8);
				this.bi_buf = 0;
				this.bi_valid = 0;
				return;
			}
			if (this.bi_valid >= 8)
			{
				byte[] array3 = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array3[num] = (byte)this.bi_buf;
				this.bi_buf = (short)(this.bi_buf >> 8);
				this.bi_valid -= 8;
			}
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x0000DAD4 File Offset: 0x0000BCD4
		internal void bi_windup()
		{
			if (this.bi_valid > 8)
			{
				byte[] array = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array[num] = (byte)this.bi_buf;
				byte[] array2 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array2[num] = (byte)(this.bi_buf >> 8);
			}
			else if (this.bi_valid > 0)
			{
				byte[] array3 = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array3[num] = (byte)this.bi_buf;
			}
			this.bi_buf = 0;
			this.bi_valid = 0;
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x0000DB64 File Offset: 0x0000BD64
		internal void copy_block(int buf, int len, bool header)
		{
			this.bi_windup();
			this.last_eob_len = 8;
			if (header)
			{
				byte[] array = this.pending;
				int num = this.pendingCount;
				this.pendingCount = num + 1;
				array[num] = (byte)len;
				byte[] array2 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array2[num] = (byte)(len >> 8);
				byte[] array3 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array3[num] = (byte)(~(byte)len);
				byte[] array4 = this.pending;
				num = this.pendingCount;
				this.pendingCount = num + 1;
				array4[num] = (byte)(~len >> 8);
			}
			this.put_bytes(this.window, buf, len);
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00007F85 File Offset: 0x00006185
		internal void flush_block_only(bool eof)
		{
			this.m_tr_flush_block((this.block_start >= 0) ? this.block_start : -1, this.strstart - this.block_start, eof);
			this.block_start = this.strstart;
			this.m_codec.flush_pending();
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x0000DC00 File Offset: 0x0000BE00
		internal BlockState DeflateNone(FlushType flush)
		{
			int num = 65535;
			if (65535 > this.pending.Length - 5)
			{
				num = this.pending.Length - 5;
			}
			for (;;)
			{
				if (this.lookahead <= 1)
				{
					this.m_fillWindow();
					if (this.lookahead == 0 && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (this.lookahead == 0)
					{
						goto IL_E9;
					}
				}
				this.strstart += this.lookahead;
				this.lookahead = 0;
				int num2 = this.block_start + num;
				if (this.strstart == 0 || this.strstart >= num2)
				{
					this.lookahead = this.strstart - num2;
					this.strstart = num2;
					this.flush_block_only(false);
					if (this.m_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
				if (this.strstart - this.block_start >= this.w_size - DeflateManager.MIN_LOOKAHEAD)
				{
					this.flush_block_only(false);
					if (this.m_codec.AvailableBytesOut == 0)
					{
						break;
					}
				}
			}
			return BlockState.NeedMore;
			IL_E9:
			this.flush_block_only(flush == FlushType.Finish);
			if (this.m_codec.AvailableBytesOut == 0)
			{
				if (flush != FlushType.Finish)
				{
					return BlockState.NeedMore;
				}
				return BlockState.FinishStarted;
			}
			else
			{
				if (flush != FlushType.Finish)
				{
					return BlockState.BlockDone;
				}
				return BlockState.FinishDone;
			}
			return BlockState.NeedMore;
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x00007FC4 File Offset: 0x000061C4
		internal void m_tr_stored_block(int buf, int stored_len, bool eof)
		{
			this.send_bits((DeflateManager.STORED_BLOCK << 1) + (eof ? 1 : 0), 3);
			this.copy_block(buf, stored_len, true);
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x0000DD20 File Offset: 0x0000BF20
		internal void m_tr_flush_block(int buf, int stored_len, bool eof)
		{
			int num = 0;
			int num2;
			int num3;
			if (this.compressionLevel > CompressionLevel.None)
			{
				if ((int)this.data_type == DeflateManager.Z_UNKNOWN)
				{
					this.set_data_type();
				}
				this.treeLiterals.build_tree(this);
				this.treeDistances.build_tree(this);
				num = this.build_bl_tree();
				num2 = this.opt_len + 3 + 7 >> 3;
				num3 = this.static_len + 3 + 7 >> 3;
				if (num3 <= num2)
				{
					num2 = num3;
				}
			}
			else
			{
				num3 = (num2 = stored_len + 5);
			}
			if (stored_len + 4 <= num2 && buf != -1)
			{
				this.m_tr_stored_block(buf, stored_len, eof);
			}
			else if (num3 == num2)
			{
				this.send_bits((DeflateManager.STATIC_TREES << 1) + (eof ? 1 : 0), 3);
				this.send_compressed_block(StaticTree.lengthAndLiteralsTreeCodes, StaticTree.distTreeCodes);
			}
			else
			{
				this.send_bits((DeflateManager.DYN_TREES << 1) + (eof ? 1 : 0), 3);
				this.send_all_trees(this.treeLiterals.max_code + 1, this.treeDistances.max_code + 1, num + 1);
				this.send_compressed_block(this.dyn_ltree, this.dyn_dtree);
			}
			this.m_InitializeBlocks();
			if (eof)
			{
				this.bi_windup();
			}
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x0000DE30 File Offset: 0x0000C030
		private void m_fillWindow()
		{
			do
			{
				int num = this.window_size - this.lookahead - this.strstart;
				int num2;
				if (num == 0 && this.strstart == 0 && this.lookahead == 0)
				{
					num = this.w_size;
				}
				else if (num == -1)
				{
					num--;
				}
				else if (this.strstart >= this.w_size + this.w_size - DeflateManager.MIN_LOOKAHEAD)
				{
					Array.Copy(this.window, this.w_size, this.window, 0, this.w_size);
					this.match_start -= this.w_size;
					this.strstart -= this.w_size;
					this.block_start -= this.w_size;
					num2 = this.hash_size;
					int num3 = num2;
					do
					{
						int num4 = (int)this.head[--num3] & 65535;
						this.head[num3] = (short)((num4 < this.w_size) ? 0 : (num4 - this.w_size));
					}
					while (--num2 != 0);
					num2 = this.w_size;
					num3 = num2;
					do
					{
						int num4 = (int)this.prev[--num3] & 65535;
						this.prev[num3] = (short)((num4 < this.w_size) ? 0 : (num4 - this.w_size));
					}
					while (--num2 != 0);
					num += this.w_size;
				}
				if (this.m_codec.AvailableBytesIn == 0)
				{
					return;
				}
				num2 = this.m_codec.read_buf(this.window, this.strstart + this.lookahead, num);
				this.lookahead += num2;
				if (this.lookahead >= DeflateManager.MIN_MATCH)
				{
					this.ins_h = (int)(this.window[this.strstart] & byte.MaxValue);
					this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + 1] & byte.MaxValue)) & this.hash_mask);
				}
				if (this.lookahead >= DeflateManager.MIN_LOOKAHEAD)
				{
					break;
				}
			}
			while (this.m_codec.AvailableBytesIn != 0);
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x0000E040 File Offset: 0x0000C240
		internal BlockState DeflateFast(FlushType flush)
		{
			int num = 0;
			for (;;)
			{
				if (this.lookahead < DeflateManager.MIN_LOOKAHEAD)
				{
					this.m_fillWindow();
					if (this.lookahead < DeflateManager.MIN_LOOKAHEAD && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (this.lookahead == 0)
					{
						goto IL_2EE;
					}
				}
				if (this.lookahead >= DeflateManager.MIN_MATCH)
				{
					this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & byte.MaxValue)) & this.hash_mask);
					num = ((int)this.head[this.ins_h] & 65535);
					this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
					this.head[this.ins_h] = (short)this.strstart;
				}
				if ((long)num != 0L && (this.strstart - num & 65535) <= this.w_size - DeflateManager.MIN_LOOKAHEAD && this.compressionStrategy != CompressionStrategy.HuffmanOnly)
				{
					this.match_length = this.longest_match(num);
				}
				bool flag;
				if (this.match_length >= DeflateManager.MIN_MATCH)
				{
					flag = this.m_tr_tally(this.strstart - this.match_start, this.match_length - DeflateManager.MIN_MATCH);
					this.lookahead -= this.match_length;
					if (this.match_length <= this.config.MaxLazy && this.lookahead >= DeflateManager.MIN_MATCH)
					{
						this.match_length--;
						int num2;
						do
						{
							this.strstart++;
							this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & byte.MaxValue)) & this.hash_mask);
							num = ((int)this.head[this.ins_h] & 65535);
							this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
							this.head[this.ins_h] = (short)this.strstart;
							num2 = this.match_length - 1;
							this.match_length = num2;
						}
						while (num2 != 0);
						this.strstart++;
					}
					else
					{
						this.strstart += this.match_length;
						this.match_length = 0;
						this.ins_h = (int)(this.window[this.strstart] & byte.MaxValue);
						this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + 1] & byte.MaxValue)) & this.hash_mask);
					}
				}
				else
				{
					flag = this.m_tr_tally(0, (int)(this.window[this.strstart] & byte.MaxValue));
					this.lookahead--;
					this.strstart++;
				}
				if (flag)
				{
					this.flush_block_only(false);
					if (this.m_codec.AvailableBytesOut == 0)
					{
						break;
					}
				}
			}
			return BlockState.NeedMore;
			IL_2EE:
			this.flush_block_only(flush == FlushType.Finish);
			if (this.m_codec.AvailableBytesOut == 0)
			{
				if (flush == FlushType.Finish)
				{
					return BlockState.FinishStarted;
				}
				return BlockState.NeedMore;
			}
			else
			{
				if (flush != FlushType.Finish)
				{
					return BlockState.BlockDone;
				}
				return BlockState.FinishDone;
			}
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x0000E364 File Offset: 0x0000C564
		internal BlockState DeflateSlow(FlushType flush)
		{
			int num = 0;
			for (;;)
			{
				if (this.lookahead < DeflateManager.MIN_LOOKAHEAD)
				{
					this.m_fillWindow();
					if (this.lookahead < DeflateManager.MIN_LOOKAHEAD && flush == FlushType.None)
					{
						return BlockState.NeedMore;
					}
					if (this.lookahead == 0)
					{
						goto IL_383;
					}
				}
				if (this.lookahead >= DeflateManager.MIN_MATCH)
				{
					this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & byte.MaxValue)) & this.hash_mask);
					num = ((int)this.head[this.ins_h] & 65535);
					this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
					this.head[this.ins_h] = (short)this.strstart;
				}
				this.prev_length = this.match_length;
				this.prev_match = this.match_start;
				this.match_length = DeflateManager.MIN_MATCH - 1;
				if (num != 0 && this.prev_length < this.config.MaxLazy && (this.strstart - num & 65535) <= this.w_size - DeflateManager.MIN_LOOKAHEAD)
				{
					if (this.compressionStrategy != CompressionStrategy.HuffmanOnly)
					{
						this.match_length = this.longest_match(num);
					}
					if (this.match_length <= 5 && (this.compressionStrategy == CompressionStrategy.Filtered || (this.match_length == DeflateManager.MIN_MATCH && this.strstart - this.match_start > 4096)))
					{
						this.match_length = DeflateManager.MIN_MATCH - 1;
					}
				}
				if (this.prev_length >= DeflateManager.MIN_MATCH && this.match_length <= this.prev_length)
				{
					int num2 = this.strstart + this.lookahead - DeflateManager.MIN_MATCH;
					bool flag = this.m_tr_tally(this.strstart - 1 - this.prev_match, this.prev_length - DeflateManager.MIN_MATCH);
					this.lookahead -= this.prev_length - 1;
					this.prev_length -= 2;
					int num3;
					do
					{
						num3 = this.strstart + 1;
						this.strstart = num3;
						if (num3 <= num2)
						{
							this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[this.strstart + (DeflateManager.MIN_MATCH - 1)] & byte.MaxValue)) & this.hash_mask);
							num = ((int)this.head[this.ins_h] & 65535);
							this.prev[this.strstart & this.w_mask] = this.head[this.ins_h];
							this.head[this.ins_h] = (short)this.strstart;
						}
						num3 = this.prev_length - 1;
						this.prev_length = num3;
					}
					while (num3 != 0);
					this.match_available = 0;
					this.match_length = DeflateManager.MIN_MATCH - 1;
					this.strstart++;
					if (flag)
					{
						this.flush_block_only(false);
						if (this.m_codec.AvailableBytesOut == 0)
						{
							break;
						}
					}
				}
				else if (this.match_available != 0)
				{
					if (this.m_tr_tally(0, (int)(this.window[this.strstart - 1] & 255)))
					{
						this.flush_block_only(false);
					}
					this.strstart++;
					this.lookahead--;
					if (this.m_codec.AvailableBytesOut == 0)
					{
						return BlockState.NeedMore;
					}
				}
				else
				{
					this.match_available = 1;
					this.strstart++;
					this.lookahead--;
				}
			}
			return BlockState.NeedMore;
			IL_383:
			if (this.match_available != 0)
			{
				bool flag = this.m_tr_tally(0, (int)(this.window[this.strstart - 1] & byte.MaxValue));
				this.match_available = 0;
			}
			this.flush_block_only(flush == FlushType.Finish);
			if (this.m_codec.AvailableBytesOut == 0)
			{
				if (flush == FlushType.Finish)
				{
					return BlockState.FinishStarted;
				}
				return BlockState.NeedMore;
			}
			else
			{
				if (flush != FlushType.Finish)
				{
					return BlockState.BlockDone;
				}
				return BlockState.FinishDone;
			}
		}

		// Token: 0x060005AF RID: 1455 RVA: 0x0000E74C File Offset: 0x0000C94C
		internal int longest_match(int cur_match)
		{
			int num = this.config.MaxChainLength;
			int num2 = this.strstart;
			int num3 = this.prev_length;
			int num4 = (this.strstart > this.w_size - DeflateManager.MIN_LOOKAHEAD) ? (this.strstart - (this.w_size - DeflateManager.MIN_LOOKAHEAD)) : 0;
			int niceLength = this.config.NiceLength;
			int num5 = this.w_mask;
			int num6 = this.strstart + DeflateManager.MAX_MATCH;
			byte b = this.window[num2 + num3 - 1];
			byte b2 = this.window[num2 + num3];
			if (this.prev_length >= this.config.GoodLength)
			{
				num >>= 2;
			}
			if (niceLength > this.lookahead)
			{
				niceLength = this.lookahead;
			}
			do
			{
				int num7 = cur_match;
				if (this.window[num7 + num3] == b2 && this.window[num7 + num3 - 1] == b && this.window[num7] == this.window[num2] && this.window[++num7] == this.window[num2 + 1])
				{
					num2 += 2;
					num7++;
					while (this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && this.window[++num2] == this.window[++num7] && num2 < num6)
					{
					}
					int num8 = DeflateManager.MAX_MATCH - (num6 - num2);
					num2 = num6 - DeflateManager.MAX_MATCH;
					if (num8 > num3)
					{
						this.match_start = cur_match;
						num3 = num8;
						if (num8 >= niceLength)
						{
							break;
						}
						b = this.window[num2 + num3 - 1];
						b2 = this.window[num2 + num3];
					}
				}
				if ((cur_match = ((int)this.prev[cur_match & num5] & 65535)) <= num4)
				{
					break;
				}
			}
			while (--num != 0);
			if (num3 <= this.lookahead)
			{
				return num3;
			}
			return this.lookahead;
		}

		internal bool WantRfc1950HeaderBytes { get; set; } = true;

		// Token: 0x060005B2 RID: 1458 RVA: 0x00007FF6 File Offset: 0x000061F6
		internal int Initialize(ZLibCodec codec, CompressionLevel level)
		{
			return this.Initialize(codec, level, 15);
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x00008002 File Offset: 0x00006202
		internal int Initialize(ZLibCodec codec, CompressionLevel level, int bits)
		{
			return this.Initialize(codec, level, bits, DeflateManager.MEM_LEVEL_DEFAULT, CompressionStrategy.Default);
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x00008013 File Offset: 0x00006213
		internal int Initialize(ZLibCodec codec, CompressionLevel level, int bits, CompressionStrategy compressionStrategy)
		{
			return this.Initialize(codec, level, bits, DeflateManager.MEM_LEVEL_DEFAULT, compressionStrategy);
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x0000E9E0 File Offset: 0x0000CBE0
		internal int Initialize(ZLibCodec codec, CompressionLevel level, int windowBits, int memLevel, CompressionStrategy strategy)
		{
			this.m_codec = codec;
			this.m_codec.Message = null;
			if (windowBits < 9 || windowBits > 15)
			{
				throw new ZLibException("windowBits must be in the range 9..15.");
			}
			if (memLevel < 1 || memLevel > DeflateManager.MEM_LEVEL_MAX)
			{
				throw new ZLibException(string.Format("memLevel must be in the range 1.. {0}", DeflateManager.MEM_LEVEL_MAX));
			}
			this.m_codec.dstate = this;
			this.w_bits = windowBits;
			this.w_size = 1 << this.w_bits;
			this.w_mask = this.w_size - 1;
			this.hash_bits = memLevel + 7;
			this.hash_size = 1 << this.hash_bits;
			this.hash_mask = this.hash_size - 1;
			this.hash_shift = (this.hash_bits + DeflateManager.MIN_MATCH - 1) / DeflateManager.MIN_MATCH;
			this.window = new byte[this.w_size * 2];
			this.prev = new short[this.w_size];
			this.head = new short[this.hash_size];
			this.lit_bufsize = 1 << memLevel + 6;
			this.pending = new byte[this.lit_bufsize * 4];
			this.m_distanceOffset = this.lit_bufsize;
			this.m_lengthOffset = 3 * this.lit_bufsize;
			this.compressionLevel = level;
			this.compressionStrategy = strategy;
			this.Reset();
			return 0;
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x0000EB3C File Offset: 0x0000CD3C
		internal void Reset()
		{
			ZLibCodec codec = this.m_codec;
			this.m_codec.TotalBytesOut = 0L;
			codec.TotalBytesIn = 0L;
			this.m_codec.Message = null;
			this.pendingCount = 0;
			this.nextPending = 0;
			this.Rfc1950BytesEmitted = false;
			this.status = (this.WantRfc1950HeaderBytes ? DeflateManager.INIT_STATE : DeflateManager.BUSY_STATE);
			this.m_codec.m_Adler32 = Adler.Adler32(0U, null, 0, 0);
			this.last_flush = 0;
			this.m_InitializeTreeData();
			this.m_InitializeLazyMatch();
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x0000EBD4 File Offset: 0x0000CDD4
		internal int End()
		{
			if (this.status != DeflateManager.INIT_STATE && this.status != DeflateManager.BUSY_STATE && this.status != DeflateManager.FINISH_STATE)
			{
				return -2;
			}
			this.pending = null;
			this.head = null;
			this.prev = null;
			this.window = null;
			if (this.status != DeflateManager.BUSY_STATE)
			{
				return 0;
			}
			return -3;
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x0000EC38 File Offset: 0x0000CE38
		private void SetDeflater()
		{
			switch (this.config.Flavor)
			{
			case DeflateFlavor.Store:
				this.DeflateFunction = new DeflateManager.CompressFunc(this.DeflateNone);
				return;
			case DeflateFlavor.Fast:
				this.DeflateFunction = new DeflateManager.CompressFunc(this.DeflateFast);
				return;
			case DeflateFlavor.Slow:
				this.DeflateFunction = new DeflateManager.CompressFunc(this.DeflateSlow);
				return;
			default:
				return;
			}
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x0000EC9C File Offset: 0x0000CE9C
		internal int SetParams(CompressionLevel level, CompressionStrategy strategy)
		{
			int result = 0;
			if (this.compressionLevel != level)
			{
				DeflateManager.Config config = DeflateManager.Config.Lookup(level);
				if (config.Flavor != this.config.Flavor && this.m_codec.TotalBytesIn != 0L)
				{
					result = this.m_codec.Deflate(FlushType.Partial);
				}
				this.compressionLevel = level;
				this.config = config;
				this.SetDeflater();
			}
			this.compressionStrategy = strategy;
			return result;
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x0000ED04 File Offset: 0x0000CF04
		internal int SetDictionary(byte[] dictionary)
		{
			int num = dictionary.Length;
			int sourceIndex = 0;
			if (dictionary != null)
			{
				if (this.status == DeflateManager.INIT_STATE)
				{
					this.m_codec.m_Adler32 = Adler.Adler32(this.m_codec.m_Adler32, dictionary, 0, dictionary.Length);
					if (num < DeflateManager.MIN_MATCH)
					{
						return 0;
					}
					if (num > this.w_size - DeflateManager.MIN_LOOKAHEAD)
					{
						num = this.w_size - DeflateManager.MIN_LOOKAHEAD;
						sourceIndex = dictionary.Length - num;
					}
					Array.Copy(dictionary, sourceIndex, this.window, 0, num);
					this.strstart = num;
					this.block_start = num;
					this.ins_h = (int)(this.window[0] & byte.MaxValue);
					this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[1] & byte.MaxValue)) & this.hash_mask);
					for (int i = 0; i <= num - DeflateManager.MIN_MATCH; i++)
					{
						this.ins_h = ((this.ins_h << this.hash_shift ^ (int)(this.window[i + (DeflateManager.MIN_MATCH - 1)] & byte.MaxValue)) & this.hash_mask);
						this.prev[i & this.w_mask] = this.head[this.ins_h];
						this.head[this.ins_h] = (short)i;
					}
					return 0;
				}
			}
			throw new ZLibException("Stream error.");
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x0000EE58 File Offset: 0x0000D058
		internal int Deflate(FlushType flush)
		{
			if (this.m_codec.OutputBuffer != null && (this.m_codec.InputBuffer != null || this.m_codec.AvailableBytesIn == 0))
			{
				if (this.status != DeflateManager.FINISH_STATE || flush == FlushType.Finish)
				{
					if (this.m_codec.AvailableBytesOut == 0)
					{
						this.m_codec.Message = DeflateManager.m_ErrorMessage[7];
						throw new ZLibException("OutputBuffer is full (AvailableBytesOut == 0)");
					}
					int num = this.last_flush;
					this.last_flush = (int)flush;
					int num4;
					if (this.status == DeflateManager.INIT_STATE)
					{
						int num2 = DeflateManager.Z_DEFLATED + (this.w_bits - 8 << 4) << 8;
						int num3 = (this.compressionLevel - CompressionLevel.BestSpeed & 255) >> 1;
						if (num3 > 3)
						{
							num3 = 3;
						}
						num2 |= num3 << 6;
						if (this.strstart != 0)
						{
							num2 |= DeflateManager.PRESET_DICT;
						}
						num2 += 31 - num2 % 31;
						this.status = DeflateManager.BUSY_STATE;
						byte[] array = this.pending;
						num4 = this.pendingCount;
						this.pendingCount = num4 + 1;
						array[num4] = (byte)(num2 >> 8);
						byte[] array2 = this.pending;
						num4 = this.pendingCount;
						this.pendingCount = num4 + 1;
						array2[num4] = (byte)num2;
						if (this.strstart != 0)
						{
							byte[] array3 = this.pending;
							num4 = this.pendingCount;
							this.pendingCount = num4 + 1;
							array3[num4] = (byte)((this.m_codec.m_Adler32 & 4278190080U) >> 24);
							byte[] array4 = this.pending;
							num4 = this.pendingCount;
							this.pendingCount = num4 + 1;
							array4[num4] = (byte)((this.m_codec.m_Adler32 & 16711680U) >> 16);
							byte[] array5 = this.pending;
							num4 = this.pendingCount;
							this.pendingCount = num4 + 1;
							array5[num4] = (byte)((this.m_codec.m_Adler32 & 65280U) >> 8);
							byte[] array6 = this.pending;
							num4 = this.pendingCount;
							this.pendingCount = num4 + 1;
							array6[num4] = (byte)(this.m_codec.m_Adler32 & 255U);
						}
						this.m_codec.m_Adler32 = Adler.Adler32(0U, null, 0, 0);
					}
					if (this.pendingCount != 0)
					{
						this.m_codec.flush_pending();
						if (this.m_codec.AvailableBytesOut == 0)
						{
							this.last_flush = -1;
							return 0;
						}
					}
					else if (this.m_codec.AvailableBytesIn == 0 && flush <= (FlushType)num && flush != FlushType.Finish)
					{
						return 0;
					}
					if (this.status == DeflateManager.FINISH_STATE && this.m_codec.AvailableBytesIn != 0)
					{
						this.m_codec.Message = DeflateManager.m_ErrorMessage[7];
						throw new ZLibException("status == FINISH_STATE && m_codec.AvailableBytesIn != 0");
					}
					if (this.m_codec.AvailableBytesIn != 0 || this.lookahead != 0 || (flush != FlushType.None && this.status != DeflateManager.FINISH_STATE))
					{
						BlockState blockState = this.DeflateFunction(flush);
						if (blockState == BlockState.FinishStarted || blockState == BlockState.FinishDone)
						{
							this.status = DeflateManager.FINISH_STATE;
						}
						if (blockState != BlockState.NeedMore)
						{
							if (blockState != BlockState.FinishStarted)
							{
								if (blockState != BlockState.BlockDone)
								{
									goto IL_319;
								}
								if (flush == FlushType.Partial)
								{
									this.m_tr_align();
								}
								else
								{
									this.m_tr_stored_block(0, 0, false);
									if (flush == FlushType.Full)
									{
										for (int i = 0; i < this.hash_size; i++)
										{
											this.head[i] = 0;
										}
									}
								}
								this.m_codec.flush_pending();
								if (this.m_codec.AvailableBytesOut == 0)
								{
									this.last_flush = -1;
									return 0;
								}
								goto IL_319;
							}
						}
						if (this.m_codec.AvailableBytesOut == 0)
						{
							this.last_flush = -1;
						}
						return 0;
					}
					IL_319:
					if (flush != FlushType.Finish)
					{
						return 0;
					}
					if (!this.WantRfc1950HeaderBytes || this.Rfc1950BytesEmitted)
					{
						return 1;
					}
					byte[] array7 = this.pending;
					num4 = this.pendingCount;
					this.pendingCount = num4 + 1;
					array7[num4] = (byte)((this.m_codec.m_Adler32 & 4278190080U) >> 24);
					byte[] array8 = this.pending;
					num4 = this.pendingCount;
					this.pendingCount = num4 + 1;
					array8[num4] = (byte)((this.m_codec.m_Adler32 & 16711680U) >> 16);
					byte[] array9 = this.pending;
					num4 = this.pendingCount;
					this.pendingCount = num4 + 1;
					array9[num4] = (byte)((this.m_codec.m_Adler32 & 65280U) >> 8);
					byte[] array10 = this.pending;
					num4 = this.pendingCount;
					this.pendingCount = num4 + 1;
					array10[num4] = (byte)(this.m_codec.m_Adler32 & 255U);
					this.m_codec.flush_pending();
					this.Rfc1950BytesEmitted = true;
					if (this.pendingCount == 0)
					{
						return 1;
					}
					return 0;
				}
			}
			this.m_codec.Message = DeflateManager.m_ErrorMessage[4];
			throw new ZLibException(string.Format("Something is fishy. [{0}]", this.m_codec.Message));
		}

		// Token: 0x0400023C RID: 572
		private static readonly int MEM_LEVEL_MAX = 9;

		// Token: 0x0400023D RID: 573
		private static readonly int MEM_LEVEL_DEFAULT = 8;

		// Token: 0x0400023E RID: 574
		private DeflateManager.CompressFunc DeflateFunction;

		// Token: 0x0400023F RID: 575
		private static readonly string[] m_ErrorMessage = new string[]
		{
			"need dictionary",
			"stream end",
			"",
			"file error",
			"stream error",
			"data error",
			"insufficient memory",
			"buffer error",
			"incompatible version",
			""
		};

		// Token: 0x04000240 RID: 576
		private static readonly int PRESET_DICT = 32;

		// Token: 0x04000241 RID: 577
		private static readonly int INIT_STATE = 42;

		// Token: 0x04000242 RID: 578
		private static readonly int BUSY_STATE = 113;

		// Token: 0x04000243 RID: 579
		private static readonly int FINISH_STATE = 666;

		// Token: 0x04000244 RID: 580
		private static readonly int Z_DEFLATED = 8;

		// Token: 0x04000245 RID: 581
		private static readonly int STORED_BLOCK = 0;

		// Token: 0x04000246 RID: 582
		private static readonly int STATIC_TREES = 1;

		// Token: 0x04000247 RID: 583
		private static readonly int DYN_TREES = 2;

		// Token: 0x04000248 RID: 584
		private static readonly int Z_BINARY = 0;

		// Token: 0x04000249 RID: 585
		private static readonly int Z_ASCII = 1;

		// Token: 0x0400024A RID: 586
		private static readonly int Z_UNKNOWN = 2;

		// Token: 0x0400024B RID: 587
		private static readonly int Buf_size = 16;

		// Token: 0x0400024C RID: 588
		private static readonly int MIN_MATCH = 3;

		// Token: 0x0400024D RID: 589
		private static readonly int MAX_MATCH = 258;

		// Token: 0x0400024E RID: 590
		private static readonly int MIN_LOOKAHEAD = DeflateManager.MAX_MATCH + DeflateManager.MIN_MATCH + 1;

		// Token: 0x0400024F RID: 591
		private static readonly int HEAP_SIZE = 2 * InternalConstants.L_CODES + 1;

		// Token: 0x04000250 RID: 592
		private static readonly int END_BLOCK = 256;

		// Token: 0x04000251 RID: 593
		internal ZLibCodec m_codec;

		// Token: 0x04000252 RID: 594
		internal int status;

		// Token: 0x04000253 RID: 595
		internal byte[] pending;

		// Token: 0x04000254 RID: 596
		internal int nextPending;

		// Token: 0x04000255 RID: 597
		internal int pendingCount;

		// Token: 0x04000256 RID: 598
		internal sbyte data_type;

		// Token: 0x04000257 RID: 599
		internal int last_flush;

		// Token: 0x04000258 RID: 600
		internal int w_size;

		// Token: 0x04000259 RID: 601
		internal int w_bits;

		// Token: 0x0400025A RID: 602
		internal int w_mask;

		// Token: 0x0400025B RID: 603
		internal byte[] window;

		// Token: 0x0400025C RID: 604
		internal int window_size;

		// Token: 0x0400025D RID: 605
		internal short[] prev;

		// Token: 0x0400025E RID: 606
		internal short[] head;

		// Token: 0x0400025F RID: 607
		internal int ins_h;

		// Token: 0x04000260 RID: 608
		internal int hash_size;

		// Token: 0x04000261 RID: 609
		internal int hash_bits;

		// Token: 0x04000262 RID: 610
		internal int hash_mask;

		// Token: 0x04000263 RID: 611
		internal int hash_shift;

		// Token: 0x04000264 RID: 612
		internal int block_start;

		// Token: 0x04000265 RID: 613
		private DeflateManager.Config config;

		// Token: 0x04000266 RID: 614
		internal int match_length;

		// Token: 0x04000267 RID: 615
		internal int prev_match;

		// Token: 0x04000268 RID: 616
		internal int match_available;

		// Token: 0x04000269 RID: 617
		internal int strstart;

		// Token: 0x0400026A RID: 618
		internal int match_start;

		// Token: 0x0400026B RID: 619
		internal int lookahead;

		// Token: 0x0400026C RID: 620
		internal int prev_length;

		// Token: 0x0400026D RID: 621
		internal CompressionLevel compressionLevel;

		// Token: 0x0400026E RID: 622
		internal CompressionStrategy compressionStrategy;

		// Token: 0x0400026F RID: 623
		internal short[] dyn_ltree;

		// Token: 0x04000270 RID: 624
		internal short[] dyn_dtree;

		// Token: 0x04000271 RID: 625
		internal short[] bl_tree;

		// Token: 0x04000272 RID: 626
		internal Tree treeLiterals = new Tree();

		// Token: 0x04000273 RID: 627
		internal Tree treeDistances = new Tree();

		// Token: 0x04000274 RID: 628
		internal Tree treeBitLengths = new Tree();

		// Token: 0x04000275 RID: 629
		internal short[] bl_count = new short[InternalConstants.MAX_BITS + 1];

		// Token: 0x04000276 RID: 630
		internal int[] heap = new int[2 * InternalConstants.L_CODES + 1];

		// Token: 0x04000277 RID: 631
		internal int heap_len;

		// Token: 0x04000278 RID: 632
		internal int heap_max;

		// Token: 0x04000279 RID: 633
		internal sbyte[] depth = new sbyte[2 * InternalConstants.L_CODES + 1];

		// Token: 0x0400027A RID: 634
		internal int m_lengthOffset;

		// Token: 0x0400027B RID: 635
		internal int lit_bufsize;

		// Token: 0x0400027C RID: 636
		internal int last_lit;

		// Token: 0x0400027D RID: 637
		internal int m_distanceOffset;

		// Token: 0x0400027E RID: 638
		internal int opt_len;

		// Token: 0x0400027F RID: 639
		internal int static_len;

		// Token: 0x04000280 RID: 640
		internal int matches;

		// Token: 0x04000281 RID: 641
		internal int last_eob_len;

		// Token: 0x04000282 RID: 642
		internal short bi_buf;

		// Token: 0x04000283 RID: 643
		internal int bi_valid;

		// Token: 0x04000284 RID: 644
		private bool Rfc1950BytesEmitted;

		internal delegate BlockState CompressFunc(FlushType flush);

		// Token: 0x020000C8 RID: 200
		internal class Config
		{
			// Token: 0x060005C1 RID: 1473 RVA: 0x00008025 File Offset: 0x00006225
			private Config(int goodLength, int maxLazy, int niceLength, int maxChainLength, DeflateFlavor flavor)
			{
				this.GoodLength = goodLength;
				this.MaxLazy = maxLazy;
				this.NiceLength = niceLength;
				this.MaxChainLength = maxChainLength;
				this.Flavor = flavor;
			}

			// Token: 0x060005C2 RID: 1474 RVA: 0x00008052 File Offset: 0x00006252
			public static DeflateManager.Config Lookup(CompressionLevel level)
			{
				return DeflateManager.Config.Table[(int)level];
			}

			// Token: 0x04000286 RID: 646
			internal int GoodLength;

			// Token: 0x04000287 RID: 647
			internal int MaxLazy;

			// Token: 0x04000288 RID: 648
			internal int NiceLength;

			// Token: 0x04000289 RID: 649
			internal int MaxChainLength;

			// Token: 0x0400028A RID: 650
			internal DeflateFlavor Flavor;

			// Token: 0x0400028B RID: 651
			private static readonly DeflateManager.Config[] Table = new DeflateManager.Config[]
			{
				new DeflateManager.Config(0, 0, 0, 0, DeflateFlavor.Store),
				new DeflateManager.Config(4, 4, 8, 4, DeflateFlavor.Fast),
				new DeflateManager.Config(4, 5, 16, 8, DeflateFlavor.Fast),
				new DeflateManager.Config(4, 6, 32, 32, DeflateFlavor.Fast),
				new DeflateManager.Config(4, 4, 16, 16, DeflateFlavor.Slow),
				new DeflateManager.Config(8, 16, 32, 32, DeflateFlavor.Slow),
				new DeflateManager.Config(8, 16, 128, 128, DeflateFlavor.Slow),
				new DeflateManager.Config(8, 32, 128, 256, DeflateFlavor.Slow),
				new DeflateManager.Config(32, 128, 258, 1024, DeflateFlavor.Slow),
				new DeflateManager.Config(32, 258, 258, 4096, DeflateFlavor.Slow)
			};
		}
	}
}
