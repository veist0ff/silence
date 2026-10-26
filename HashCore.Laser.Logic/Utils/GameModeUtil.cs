namespace HashCore.Laser.Logic.Util
{
    using System.Reflection.Metadata.Ecma335;
    using HashCore.Laser.Logic.Battle;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Server;

    public static class GameModeUtil
    {
        public static bool PlayersCollectPowerCubes(int variation)
        {
            GameModeVariationData data = GetVariationData(variation);
            if (data != null) return data.PlayersCollectPowerCubes;

            int v1 = variation - 6;
            if (v1 <= 8)
                return ((0x119 >> v1) & 1) != 0;
            else
                return false;
        }

        /// <summary>
        /// Королевская битва (game_mode_variations.csv, IsBattleRoyale). Раньше движок
        /// сравнивал вариацию с шестёркой напрямую, из-за чего рогалик (67) шёл по
        /// командной ветке: без рангов, без ядовитой зоны и без условия конца боя.
        /// </summary>
        public static bool IsBattleRoyale(int variation)
        {
            GameModeVariationData data = GetVariationData(variation);
            return data != null ? data.IsBattleRoyale : variation == 6;
        }

        /// <summary>Заполняющий яд по краям карты (ModeHasFillingPoison).</summary>
        public static bool ModeHasFillingPoison(int variation)
        {
            GameModeVariationData data = GetVariationData(variation);
            return data != null ? data.ModeHasFillingPoison : variation == 6;
        }

        public static int GetBattleTicks(int v)
        {
            int v2 = 2400;
            switch (v)
            {
                case 0:
                case 5:
                case 16:
                case 22:
                case 23:
                    v2 = 4200;
                    goto LABEL_9;
                case 3:
                case 7:
                case 8:
                    goto LABEL_9;
                case 6:
                case 9:
                case 10:
                case 12:
                case 13:
                case 18:
                case 20:
                case 67:
                    return BattleMode.NO_TIME_TICKS;
                case 14:
                    v2 = 9600;
                    goto LABEL_9;
                case 17:
                case 21:
                    v2 = 3600;
                    goto LABEL_9;
                default:
                    v2 = 20 * ((BattleMode.NORMAL_TICKS - BattleMode.INTRO_TICKS) / 20);
                LABEL_9:
                    return BattleMode.INTRO_TICKS + v2;
                    break;
            }
        }

        public static int GetRespawnSeconds(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3;
                case 3:
                    return 1;
                case 7:
                    return 3;
                default:
                    return 5;
            }
        }

        public static bool PlayersCollectBountyStars(int variation)
        {
            return variation == 3 || variation == 15;
        }

        /// <summary>Число команд в режиме (game_mode_variations.csv, TeamCount).</summary>
        public static int GetTeamCount(int variation)
        {
            int count = GetVariationData(variation)?.TeamCount ?? 2;
            return count < 1 ? 1 : count;
        }

        /// <summary>Размер команды (game_mode_variations.csv, TeamSize).</summary>
        public static int GetTeamSize(int variation)
        {
            int size = GetVariationData(variation)?.TeamSize ?? 3;
            return size < 1 ? 1 : size;
        }

        public static bool HasTwoTeams(int variation)
        {
            // Было `variation != 6 || variation != 15 || variation != 9` — такое условие
            // истинно для ЛЮБОГО режима, поэтому выживание тоже шло по командной ветке.
            return GetTeamCount(variation) == 2;
        }

        public static bool HasTwoBases(int variation)
        {
            return variation == 2 || variation == 11;
        }

        public static GameModeVariationData GetVariationData(int variation)
        {
            DataTable table = DataTables.Get(DataType.GameModeVariation);
            if (table == null || variation < 0 || variation >= table.Count) return null;
            return table.GetData<GameModeVariationData>(variation);
        }

        public static bool HasTimerAndCanEndBeforeTimerRunsOut(int variation)
        {
            return GetVariationData(variation)?.HasTimerAndCanEndBeforeTimerRunsOut ?? false;
        }

        public static bool RoundResetsWhenObjectiveIsMissing(int variation)
        {
            return GetVariationData(variation)?.RoundResetsWhenObjectiveIsMissing ?? false;
        }

        public static bool ModeHasCarryables(int variation)
        {
            return GetVariationData(variation)?.ModeHasCarryables ?? false;
        }

        public static int GetGameModeVariation(string mode)
        {
            switch (mode)
            {
                case "CoinRush":
                    return 0;
                case "GemGrab":
                    return 0;
                case "Heist":
                    return 2;
                case "BossFight":
                    return 7;
                case "Bounty":
                    return 3;
                case "Artifact":
                    return 4;
                case "LaserBall":
                    return 5;
                case "Showdown":
                    return 6;
                case "BigGame":
                    return 7;
                case "BattleRoyaleTeam":
                    return 9;
                case "Survival":
                    return 8;
                case "Raid":
                    return 10;
                case "RoboWars":
                    return 11;
                case "Tutorial":
                    return 12;
                case "Training":
                    return 13;
                case "LoneStar":
                    return 15;
                case "CTF":
                    return 16;
                case "TagTeam":
                    return 24;
                case "Invasion":
                    return 27;
                case "DeathmatchFFA":
                    return 28;
                case "LastStand":
                    return 29;
                case "ReachExit":
                    return 30;
                case "MapPrint":
                    return 99;
                // game_mode_variations.csv: строка 67, колонка Variation = 67.
                // Пятьдесят четвёртая — это PayloadSingle2v2, ошибка старой таблицы.
                case "Roguelite":
                    return 67;
                case "Godzilla_TownCrush":
                    return 44;    
                case "MegaBoss":
                    return 61; // game_mode_variations.csv, Variation = 61 (было 92)
                case "Arena":
                    return 88;   
                default:
                    // Имена из game_mode_variations.csv (BrawlBall, Knockout, …): номер берём из таблицы.
                    DataTable table = DataTables.Get(DataType.GameModeVariation);
                    if (table != null)
                        foreach (LogicData d in table.GetDatas())
                            if (d is GameModeVariationData g && g.GetName() == mode) return g.Variation;
                    Logger.Error("Wrong game mode! " + mode);
                    return -1;
            }
        }
    }
}
