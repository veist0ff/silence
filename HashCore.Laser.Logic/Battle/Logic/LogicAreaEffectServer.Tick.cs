namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Отчего зона кончается. Колонка ExpireTypes таблицы зон, разобранная в
    /// createReferences (0x1003e1eb4); значения разделены точкой с запятой, и
    /// зоне хватает любого одного, чтобы кончиться.
    /// </summary>
    public static class LogicAreaEffectExpireType
    {
        /// <summary>Вышло время. Добавляется само, когда у зоны задан срок жизни.</summary>
        public const int Duration = 0;

        /// <summary>Источник перестал копить заряд.</summary>
        public const int TargetNotCharging = 1;

        /// <summary>Зона кого-то задела.</summary>
        public const int HitEnemy = 2;

        /// <summary>С игрока сняли сверхзаряд.</summary>
        public const int NotHypercharged = 3;

        /// <summary>Тот, за кем зона следовала, исчез.</summary>
        public const int FollowTargetGone = 4;

        /// <summary>Источник перестал применять то умение, из которого зона вышла.</summary>
        public const int SkillNotActive = 5;

        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;

            if (name == "targetNotCharging") return TargetNotCharging;
            if (name == "hitEnemy") return HitEnemy;
            if (name == "notHypercharged") return NotHypercharged;
            if (name == "followTargetGone") return FollowTargetGone;
            if (name == "skillNotActive") return SkillNotActive;

            return -1;
        }

        /// <summary>
        /// Разбирает всю колонку. Срок жизни идёт первым и отдельно: в оригинале
        /// он добавляется в список ещё до разбора строки.
        /// </summary>
        public static List<int> ParseList(string value, bool hasDuration)
        {
            List<int> types = new List<int>();

            if (hasDuration) types.Add(Duration);

            if (string.IsNullOrEmpty(value)) return types;

            string[] parts = value.Split(';');

            for (int i = 0; i < parts.Length; i++) types.Add(Parse(parts[i]));

            return types;
        }
    }

    /// <summary>
    /// Кадр зоны, 0x100445094.
    /// </summary>
    public partial class LogicAreaEffectServer
    {
        /// <summary>Потолок доли прожитого, в тысячных.</summary>
        public const int MaxProgress = 1000;

        /// <summary>Трейт, растягивающий радиус зоны.</summary>
        public const LogicTraitType RadiusTrait = (LogicTraitType)142;

        /// <summary>Поле +128. Сколько зона уже прожила, в тысячных от срока.</summary>
        public int Progress;

        /// <summary>Поле +120. Раньше этого тика зона не кончается ни при каких условиях.</summary>
        public int EarliestExpireTick;

        /// <summary>Поле +152. Зона отработала и снимается.</summary>
        public bool Expired;

        /// <summary>Поле +588. Куда источник смотрел, когда зона за ним пошла.</summary>
        public int FollowedMoveAngle;

        /// <summary>Поле +280. Радиус, заданный поверх таблицы.</summary>
        public int RadiusOverride;

        /// <summary>Поле +580. Радиус в процентах от заданного; минус один — не трогать.</summary>
        public int RadiusPercent = -1;

        /// <summary>Отчего эта зона может кончиться.</summary>
        public readonly List<int> ExpireTypes = new List<int>();

        /// <summary>
        /// 0x100445094. Кадр зоны.
        ///
        /// Зона, привязанная к источнику, каждый кадр встаёт туда, где он стоит, —
        /// и если данные это позволяют, ещё и разворачивается вслед за ним.
        ///
        /// Кончается зона по списку условий из таблицы, и достаточно любого одного.
        /// Но не раньше своего тика: у зон, которые должны отработать хотя бы раз,
        /// он отодвинут вперёд, иначе условие сработало бы в тот же кадр, в котором
        /// зона появилась.
        /// </summary>
        public override void Tick()
        {
            if (Source != null)
            {
                SetPosition(Source.X, Source.Y);

                LogicCharacterServer character = Source as LogicCharacterServer;

                if (FollowsSourceAngle() && character != null
                    && character.GetGameObjectType() == LogicGameObjectType.Character)
                {
                    FollowedMoveAngle = character.GetMoveAngle();
                }
            }

            int now = GetCurrentTick();

            int left = ExpireTick - now;
            if (!IsIgnoreFadeout() && left < FadeoutTicks) FadeoutCounter = left > 0 ? left : 0;

            int span = ExpireTick - StartTick;

            if (span < 1)
            {
                Progress = 0;
            }
            else
            {
                int progress = MaxProgress * (now - StartTick) / span;
                if (progress > MaxProgress) progress = MaxProgress;
                Progress = progress < 0 ? 0 : progress;
            }

            if (now <= EarliestExpireTick) return;

            for (int i = 0; i < ExpireTypes.Count; i++)
            {
                if (!IsExpireConditionMet(ExpireTypes[i], now)) continue;

                Expired = true;
                OnExpire();
                return;
            }
        }

        /// <summary>Сработало ли одно конкретное условие окончания.</summary>
        protected virtual bool IsExpireConditionMet(int type, int now)
        {
            LogicCharacterServer character = Source as LogicCharacterServer;

            switch (type)
            {
                case LogicAreaEffectExpireType.Duration:
                    return now >= ExpireTick;

                case LogicAreaEffectExpireType.TargetNotCharging:
                    if (Source == null) return true;
                    if (Source.GetGameObjectType() != LogicGameObjectType.Character) return true;
                    return character == null || !character.Charging;

                case LogicAreaEffectExpireType.NotHypercharged:
                {
                    LogicPlayer player = GetPlayer();
                    if (player == null) return false;
                    return !player.IsHypercharged;
                }

                case LogicAreaEffectExpireType.FollowTargetGone:
                    return Source == null;

                case LogicAreaEffectExpireType.SkillNotActive:
                    if (Source == null) return true;
                    if (Source.GetGameObjectType() != LogicGameObjectType.Character) return true;
                    if (character == null) return true;

                    return !character.IsCastingAreaEffect(AreaEffectData);
            }

            // Условие «задела врага» взводится не здесь, а в самом попадании.
            return false;
        }


        /// <summary>Отработавшая зона снимается с поля ближайшим проходом уборки.</summary>
        public override bool ShouldDestruct()
        {
            return Expired || base.ShouldDestruct();
        }

        /// <summary>Поле данных +340. Зона разворачивается вслед за источником.</summary>
        protected virtual bool FollowsSourceAngle()
        {
            return false;
        }

        /// <summary>
        /// 0x100446a40. Радиус зоны.
        ///
        /// Он берётся из таблицы, но может быть задан поверх неё, срезан в
        /// процентах и растянут трейтом — либо своим, либо чужим, от источника.
        /// </summary>
        public override int GetRadius()
        {
            int radius = RadiusOverride != 0
                ? RadiusOverride
                : (AreaEffectData != null ? AreaEffectData.Radius : 0);

            if (RadiusPercent != -1) radius = radius * RadiusPercent / 100;

            int added;

            if (Traits.TryGetValue(RadiusTrait, out added)) return (added + 100) * radius / 100;

            if (Source != null && Source.Traits.TryGetValue(RadiusTrait, out added))
                return (added + 100) * radius / 100;

            return radius;
        }
    }
}
