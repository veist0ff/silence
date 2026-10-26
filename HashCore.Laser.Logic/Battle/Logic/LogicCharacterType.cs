namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Вид бойца. Колонка Type таблицы бойцов, разобранная в createReferences
    /// (0x1003f1018) в номер, который дальше читает весь бой.
    ///
    /// Номера пять, девять и четырнадцать пропущены: имён под них в бинаре нет.
    /// </summary>
    public static class LogicCharacterType
    {
        public const int Hero = 0;
        public const int NpcBoss = 1;
        public const int MinionFollowOwner = 2;
        public const int MinionFindEnemies = 3;
        public const int MinionBuilding = 4;
        public const int PvpBase = 6;
        public const int MinionBuildingChargesUlti = 7;
        public const int MinionDog = 8;
        public const int LootBox = 10;
        public const int MinionFindEnemies2 = 11;
        public const int RoboWars = 12;
        public const int Train = 13;
        public const int MinionMirage = 15;
        public const int NpcBossTownCrush = 16;
        public const int Carryable = 17;
        public const int MinionDuplicate = 18;
        public const int Payload = 19;
        public const int MinionInvasion = 20;
        public const int MinionLastStand = 21;
        public const int MinionPercenter = 22;
        public const int MinionTwin = 23;
        public const int MinionCritter = 24;
        public const int MinionOrbiting = 25;
        public const int Her0 = 26;
        public const int ArenaMinion = 27;
        public const int ArenaJungle = 28;
        public const int Decoration = 29;
        public const int MegaBoss = 30;

        /// <summary>Незнакомое имя вида. Ни под один разбор такой боец не подойдёт.</summary>
        public const int Unknown = -1;

        private static readonly Dictionary<string, int> ByName = new Dictionary<string, int>
        {
            { "Hero", Hero },
            { "Npc_Boss", NpcBoss },
            { "Minion_FollowOwner", MinionFollowOwner },
            { "Minion_FindEnemies", MinionFindEnemies },
            { "Minion_Building", MinionBuilding },
            { "Pvp_Base", PvpBase },
            { "Minion_Building_charges_ulti", MinionBuildingChargesUlti },
            { "Minion_Dog", MinionDog },
            { "LootBox", LootBox },
            { "Minion_FindEnemies2", MinionFindEnemies2 },
            { "RoboWars", RoboWars },
            { "Train", Train },
            { "Minion_Mirage", MinionMirage },
            { "Npc_Boss_TownCrush", NpcBossTownCrush },
            { "Carryable", Carryable },
            { "Minion_Duplicate", MinionDuplicate },
            { "Payload", Payload },
            { "Minion_Invasion", MinionInvasion },
            { "Minion_LastStand", MinionLastStand },
            { "Minion_Percenter", MinionPercenter },
            { "Minion_Twin", MinionTwin },
            { "Minion_Critter", MinionCritter },
            { "Minion_Orbiting", MinionOrbiting },
            { "Her0", Her0 },
            { "ArenaMinion", ArenaMinion },
            { "ArenaJungle", ArenaJungle },
            { "Decoration", Decoration },
            { "Mega_Boss", MegaBoss },
        };

        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return Unknown;

            int value;
            return ByName.TryGetValue(name, out value) ? value : Unknown;
        }
    }
}
