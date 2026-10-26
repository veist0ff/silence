namespace HashCore.Laser.Logic.Battle.Logic
{
    /// <summary>
    /// Кодирование общей части объекта, 0x100473480.
    ///
    /// Эту часть пишут все объекты, и только потом каждый дописывает своё.
    /// </summary>
    public partial class LogicGameObjectServer
    {
        /// <summary>Поле +96. Высота над землёй.</summary>
        public int EncodeZ;

        /// <summary>
        /// Поле +100. Номер игрока-владельца, -1 если объект ничей.
        /// В паре с командой уходит клиенту одним числом.
        /// </summary>
        public int PlayerIndex = -1;

        /// <summary>
        /// Игрок, которому объект принадлежит. Ничей объект его не имеет.
        ///
        /// Ссылка берётся напрямую, а не по номеру: номера игроков внутри команд
        /// повторяются, и поиск по номеру в бою двух команд отдавал бы чужого.
        /// </summary>
        public LogicPlayer OwnerPlayer;

        public virtual LogicPlayer GetPlayer()
        {
            if (OwnerPlayer != null) return OwnerPlayer;

            if (PlayerIndex < 0) return null;

            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            return battle != null ? battle.GetPlayer(PlayerIndex) : null;
        }

        /// <summary>
        /// 0x100473814. Команда, за которую объект считается прямо сейчас.
        ///
        /// У объекта есть своя команда, но если за ним стоит игрок, верной
        /// считается команда игрока: в режимах, где команды меняются по ходу боя,
        /// объект должен менять сторону вместе со своим хозяином.
        /// </summary>
        public int GetCurrentTeamIndex()
        {
            LogicPlayer player = GetPlayer();
            if (player == null || player.TeamIndex == -1) return TeamIndex;

            return player.TeamIndex;
        }

        /// <summary>
        /// Счётчик проявления. Объект не возникает мгновенно, а всплывает за
        /// несколько кадров; клиент читает это как число от нуля до пятнадцати.
        /// </summary>
        /// <summary>
        /// Поле +112. Насколько объект проявлен, от нуля до десяти. Появляется он
        /// сразу полностью проявленным: от этого же счётчика зависит, попадёт ли
        /// он в кадр чужой команде.
        /// </summary>
        public int FadeCounter = MaxFadeCounter;

        /// <summary>Потолок проявления.</summary>
        public const int MaxFadeCounter = 10;

        /// <summary>
        /// Режим, в котором число игроков принудительно равно трём. Нужен, чтобы
        /// «ничей» объект закодировался тем же числом, что ждёт клиент.
        /// </summary>
        public const int ForcedThreePlayerGameMode = 30;

        /// <summary>
        /// 0x100473480. Общая часть состояния объекта.
        ///
        /// Позиция кодируется тремя разными способами, и выбор не косметический:
        ///   — предмет, выровненный по клеткам, отдаёт номер клетки, а не координату;
        ///   — объект, которому разрешено вылетать за карту, пишет знаковое число;
        ///   — всем остальным координата зажимается в ноль..65535 и идёт беззнаково.
        ///
        /// Владелец пишется одним числом: команда плюс шестнадцать номеров игрока.
        /// Ничей объект подставляет вместо обоих число игроков в бою, отчего
        /// получается ровно семнадцать таких чисел — клиент по этому и узнаёт,
        /// что владельца нет.
        /// </summary>
        public virtual void Encode(BitStream stream, bool isOwn, int ownObjectIndex, int teamIndex)
        {
            bool alignedToTiles = GetGameObjectType() == LogicGameObjectType.Item && IsAlignedToTiles();

            if (alignedToTiles)
            {
                stream.WritePositiveIntMax63(X / LogicTile.TileSize);
                stream.WritePositiveIntMax63(Y / LogicTile.TileSize);
            }
            else
            {
                if (IsAllowedOutsideOfMap())
                {
                    stream.WriteIntMax65535(X);
                    stream.WriteIntMax65535(Y);
                }
                else
                {
                    stream.WritePositiveVIntMax65535(ClampCoordinate(X));
                    stream.WritePositiveVIntMax65535(ClampCoordinate(Y));
                }

                stream.WritePositiveVIntMax65535(EncodeZ);
            }

            int playerCount = GetPlayerCountForEncoding();

            int ownerTeam = TeamIndex == -1 ? playerCount : TeamIndex;
            int ownerPlayer = PlayerIndex == -1 ? playerCount : PlayerIndex;

            // Команда идёт старшей половиной, игрок младшей: именно так индекс
            // собирается при появлении бойца (team * 16 + player), и клиент по
            // нему же ищет свой объект.
            stream.WritePositiveVIntMax255(16 * ownerTeam + ownerPlayer);

            // Измерение, в котором объект находится. Рабочий энкодер пишет сюда
            // постоянный ноль, и отличить одно от другого по его кадру нельзя:
            // в обычном бою измерение и так нулевое.
            stream.WritePositiveIntMax3(DimensionIndex);

            // Снаряд проявления не имеет: он и так живёт считаные кадры.
            if (GetGameObjectType() != LogicGameObjectType.Projectile)
                stream.WritePositiveIntMax15(FadeCounter);

            EncodeBattleHookData(stream);
        }

        /// <summary>Координата зажимается в диапазон, который влезает в беззнаковое поле.</summary>
        private static int ClampCoordinate(int value)
        {
            if (value >= 0xFFFF) return 0xFFFF;
            return value < 0 ? 0 : value;
        }

        /// <summary>
        /// Сколько игроков считать в бою при кодировании владельца. Режим 30
        /// всегда отдаёт тройку независимо от настоящего состава.
        /// </summary>
        protected virtual int GetPlayerCountForEncoding()
        {
            LogicBattleModeServer battle = Manager != null ? Manager.BattleMode : null;
            if (battle == null) return 3;

            return battle.GameMode == ForcedThreePlayerGameMode ? 3 : battle.GetPlayerCount();
        }

        /// <summary>Предмет кодирует позицию номером клетки, а не координатой.</summary>
        protected virtual bool IsAlignedToTiles()
        {
            return false;
        }

        /// <summary>
        /// 0x1004735f0. Объекту разрешено находиться за краем карты. Для снаряда
        /// это колонка IgnoreLevelBorder.
        /// </summary>
        public virtual bool IsAllowedOutsideOfMap()
        {
            return false;
        }

        /// <summary>
        /// LogicK2BattleHookServer::uponEncodeBattleModeData. Два флага, которые
        /// в обычном бою всегда пустые.
        /// </summary>
        protected virtual void EncodeBattleHookData(BitStream stream)
        {
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
        }
    }
}
