namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Кодирование предмета, 0x100474080.
    /// </summary>
    public partial class LogicItemServer
    {
        /// <summary>
        /// Маска типов предметов, которые дописывают два числа сверх общей части.
        /// В оригинале это константа 0x4200000000002980, то есть типы
        /// 7, 8, 11, 13, 57 и 62; отдельно к ним добавлены типы 5 и 24.
        /// </summary>
        public const long ExtraFieldItemTypeMask = 0x4200000000002980L;

        /// <summary>Тип предмета, который сверх двух чисел пишет ещё одно.</summary>
        public const int ThirdFieldItemType = 24;

        /// <summary>Тип, который пишет дополнительные поля вне общей маски.</summary>
        public const int ExtraFieldItemType = 5;

        /// <summary>Наибольший тип, который ещё помещается в маску.</summary>
        public const int MaxMaskedItemType = 62;

        /// <summary>Поля +184 и +188. Тики появления и появления содержимого.</summary>
        public int SpawningTick;
        public int SpawnedTick;

        /// <summary>Поле +228. Третье число, только у одного типа предметов.</summary>
        public int ThirdTick;

        /// <summary>Поле +132. Собственный счётчик затухания.</summary>
        public int FadeoutCounter = int.MaxValue;

        /// <summary>
        /// 0x100474080. Состояние предмета.
        ///
        /// Сверх общей части предмет пишет что-либо далеко не всегда: набор типов,
        /// которым положены дополнительные числа, задан в оригинале битовой маской.
        /// Проверка на переполнение сдвига там обязательна — тип может быть больше
        /// шестидесяти двух, и тогда маску трогать нельзя.
        /// </summary>
        public override void Encode(BitStream stream, bool isOwn, int ownObjectIndex, int teamIndex)
        {
            int fade = FadeoutCounter < FadeCounter ? FadeoutCounter : FadeCounter;
            FadeCounter = fade;

            base.Encode(stream, isOwn, ownObjectIndex, teamIndex);

            int type = GetItemType();
            if (!WritesExtraFields(type)) return;

            stream.WritePositiveIntMax16383(SpawningTick);
            stream.WritePositiveIntMax16383(SpawnedTick);

            if (type == ThirdFieldItemType) stream.WritePositiveIntMax16383(ThirdTick);
        }

        /// <summary>Положены ли этому типу предмета дополнительные числа.</summary>
        public static bool WritesExtraFields(int type)
        {
            if (type == ExtraFieldItemType) return true;
            if (type == ThirdFieldItemType) return true;

            if (type < 0 || type > MaxMaskedItemType) return false;

            return ((ExtraFieldItemTypeMask >> type) & 1) != 0;
        }

        /// <summary>Поле данных +84. Тип предмета.</summary>
        protected virtual int GetItemType()
        {
            return 0;
        }
    }
}
