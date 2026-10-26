using System;
using System.Runtime.InteropServices;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000DA RID: 218
	[Guid("ebc25cf6-9120-4283-b972-0e5520d0000E")]
	public class ZLibException : Exception
	{
		// Token: 0x0600064F RID: 1615 RVA: 0x000085D0 File Offset: 0x000067D0
		public ZLibException()
		{
		}

		// Token: 0x06000650 RID: 1616 RVA: 0x0000461C File Offset: 0x0000281C
		public ZLibException(string s) : base(s)
		{
		}
	}
}
