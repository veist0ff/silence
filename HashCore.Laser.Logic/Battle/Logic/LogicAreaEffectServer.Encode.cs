namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Кодирование зоны эффекта, 0x100444f78.
    /// </summary>
    public partial class LogicAreaEffectServer
    {
        /// <summary>Тип зоны, при котором отдельно пишется признак активности.</summary>
        public const int DelayedDamageEffectType = 8;

        /// <summary>Тип воспроизведения, при котором дописывается ещё одно число.</summary>
        public const int ExtraPlaybackType = 4;

        /// <summary>Поле +160. Зона уже сработала.</summary>
        public bool IsActive;

        /// <summary>Поле +128. Сколько процентов срока зона уже прожила, в тысячных.</summary>
        public int LifeTimeConsumedPromille;

        /// <summary>Поле +132. Дополнительное число для особого воспроизведения.</summary>
        public int PlaybackValue;

        /// <summary>Поля +568 и +572. Масштаб отрисовки и парное к нему число.</summary>
        public int DisplayScale;
        public int DisplayScaleSecond;

        /// <summary>Поле +588. Угол, который пишется только у вращающихся зон.</summary>
        public int RotationAngle;

        /// <summary>
        /// 0x100444f78. Состояние зоны.
        ///
        /// Счётчик проявления берётся как наименьший из собственного счётчика
        /// затухания и того, что пришёл снаружи: гаснущая зона не может выглядеть
        /// ярче, чем ей осталось жить.
        ///
        /// Последний флаг пишется ТОЛЬКО у зон с типом вращения 1 или 3. Условие в
        /// оригинале записано как (тип и не два) равно единице. Если писать его
        /// всегда, на каждой зоне добавляется лишний бит и весь дальнейший поток
        /// зрения разъезжается.
        /// </summary>
        public override void Encode(BitStream stream, bool isOwn, int ownObjectIndex, int teamIndex)
        {
            int fade = FadeoutCounter < FadeCounter ? FadeoutCounter : FadeCounter;
            FadeCounter = fade;

            base.Encode(stream, isOwn, ownObjectIndex, teamIndex);

            if (GetEffectType() == DelayedDamageEffectType) stream.WriteBoolean(IsActive);

            stream.WritePositiveIntMax1023(LifeTimeConsumedPromille);

            if (GetPlaybackType() == ExtraPlaybackType) stream.WritePositiveIntMax127(PlaybackValue);

            if (stream.WriteBoolean(Source != null))
                stream.WritePositiveIntMax134217727(Source.GlobalId);

            stream.WritePositiveVIntMax65535OftenZero(DisplayScale);
            stream.WritePositiveVIntMax65535OftenZero(DisplayScaleSecond);

            int rotationType = GetRotationType();

            if ((rotationType & ~2) == 1)
            {
                if (stream.WriteBoolean(RotationAngle > 0))
                    stream.WritePositiveIntMax511(RotationAngle);
            }
        }

        /// <summary>Поле данных +224.</summary>
        protected virtual int GetEffectType()
        {
            return 0;
        }

        /// <summary>Поле данных +328.</summary>
        protected virtual int GetPlaybackType()
        {
            return 0;
        }

        /// <summary>Поле данных +332.</summary>
        protected virtual int GetRotationType()
        {
            return 0;
        }
    }
}
