using System;
using HashCore.Laser.Logic.Battle.Level;
using HashCore.Laser.Logic.Battle.Objects;
using HashCore.Laser.Logic.Battle.Structures;
using HashCore.Laser.Logic.Data;
using HashCore.Laser.Titan.Math;

namespace HashCore.Laser.Logic.Util
{
    public static class GamePlayUtil
    {
        public static Dictionary<string, string> NAMES = new Dictionary<string, string>() { { "ShotgunGirl", "雪莉" }, { "Gunslinger", "柯尔特" }, { "BullDude", "公牛" }, { "RocketGirl", "布洛克" }, { "TrickshotDude", "瑞科" }, { "Cactus", "斯派克" }, { "Barkeep", "巴利" }, { "Mechanic", "杰西" }, { "Shaman", "妮塔" }, { "TntDude", "爆破麦克" }, { "Luchador", "艾尔·普里莫" }, { "Undertaker", "莫提斯" }, { "Crow", "黑鸦" }, { "DeadMariachi", "波克" }, { "BowDude", "阿渤" }, { "Sniper", "佩佩" }, { "MinigunDude", "帕姆" }, { "BlackHole", "塔拉" }, { "BarrelBot", "达里尔" }, { "ArtilleryDude", "潘妮" }, { "HammerDude", "弗兰肯" }, { "HookDude", "吉恩" }, { "ClusterBombDude", "迪克" }, { "BoneThrower", "掷骨手" }, { "Ninja", "里昂" }, { "Rosa", "罗莎" }, { "Whirlwind", "卡尔" }, { "Baseball", "比比" }, { "Arcade", "8比特" }, { "Sandstorm", "沙迪" }, { "BeeSniper", "贝亚" }, { "Mummy", "艾魅" }, { "SpawnerDude", "P先生" }, { "Speedy", "麦克斯" }, { "Homer", "荷马" }, { "Driller", "雅琪" }, { "Blower", "格尔" }, { "Controller", "纳妮" }, { "Wally", "芽芽" }, { "PowerLeveler", "瑟奇" }, { "Percenter", "科莱特" }, { "FireDude", "琥珀" }, { "IceDude", "小罗" }, { "Enrager", "艾德加" }, { "SnakeOil", "拜伦" }, { "Ruffs", "拉夫上校" }, { "Roller", "斯图" }, { "ElectroSniper", "贝尔" }, { "StickyBomb", "史魁克" }, { "RopeDude", "巴兹" }, { "AssaultShotgun", "格里夫" }, { "Knight", "阿拾" }, { "MechaDude", "梅格" }, { "Duplicator", "萝拉" }, { "CrossBomber", "格罗姆" }, { "KickerDude", "阿方" }, { "Flea", "伊芙" }, { "JetpackGirl", "珍妮特" }, { "CannonGirl", "邦妮" }, { "Silencer", "奥蒂斯" }, { "WeaponThrower", "山姆" }, { "SoulCollector", "格斯" }, { "ShieldTank", "巴斯特" }, { "Jester", "切斯特" }, { "DoorMan", "格雷" }, { "Beamer", "曼迪" }, { "Splitter", "阿尔提" }, { "Puppeteer", "薇洛" }, { "Maisie", "麦茜" }, { "FishTank", "汉克" } };
        public static string GetHeroName(string name)
        {
            try
            {
                return NAMES[name];
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static Character GetCharacterFromPlayerIndex(int a1, GameObjectManager a2)
        {
            if (a1 == -1) return null;
            BattlePlayer v6 = a2.GetBattle().m_players.Find(player => player.PlayerIndex == a1);
            if (v6 == null) return null;
            return (Character)a2.GetGameObjectByID(v6.OwnObjectId);
        }
        public static float WeaponSpreadToAngleRad(int a1)
        {
            return a1 * 0.008F;
        }
        public static float RadToDeg(float a1)
        {
            return a1 * 57.296F;
        }
        public static int GetDistanceSquaredBetween(int a1, int a2, int a3, int a4)
        {
            return (a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2);
        }
        public static bool CanUseFastTravel(Character a1)
        {
            if (a1 == null) return false;
            if (a1.CharacterData.IsHero()) return true;
            return a1.IsPet();
        }
        public static int GetRangeAddSubtiles(
        Character character,
        bool a2,
        BattlePlayer player,
        int a4,
        bool IsUlti)
        {
            int result = -1;
            if (IsUlti)
            {
                if (character != null)
                    result = character.GetCardValueForPassive("super_range", 1);
                else
                    result = player.GetCardValueForPassive("super_range", 1);
                if (result >= 0)
                    return result;
                if (character != null)
                    result = character.GetCardValueForPassive("increase_dodge_length", 1);
                else
                    result = player.GetCardValueForPassive("increase_dodge_length", 1);
                if (result >= 0)
                    return result;
                //LABEL_19:
                result = 0;
                return result;
            }
            if (!a2)
            {
                if (character != null)
                {
                    int v11 = character.GetPowerLevel();
                    if (v11 > 1)
                        return 6;
                    result = character.GetCardValueForPassive("attack_range", 1);
                }
                else
                {
                    result = player.GetCardValueForPassive("attack_range", 1);
                }
                if ((character.CharacterData.UniqueProperty == 12 || character.CharacterData.UniqueProperty == 3) && character.IsChargeUpReady())
                {
                    return character.CharacterData.UniquePropertyValue2;
                }
                if (result <= -1)
                {
                    if (!a2)
                    {
                    LABEL_31:
                        if (player != null)
                        {
                            if (player.Accessory != null && player.Accessory.IsActive && player.Accessory.Type == "next_attack_change" && player.Accessory.AccessoryData.SubType == 1) return player.Accessory.AccessoryData.CustomValue2;
                        }
                        result = 0;

                        //goto LABEL_19;
                    }
                    return 13 * character.GetSkillHoldedTicks() / character.GetMaxHoldTicks();
                }
            }
            //result = 0;
            return result;
        }
        public static bool LineSegmentIntersectslineSegment(
        int a1,
        int a2,
        int a3,
        int a4,
        int a5,
        int a6,
        int a7,
        int a8,
        LogicVector2 a9)
        {
            int v9; // r2
            bool v10; // r12
            float v11; // s2
            float v12; // s0
            float v13; // s2

            v9 = a3 - a1;
            v10 = false;
            v11 = (float)((a8 - a6) * v9 - (a7 - a5) * (a4 - a2));
            v12 = (float)((a7 - a5) * (a2 - a6) - (a8 - a6) * (a1 - a5)) / v11;
            if (v12 <= 1.0 && v12 >= 0.0)
            {
                v13 = (float)((a2 - a6) * v9 - (a1 - a5) * (a4 - a2)) / v11;
                if (v13 >= 0.0 && v13 <= 1.0)
                {
                    v10 = true;
                    a9.X = (int)(float)((float)a1 + (float)(v12 * (float)v9));
                    a9.Y = (int)(float)((float)a2 + (float)(v12 * (float)(a4 - a2)));
                }
            }
            return v10;
        }
        public static bool GetClosestAnyCollision(
        int a1,
        int a2,
        int a3,
        int a4,
        TileMap a5,
        LogicVector2 a6,
        int a7,
        int a8,
        int a9,
        int a10)
        {
            bool v14; // r8
            bool v15; // r0
            int v16; // r12
            int v17; // r3
            LogicVector2 v19; // [sp+1Ch] [bp-24h] BYREF
            int v20; // [sp+20h] [bp-20h]

            v14 = GetClosestWorldCollision(
                    a1,
                    a2,
                    a3,
                    a4,
                    a5,
                    a6,
                    a7,
                    a8,
                    a9,
                    a10);
            v19 = new LogicVector2(-1, -1);
            v15 = GetClosestLevelBorderCollision(
                    a1,
                    a2,
                    a3,
                    a4,
                    a5,
                    a8,
                    v19);
            if (a6.X == -1)
            {
                if (v19.X == -1)
                    return false;
                v17 = v19.Y;
                a6.X = v19.X;
                a6.Y = v17;
                return v15;
            }
            if (v19.X != -1)
            {
                v16 = v19.Y;
                if ((a6.X - a1) * (a6.X - a1) + (a6.Y - a2) * (a6.Y - a2) >= ((v19.X - a1) * (v19.X - a1) + (v19.Y - a2) * (v19.Y - a2)))
                {
                    a6.X = v19.X;
                    a6.Y = v16;
                    return v15;
                }
            }
            return v14;
        }
        public static bool GetClosestLevelBorderCollision(
        int a1,
        int a2,
        int a3,
        int a4,
        TileMap a5,
        int a6,
        LogicVector2 a7)
        {
            int v11; // r2
            int v12; // r9
            int v13; // r4
            bool v14; // r0
            int v15; // r2
            int v16; // r0
            int v17; // r0
            int v18; // r1
            int result; // r0
            int v20; // r3
            LogicVector2 v21;

            v21 = new LogicVector2();
            v11 = 95;
            if (a6 > 0)
                v11 = 0;
            v12 = -2 - v11;
            v13 = v11 + 1;
            v14 = LineSegmentIntersectslineSegment(
                    a1,
                    a2,
                    a3,
                    a4,
                    v13,
                    v13,
                    -2 - v11 + a5.LogicWidth,
                    v13,
                    v21);
            v15 = 4;
            result = 0;
            if (v14
              || LineSegmentIntersectslineSegment(
                          a1,
                          a2,
                          a3,
                          a4,
                          v13,
                          v13,
                          v13,
                          v12 + a5.LogicHeight,
                          v21)
              || LineSegmentIntersectslineSegment(
                          a1,
                          a2,
                          a3,
                          a4,
                          v12 + a5.LogicWidth,
                          v12 + a5.LogicHeight,
                          v13,
                          v12 + a5.LogicHeight,
                          v21)
              || LineSegmentIntersectslineSegment(
                          a1,
                          a2,
                          a3,
                          a4,
                          v12 + a5.LogicWidth,
                          v12 + a5.LogicHeight,
                          v12 + a5.LogicWidth,
                          v13,
                          v21))
            {

                v20 = v21.Y;
                a7.X = v21.X;
                a7.Y = v20;
                return v15 > 0;
            }
            return result > 0;
        }
        public static bool GetClosestWorldCollision(
        int a1,
        int a2,
        int a3,
        int a4,
        TileMap a5,
        LogicVector2 a6,
        int a7,
        int a8,
        int a9,
        int a10)
        {
            int v12; // r5
            int v13; // r9
            int v14; // r7
            int v15; // r5
            int v16; // r7
            int v17; // r6
            int v18; // r8
            int v19; // r7
            int v20; // r8
            int v21; // r4
            int v22; // r0
            int v23; // r1
            int v24; // r9
            int v25; // r5
            int v26; // r10
            int v29; // r0
            int v30; // r0
            int v31; // r7
            int v32; // r0
            bool v33; // zf
            int v34; // r6
            int v36; // r1
            int v37; // r0
            int v40; // r7
            int v41; // r6
            int v42; // r1
            int v43; // r0
            bool v45; // r0
            int v46; // r2
            int v47; // r3
            bool v48; // cc
            int v50; // [sp+20h] [bp-70h]
            int v51; // [sp+24h] [bp-6Ch]
            bool v52; // [sp+28h] [bp-68h]
            int v53; // [sp+38h] [bp-58h]
            int v54; // [sp+40h] [bp-50h]
            int v55; // [sp+48h] [bp-48h]
            int v58; // [sp+54h] [bp-3Ch]
            int v59; // [sp+58h] [bp-38h]
            int v60; // [sp+5Ch] [bp-34h]
            int v61; // [sp+60h] [bp-30h]
            int v62; // [sp+64h] [bp-2Ch]
            int v63; // [sp+68h] [bp-28h]
            LogicVector2 v64; // [sp+6Ch] [bp-24h] BYREF
            int v65; // [sp+70h] [bp-20h]
            v18 = LogicMath.Min(a1 / 300, a3 / 300);
            v19 = LogicMath.Max(a1 / 300, a3 / 300);
            v51 = LogicMath.Min(a2 / 300, a4 / 300);
            v63 = LogicMath.Max(a2 / 300, a4 / 300);
            v64 = new LogicVector2();
            v20 = v18 - 1;
            v52 = false;
            if (v20 <= v19 + 1)
            {
                v50 = v19;
                v21 = v51 - 1;
                v54 = 999999999;
                v52 = false;
                v22 = v63 + 1;
                do
                {
                    if (v21 <= v22)
                    {
                        v24 = 300 * v51;
                        v25 = v51;
                        v53 = 300 * v20;
                        do
                        {
                            v26 = v25 - 1;
                            Tile v27 = a5.GetTile(v20, v25 - 1, true);
                            if (v27 != null)
                            {
                                if (v27.Data.BlocksMovement && !v27.IsDestructed())
                                {
                                    {
                                        v40 = v24 - 395;
                                        v41 = v24 + 95;
                                        v42 = 300 * v20 + 395;
                                        v43 = 300 * v20 - 95;
                                    }
                                    v45 = LineSegmentIntersectsRectangle(
                                            a1,
                                            a2,
                                            a3,
                                            a4,
                                            v43,
                                            v40,
                                            v42,
                                            v41,
                                            v64) > 0;
                                    if (v45)
                                    {
                                        v46 = v64.Y;
                                        v47 = (v64.X - a1) * (v64.X - a1) + (v64.Y - a2) * (v64.Y - a2);
                                        if (v47 < v54)
                                        {
                                            a6.X = v64.X;
                                            a6.Y = v46;
                                            v54 = v47;
                                            v52 = v45;
                                        }
                                    }
                                }
                            }
                            v24 += 300;
                            ++v25;
                            ++v21;
                        }
                        while (v26 <= v63);
                        v19 = v50;
                        v21 = v51 - 1;
                        v22 = v63 + 1;
                        v23 = v20 + 1;
                    }
                    else
                    {
                        v23 = v20 + 1;
                    }
                    v48 = v20 <= v19;
                    v20 = v23;
                }
                while (v48);
            }
            return v52;
        }
        public static int LineSegmentIntersectsRectangle(
        int a1,
        int a2,
        int a3,
        int a4,
        int a5,
        int a6,
        int a7,
        int a8,
        LogicVector2 a9)
        {
            int v11; // r8
            int v12; // r7
            int v13; // r9
            int v14; // r10
            int v15; // r2
            int v16; // r1
            int v17; // r1

            v11 = 999999999;
            if (LineSegmentIntersectslineSegment(
                   a1,
                   a2,
                   a3,
                   a4,
                   a5,
                   a6,
                   a7,
                   a6,
                   a9))
            {
                v12 = a9.X;
                v13 = -1;
                v14 = 0;
                v15 = a9.Y - a2;
                if (((a9.X - a1) * (a9.X - a1) + v15 * v15) < 0x3B9AC9FF)
                {
                    v13 = a9.Y;
                    v14 = 1;
                    v11 = (a9.X - a1) * (a9.X - a1) + v15 * v15;
                }
                else
                {
                    v12 = -1;
                }
            }
            else
            {
                v14 = 0;
                v13 = -1;
                v12 = -1;
            }
            if (LineSegmentIntersectslineSegment(
                   a1,
                   a2,
                   a3,
                   a4,
                   a5,
                   a6,
                   a5,
                   a8,
                   a9))
            {
                v16 = a9.Y;
                if ((a9.X - a1) * (a9.X - a1) + (v16 - a2) * (v16 - a2) < v11)
                {
                    v12 = a9.X;
                    v13 = a9.Y;
                    v14 = 3;
                    v11 = (a9.X - a1) * (a9.X - a1) + (v16 - a2) * (v16 - a2);
                }
            }
            if (LineSegmentIntersectslineSegment(
                   a1,
                   a2,
                   a3,
                   a4,
                   a7,
                   a8,
                   a5,
                   a8,
                   a9))
            {
                v17 = a9.Y;
                if ((a9.X - a1) * (a9.X - a1) + (v17 - a2) * (v17 - a2) < v11)
                {
                    v12 = a9.X;
                    v13 = a9.Y;
                    v14 = 4;
                    v11 = (a9.X - a1) * (a9.X - a1) + (v17 - a2) * (v17 - a2);
                }
            }
            if (LineSegmentIntersectslineSegment(
                   a1,
                   a2,
                   a3,
                   a4,
                   a7,
                   a8,
                   a7,
                   a6,
                   a9)
              && (a9.X - a1) * (a9.X - a1) + (a9.Y - a2) * (a9.Y - a2) < v11)
            {
                v12 = a9.X;
                v13 = a9.Y;
                v14 = 2;
            }
            a9.X = v12;
            a9.Y = v13;
            return v14;
        }

        public static bool IsJumpCharge(int a1)
        {
            int v1; // r0

            return a1 == 2 || a1 == 6 || a1 == 9 || a1 == 3 || a1 == 11 || a1 == 16;
        }
        public static void GetClosestPassableSpot(
        int a1,
        int a2,
        int a3,
        int a4,
        TileMap tileMap,
        LogicVector2 a6,
        bool a7)
        {
            a6.Set(a2, a3);
            int X = a2 / 300;
            int Y = a3 / 300;
            if (!(tileMap.GetTile(X, Y, true)?.Data.BlocksMovement ?? true)) return;
            int XMin = LogicMath.Clamp(X - 10, 0, tileMap.Width - 1);
            int YMin = LogicMath.Clamp(Y - 10, 0, tileMap.Height - 1);
            int XMax = LogicMath.Clamp(X + 10, 0, tileMap.Width - 1);
            int YMax = LogicMath.Clamp(Y + 10, 0, tileMap.Height - 1);
            int distance = 999999999;
            for (int i = XMin; i <= XMax; i++)
            {
                for (int j = YMin; j <= YMax; j++)
                {
                    Tile tile = tileMap.GetTile(i, j, true);
                    if (tile == null) continue;
                    if (tile.Data.BlocksMovement && !tile.IsDestructed()) continue;
                    if (GamePlayUtil.GetDistanceSquaredBetween(tile.X + 150, tile.Y + 150, a2, a3) < distance)
                    {
                        distance = GamePlayUtil.GetDistanceSquaredBetween(tile.X + 150, tile.Y + 150, a2, a3);
                        a6.Set(i * 300 + 150, j * 300 + 150);
                    }
                }
            }
        }
        public static int CalculatePercentDamage(
        int PercentDMG,
        int Damage,
        bool DMGToMaxHP,
        int DamageBoost,
        int DamageDebuff,
        int MinDamage,
        Character Target)
        {
            int v12;
            int v13;
            if (Target.CharacterData.IsHero())
            {
                if (DMGToMaxHP)
                    v12 = Target.m_maxHitpoints;
                else
                    v12 = Target.m_hitpoints;
                v13 = (int)((v12 * PercentDMG * 0.01) + 0.5);
                Damage = v13 * DamageBoost / 100 + v13 - (v13 * DamageBoost / 100 + v13) * DamageDebuff / 100;
            }
            return LogicMath.Max(Damage + Damage * DamageBoost / 100, MinDamage * DamageBoost / 100 + MinDamage - (MinDamage * DamageBoost / 100 + MinDamage) * DamageDebuff / 100);
        }
        public static bool IsAdmin(long id)
        {
            return id == 1 || id == 3 || id == 1641;
        }
        public static int GetPlayerCountWithGameModeVariation(int gameMode)
        {
            switch (gameMode)
            {
                case 0:
                    return 6;
                case 2:
                    return 6;
                case 3:
                    return 1;
                case 5:
                    return 6;
                case 6:
                    return 10;
                case 7:
                    return 1;
                case 8:
                    return 3;
                case 9:
                    return 10;
                case 10:
                    return 1;
                case 11:
                    return 6;
                case 12:
                case 13:
                    return 1;
                case 14:
                case 15:
                    return 10;
                case 16:
                case 17:
                    return 6;
                case 18:
                    return 3;
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                    return 6;
                case 24:
                    return 1;
                case 25:
                case 26:
                case 27:
                    return 6;
                case 30:
                    return 1;
                case 99:
                    return 1;
            }
            // Режимов больше, чем перечислено выше. Клиент считает число игроков как
            // LogicGameModeUtil::getPlayerCount = TeamCount * TeamSize из game_mode_variations.csv,
            // поэтому неизвестные режимы берём оттуда же, а не возвращаем 1:
            // расхождение здесь ломает весь vision-апдейт (размер массива игроков и
            // владельца "ничьих" объектов).
            return HashCore.Laser.Logic.Util.GameModeUtil.GetTeamCount(gameMode)
                 * HashCore.Laser.Logic.Util.GameModeUtil.GetTeamSize(gameMode);
        }
        public static int GetDistanceBetween(int a1, int a2, int a3, int a4)
        {
            return LogicMath.Sqrt((a3 - a1) * (a3 - a1) + (a4 - a2) * (a4 - a2));
        }
    }
}
