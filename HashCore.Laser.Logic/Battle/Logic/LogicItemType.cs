namespace HashCore.Laser.Logic.Battle.Logic
{
    using System.Collections.Generic;

    /// <summary>
    /// Вид предмета. Поле данных +84, разобранное в createReferences таблицы
    /// предметов (0x100417cd4) из имени строки.
    ///
    /// Номер тридцать пропущен: имени под него в бинаре нет. По этим номерам
    /// решается в том числе, какие лишние числа предмет дописывает в кадр, так
    /// что имена переписаны буква в букву.
    /// </summary>
    public static class LogicItemType
    {
        public const int Health = 0;
        public const int Speed = 1;
        public const int DamageAndSpeed = 2;

        /// <summary>Кристалл захвата.</summary>
        public const int Point = 3;

        public const int Corpse = 4;

        /// <summary>Источник кристаллов посреди карты.</summary>
        public const int OrbSpawner = 5;

        public const int Mine = 6;
        public const int SupplyCrate = 7;
        public const int BoxOfMines = 8;
        public const int BattleRoyaleBuff = 9;
        public const int Money = 10;
        public const int BoxOfBombs = 11;
        public const int Bomb = 12;
        public const int BoxOfSelfDestructBombs = 13;
        public const int SelfDestructBomb = 14;
        public const int Scrap = 15;
        public const int SpringBoardLeft = 16;
        public const int SpringBoardRight = 17;
        public const int SpringBoardUp = 18;
        public const int SpringBoardDown = 19;
        public const int SpringBoardUpLeft = 20;
        public const int SpringBoardUpRight = 21;
        public const int SpringBoardDownLeft = 22;
        public const int SpringBoardDownRight = 23;
        public const int TrainSpawner = 24;
        public const int KickerDudeMine = 25;
        public const int ScorePole = 26;
        public const int ScoreFlag = 27;
        public const int ScoreFlagHoisted = 28;
        public const int GroundFiller = 29;
        public const int RuffsBuff = 31;
        public const int StickyBombUlti = 32;
        public const int ElectroTrap = 33;
        public const int BasketBrawlHoop = 34;
        public const int Healing = 35;
        public const int WindWall = 36;
        public const int Spray = 37;
        public const int InvasionToken = 38;
        public const int InvasionBase = 39;
        public const int SilencerHugger = 40;
        public const int SilencerHuggerMine = 41;
        public const int Turret = 42;
        public const int WeaponThrowerWeapon = 43;
        public const int SoulCollectorSoul = 44;
        public const int LastStandCoin = 45;
        public const int DoorHorizontal = 46;
        public const int DoorVertical = 47;
        public const int ButtonHold = 48;
        public const int Portal = 49;
        public const int EventCollectToken = 50;
        public const int ShadowMushroom = 51;
        public const int ShadowRealmMarker = 52;
        public const int ConductorSign = 53;
        public const int ExtractionZone = 54;
        public const int Hypercharge = 55;
        public const int GodzillaTransform = 56;
        public const int OverchargedBoxOfBombs = 57;
        public const int OverchargedBomb = 58;
        public const int SamuraiPoint = 59;
        public const int Cash = 60;
        public const int OverchargedMine = 61;
        public const int BoxOfOverchargedMines = 62;
        public const int UNODeck = 63;
        public const int TreasureSpawner = 64;
        public const int Treasure = 65;
        public const int TrailWaypoint = 66;
        public const int BoomBox = 67;
        public const int BulletstormShell = 68;

        private static readonly Dictionary<string, int> ByName = new Dictionary<string, int>
        {
            { "Health", Health }, { "Speed", Speed }, { "DamageAndSpeed", DamageAndSpeed },
            { "Point", Point }, { "Corpse", Corpse }, { "OrbSpawner", OrbSpawner },
            { "Mine", Mine }, { "SupplyCrate", SupplyCrate }, { "BoxOfMines", BoxOfMines },
            { "BattleRoyaleBuff", BattleRoyaleBuff }, { "Money", Money },
            { "BoxOfBombs", BoxOfBombs }, { "Bomb", Bomb },
            { "BoxOfSelfDestructBombs", BoxOfSelfDestructBombs },
            { "SelfDestructBomb", SelfDestructBomb }, { "Scrap", Scrap },
            { "SpringBoardLeft", SpringBoardLeft }, { "SpringBoardRight", SpringBoardRight },
            { "SpringBoardUp", SpringBoardUp }, { "SpringBoardDown", SpringBoardDown },
            { "SpringBoardUpLeft", SpringBoardUpLeft }, { "SpringBoardUpRight", SpringBoardUpRight },
            { "SpringBoardDownLeft", SpringBoardDownLeft }, { "SpringBoardDownRight", SpringBoardDownRight },
            { "TrainSpawner", TrainSpawner }, { "KickerDudeMine", KickerDudeMine },
            { "ScorePole", ScorePole }, { "ScoreFlag", ScoreFlag },
            { "ScoreFlagHoisted", ScoreFlagHoisted }, { "GroundFiller", GroundFiller },
            { "RuffsBuff", RuffsBuff }, { "StickyBombUlti", StickyBombUlti },
            { "ElectroTrap", ElectroTrap }, { "BasketBrawlHoop", BasketBrawlHoop },
            { "Healing", Healing }, { "WindWall", WindWall }, { "Spray", Spray },
            { "InvasionToken", InvasionToken }, { "InvasionBase", InvasionBase },
            { "SilencerHugger", SilencerHugger }, { "SilencerHuggerMine", SilencerHuggerMine },
            { "Turret", Turret }, { "WeaponThrowerWeapon", WeaponThrowerWeapon },
            { "SoulCollectorSoul", SoulCollectorSoul }, { "LastStandCoin", LastStandCoin },
            { "DoorHorizontal", DoorHorizontal }, { "DoorVertical", DoorVertical },
            { "ButtonHold", ButtonHold }, { "Portal", Portal },
            { "EventCollectToken", EventCollectToken }, { "ShadowMushroom", ShadowMushroom },
            { "ShadowRealmMarker", ShadowRealmMarker }, { "ConductorSign", ConductorSign },
            { "ExtractionZone", ExtractionZone }, { "Hypercharge", Hypercharge },
            { "GodzillaTransform", GodzillaTransform },
            { "OverchargedBoxOfBombs", OverchargedBoxOfBombs }, { "OverchargedBomb", OverchargedBomb },
            { "SamuraiPoint", SamuraiPoint }, { "Cash", Cash },
            { "OverchargedMine", OverchargedMine }, { "BoxOfOverchargedMines", BoxOfOverchargedMines },
            { "UNODeck", UNODeck }, { "TreasureSpawner", TreasureSpawner },
            { "Treasure", Treasure }, { "TrailWaypoint", TrailWaypoint },
            { "BoomBox", BoomBox }, { "BulletstormShell", BulletstormShell },
        };

        /// <summary>
        /// Номер по имени. Оригинал сравнивает не с самим именем строки, а с
        /// именем её родителя, поэтому облик предмета вида не меняет.
        /// </summary>
        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return Health;

            int value;
            return ByName.TryGetValue(name, out value) ? value : Health;
        }
    }
}
