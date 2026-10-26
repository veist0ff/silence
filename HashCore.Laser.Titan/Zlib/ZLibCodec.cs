using System;
using System.Runtime.InteropServices;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000E2 RID: 226
	[Guid("ebc25cf6-9120-4283-b972-0e5520d0000D")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public sealed class ZLibCodec
	{
		public int Adler32
		{
			get
			{
				return (int)this.m_Adler32;
			}
		}

		// Token: 0x06000676 RID: 1654 RVA: 0x0000870B File Offset: 0x0000690B
		public ZLibCodec()
		{
		}

		// Token: 0x06000677 RID: 1655 RVA: 0x00014DDC File Offset: 0x00012FDC
		public ZLibCodec(CompressionMode mode)
		{
			if (mode == CompressionMode.Compress)
			{
				if (this.InitializeDeflate() != 0)
				{
					throw new ZLibException("Cannot initialize for deflate.");
				}
			}
			else
			{
				if (mode != CompressionMode.Decompress)
				{
					throw new ZLibException("Invalid ZlibStreamFlavor.");
				}
				if (this.InitializeInflate() != 0)
				{
					throw new ZLibException("Cannot initialize for inflate.");
				}
			}
		}

		// Token: 0x06000678 RID: 1656 RVA: 0x00008722 File Offset: 0x00006922
		public int InitializeInflate()
		{
			return this.InitializeInflate(this.WindowBits);
		}

		// Token: 0x06000679 RID: 1657 RVA: 0x00008730 File Offset: 0x00006930
		public int InitializeInflate(bool expectRfc1950Header)
		{
			return this.InitializeInflate(this.WindowBits, expectRfc1950Header);
		}

		// Token: 0x0600067A RID: 1658 RVA: 0x0000873F File Offset: 0x0000693F
		public int InitializeInflate(int windowBits)
		{
			this.WindowBits = windowBits;
			return this.InitializeInflate(windowBits, true);
		}

		// Token: 0x0600067B RID: 1659 RVA: 0x00008750 File Offset: 0x00006950
		public int InitializeInflate(int windowBits, bool expectRfc1950Header)
		{
			this.WindowBits = windowBits;
			if (this.dstate != null)
			{
				throw new ZLibException("You may not call InitializeInflate() after calling InitializeDeflate().");
			}
			this.istate = new InflateManager(expectRfc1950Header);
			return this.istate.Initialize(this, windowBits);
		}

		// Token: 0x0600067C RID: 1660 RVA: 0x00008785 File Offset: 0x00006985
		public int Inflate(FlushType flush)
		{
			if (this.istate == null)
			{
				throw new ZLibException("No Inflate State!");
			}
			return this.istate.Inflate(flush);
		}

		// Token: 0x0600067D RID: 1661 RVA: 0x000087A6 File Offset: 0x000069A6
		public int EndInflate()
		{
			if (this.istate == null)
			{
				throw new ZLibException("No Inflate State!");
			}
			int result = this.istate.End();
			this.istate = null;
			return result;
		}

		// Token: 0x0600067E RID: 1662 RVA: 0x000087CD File Offset: 0x000069CD
		public int SyncInflate()
		{
			if (this.istate == null)
			{
				throw new ZLibException("No Inflate State!");
			}
			return this.istate.Sync();
		}

		// Token: 0x0600067F RID: 1663 RVA: 0x000087ED File Offset: 0x000069ED
		public int InitializeDeflate()
		{
			return this.m_InternalInitializeDeflate(true);
		}

		// Token: 0x06000680 RID: 1664 RVA: 0x000087F6 File Offset: 0x000069F6
		public int InitializeDeflate(CompressionLevel level)
		{
			this.CompressLevel = level;
			return this.m_InternalInitializeDeflate(true);
		}

		// Token: 0x06000681 RID: 1665 RVA: 0x00008806 File Offset: 0x00006A06
		public int InitializeDeflate(CompressionLevel level, bool wantRfc1950Header)
		{
			this.CompressLevel = level;
			return this.m_InternalInitializeDeflate(wantRfc1950Header);
		}

		// Token: 0x06000682 RID: 1666 RVA: 0x00008816 File Offset: 0x00006A16
		public int InitializeDeflate(CompressionLevel level, int bits)
		{
			this.CompressLevel = level;
			this.WindowBits = bits;
			return this.m_InternalInitializeDeflate(true);
		}

		// Token: 0x06000683 RID: 1667 RVA: 0x0000882D File Offset: 0x00006A2D
		public int InitializeDeflate(CompressionLevel level, int bits, bool wantRfc1950Header)
		{
			this.CompressLevel = level;
			this.WindowBits = bits;
			return this.m_InternalInitializeDeflate(wantRfc1950Header);
		}

		// Token: 0x06000684 RID: 1668 RVA: 0x00014E38 File Offset: 0x00013038
		private int m_InternalInitializeDeflate(bool wantRfc1950Header)
		{
			if (this.istate != null)
			{
				throw new ZLibException("You may not call InitializeDeflate() after calling InitializeInflate().");
			}
			this.dstate = new DeflateManager();
			this.dstate.WantRfc1950HeaderBytes = wantRfc1950Header;
			return this.dstate.Initialize(this, this.CompressLevel, this.WindowBits, this.Strategy);
		}

		// Token: 0x06000685 RID: 1669 RVA: 0x00008844 File Offset: 0x00006A44
		public int Deflate(FlushType flush)
		{
			if (this.dstate == null)
			{
				throw new ZLibException("No Deflate State!");
			}
			return this.dstate.Deflate(flush);
		}

		// Token: 0x06000686 RID: 1670 RVA: 0x00008865 File Offset: 0x00006A65
		public int EndDeflate()
		{
			if (this.dstate == null)
			{
				throw new ZLibException("No Deflate State!");
			}
			this.dstate = null;
			return 0;
		}

		// Token: 0x06000687 RID: 1671 RVA: 0x00008882 File Offset: 0x00006A82
		public void ResetDeflate()
		{
			if (this.dstate == null)
			{
				throw new ZLibException("No Deflate State!");
			}
			this.dstate.Reset();
		}

		// Token: 0x06000688 RID: 1672 RVA: 0x000088A2 File Offset: 0x00006AA2
		public int SetDeflateParams(CompressionLevel level, CompressionStrategy strategy)
		{
			if (this.dstate == null)
			{
				throw new ZLibException("No Deflate State!");
			}
			return this.dstate.SetParams(level, strategy);
		}

		// Token: 0x06000689 RID: 1673 RVA: 0x000088C4 File Offset: 0x00006AC4
		public int SetDictionary(byte[] dictionary)
		{
			if (this.istate != null)
			{
				return this.istate.SetDictionary(dictionary);
			}
			if (this.dstate == null)
			{
				throw new ZLibException("No Inflate or Deflate state!");
			}
			return this.dstate.SetDictionary(dictionary);
		}

		// Token: 0x0600068A RID: 1674 RVA: 0x00014E90 File Offset: 0x00013090
		internal void flush_pending()
		{
			int num = this.dstate.pendingCount;
			if (num > this.AvailableBytesOut)
			{
				num = this.AvailableBytesOut;
			}
			if (num == 0)
			{
				return;
			}
			if (this.dstate.pending.Length > this.dstate.nextPending && this.OutputBuffer.Length > this.NextOut && this.dstate.pending.Length >= this.dstate.nextPending + num && this.OutputBuffer.Length >= this.NextOut + num)
			{
				Array.Copy(this.dstate.pending, this.dstate.nextPending, this.OutputBuffer, this.NextOut, num);
				this.NextOut += num;
				this.dstate.nextPending += num;
				this.TotalBytesOut += (long)num;
				this.AvailableBytesOut -= num;
				this.dstate.pendingCount -= num;
				if (this.dstate.pendingCount == 0)
				{
					this.dstate.nextPending = 0;
				}
				return;
			}
			throw new ZLibException(string.Format("Invalid State. (pending.Length={0}, pendingCount={1})", this.dstate.pending.Length, this.dstate.pendingCount));
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x00014FE8 File Offset: 0x000131E8
		internal int read_buf(byte[] buf, int start, int size)
		{
			int num = this.AvailableBytesIn;
			if (num > size)
			{
				num = size;
			}
			if (num == 0)
			{
				return 0;
			}
			this.AvailableBytesIn -= num;
			if (this.dstate.WantRfc1950HeaderBytes)
			{
				this.m_Adler32 = Adler.Adler32(this.m_Adler32, this.InputBuffer, this.NextIn, num);
			}
			Array.Copy(this.InputBuffer, this.NextIn, buf, start, num);
			this.NextIn += num;
			this.TotalBytesIn += (long)num;
			return num;
		}

		// Token: 0x04000397 RID: 919
		public byte[] InputBuffer;

		// Token: 0x04000398 RID: 920
		public int NextIn;

		// Token: 0x04000399 RID: 921
		public int AvailableBytesIn;

		// Token: 0x0400039A RID: 922
		public long TotalBytesIn;

		// Token: 0x0400039B RID: 923
		public byte[] OutputBuffer;

		// Token: 0x0400039C RID: 924
		public int NextOut;

		// Token: 0x0400039D RID: 925
		public int AvailableBytesOut;

		// Token: 0x0400039E RID: 926
		public long TotalBytesOut;

		// Token: 0x0400039F RID: 927
		public string Message;

		// Token: 0x040003A0 RID: 928
		internal DeflateManager dstate;

		// Token: 0x040003A1 RID: 929
		internal InflateManager istate;

		// Token: 0x040003A2 RID: 930
		internal uint m_Adler32;

		// Token: 0x040003A3 RID: 931
		public CompressionLevel CompressLevel = CompressionLevel.Default;

		// Token: 0x040003A4 RID: 932
		public int WindowBits = 15;

		// Token: 0x040003A5 RID: 933
		public CompressionStrategy Strategy;
	}
}
