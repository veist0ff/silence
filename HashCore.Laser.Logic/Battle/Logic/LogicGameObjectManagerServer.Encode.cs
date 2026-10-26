namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Util;

    /// <summary>
    /// Кодирование состояния боя для клиента, 0x10046f3a0.
    ///
    /// Поток битовый и НЕ выровнен по байтам: один лишний или пропущенный бит
    /// сдвигает всё, что идёт дальше, и клиент рисует карту, но не рисует
    /// персонажей. Поэтому порядок полей здесь неприкосновенен.
    ///
    /// Формат сверен с рабочим энкодером старого движка, который клиент этой
    /// версии принимает: расхождений не найдено. Необязательные блоки, которые
    /// старый энкодер закрывал простым нулём, здесь названы по смыслу из бинаря.
    /// </summary>
    public partial class LogicGameObjectManagerServer
    {
        /// <summary>
        /// Порог ширины карты, после которого номер клетки по X занимает шесть бит
        /// вместо пяти. Из заголовка кадра: узкие карты кодируются экономнее.
        /// </summary>
        public const int WideMapThreshold = 22;

        /// <summary>
        /// 0x10046f3a0. Пишет полный кадр состояния в поток одного игрока.
        /// </summary>
        /// <summary>
        /// Где кончилась каждая часть кадра, в байтах. Нужно только для сверки со
        /// старым энкодером: по этим границам видно, какая часть разошлась.
        /// </summary>
        public static readonly List<string> SectionOffsets = new List<string>();

        /// <summary>
        /// Включает разметку кадра по частям. В бою она не нужна и стоит лишней
        /// работы на каждый кадр, поэтому по умолчанию выключена; включает её
        /// только сверка с рабочим энкодером.
        /// </summary>
        public static bool DebugSections;

        private static void Section(BitStream stream, string name)
        {
            if (DebugSections) SectionOffsets.Add(name + " " + stream.GetWriteOffset());
        }

        public void Encode(
            BitStream stream,
            LogicTileMap map,
            int ownObjectGlobalId,
            int playerIndex,
            int teamIndex,
            int gameModeVariation,
            int countdown,
            IList<LogicPlayer> players,
            IList<LogicGameObjectServer> visibleObjects)
        {
            if (DebugSections) SectionOffsets.Clear();

            EncodeHeader(stream, ownObjectGlobalId, gameModeVariation, countdown);
            Section(stream, "header");

            EncodeMap(stream, map);
            Section(stream, "map");

            EncodePlayers(stream, players, playerIndex, teamIndex, gameModeVariation);
            Section(stream, "players");

            EncodeGameModeTail(stream, players, gameModeVariation);
            Section(stream, "modetail");

            EncodeScores(stream, players, gameModeVariation);
            Section(stream, "scores");

            EncodeObjects(stream, visibleObjects, ownObjectGlobalId, playerIndex, teamIndex);
            Section(stream, "objects");

            // Клиент читает этот флаг после всех объектов (поле +216 в
            // LogicGameObjectManagerClient::decode). В оригинале его возвращает
            // энкодер режима боя, здесь он пишется на месте.
            stream.WriteBoolean(false);

            if (gameModeVariation == 7)
            {
                if (stream.WriteBoolean(true))
                {
                    stream.WritePositiveIntMax32767(10000);
                    stream.WritePositiveIntMax65535(10000);
                }
            }
        }

        /// <summary>
        /// Заголовок кадра. Идентификатор собственного объекта пишется дважды:
        /// первый раз это объект игрока, второй — тот, которым он сейчас управляет.
        /// Они расходятся, когда игрок кем-то одержим или сидит в машине.
        /// </summary>
        private void EncodeHeader(BitStream stream, int ownObjectGlobalId, int gameModeVariation, int countdown)
        {
            stream.WritePositiveIntMax2097151(ownObjectGlobalId);
            stream.WritePositiveIntMax2097151(0); // LogicBattleModeClient+260: не ноль прячет ульту и пины

            // Поле +344 режима боя: список чисел по шесть бит. Пока список пуст,
            // пишется один нулевой бит и ничего больше.
            stream.WriteBoolean(false);

            // Счётчик серии, поле статистики игрока. Ноль означает «не показывать».
            stream.WriteBoolean(false);

            if (gameModeVariation == 0) stream.WritePositiveVIntMax65535(countdown);

            stream.WriteBoolean(false);
            stream.WriteInt(-1, 4);

            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
        }

        /// <summary>
        /// 0x100471fa4. Карта: границы видимой области и состояние клеток.
        ///
        /// Разрушаемость передаётся только для тех клеток, которые вообще могут
        /// её менять; для остальных бит не тратится. Клетка, держащая снаряды,
        /// дополнительно отдаёт один бит своего состояния.
        /// </summary>
        private void EncodeMap(BitStream stream, LogicTileMap map)
        {
            if (map.Width < WideMapThreshold)
            {
                stream.WritePositiveInt(0, 5);
                stream.WritePositiveInt(0, 6);
                stream.WritePositiveInt(map.Width - 1, 5);
            }
            else
            {
                stream.WritePositiveInt(0, 6);
                stream.WritePositiveInt(0, 6);
                stream.WritePositiveInt(map.Width - 1, 6);
            }

            stream.WritePositiveInt(map.Height - 1, 6);

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    LogicTile tile = map.GetTile(x, y);
                    if (tile == null || tile.Data == null) continue;

                    bool destructible = tile.Data.IsDestructible;
                    bool destructibleNormal = tile.Data.IsDestructibleNormalWeapon;
                    bool blocksMovement = tile.Data.BlocksMovement;
                    bool blocksProjectiles = tile.Data.BlocksProjectiles;

                    bool encodesDestruction =
                        destructible
                        || (destructibleNormal && (blocksMovement || blocksProjectiles))
                        || (blocksMovement && blocksProjectiles && !destructible)
                        || tile.Data.RestoreAfterDynamicOverlap;

                    if (encodesDestruction) stream.WriteBoolean(tile.Destroyed);
                    if (blocksProjectiles) stream.WriteBoolean(false);
                }
            }

            // 0x1004720ec. Клетки, появившиеся по ходу боя.
            stream.WritePositiveVIntMax65535OftenZero(0);
            stream.WritePositiveVIntMax65535OftenZero(0);

            // Разлитое топливо и подобные метки на земле.
            stream.WritePositiveVIntMax65535OftenZero(0);
        }

        /// <summary>
        /// Блоки игроков. Свой блок клиент узнаёт не по порядку в массиве, а по
        /// совпадению команды и номера игрока — позиционное сравнение попадает
        /// не в того игрока у второй команды.
        /// </summary>
        private void EncodePlayers(
            BitStream stream, IList<LogicPlayer> players, int playerIndex, int teamIndex, int gameModeVariation)
        {
            for (int i = 0; i < players.Count; i++)
            {
                LogicPlayer player = players[i];

                bool isOwn = player != null && player.TeamIndex == teamIndex && player.PlayerIndex == playerIndex;

                if (isOwn)
                {
                    stream.WritePositiveIntMax4095(player.UltiCharge);
                    stream.WritePositiveIntMax3(0);
                    stream.WritePositiveIntMax4095(player.OverCharge);
                    stream.WritePositiveInt(0, 1);
                    stream.WritePositiveInt(0, 1);
                    stream.WritePositiveVIntMax255OftenZero(0);

                    // Отдача в руку: тип и тик идут следом, только если флаг взведён.
                    stream.WriteBoolean(false);
                }

                stream.WritePositiveIntMax15(player != null ? player.HeroIndex : 0);

                // Необязательная ссылка на данные игрока. Пока флаг ложный, клиент
                // не зовёт uponDecodeOwnerPlayer и не читает ни одного бита дальше.
                stream.WriteBoolean(false);

                stream.WritePositiveIntMax131071(0);
                stream.WriteBoolean(player != null && player.IsAlive);
                stream.WriteBoolean(player != null && player.HasUlti);
                stream.WriteBoolean(false);
                stream.WriteBoolean(false);
                stream.WriteBoolean(false);
                stream.WriteBoolean(false);

                // Клиент читает это поле сразу в трёх ветках: для парного выживания
                // (режимы 6 и 67) и для режимов 39 и 34. Все три сходятся на одном
                // readPositiveIntMax15, поэтому писать его надо для всех четырёх.
                if (gameModeVariation == 6 || gameModeVariation == 67
                    || gameModeVariation == 39 || gameModeVariation == 34)
                    stream.WritePositiveIntMax15(1); // 0 = индекс -1 у клиента, вылет шд

                if (gameModeVariation == 3) stream.WriteBoolean(false);

                stream.WritePositiveVIntMax255OftenZero(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);

                stream.WriteBoolean(false);

                stream.WritePositiveIntMax1(0);
                stream.WriteIntMax16383(0);
                stream.WritePositiveIntMax1048575(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);
                stream.WriteIntMax31(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);
                stream.WriteIntMax31(0);
                stream.WritePositiveIntMax1(0);
                stream.WritePositiveIntMax1(0);
            }

            stream.WritePositiveVIntMax65535OftenZero(0);
            stream.WritePositiveVIntMax255OftenZero(0);

            if (GameModeUtil.HasTimerAndCanEndBeforeTimerRunsOut(gameModeVariation))
                stream.WritePositiveVIntMax65535OftenZero(0);

            if (GameModeUtil.RoundResetsWhenObjectiveIsMissing(gameModeVariation))
            {
                stream.WriteBoolean(false);
                stream.WriteIntMax1(0);
            }
        }

        /// <summary>
        /// Хвост режима. В клиенте это цепочка ВЗАИМОИСКЛЮЧАЮЩИХ веток: каждая
        /// заканчивается переходом на список счёта. Пропустить свою ветку значит
        /// недодать бит и сдвинуть весь остаток кадра.
        /// </summary>
        private void EncodeGameModeTail(BitStream stream, IList<LogicPlayer> players, int gameModeVariation)
        {
            bool hasTwoBases = GameModeUtil.HasTwoBases(gameModeVariation);

            if (gameModeVariation == 34 || gameModeVariation == 6 || gameModeVariation == 67)
            {
                int alive = 0;
                for (int i = 0; i < players.Count; i++)
                    if (players[i] != null && players[i].IsAlive) alive++;

                stream.WritePositiveIntMax15(alive > 15 ? 15 : alive);
            }
            else if (gameModeVariation == 9 || gameModeVariation == 38
                     || gameModeVariation == 44 || gameModeVariation == 59)
            {
                List<int> aliveTeams = new List<int>();

                for (int i = 0; i < players.Count; i++)
                {
                    LogicPlayer player = players[i];
                    if (player == null || !player.IsAlive) continue;
                    if (!aliveTeams.Contains(player.TeamIndex)) aliveTeams.Add(player.TeamIndex);
                }

                stream.WritePositiveIntMax7(aliveTeams.Count > 7 ? 7 : aliveTeams.Count);
            }
            else if (gameModeVariation == 16)
            {
                stream.WritePositiveIntMax8191(0);
                stream.WritePositiveIntMax8191(0);
            }
            else if (gameModeVariation == 8 || hasTwoBases)
            {
                int firstBaseHealth = 0;
                int secondBaseHealth = 0;

                for (int i = 0; i < Characters.Count; i++)
                {
                    LogicCharacterServer character = Characters[i] as LogicCharacterServer;
                    if (character == null || !character.IsAlive() || !character.IsBase()) continue;

                    int percent = character.MaxHealth > 0 ? 100 * character.Health / character.MaxHealth : 0;

                    if (character.TeamIndex == 0) firstBaseHealth = percent;
                    else secondBaseHealth = percent;
                }

                // Первое число пишется всегда, второе — только когда баз две.
                stream.WritePositiveIntMax127(firstBaseHealth);
                if (hasTwoBases) stream.WritePositiveIntMax127(secondBaseHealth);
            }
        }

        /// <summary>
        /// Счёт и список убийств. Ширина счёта своя на каждый режим — там, где
        /// счёт может быть большим, под него отводится больше бит.
        /// </summary>
        private void EncodeScores(BitStream stream, IList<LogicPlayer> players, int gameModeVariation)
        {
            for (int i = 0; i < players.Count; i++)
            {
                LogicPlayer player = players[i];
                int score = player != null ? player.Score : 0;

                if (stream.WriteBoolean(score != 0))
                {
                    switch (gameModeVariation)
                    {
                        case 14:
                            stream.WritePositiveIntMax524287(Clamp(score, 0, 0x7FFFF));
                            break;

                        case 15:
                            stream.WritePositiveIntMax134217727(Clamp(score, 0, 0x7FFFFFF));
                            break;

                        case 28:
                        case 39:
                        case 64:
                            stream.WritePositiveIntMax15(Clamp(score, 0, 15));
                            stream.WritePositiveIntMax8191(0);
                            break;

                        case 36:
                            stream.WritePositiveIntMax1023(Clamp(score, 0, 1023));
                            break;

                        default:
                            stream.WritePositiveVIntMax255(score);
                            break;
                    }
                }

                int kills = player != null ? player.Kills.Count : 0;

                if (stream.WriteBoolean(kills > 0))
                {
                    stream.WritePositiveIntMax15(kills);

                    for (int j = 0; j < kills; j++)
                    {
                        stream.WritePositiveIntMax15(player.Kills[j].PlayerIndex);
                        stream.WriteIntMax7(player.Kills[j].BountyStarsEarned);
                    }
                }
            }
        }

        /// <summary>
        /// Объекты. Сначала клиент создаёт или переиспользует их по ссылке на
        /// данные и бегущему номеру, и только потом читает состояние — в том же
        /// порядке, в каком объекты перечислены.
        /// </summary>
        private void EncodeObjects(
            BitStream stream, IList<LogicGameObjectServer> objects,
            int ownObjectGlobalId, int playerIndex, int teamIndex)
        {
            stream.WritePositiveVIntMax65535(objects.Count);

            for (int i = 0; i < objects.Count; i++)
                ByteStreamHelper.WriteDataReference(stream, objects[i].GetDataGlobalId());

            for (int i = 0; i < objects.Count; i++)
                stream.WritePositiveVIntMax65535(GlobalId.GetInstanceId(objects[i].GlobalId));

            stream.WriteBoolean(false);

            int ownObjectIndex = teamIndex * 16 + playerIndex;

            Section(stream, "objstart");

            for (int i = 0; i < objects.Count; i++)
            {
                LogicGameObjectServer gameObject = objects[i];
                gameObject.Encode(stream, gameObject.GlobalId == ownObjectGlobalId, ownObjectIndex, teamIndex);

                Section(stream, "obj" + i);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
