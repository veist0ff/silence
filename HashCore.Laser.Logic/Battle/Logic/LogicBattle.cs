namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    /// <summary>
    /// Готовый к запуску бой на новом движке.
    ///
    /// Это первая точка, где всё собирается вместе: карта грузится из таблицы,
    /// бойцы появляются на своих точках, кадр крутится по восстановленному из
    /// бинаря порядку, и состояние кодируется в поток для каждого игрока.
    /// </summary>
    public class LogicBattle : LogicBattleModeServer
    {
        /// <summary>Сколько тиков длится вступление, пока бойцы стоят на местах.</summary>
        public int IntroTicks = 60;

        /// <summary>Модификаторы, включённые в этом бою.</summary>
        public readonly List<int> EventModifiers = new List<int>();

        /// <summary>Загруженная карта вместе с точками появления.</summary>
        public LogicMapLoader.LoadedMap Map;

        /// <summary>Потоки, в которые кодируется кадр, по одному на игрока.</summary>
        public readonly List<BitStream> PlayerStreams = new List<BitStream>();

        /// <summary>Персонаж каждого игрока.</summary>
        public readonly List<LogicCharacterServer> PlayerCharacters = new List<LogicCharacterServer>();

        /// <summary>Сколько кадров уже закодировано. Нужно для проверок.</summary>
        public int EncodedFrames;

        /// <summary>Размер буфера под кадр одного игрока.</summary>
        public const int PlayerStreamCapacity = 4096;

        public override int GetIntroTicks()
        {
            return IntroTicks;
        }

        public override bool HasEventModifier(int modifier)
        {
            return EventModifiers.Contains(modifier);
        }

        /// <summary>
        /// Готовит бой: грузит карту и ставит зерно случайных чисел.
        /// Возвращает ложь, если карты с таким именем нет.
        /// </summary>
        public bool LoadMap(string mapName)
        {
            LogicMapLoader.LoadedMap loaded = LogicMapLoader.Load(mapName);
            if (loaded == null) return false;

            Map = loaded;
            TileMap = loaded.TileMap;
            return true;
        }

        /// <summary>Готовит бой из готовых строк карты. Удобно для проверок.</summary>
        public void LoadMapRows(string[] rows)
        {
            Map = LogicMapLoader.Load(rows);
            TileMap = Map.TileMap;
        }

        /// <summary>
        /// Добавляет игрока с бойцом и ставит его на точку появления своей команды.
        /// </summary>
        public LogicHeroCharacter AddPlayer(CharacterData characterData, int teamIndex, int playerIndex)
        {
            LogicPlayer player = new LogicPlayer();
            player.TeamIndex = teamIndex;
            player.PlayerIndex = playerIndex;
            // Это номер выбранного бойца, а не номер игрока: у большинства он
            // нулевой, и подставлять сюда номер игрока значит врать клиенту.
            player.HeroIndex = 0;
            player.IsAlive = true;

            Players.Add(player);
            PlayerStreams.Add(new BitStream(PlayerStreamCapacity));

            LogicHeroCharacter character = new LogicHeroCharacter(characterData);
            character.TeamIndex = teamIndex;
            character.PlayerIndex = playerIndex;
            character.Player = player;
            character.OwnerPlayer = player;

            LogicVector2 spawn = Map != null
                ? Map.GetSpawnPoint(teamIndex, playerIndex)
                : new LogicVector2(0, 0);

            character.SetPosition(spawn.X, spawn.Y);

            GameObjectManager.AddGameObject(character);

            player.ControlledCharacterGlobalId = character.GlobalId;
            PlayerCharacters.Add(character);

            return character;
        }

        /// <summary>
        /// Кодирует кадр каждому игроку. Поток у каждого свой: клиент видит бой
        /// своими глазами, и содержимое кадра зависит от того, кто смотрит.
        /// </summary>
        protected override void EncodeState()
        {
            List<LogicGameObjectServer> visible = GameObjectManager.GameObjects;

            for (int i = 0; i < Players.Count && i < PlayerStreams.Count; i++)
            {
                LogicPlayer player = Players[i];
                BitStream stream = PlayerStreams[i];

                GameObjectManager.Encode(
                    stream,
                    TileMap,
                    player.ControlledCharacterGlobalId,
                    player.PlayerIndex,
                    player.TeamIndex,
                    GameMode,
                    GetCountdown(),
                    Players,
                    visible);
            }

            EncodedFrames++;
        }

        /// <summary>Обратный отсчёт режима. Пока это просто оставшееся время боя.</summary>
        protected virtual int GetCountdown()
        {
            return 0;
        }

        /// <summary>
        /// Бой кончается, когда в одной из команд не осталось живых. Пока команд
        /// меньше двух, бой не заканчивается — иначе одиночный тест обрывался бы
        /// на первом же кадре.
        /// </summary>
        protected override bool CalculateIsGameOver()
        {
            List<int> aliveTeams = new List<int>();

            for (int i = 0; i < PlayerCharacters.Count; i++)
            {
                LogicCharacterServer character = PlayerCharacters[i];
                if (character == null || !character.IsAlive()) continue;
                if (!aliveTeams.Contains(character.TeamIndex)) aliveTeams.Add(character.TeamIndex);
            }

            return GetTeamCount() >= 2 && aliveTeams.Count <= 1;
        }

        /// <summary>Сколько разных команд участвует в бою.</summary>
        public int GetTeamCount()
        {
            List<int> teams = new List<int>();

            for (int i = 0; i < Players.Count; i++)
            {
                LogicPlayer player = Players[i];
                if (player == null) continue;
                if (!teams.Contains(player.TeamIndex)) teams.Add(player.TeamIndex);
            }

            return teams.Count;
        }

        /// <summary>Находит бойца в таблице по имени.</summary>
        public static CharacterData FindCharacter(string name)
        {
            DataTable table = DataTables.Get(DataType.Character);
            if (table == null) return null;

            foreach (LogicData data in table.GetDatas())
            {
                CharacterData character = data as CharacterData;
                if (character != null && character.Name == name) return character;
            }

            return null;
        }

        /// <summary>Первый боец с типом Hero. Нужен, когда конкретный не важен.</summary>
        public static CharacterData FindAnyHero()
        {
            DataTable table = DataTables.Get(DataType.Character);
            if (table == null) return null;

            foreach (LogicData data in table.GetDatas())
            {
                CharacterData character = data as CharacterData;
                if (character != null && character.IsHero() && character.Hitpoints > 0) return character;
            }

            return null;
        }
    }
}
