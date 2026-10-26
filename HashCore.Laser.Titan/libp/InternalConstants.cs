using System;

namespace HashCore.Laser.Titan.ZLib
{
	// Token: 0x020000DC RID: 220
	internal static class InternalConstants
	{
		// Token: 0x04000367 RID: 871
		internal static readonly int MAX_BITS = 15;

		// Token: 0x04000368 RID: 872
		internal static readonly int BL_CODES = 19;

		// Token: 0x04000369 RID: 873
		internal static readonly int D_CODES = 30;

		// Token: 0x0400036A RID: 874
		internal static readonly int LITERALS = 256;

		// Token: 0x0400036B RID: 875
		internal static readonly int LENGTH_CODES = 29;

		// Token: 0x0400036C RID: 876
		internal static readonly int L_CODES = InternalConstants.LITERALS + 1 + InternalConstants.LENGTH_CODES;

		// Token: 0x0400036D RID: 877
		internal static readonly int MAX_BL_BITS = 7;

		// Token: 0x0400036E RID: 878
		internal static readonly int REP_3_6 = 16;

		// Token: 0x0400036F RID: 879
		internal static readonly int REPZ_3_10 = 17;

		// Token: 0x04000370 RID: 880
		internal static readonly int REPZ_11_138 = 18;
	}
}
