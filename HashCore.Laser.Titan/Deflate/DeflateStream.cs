using System;
using System.IO;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000C9 RID: 201
	public class DeflateStream : Stream
	{
		// Token: 0x060005C4 RID: 1476 RVA: 0x0000805B File Offset: 0x0000625B
		public DeflateStream(Stream stream, CompressionMode mode) : this(stream, mode, CompressionLevel.Default, false)
		{
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00008067 File Offset: 0x00006267
		public DeflateStream(Stream stream, CompressionMode mode, CompressionLevel level) : this(stream, mode, level, false)
		{
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00008073 File Offset: 0x00006273
		public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, CompressionLevel.Default, leaveOpen)
		{
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x0000807F File Offset: 0x0000627F
		public DeflateStream(Stream stream, CompressionMode mode, CompressionLevel level, bool leaveOpen)
		{
			this.m_innerStream = stream;
			this.m_baseStream = new ZLibBaseStream(stream, mode, level, ZlibStreamFlavor.DEFLATE, leaveOpen);
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
					throw new ObjectDisposedException("DeflateStream");
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
					throw new ObjectDisposedException("DeflateStream");
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

		public CompressionStrategy Strategy
		{
			get
			{
				return this.m_baseStream.Strategy;
			}
			set
			{
				if (this.m_disposed)
				{
					throw new ObjectDisposedException("DeflateStream");
				}
				this.m_baseStream.Strategy = value;
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

		// Token: 0x060005D0 RID: 1488 RVA: 0x0000F4F0 File Offset: 0x0000D6F0
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
					throw new ObjectDisposedException("DeflateStream");
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
					throw new ObjectDisposedException("DeflateStream");
				}
				return this.m_baseStream.m_stream.CanWrite;
			}
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x0000817A File Offset: 0x0000637A
		public override void Flush()
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("DeflateStream");
			}
			this.m_baseStream.Flush();
		}

		public override long Length
		{
			get
			{
				throw new NotImplementedException();
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
				throw new NotImplementedException();
			}
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x000081A1 File Offset: 0x000063A1
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("DeflateStream");
			}
			return this.m_baseStream.Read(buffer, offset, count);
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x0000819A File Offset: 0x0000639A
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x0000819A File Offset: 0x0000639A
		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x000081C4 File Offset: 0x000063C4
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this.m_disposed)
			{
				throw new ObjectDisposedException("DeflateStream");
			}
			this.m_baseStream.Write(buffer, offset, count);
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x0000F590 File Offset: 0x0000D790
		public static byte[] CompressString(string s)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				Stream compressor = new DeflateStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
				ZLibBaseStream.CompressString(s, compressor);
				result = memoryStream.ToArray();
			}
			return result;
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x0000F5D8 File Offset: 0x0000D7D8
		public static byte[] CompressBuffer(byte[] b)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				Stream compressor = new DeflateStream(memoryStream, CompressionMode.Compress, CompressionLevel.BestCompression);
				ZLibBaseStream.CompressBuffer(b, compressor);
				result = memoryStream.ToArray();
			}
			return result;
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x0000F620 File Offset: 0x0000D820
		public static string UncompressString(byte[] compressed)
		{
			string result;
			using (MemoryStream memoryStream = new MemoryStream(compressed))
			{
				Stream decompressor = new DeflateStream(memoryStream, CompressionMode.Decompress);
				result = ZLibBaseStream.UncompressString(compressed, decompressor);
			}
			return result;
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x0000F664 File Offset: 0x0000D864
		public static byte[] UncompressBuffer(byte[] compressed)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream(compressed))
			{
				Stream decompressor = new DeflateStream(memoryStream, CompressionMode.Decompress);
				result = ZLibBaseStream.UncompressBuffer(compressed, decompressor);
			}
			return result;
		}

		// Token: 0x0400028C RID: 652
		internal ZLibBaseStream m_baseStream;

		// Token: 0x0400028D RID: 653
		internal Stream m_innerStream;

		// Token: 0x0400028E RID: 654
		private bool m_disposed;
	}
}
