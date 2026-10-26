using System;
using System.IO;
using System.IO.Compression;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000E4 RID: 228
	public class ZLibStream : Stream
	{
		// Token: 0x0600068C RID: 1676 RVA: 0x000088FA File Offset: 0x00006AFA
		public ZLibStream(Stream stream, CompressionMode mode) : this(stream, mode, CompressionLevel.Default, false)
		{
		}

		// Token: 0x0600068D RID: 1677 RVA: 0x00008906 File Offset: 0x00006B06
		public ZLibStream(Stream stream, CompressionMode mode, CompressionLevel level) : this(stream, mode, level, false)
		{
		}

		// Token: 0x0600068E RID: 1678 RVA: 0x00008912 File Offset: 0x00006B12
		public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, CompressionLevel.Default, leaveOpen)
		{
		}

		// Token: 0x0600068F RID: 1679 RVA: 0x0000891E File Offset: 0x00006B1E
		public ZLibStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
		{
			this.m_baseStream = new ZLibBaseStream(stream, mode, level, ZlibStreamFlavor.ZLIB, leaveOpen);
		}

		public virtual FlushType FlushMode
		{
			get
			{
				return this.m_baseStream.m_flushMode;
			}
			set
			{
				if (this.m_disposed)
				{
					throw new ObjectDisposedException("ZLibStream");
				}
				this.m_baseStream.m_flushMode = value;
			}
		}

		public int BufferSize
		{
			get
			{
				return this.m_baseStream.m_bufferSize;
			}
			set
			{
				if (this.m_disposed)
				{
					throw new ObjectDisposedException("ZLibStream");
				}
				if (this.m_baseStream.m_workingBuffer != null)
				{
					throw new ZLibException("The working buffer is already set.");
				}
				if (value < 1024)
				{
					throw new ZLibException(string.Format("Don't be silly. {0} bytes?? Use a bigger buffer, at least {1}.", value, 1024));
				}
				this.m_baseStream.m_bufferSize = value;
			}
		}

		public virtual long TotalIn
		{
			get
			{
				return this.m_baseStream.m_z.TotalBytesIn;
			}
		}

		public virtual long TotalOut
		{
			get
			{
				return this.m_baseStream.m_z.TotalBytesOut;
			}
		}

		// Token: 0x06000696 RID: 1686 RVA: 0x000150E0 File Offset: 0x000132E0
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (!this.m_disposed)
				{
					if (disposing && this.m_baseStream != null)
					{
						this.m_baseStream.Close();
					}
					this.m_disposed = true;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		public override bool CanRead
		{
			get
			{
				if (this.m_disposed)
				{
					throw new ObjectDisposedException("ZLibStream");
				}
				return this.m_baseStream.m_stream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				if (this.m_disposed)
				{
					throw new ObjectDisposedException("ZLibStream");
				}
				return this.m_baseStream.m_stream.CanWrite;
			}
		}

		// Token: 0x0600069A RID: 1690 RVA: 0x000089E4 File Offset: 0x00006BE4
		public override void Flush()
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("ZLibStream");
			}
			this.m_baseStream.Flush();
		}

		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public override long Position
		{
			get
			{
				if (this.m_baseStream.m_streamMode == ZLibBaseStream.StreamMode.Writer)
				{
					return this.m_baseStream.m_z.TotalBytesOut;
				}
				if (this.m_baseStream.m_streamMode == ZLibBaseStream.StreamMode.Reader)
				{
					return this.m_baseStream.m_z.TotalBytesIn;
				}
				return 0L;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x00008A04 File Offset: 0x00006C04
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("ZLibStream");
			}
			return this.m_baseStream.Read(buffer, offset, count);
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x00007F27 File Offset: 0x00006127
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x00007F27 File Offset: 0x00006127
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x00008A27 File Offset: 0x00006C27
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("ZLibStream");
			}
			this.m_baseStream.Write(buffer, offset, count);
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x00015180 File Offset: 0x00013380
		public static byte[] CompressString(string s)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				Stream compressor = new ZLibStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
				ZLibBaseStream.CompressString(s, compressor);
				result = memoryStream.ToArray();
			}
			return result;
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x000151C8 File Offset: 0x000133C8
		public static byte[] CompressBuffer(byte[] b, CompressionLevel level)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				Stream compressor = new ZLibStream(memoryStream, CompressionMode.Compress, level);
				ZLibBaseStream.CompressBuffer(b, compressor);
				result = memoryStream.ToArray();
			}
			return result;
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x00015210 File Offset: 0x00013410
		public static string UncompressString(byte[] compressed)
		{
			string result;
			using (MemoryStream memoryStream = new MemoryStream(compressed))
			{
				Stream decompressor = new ZLibStream(memoryStream, CompressionMode.Decompress);
				result = ZLibBaseStream.UncompressString(compressed, decompressor);
			}
			return result;
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x00015254 File Offset: 0x00013454
		public static byte[] UncompressBuffer(byte[] compressed)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream(compressed))
			{
				Stream decompressor = new ZLibStream(memoryStream, CompressionMode.Decompress);
				result = ZLibBaseStream.UncompressBuffer(compressed, decompressor);
			}
			return result;
		}

		// Token: 0x040003B0 RID: 944
		internal ZLibBaseStream m_baseStream;

		// Token: 0x040003B1 RID: 945
		private bool m_disposed;
	}
}
