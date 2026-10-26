namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Что за режим идёт. Разбор номера вариации, 0x100505178 и соседние.
    ///
    /// Номера здесь не сплошные: одному режиму отвечает несколько вариаций —
    /// обычная и те, что заведены под события, — и различать их приходится по
    /// битовой маске, как это и сделано в оригинале.
    /// </summary>
    public static class LogicGameModeUtilNew
    {
        /// <summary>Обычный захват кристаллов.</summary>
        public const int GemGrabVariation = 0;

        /// <summary>С какой вариации отсчитывается маска событийных захватов.</summary>
        public const int GemGrabMaskBase = 33;

        /// <summary>
        /// Маска событийных вариаций захвата. Из isGemGrab: единицы стоят на
        /// нулевом, тринадцатом и тридцать втором местах от базы, то есть на
        /// вариациях 33, 46 и 65.
        /// </summary>
        public const long GemGrabVariationMask = 0x100002001L;

        /// <summary>0x100505178. Захват ли это кристаллов.</summary>
        public static bool IsGemGrab(int variation)
        {
            int shifted = variation - GemGrabMaskBase;

            if ((uint)shifted <= 0x20 && ((GemGrabVariationMask >> shifted) & 1) != 0) return true;

            return variation == GemGrabVariation;
        }
    }
}
