namespace HashCore.Laser.Logic.Home.Skins
{
    using System.Collections.Generic;

    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home;
    using HashCore.Laser.Server;

    /// <summary>
    /// Прогрессионные скины: те, что растут по уровням (Astral Colt, Occultist
    /// Dynamike, Tiger Leon и прочие).
    ///
    /// Отдельного поля под уровень у клиента НЕТ. `LogicDailyData::getUnlockedProgressionSkinLevel`
    /// (0x1004c28e0) просто идёт по списку открытых скинов игрока и берёт максимум
    /// `ProgressionSkinLevel` среди тех, у кого `ProgressionSkinBase` совпадает с
    /// искомой базой. То есть «повысить уровень» — это открыть скин следующего
    /// уровня в том же семействе, и больше ничего.
    ///
    /// В skins.csv база — это сам скин первого уровня: у `TntGuyProgressionSkinLevel1`
    /// колонка `ProgressionSkinBase` указывает на него же. Клиент это и проверяет,
    /// падая с «Trying to look up ... using a non-base progression skin», если дать
    /// ему не базу.
    /// </summary>
    public static class ProgressionSkins
    {
        /// <summary>Класс таблицы скинов.</summary>
        public const int SkinClassId = 29;

        /// <summary>
        /// Текущий открытый уровень семейства. Ноль — не открыт ни один.
        /// Считается ровно так же, как считает клиент.
        /// </summary>
        public static int GetUnlockedLevel(ClientHome home, string baseName)
        {
            if (home?.UnlockedSkins == null || string.IsNullOrEmpty(baseName)) return 0;

            DataTable table = DataTables.Get(DataType.Skin);
            if (table == null) return 0;

            int level = 0;

            foreach (int globalId in home.UnlockedSkins)
            {
                SkinData skin = table.GetDataByGlobalId<SkinData>(globalId);
                if (skin == null) continue;
                if (skin.ProgressionSkinBase != baseName) continue;
                if (skin.ProgressionSkinLevel > level) level = skin.ProgressionSkinLevel;
            }

            return level;
        }

        /// <summary>Скин нужного уровня в семействе, или null.</summary>
        public static SkinData FindByLevel(string baseName, int level)
        {
            DataTable table = DataTables.Get(DataType.Skin);
            if (table == null || string.IsNullOrEmpty(baseName)) return null;

            foreach (LogicData data in table.GetDatas())
            {
                if (data is not SkinData skin) continue;
                if (skin.ProgressionSkinBase != baseName) continue;
                if (skin.ProgressionSkinLevel != level) continue;
                return skin;
            }

            return null;
        }

        /// <summary>Самый высокий уровень, который вообще есть у семейства.</summary>
        public static int FindMaxLevel(string baseName)
        {
            DataTable table = DataTables.Get(DataType.Skin);
            if (table == null || string.IsNullOrEmpty(baseName)) return 0;

            int max = 0;

            foreach (LogicData data in table.GetDatas())
            {
                if (data is not SkinData skin) continue;
                if (skin.ProgressionSkinBase != baseName) continue;
                if (skin.ProgressionSkinLevel > max) max = skin.ProgressionSkinLevel;
            }

            return max;
        }

        /// <summary>
        /// Повышает семейство на один уровень: открывает скин следующего уровня.
        /// Возвращает открытый скин или null, если семейства нет либо оно уже
        /// докачано до конца.
        /// </summary>
        public static SkinData LevelUp(HomeMode homeMode, string baseName)
        {
            ClientHome home = homeMode?.Home;
            if (home == null || string.IsNullOrWhiteSpace(baseName)) return null;

            baseName = baseName.Trim();

            SkinData baseSkin = DataTables.Get(DataType.Skin)?.GetData<SkinData>(baseName);
            if (baseSkin == null)
            {
                Logger.Warning($"[progressionSkin] базы «{baseName}» нет в skins.csv");
                return null;
            }

            if (baseSkin.ProgressionSkinBase != baseName)
            {
                Logger.Warning($"[progressionSkin] «{baseName}» не база семейства, база у него «{baseSkin.ProgressionSkinBase}»");
                return null;
            }

            int level = GetUnlockedLevel(home, baseName);
            int max = FindMaxLevel(baseName);

            if (level >= max)
            {
                Logger.Print($"[progressionSkin] «{baseName}» уже на максимуме {max}");
                return null;
            }

            SkinData next = FindByLevel(baseName, level + 1);
            if (next == null)
            {
                Logger.Warning($"[progressionSkin] уровня {level + 1} у «{baseName}» нет");
                return null;
            }

            if (home.UnlockedSkins == null) home.UnlockedSkins = new List<int>();

            int nextGlobalId = next.GetGlobalId();
            if (!home.UnlockedSkins.Contains(nextGlobalId)) home.UnlockedSkins.Add(nextGlobalId);

            Logger.Print($"[progressionSkin] «{baseName}»: уровень {level} -> {next.ProgressionSkinLevel}");
            return next;
        }
    }
}
