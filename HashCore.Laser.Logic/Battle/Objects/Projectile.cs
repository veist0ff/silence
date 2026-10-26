namespace HashCore.Laser.Logic.Battle.Objects
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using HashCore.Laser.Logic.Battle.Component;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Home.Items;
    using HashCore.Laser.Logic.Notification;
    using HashCore.Laser.Logic.Util;
     
    using HashCore.Laser.Titan.Math;

    public class Ignoring
    {
        public int GID;
        public int Ticks;
        public Ignoring(int GID, int Ticks)
        {
            this.GID = GID;
            this.Ticks = Ticks;
        }
    }
    public class Projectile : GameObject
    {
        public ProjectileData ProjectileData;
        public ProjectileData SkinProjectileData;
        public bool IsCamera;
        public List<Ignoring> IgnoredTargets;
        public List<Ignoring> TargetHitCounts;
        public List<int> LinkedProjectileGIDs = new List<int>();
        public int TicksGone => GameObjectManager.GetBattle().GetTicksGone();
        public int MapHeight => GameObjectManager.GetBattle().GetTileMap().LogicHeight;
        public int MapWidth => GameObjectManager.GetBattle().GetTileMap().LogicWidth;
        public int NormalDMG;
        public int DeployedTick;
        public int EndingTick;
        public int StartX;
        public int StartY;
        public LogicVector2 Pos;
        public int TargetX;
        public int TargetY;
        public int NowX;
        public int NowY;
        public int PrevX;
        public int PrevY;
        public int Healing;
        public int SkillType;
        public bool OnDeploy;
        public int DefaultZ;
        public int EarlyTicks;
        public int LengthDelta;
        public int Curving;
        private Character Owner;
        public int Angle;
        public int Damage;
        public int PercentDamage;
        public int SpeedModifier;
        public int State;
        public int OrginalEndX;
        public int OrginalEndY;
        public bool ShouldDestructImmediately;
        public int TriggerDelay;
        public int dword148;
        public int dword14C;
        public int IndexForTP3;
        public bool RotatedForTP6;
        public int SteerAngle;
        public Character HomingTarget;
        public bool SpecialEffect;
        public int BounceTimes;
        public int BounceDirection;
        public int DamageAddFromBounce;
        public int BounceTileIndex;
        public int CripplePercent;
        public bool Chained;
        public bool CanPierceCharacter;
        public bool AreaEffectCantPushback;
        public AreaEffectData AttackSpecialParams_AreaEffectData;
        public int AttackSpecialParams_Stun;
        public int AttackSpecialParams_WeaklyStun;
        public int AttackSpecialParams_BounceHeal;
        public int AttackSpecialParams_StealBulletPercent;
        public int AttackSpecialParams_StealBulletPercentSelf;
        public int AttackSpecialParams_DamageDebuff;
        public int AttackSpecialParams_DamageBuff;
        public int AttackSpecialParams_StaticPercentDamage;
        public int AttackSpecialParams_MinPercentDamage;
        public string AttackSpecialParams_TrailArea;
        public int AttackSpecialParams_TrailAreaInterval;
        public int AttackSpecialParams_Pushback;
        public int HealOthersAreaEffect;
        public int aoe_dot;
        public int TrailX;
        public int TrailY;
        public int StealSouls2Buff;
        public int StealSouls2BuffTicks;
        public int DisplayScale;
        public bool ChargedShotHit;
        public int BelleWeaponBounces;
        public ItemData SpawnedItem;

        private LogicVector2 TargetPosition;

        private int DestroyedTicks;

        public int SpeedBuffModifier;
        public int SpeedBuffLength;

        public int PreyOnTheWeakDamage;
        public int PreyOnTheWeakPercent;

        public CharacterData SummonedCharacter;
        public int NumSpawns;
        public int NumSpawnsItem;
        public int MaxSpawns;
        public int MaxSpawnsItem;
        public int SpawnDamage;
        public int SpawnedItemDamage;
        public int SpawnHealth;
        public bool SpawnStarPowerIC;

        public int FreezeStrength;
        public int FreezeDuration;

        public int MaxRange;
        public int Speed => ProjectileData.Speed + SpeedModifier;
        public List<Character> PassedEnrager;

        public Projectile(ProjectileData data) : base(data)
        {
            ProjectileData = data;
            SkinProjectileData = data;
            if (!string.IsNullOrEmpty(ProjectileData.ParentProjectileForSkin)) // пустая колонка = "", не null
            {
                // Родителя может не оказаться в таблице (серверный projectiles.csv не совпадает
                // с клиентским) — тогда остаёмся на исходных данных, а не роняем весь бой NRE.
                ProjectileData parent = DataTables.Get(6).GetData<ProjectileData>(ProjectileData.ParentProjectileForSkin);
                if (parent != null)
                {
                    ProjectileData = parent;
                }
            }
            FullTravelTicks = -1;
            Z = 500;
            Scale = ProjectileData.Scale;
            if (ProjectileData.ScaleStart != 0)
            {
                Scale = ProjectileData.ScaleStart;
            }
            LinkedProjectileGIDs = new List<int>();

            TargetPosition = new LogicVector2();
            IgnoredTargets = new List<Ignoring>();
            TargetHitCounts = new List<Ignoring>();

            PassedEnrager = new List<Character>();
            StartX = 0;
            StartY = 0;
            TargetX = 0;
            TargetY = 0;
            EarlyTicks = 0;
            Pos = new LogicVector2();
            DestroyedTicks = 0;
            OnDeploy = true;
            SteerAngle = -1;
            BounceTileIndex = -1;
        }
        public void SetStealSouls2BuffingData(int buff, int ticks)
        {
            StealSouls2Buff = buff;
            StealSouls2BuffTicks = ticks;
        }
        public void SetPreyOnTheWeak(int Percent, int Damage)
        {
            PreyOnTheWeakPercent = Percent;
            PreyOnTheWeakDamage = Damage;
        }
        public void SetCharacterSummonProjectileData(
        CharacterData a2,
        int a3,
        int a4,
        int a5,
        int a6,
        bool StarPowerIC)
        {
            SummonedCharacter = a2;
            NumSpawns = a3;
            MaxSpawns = a4;
            SpawnDamage = a5;
            SpawnHealth = a6;
            SpawnStarPowerIC = StarPowerIC;
        }
        public void SetItemSummonProjectileData(ItemData a2, int NumSpawns, int MaxSpawns, int Damage, int NormalDMG)
        {
            SpawnedItem = a2;
            NumSpawnsItem = NumSpawns;
            MaxSpawnsItem = MaxSpawns;
            SpawnedItemDamage = Damage;
            this.NormalDMG = NormalDMG;
        }
        public void AddIgnoredTarget(int a2, int a3)
        {
            if (LinkedProjectileGIDs.Count >= 1)
            {
                foreach (int id in LinkedProjectileGIDs)
                {
                    Projectile result = (Projectile)GameObjectManager.GetGameObjectByID(id);
                    if (result != null)
                    {
                        result.IgnoredTargets.Add(new Ignoring(a2, a3));
                        //Debugger.Print("id: " + id + " this id: " + GetGlobalID());
                    }
                }
            }
        }
        public void SetFreeze(int a2, int a3)
        {
            FreezeDuration = a2;
            FreezeStrength = a3;
        }
        public int GetModifiedDamage(int a2, bool a3, int a4)
        {
            int v8; // r0
            int started; // r7
            int v10; // r0
            int v11; // r0
            bool v12; // zf
            int v13; // r4
            int v14; // r0
            int v15; // r0
            long v16; // r1
            long v17; // r0
            int v18; // r0
            int v19; // r12
            int v27; // r0
            int v28; // r0
            int v29; // r4

            started = ProjectileData.GetDamagePercentStart();
            v11 = ProjectileData.GetDamagePercentEnd();
            v12 = started == 100;
            v13 = v11;
            if (started == 100)
                v12 = v11 == 100;
            if (!v12)
            {
                v14 = -150;
                if (v13 > started)
                    v14 = 150;
                v15 = LogicMath.Clamp(TotalDelta + v14, 0, 1000);
                v16 = v13 * a2 * v15;
                v17 = (351843721L * started * a2 * (1000 - v15)) >> 32;
                a2 = (int)((v17 >> 13) + (v17 >> 31) + v16 / 100000);
            }
            if (!a3)
            {
                v18 = GetCardValueForPassiveFromPlayer("increase_dmg_consecutive_dot", 1);
                if ((v18 | a4) >= 0)
                {
                    v19 = 0;
                    foreach (Ignoring ignoring in TargetHitCounts)
                    {
                        if (ignoring.GID == a4)
                        {
                            v19 = ignoring.Ticks - 1;
                            break;
                        }
                    }
                    a2 = a2 * (100 + v19 * v18) / 100;
                }
            }
            v28 = GetCardValueForPassiveFromPlayer("increase_missile_dmg_over_distance", 1);
            if (v28 >= 1)
            {
                v29 = v28;
                if (SkillType == 2)
                {
                    a2 += LogicMath.Clamp(TotalDelta, 0, 1000) * v29 / 1000;
                }
            }
            v28 = GetCardValueForPassiveFromPlayer("grow_damage_shrapnel", 1);
            if (v28 >= 1 && SkillType == 1 && Owner.WeaponSkillData.Projectile != ProjectileData.Name)
            {
                started = 100;
                v13 = 100 + GetCardValueForPassiveFromPlayer("grow_damage_shrapnel", 1);
                v14 = -150;
                if (TotalDelta >= 800) v14 = 0;
                v15 = LogicMath.Clamp(TotalDelta + v14, 0, 1000);
                v16 = v13 * a2 * v15;
                v17 = (351843721L * started * a2 * (1000 - v15)) >> 32;
                a2 = (int)((v17 >> 13) + (v17 >> 31) + v16 / 100000);
            }
            //Debugger.Print(a2.ToString());
            return a2;
        }
        public int GetNextSteeredPos(LogicVector2 a2)
        {
            int v4; // r8
            int v5; // r0
            int v6; // r5
            int v8; // r6
            int v9; // r9 MAPDST
            int v10; // r5
            int v11; // r9
            Character v15; // r7
            int v16; // r10
            Character v17; // r9
            int v18; // r0
            int v19; // r0
            int v20; // r0
            int v21; // r1
            int v22; // r5
            int v23; // r5
            int v24; // r5
            int v25; // r6
            int v28; // r0
            int v29; // r6
            int v30; // r5
            int v31; // r0
            int v32; // r0
            int v33; // r5
            int v34; // r5
            int v35; // r0
            int v39; // [sp+Ch] [bp-3Ch]
            int v40; // [sp+10h] [bp-38h]
            int v41; // [sp+14h] [bp-34h]
            int v42; // [sp+18h] [bp-30h]

            v6 = TicksGone;
            v8 = ProjectileData.SteerIgnoreTicks;
            v9 = ProjectileData.Speed;
            v10 = EarlyTicks + v6;
            if (v10 - DeployedTick <= v8)
            {
                v22 = Angle;
                goto LABEL_24;
            }
            if (ProjectileData.TravelType == 4)
            {
                if (v10 - DeployedTick > v8)
                {
                    v11 = ProjectileData.HomeDistance;
                    v15 = null;
                    v16 = 0;
                    v42 = 300 * v11 * 300 * v11;
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        v17 = (Character)gameObject;
                        if (v17.IsAlive())
                        {
                            v18 = v17.GetIndex() / 16;
                            if (v18 != GetIndex() / 16
                              && (v18 != -1 || !v17.IsObject())
                              && (!v17.IsInvisible || v17.YellowEye))
                            {
                                v41 = GetX();
                                v40 = GetY();
                                v39 = v17.GetX();
                                v19 = v17.GetY();
                                v20 = GamePlayUtil.GetDistanceSquaredBetween(v41, v40, v39, v19);
                                v21 = v42;
                                if (v20 < v42)
                                {
                                    v15 = v17;
                                    v21 = v20;
                                }
                                v42 = v21;
                            }
                        }

                    }
                    v22 = Angle;
                    if (v15 != null)
                    {
                        v23 = v15.GetX();
                        v24 = v23 - GetX();
                        v25 = v15.GetY();
                        v22 = LogicMath.GetAngle(v24, v25 - GetY());
                    }
                    goto LABEL_24;

                }
            }
            if (ProjectileData.TravelType != 5)
            {
                v22 = Angle;
                goto LABEL_24;
            }
            v22 = Angle;
            if (SteerAngle > -1)
                v22 = SteerAngle;
            LABEL_24:
            if (ProjectileData.TravelType == 5)
            {
                v28 = ProjectileData.Speed;
                v9 = (int)((float)((float)v28 * 0.4F) + (double)v28 * 0.6 * (float)((float)TotalDelta * 0.001F));
            }
            v29 = -1;
            if (LogicMath.NormalizeAngle360(v22 - Angle) < 180)
                v29 = 1;
            v30 = LogicMath.GetAngleBetween(Angle, v22);
            v31 = ProjectileData.SteerStrength;
            v32 = LogicMath.Min(v30, v31);
            Angle = LogicMath.NormalizeAngle360(Angle + v32 * v29);
            v33 = GetX();
            a2.X = v33 + ((LogicMath.Cos(Angle) * v9 / 20) >> 10);
            v34 = GetY();
            v35 = (int)((1717986919L * LogicMath.Sin(Angle) * v9) >> 32);
            a2.Y = v34 + (((v35 >> 3) + (v35 >> 31)) >> 10);
            return DefaultZ;
        }
        public int GetTravelTicks(int a2, int a3)
        {
            int v5; // r1
            int v6; // r0
            int v7; // r0

            v5 = LogicMath.Max(1, a3);
            v6 = 18000;
            if (!ProjectileData.ConstantFlyTime)
                v6 = 20 * a2;
            v7 = (int)(v6 / (float)v5 + 0.5);
            return LogicMath.Max(1, v7);
        }
        private int TotalDelta;
        public static int GetSpreadMax(ProjectileData ProjectileData, int a2, int a3)
        {
            int v5; // r0
            int v6; // r5

            v5 = LogicMath.Abs(a2);
            v6 = v5;
            if (ProjectileData.ConstantFlyTime)
                return 3 * v5;
            if (ProjectileData.Indirect)
            {
                if (ProjectileData.MinDistanceForSpread > a3)
                    a3 = ProjectileData.MinDistanceForSpread;
            }
            return a3 * v6 / 300;
        }
        public static Projectile ShootProjectile(
        int a1,
        int a2,
        Character a3,
        GameObject a4,
        ProjectileData a5,
        int a6,
        int a7,
        int a8,
        int a9,
        int a10,
        bool a11,
        int a12,
        BattleMode a13,
        int a14,
        int a15)
        {
            int v16; // r6
            int v17; // r0
            Projectile v18; // r0 MAPDST
            int v19; // r5
            int v20; // r4
            int v21; // r9
            int v22; // r8
            int v23; // r8
            int v24; // r7
            int v25; // r10
            int v26; // r4
            int v27; // r7
            int v28; // r6
            int v29; // r5
            int v30; // r7
            int v31; // r6
            int v32; // r9
            int v33; // r5
            int v34; // r0
            int v35; // r0
            int v36; // r6
            int v37; // r4
            int v40; // r0
            int v41; // r1
            int v42; // r3
            int v43; // r5
            int v44; // r4
            int v45; // r0
            int v46; // r0
            int v48; // r7
            int v49; // r5
            int v52; // [sp+Ch] [bp-64h]
            int v53; // [sp+10h] [bp-60h]
            int v54; // [sp+2Ch] [bp-44h]
            int v55; // [sp+2Ch] [bp-44h]
            int v56; // [sp+30h] [bp-40h]
            int v61; // [sp+40h] [bp-30h]

            v16 = a7;
            v61 = a13.GetTicksGone();
            v18 = new Projectile(a5);
            v18.SetIndex(a4.GetIndex());
            v56 = a4.GetX();
            v19 = a6 - v56;
            v20 = a4.GetY();
            v21 = a7 - v20;
            v22 = LogicMath.Sqrt(v19 * v19 + v21 * v21);
            if (a5.TravelType == 6)
            {
                int v58 = v22;
                v23 = a6;
                v24 = a7;
                v25 = a2;
                v54 = v20;
                v26 = 0;
                if (a14 != 1)
                {
                    v27 = LogicMath.Cos(a10 / 2);
                    v28 = LogicMath.GetRotatedX(v19, v21, a10 / 2);
                    v29 = LogicMath.GetRotatedY(v19, v21, a10 / 2);
                    v30 = (int)(2 * (((v58 + (v58 >> 31)) << 9) & 0xFFFFFC00) / v27);
                    v31 = v30 * v28 / v58;
                    v32 = v30 * a5.Speed;
                    v23 = v31 + v56;
                    v16 = a7;
                    v24 = v30 * v29 / v58 + v54;
                    v33 = v32 / v58;
                    v26 = v33 - a5.Speed;
                    v25 = a2;
                }
                goto LABEL_19;
            }
            v34 = GetSpreadMax(a5, a10, v22);
            if (v34 < 3)
            {
                v16 = a7;
                v23 = a6;
                v24 = a7;
            LABEL_18:
                v25 = a2;
                v26 = 0;
                goto LABEL_19;
            }
            v35 = v34;
            v55 = v20;
            int v38, v39, v51, v50;
            if (a11)
            {
                v36 = LogicMath.GetRotatedX(v19, v21, 90);
                v37 = LogicMath.GetRotatedY(v19, v21, 90);
                v38 = LogicMath.Sqrt(v36 * v36 + v37 * v37);
                if (v38 > 0)
                {
                    v39 = v38;
                    v36 = v36 * v35 / v38;
                    v37 = v37 * v35 / v39;
                }
                v40 = a7;
                v42 = a6;
                if (a10 < 0)
                {
                    v36 = -v36;
                    v37 = -v37;
                }
            }
            else
            {
                v36 = 2 * a13.GetLogicRandom().Rand(v34) - (v34 - 1);
                v37 = 2 * a13.GetLogicRandom().Rand(v35) - (v35 - 1);
                v40 = a7;
                v42 = a6;
            }
            v43 = v37 + v40;
            v44 = v36 + v42;
            if (a5.Indirect)
            {
                v23 = LogicMath.Clamp(v44, 1, a13.GetTileMap().LogicWidth - 2);
                v16 = a7;
                v24 = LogicMath.Clamp(v43, 1, a13.GetTileMap().LogicHeight - 2);
                v25 = a2;
                v26 = 0;
                goto LABEL_19;
            }
            v48 = v43 - v55;
            v49 = v44 - v56;
            v50 = LogicMath.Sqrt(v48 * v48 + v49 * v49);
            if (v50 > 0)
            {
                v51 = v50;
                v49 = (v49 * v22 / v50);
                v48 = (v48 * v22 / v51);
            }
            v26 = 0;
            v24 = v48 + v55;
            v23 = v49 + v56;
        LABEL_19:
            a13.GetGameObjectManager().AddGameObject(v18);
            v18.Init(
              a1,
              a2,
              a3,
              a4,
              v23,
              v24,
              0,
              0,
              v61,
              a8,
              a9,
              a12 - v26,
              a15);
            v18.dword148 = a6;
            v18.dword14C = v16;
            v18.IndexForTP3 = a14;
            return v18;
        }
        public static int GetProjectileSpeed(
        Character a1,
        ProjectileData a2,
        bool a3)
        {
            int v5; // r4
            int v6; // r0
            string v7; // r1
            int v8; // r0
            int v9; // r6
            int v10; // r7
            int v11; // r0
            int v12; // r0

            v5 = a2.Speed;
            if (a1 != null)
            {
                v6 = a1.GetCardValueForPassive("projectile_speed", 1);
                if (v6 > 0)
                    v5 += v6;
                if (!a3)
                    v7 = "attack_range";
                else
                    v7 = "super_range";
                v8 = a1.GetCardValueForPassive(v7, 1);
                if (v8 >= 0)
                {
                    v9 = v8;
                    return v5 * a1.RapidFireRange / (a1.RapidFireRange - v9);
                }
            }
            return v5;
        }
        public void Init(
        int X,
        int Y,
        Character a4,
        GameObject a5,
        int TargettX,
        int TargettY,
        int a8,
        int a9,
        int TicksGone,
        int a11,
        int a12,
        int a13,
        int a14)
        {
            int v17; // r0
            int v18; // r1
            int v19; // r7
            int v21; // r0
            int v22; // r6
            int v23; // r10
            int v26; // r9
            int v27; // r5
            int v28; // r0
            int v29; // r0
            int v30; // r0
            int v31; // r0
            int v32; // r0
            int v33; // r10
            int v34; // r0
            int v35; // r6
            int v36; // r0
            int v37; // r5
            int v38; // r0
            int v39; // r6
            int v40; // r8
            int v41; // r7
            int v42; // r0
            int v44; // r0
            int v45; // r0
            int dword74; // r0
            int dword78; // r1
            int v48; // r0
            int v52; // r0
            int result; // r0
            int v54; // [sp+0h] [bp-28h]
            int v55; // [sp+0h] [bp-28h]
            int v58; // [sp+8h] [bp-20h]

            v17 = a5.GetObjectType();
            v18 = a14;
            v19 = 0;
            if (v17 == 0)
                v19 = 200;
            CharacterData v20 = null;
            if (a4 == a5)
            {
                v20 = a4.CharacterData;
                v21 = 50;
                v18 = a14;
                v19 = v21;
            }
            SkillType = v18;
            if (X == -1)
            {
                v22 = TargettX - a5.GetX();
                v23 = TargettY - a5.GetY();
                int v24 = LogicMath.Sqrt(v22 * v22 + v23 * v23);
                if (v24 > 0)
                {
                    v22 = v22 * v19 / v24;
                    v23 = v23 * v19 / v24;
                }
                X = a5.GetX() + v22;
                Y = a5.GetY() + v23;
            }
            v54 = X;
            v26 = a5.GetZ();
            if (v20 != null)
            {
                v26 += v20.ProjectileStartZ;
            }
            else if (0 != 1)
            {
                v26 += 300;
            }
            v27 = Y;
            v58 = v55 = LogicMath.Clamp(v54, 1, MapWidth - 2);
            v55 = LogicMath.Clamp(v27, 1, MapHeight - 2);
            Damage = a11;
            NormalDMG = a12;
            NormalDMG += NormalDMG * ProjectileData.UltiChargeChangePercent / 100;
            DeployedTick = TicksGone;
            v33 = GetProjectileSpeed(a4, ProjectileData, a14 == 2);
            v35 = ProjectileData.SteerLifeTime;
            if (v35 < 1)
            {
                v40 = TargettX;
                v39 = TargettY;
                v41 = v55;
                v42 = LogicMath.Sqrt((TargettX - v58) * (TargettX - v58) + (TargettY - v55) * (TargettY - v55));
                v38 = GetTravelTicks(v42, v33 - a13);
                //Debugger.Print(v42);
            }
            else
            {
                v38 = ProjectileData.SteerLifeTime;
                v39 = TargettY;
                v40 = TargettX;
                v41 = v55;
            }
            v44 = v38 + TicksGone;
            if (ProjectileData.TravelType == 11) v44 += ProjectileData.TravelTypeVariable;
            EndingTick = v44;
            LengthDelta = 1000 / (v44 - DeployedTick);
            SetIndex(a5.GetIndex());
            Owner = a4;
            TargetX = v40;
            TargetY = v39;
            OrginalEndX = v40;
            OrginalEndY = v39;
            //*&gap24[8] = v45;
            SetPosition(v58, v41, v26);
            DefaultZ = v26;
            NowX = v58;
            NowY = v41;
            PrevX = NowX;
            PrevY = NowY;
            StartX = GetX();
            v48 = GetY();
            StartY = v48;
            Angle = LogicMath.GetAngle(TargetX - StartX, TargetY - v48);
            result = ProjectileData.TriggerWithDelayMs;
            if (result > 0)
            {
                result = result / 50 + TicksGone;
                TriggerDelay = result;
            }
        }
        private bool ShouldCheckCollision()
        {
            if (ProjectileData.Indirect || ProjectileData.IsFriendlyHomingMissile) return false;
            else return !ProjectileData.IsHomingMissile;
        }
        public void ExecuteChainBullet(
        int a2,
        int a3,
        Character a4)
        {
            int v6; // r7 MAPDST
            int v7; // r4
            int v9; // r9 MAPDST
            bool v11; // r7
            int v12; // r4
            int v13; // r6
            int v14; // r0
            int v16; // r1
            int v17; // r0
            int v18; // r0
            int v21; // r9
            int v22; // r8
            int v23; // r4
            int v24; // r0
            int v25; // r1
            //CharacterServer* v26; // r2
            int v27; // r0
            int v28; // r10
            int v29; // r0
            int v30; // r10
            int v31; // r0
            Character v32; // r6 MAPDST
            bool v33; // zf
            //__int64 v34; // r0
            int v35; // r4
            int v36; // r0 MAPDST
            int v37; // r7 MAPDST
            int v38; // r4
            int v39; // r6 MAPDST
            int v40; // r4 MAPDST
            int v41; // r4
            int v42; // r9
            int v43; // r4
            int v44; // r6
            int v45; // r9
            int v46; // r7
            int v47; // r9
            int v48; // r7
            //_DWORD* v49; // r4
            int v50; // r6
            //int** v51; // r0
            Projectile v52; // r9
            int i; // r4
            int v54; // r1
            int v55; // r6
            int v56; // r9
            int v57; // r4
            int v58; // r0 MAPDST
            int v60; // r6
            int v61; // r10
            int v62; // r8
            int v63; // r7
            Projectile v65; // r8
            int v66; // r1
            int v67; // [sp+30h] [bp-78h]
            int v70; // [sp+40h] [bp-68h]
            int v71; // [sp+44h] [bp-64h]
            int v72; // [sp+48h] [bp-60h]
            int v73; // [sp+4Ch] [bp-5Ch]
            int v74; // [sp+4Ch] [bp-5Ch]
            int v75; // [sp+50h] [bp-58h]
            int v76; // [sp+50h] [bp-58h]
            int v77; // [sp+54h] [bp-54h]
            int v78; // [sp+54h] [bp-54h]
            int v79; // [sp+54h] [bp-54h]
            int v88; // [sp+64h] [bp-44h]
            int v90; // [sp+6Ch] [bp-3Ch] BYREF
            int v91; // [sp+70h] [bp-38h] BYREF
            int v92; // [sp+74h] [bp-34h] BYREF
            int v93; // [sp+78h] [bp-30h] BYREF
            int v94; // [sp+7Ch] [bp-2Ch] BYREF
            int v95; // [sp+80h] [bp-28h] BYREF
            int v96; // [sp+84h] [bp-24h] BYREF

            v7 = ProjectileData.ChainsToEnemies;
            v9 = ProjectileData.ChainBullets;
            if (GetCardValueForPassiveFromPlayer("attack_chain_count", 1) >= 1)
            {
                v9 = GetCardValueForPassiveFromPlayer("attack_chain_count", 1);
            }
            if (GetCardValueForPassiveFromPlayer("chain_spread", 0) >= 1)
            {
                v9 = GetCardValueForPassiveFromPlayer("chain_spread", 0);
            }
            if (v9 < 1)
            {
                v11 = true;
                if (v7 < 1)
                    return;
            }
            else
            {
                if (Owner != null && Owner.GetPowerLevel() > 2)
                    v9 = 6;
                v11 = v7 > 0;
            }
            if (a4 == null && BounceDirection != 0)
            {
                v12 = GetX();
                v13 = GetY();
                v16 = 5;
                if (ProjectileData.TravelType == 7)
                    v16 = 150;
                switch (BounceDirection)
                {
                    case 4:
                        v13 += v16;
                        break;
                    case 2:
                        v12 += v16;
                        break;
                    case 1:
                        v13 -= v16;
                        break;
                    default:
                        v12 -= v16;
                        break;
                }
                SetPosition(v12, v13, GetZ());
                a4 = null;
            }
            if (v11)
            {
                v22 = 1;
                v77 = 999999999;

                Character c = null;
                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    v32 = (Character)gameObject;
                    if (v32.IsAlive())
                    {
                        if (!(v32 == a4 || v32.GetIndex() / 16 == GetIndex() / 16))
                        {
                            if (!v32.IsImmuneAndBulletsGoThrough(IsInRealm))
                            {
                                if (!v32.CharacterData.IsTrain())
                                {
                                    foreach (Ignoring ignoring in IgnoredTargets)
                                    {
                                        if (ignoring.GID == v32.GetGlobalID()) goto LABEL_40;
                                    }
                                    v27 = v32.GetX();
                                    v28 = (v27 - a2) * (v27 - a2);
                                    v29 = v32.GetY();
                                    v30 = v28 + (v29 - a3) * (v29 - a3);
                                    if (v30 <= 0x35A4E900)
                                    {
                                        if (v30 < v77)
                                        {
                                            if (v22 == 1)
                                            {
                                                v77 = v30;
                                                c = v32;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                LABEL_40:;
                }
                if (v77 == 999999999)
                    return;

                v32 = c;
                v55 = ProjectileData.ChainTravelDistance;
                v56 = v32.GetX() - a2;
                v57 = v32.GetY() - a3;
                v58 = LogicMath.Sqrt(v56 * v56 + v57 * v57);
                if (v58 > 0)
                {
                    v60 = 100 * v55;
                    v56 = v56 * v60 / v58;
                    v57 = v57 * v60 / v58;
                }
                v61 = 0;
                v62 = GetModifiedDamage(Damage, false, -1);
                v63 = GetModifiedDamage(NormalDMG, true, -1);
                v65 = Projectile.ShootProjectile(
                        -1,
                        -1,
                        Owner,
                        this,
                        DataTables.GetProjectileByName(SkinProjectileData.ChainBullet),
                        v56 + a2,
                        v57 + a3,
                        v62,
                        v63,
                        25,
                        false,
                        0,
                        GetBattle(),
                        0,
                        SkillType);
                v65.AttackSpecialParams_AreaEffectData = AttackSpecialParams_AreaEffectData;
                //ZN24LogicAttackSpecialParams12assignValuesERKS_(&v65->field_154, &a1->field_154);
                v65.Chained = true;
                v65.IgnoredTargets = IgnoredTargets.ToList();
                if (a4 != null)
                {
                    v65.IgnoredTargets.Add(new Ignoring(a4.GetGlobalID(), 999999999));
                    return;
                }
            }
            else
            {
                v71 = LogicMath.GetAngle(TargetX - StartX, TargetY - StartY);
                v35 = ProjectileData.ChainTravelDistance;
                v36 = ProjectileData.ChainSpread * 5;
                if (GetCardValueForPassiveFromPlayer("chain_spread", 1) >= 1)
                {
                    v36 = GetCardValueForPassiveFromPlayer("chain_spread", 1) * 5;
                }
                if (v9 >= 2)
                {
                    v67 = 2 * v36 / (v9 - 1);
                }
                else
                {
                    v67 = 0;
                    if (v9 != 1)
                        return;
                }
                v37 = 0;
                v70 = 100 * v35;
                do
                {
                    if (a4 != null)
                        v38 = a4.GetGlobalID();
                    else
                        v38 = -1;
                    v39 = GetModifiedDamage(Damage, false, v38);
                    v40 = GetModifiedDamage(NormalDMG, true, v38);
                    if (ProjectileData.ExecuteChainOnNoHit())
                    {
                        v39 /= v9;
                        v40 /= v9;
                    }
                    if (ProjectileData.TravelType == 7)
                    {
                        v41 = 0;
                        if (v37 <= 5)
                            v41 = new int[] { 90, 270, 60, 120, 240, 300 }[v37];
                        v78 = GetX();
                        v76 = GetY();
                        v42 = GetX();
                        v43 = v41 + v71;
                        v74 = LogicMath.GetRotatedX(v70, 0, v43) + v42;
                        v44 = GetY();
                        v72 = LogicMath.GetRotatedY(v70, 0, v43) + v44;
                        v45 = 0;
                        v46 = v40 / 2;
                        v39 /= 2;
                        //v70 /= 2;//Modified!
                    }
                    else
                    {
                        if (ProjectileData.ChainBullets == 0)
                        {
                            v47 = GetX();
                            v74 = LogicMath.GetRotatedX(v70, 0, v71 + new int[] { 0, 90, 180, 270 }[v37]) + v47;
                            v48 = GetY();
                            v72 = LogicMath.GetRotatedY(v70, 0, v71 + new int[] { 0, 90, 180, 270 }[v37]) + v48;
                            v46 = v40;
                            v45 = 0;
                            v78 = GetX();
                            v76 = GetY();
                        }
                        else
                        {
                            v47 = GetX();
                            v74 = LogicMath.GetRotatedX(v70, 0, v71) + v47;
                            v79 = v37 * v67;
                            v48 = GetY();
                            v72 = LogicMath.GetRotatedY(v70, 0, v71) + v48;
                            v46 = v40;
                            v45 = v79 - v36;
                            v78 = -1;
                            v76 = -1;
                        }
                    }
                    v52 = Projectile.ShootProjectile(
                            v78,
                            v76,
                            Owner,
                            this,
                            DataTables.GetProjectileByName(SkinProjectileData.ChainBullet),
                            v74,
                            v72,
                            v39,
                            v46,
                            v45,
                            true,
                            0,
                            GetBattle(),
                            0,
                            SkillType);
                    v52.AttackSpecialParams_AreaEffectData = AttackSpecialParams_AreaEffectData;

                    //ZN24LogicAttackSpecialParams12assignValuesERKS_(&v52->field_154, &a1->field_154);
                    v52.IgnoredTargets = IgnoredTargets.ToList();
                    if (a4 != null)
                    {
                        v52.IgnoredTargets.Add(new Ignoring(a4.GetGlobalID(), 999999999));
                    }
                    ++v37;
                }
                while (v37 != v9);
            }
            return;
        }
        public override void Tick()
        {
            int v3; // r4
            int v4; // r5
            int v5; // r0
            int v9; // r7
            int v10; // r5
            int v11; // r6
            int v12; // r9
            int v13; // r0
            int v14; // r3
            int v15; // r1
            int v16; // r7
            int v17; // r2
            float v19; // s2
            int v20; // r0
            int v21; // r1
            float v22; // s2
            int v23; // r0
            int v24; // r0
            int v26; // r1
            int v27; // r2
            int v28; // r0
            int v29; // r0
            int v30; // r0
            int v31; // r0
            int v32; // r1
            bool v35; // cc
            int v37; // r5
            int v38; // r2
            int v39; // r2
            int v41; // r10
            bool v42; // zf
            int v43; // r0
            int v44; // r0
            int v45; // r1
            int v46; // r4
            int v47; // r7
            int v48; // r9
            int v49; // r9
            int v50; // r0
            int v51; // r1
            int v53; // r5
            int v54; // r9
            int dword80; // r7
            int v56; // r0
            int dword7C; // r1
            bool v58; // zf
            int v60; // r0
            int v61; // r0
            int v63; // r0
            int v64; // r7
            int v65; // r6
            int v66; // r0
            int v67; // r0
            int v68; // r4
            int v70; // r0
            int v71; // r5
            int v72; // r5
            int v73; // r0
            int v74; // r10
            int v75; // r0
            int v76; // r0
            //int DeployedTick; // r0
            int v78; // kr00_4
            int v83; // r5
            int v84; // r6
            int v85; // r10
            int v86; // r7
            int v87; // r0
            //int TargetY; // r5
            int v89; // r0
            int v90; // r0
            int dword6C; // r5
            int v92; // r4
            int v93; // r0
            int v94; // r5
            int v95; // r0
            int v96; // r0
            int v97; // r1
            int v98; // r0
            int v99; // r5
            int v100; // r4
            int v101; // r7
            int v102; // r0
            int v103; // r4
            int v104; // r5
            int v105; // r10
            int v106; // r6
            int v107; // r7
            int v108; // r9
            int v109; // r0
            int v110; // r12
            int v111; // r3
            int v112; // r5
            int v113; // r4
            int v114; // r1
            int v116; // r1
            bool v117; // zf
            int v118; // r5
            int v119; // r0
            int v120; // r0
            int v121; // r9
            int v122; // r0
            int dword94; // r0
            int dword60; // r1
            int v125; // r3
            int v126; // r0
            int v127; // r0
            int v128; // r0
            int v129; // r5
            int v130; // r0
            int v131; // r5
            int v132; // r0
            int dwordC4; // r7
            int dwordC8; // r1
            int v135; // r3
            //_DWORD* v136; // r4
            int v137; // r0
            int v138; // r2
            int v139; // r5
            int dwordBC; // r2
            //_DWORD* v141; // r6
            int v142; // r5
            int dwordD0; // r2
            int v144; // r6
            //_DWORD* v145; // r2
            int dword1C; // r5
            int v147; // r0
            int v148; // r0
            int v149; // r0
            int dword20; // r5
            int v151; // r0
            int v152; // r0
            int result; // r0
            int v155; // [sp+4h] [bp-34h]
            int v156; // [sp+8h] [bp-30h]
            int v157; // [sp+8h] [bp-30h]
            int v158; // [sp+Ch] [bp-2Ch]
            int v159; // [sp+10h] [bp-28h]
            int v160; // [sp+10h] [bp-28h]
            int v161; // [sp+14h] [bp-24h]
            if (IsCamera) return;
            OnDeploy = false;
            if (State > 0)
            {
                ++DestroyedTicks;
                goto LABEL_106;
            }
            PrevX = NowX;
            PrevY = NowY;
            NowX = GetX();
            NowY = GetY();

            if (Curving >= 1)
            {
                v9 = OrginalEndY - StartY;
                v10 = TotalDelta * Curving / 1000;
                v11 = OrginalEndX - StartX;
                v12 = LogicMath.GetRotatedX(v11, v9, v10);
                v13 = LogicMath.GetRotatedY(v11, v9, v10);
                v14 = OrginalEndX;
                v15 = StartX + v12;
                v16 = OrginalEndY;
                v17 = StartY + v13;
                TargetX = v15;
                TargetY = v17;
                Angle = LogicMath.GetAngle(v15 - GetX(), v17 - GetY());
                //Angle = LogicMath.GetAngle(v15 - v14, v17 - v16);
            }
            if (ProjectileData.TravelType == 1)
            {
                v19 = (TotalDelta * -0.0015F) + 1.5F;
                v20 = OrginalEndY + (int)(v19 * (OrginalEndY - StartY));
                v21 = OrginalEndX + (int)(v19 * (OrginalEndX - StartX));
            LABEL_9:
                TargetX = v21;
                TargetY = v20;
                goto LABEL_10;
            }
            if (ProjectileData.TravelType == 2)
            {
                v22 = 0.5F - TotalDelta * 0.001F * TotalDelta * 0.001F * 0.5F;
                TargetY = OrginalEndY - (int)(v22 * (OrginalEndY - StartY));
                TargetX = OrginalEndX - (int)(v22 * (OrginalEndX - StartX));
            }
            if (ProjectileData.TravelType == 3)
            {
                //if (IndexForTP3 == 6) goto LABEL_10;
                v35 = IndexForTP3 <= 1;
                v37 = OrginalEndX - StartX;
                v38 = LogicMath.Max(0, (IndexForTP3 - 1)) / 2;
                v39 = 100 * v38 + 350;
                v41 = OrginalEndY - StartY;
                if (IndexForTP3 >= 1)
                    v35 = TotalDelta <= v39;
                if (v35)
                {
                    if (TotalDelta < 100)
                        goto LABEL_59;
                    v42 = IndexForTP3 == 0;
                    if (IndexForTP3 != 0)
                        v42 = (v41 | v37) == 0;
                    if (v42)
                        goto LABEL_59;
                    v43 = (180 * (IndexForTP3 % 3) + 180) * TotalDelta / v39;
                    v44 = LogicMath.Sin(v43);
                    //if (IndexForTP3 == 6) v44 = 0;
                    v45 = -300;
                    if ((IndexForTP3 & 1) == 0)
                        v45 = 300;
                    v46 = v45 * v44;
                    v47 = LogicMath.GetRotatedX(v37, v41, 90);
                    v48 = LogicMath.GetRotatedY(v37, v41, 90);
                    v46 >>= 10;
                    v49 = LogicMath.Sqrt(v37 * v37 + v41 * v41);
                    v50 = v46 * v47 / v49;
                    v51 = v46 * v48;
                    v53 = NowX;
                    v54 = NowY;
                    TargetX = v50 + OrginalEndX;
                    v56 = v51 / v49;
                    TargetY = v56 + OrginalEndY;
                    v58 = v53 == PrevX;
                    if (v53 == PrevX)
                        v58 = NowY == PrevY;
                    if (v58)
                        goto LABEL_59;
                    Angle = LogicMath.GetAngle(v53 - PrevX, v54 - PrevY);
                }
                else
                {
                    v98 = TotalDelta - v39;
                    v100 = -25 * v38 + 75;
                    if ((IndexForTP3 & 1) == 0)
                        v100 = -75 - -25 * v38;
                    //v100 = 0;
                    v101 = v39 * v37 / 1000;
                    v102 = v98 * v100;
                    v104 = v102 / (650 - 100 * v38);
                    v105 = LogicMath.Abs(v104);
                    v106 = v41 * v39 / 1000 + StartY;
                    v107 = v101 + StartX;
                    v108 = LogicMath.GetRotatedX(OrginalEndX - v107, OrginalEndY - v106, v104);
                    v109 = LogicMath.GetRotatedY(OrginalEndX - v107, OrginalEndY - v106, v104);
                    v110 = NowX;
                    v111 = NowY;
                    v112 = PrevX;
                    v113 = PrevY;
                    v114 = 100 - 50 * v105 / 180;
                    TargetX = v108 * v114 / 100 + v107;
                    TargetY = v109 * v114 / 100 + v106;
                    Angle = LogicMath.GetAngle(v110 - v112, v111 - v113);
                }

            LABEL_59:
                goto LABEL_10;
            }
            if (ProjectileData.TravelType == 6 && TotalDelta >= 500 && !RotatedForTP6)
            {
                v78 = EndingTick - DeployedTick;
                GetPosAtTick(DeployedTick + v78 / 2, Pos);
                v83 = dword148 - Pos.X;
                StartX = Pos.X;
                StartY = Pos.Y;
                TargetX = dword148;
                TargetY = dword14C;
                v84 = dword14C - Pos.Y;
                v86 = GamePlayUtil.GetDistanceBetween(0, 0, dword148 - Pos.X, dword14C - Pos.Y);
                v87 = 900 * v83 / v86;
                TargetX += v87;
                TargetY = TargetY + 900 * v84 / v86;
                v92 = EarlyTicks + TicksGone;
                DeployedTick = v92 - 1;
                v94 = GetProjectileSpeed(Owner, ProjectileData, false);
                v96 = GetTravelTicks(900, v94);
                RotatedForTP6 = true;
                v97 = v92 + v78 / 2;
                EndingTick = v97 + v96;
            }
        LABEL_10:
            if (ProjectileData.IsFriendlyHomingMissile
              || ProjectileData.IsHomingMissile
              || ProjectileData.IsBoomerang
              && ProjectileData.ChainBullet == null)
            {
                if (HomingTarget != null)
                {
                    TargetX = HomingTarget.GetX();
                    TargetY = HomingTarget.GetY();
                }
            }
            bool v62 = TickMovement();
            v63 = TotalDelta + LengthDelta;
            if (v63 >= 1000)
                v63 = 1000;
            TotalDelta = v63;
            v64 = 0;
            v65 = 1;
            if (!ShouldCheckCollision())
                goto LABEL_82;
            v66 = HandleCollisions();
            v64 = v66;
            if (v66 == 0)
                goto LABEL_82;
            if (v66 != 1)
            {
                v65 = 1;
                v62 = true;
                if (v66 == 2)
                    v65 = 3;
                goto LABEL_82;
            }
            v65 = 4;
            if (BounceTileIndex == -1)
                v65 = 2;
            v64 = 1;
            if (ProjectileData.IsBouncing)
            {
                if (AttackSpecialParams_BounceHeal > 0) Owner?.Heal(GetIndex(), AttackSpecialParams_BounceHeal, true, ProjectileData);
                v156 = TicksGone;
                v68 = DeployedTick;
                v158 = TargetX - StartX;
                v161 = TargetY - StartY;
                v157 = EarlyTicks + v156;
                int v69 = LogicMath.Sqrt(v158 * v158 + v161 * v161);
                v71 = v69 * (1000 - (1000 * (v157 - v68) / (EndingTick - v68))) / 1000;
                if (BounceTimes > 6)
                {
                    v155 = v71;
                }
                else
                {
                    if (ProjectileData.DistanceAddFromBounce < 1)
                    {
                        v155 = v71;
                    }
                    else
                    {
                        v72 = ProjectileData.DistanceAddFromBounce + v71;
                        v155 = v72;
                        v76 = GetTravelTicks(v72, ProjectileData.Speed);
                        v62 = false;
                        EndingTick = v76 + v157;
                    }
                }
                v116 = v161;
                v117 = BounceDirection == 4;
                if (BounceDirection != 4)
                    v117 = BounceDirection == 1;
                if (v117)
                    v116 = -v161;
                else
                    v158 = -v158;
                int v162 = v116;
                if (v69 != 0)
                {
                    v158 = v158 * v155 / v69;
                    v162 = v162 * v155 / v69;
                }
                StartX = GetX();
                v118 = GetY();
                v119 = BounceDirection;
                StartY = v118;
                if (v119 != 4)
                {
                    if (v119 == 2)
                    {
                        v120 = StartX + 5;
                    }
                    else
                    {
                        if (v119 == 1)
                        {
                            v118 -= 5;
                            StartY = v118;
                            v121 = StartX;
                            v122 = GetZ();
                            SetPosition(v121, v118, v122);
                            if (DamageAddFromBounce >= 1)
                            {
                                Damage += DamageAddFromBounce;
                                SpecialEffect = true;
                                DamageAddFromBounce = 0;
                            }
                            v125 = StartY;
                            TargetX = StartX + v158;
                            TargetY = v125 + v162;
                            Angle = LogicMath.GetAngle(v158, v162);
                            v126 = BounceTimes + 1;
                            DeployedTick = v157;
                            BounceTimes = v126;
                            if (ProjectileData.CanHitAgainAfterBounce)
                            {
                                IgnoredTargets.Clear();
                            }
                            goto LABEL_82;
                        }
                        v120 = StartX - 5;
                    }
                    StartX = v120;
                    goto LABEL_78;
                }
                v118 += 5;
                StartY = v118;
            LABEL_78:
                v121 = StartX;
                v122 = GetZ();
                SetPosition(v121, v118, v122);
                if (DamageAddFromBounce >= 1)
                {
                    Damage += DamageAddFromBounce;
                    SpecialEffect = true;
                    DamageAddFromBounce = 0;
                }
                v125 = StartY;
                TargetX = StartX + v158;
                TargetY = v125 + v162;
                Angle = LogicMath.GetAngle(v158, v162);
                v126 = BounceTimes + 1;
                DeployedTick = v157;
                BounceTimes = v126;
                if (ProjectileData.CanHitAgainAfterBounce)
                {
                    IgnoredTargets.Clear();
                }
                goto LABEL_82;
            }
            v62 = true;
        LABEL_82:
            if (TriggerDelay != 0)
            {
                if (EarlyTicks + TicksGone == TriggerDelay) TargetReached(v65);
                //sub_430204(a1, v65);
            }
            else if (v62)
            {
                TargetReached(v65);
                v128 = GetCardValueForPassiveFromPlayer("always_bounce_shot", 1);
                if (v64 == 0)
                {
                    v129 = v128;
                    if (ProjectileData.ChainBullets >= 1)
                    {
                        if (ProjectileData.ExecuteChainOnNoHit() || v129 > -1 || ProjectileData.ExecuteChainAlways())
                        {
                            v131 = GetX();
                            v132 = GetY();
                            ExecuteChainBullet(v131, v132, null);
                        }
                    }
                }
            }
            if (IgnoredTargets.Count >= 1)
            {
                List<Ignoring> rem = new List<Ignoring>();
                foreach (Ignoring ignoring in IgnoredTargets)
                {
                    if (ignoring.Ticks > 1) ignoring.Ticks--;
                    else rem.Add(ignoring);
                }
                foreach (Ignoring ignoring1 in rem) IgnoredTargets.Remove(ignoring1);
            }

        LABEL_106:
            if (ProjectileData.TravelType == 8 || ProjectileData.IgnoreLevelBoarder) return;
            Position.X = LogicMath.Clamp(Position.X, 1, MapWidth - 2);
            Position.Y = LogicMath.Clamp(Position.Y, 1, MapHeight - 2);
        }

        private int FullTravelTicks;

        public void SetTargetPosition(int x, int y)
        {
            TargetPosition.Set(x, y);
        }

        public void SetSummonedCharacter(CharacterData data)
        {
            SummonedCharacter = data;
        }
        public int GetModifiedPushback()
        {
            if (AttackSpecialParams_Pushback != 0) return AttackSpecialParams_Pushback;
            return ProjectileData.PushbackStrength;
        }
        public int GetModifiedScale(int a2)
        {
            int v4; // r0
            int started; // r5
            int v6; // r0
            int v7; // r0
            int v8 = 0; // r1
            bool v9; // zf

            started = ProjectileData.GetScaleStart();
            v7 = ProjectileData.GetScaleEnd();
            v9 = started == 100;
            if (started == 100)
            {
                v8 = 100;
                v9 = v7 == 100;
            }
            if (!v9)
                return v7 * a2 / 1000 + started * (1000 - a2) / 1000;
            return v8;
        }
        public void IncreaseTargetHitCount(int GID)
        {
            foreach (Ignoring ignoring in TargetHitCounts)
            {
                if (ignoring.GID == GID)
                {
                    ignoring.Ticks++;
                    return;
                }
            }
            TargetHitCounts.Add(new Ignoring(GID, 1));
        }
        public int HandleCollisions()
        {
            int v2; // r4
            int v5; // r7
            int v6; // r10
            int v7; // r5
            int v9; // r0
            int v10; // r9
            int v11; // r8
            TileMap v15; // r7
            int v16; // r4
            int v17; // r9
            int v18; // r5
            int v19; // r4
            int v20; // r8
            int v21; // r6
            int v22; // r4
            int v23; // r9
            int v24; // r7
            int v25; // r5
            int v27; // r8
            int v28; // r6
            int v29; // r7
            int v30; // r9
            int v31; // r0
            int v33; // r6
            int v34; // r7
            int v35; // r0
            int v36; // r5
            int v37; // r1
            int v38; // r5
            int v312; // r4
            int v40; // r10
            int v43; // r8
            int v45; // r7
            int v46; // r1
            bool v47; // r0
            bool v48; // r0
            bool v50; // r0
            int v51; // r4
            int v53; // r5
            int v55; // r4
            bool v56; // r8
            int v57; // r0
            int v58; // r10
            bool v59; // r0
            int v60; // r0
            int PosY; // r5
            int PosX; // r7
            bool v63; // zf
            bool v66; // cc
            int v67; // r0
            int v68; // r0
            bool v69; // r0
            bool v73; // r4
            int v74; // r0
            bool v76; // r0
            int v77; // r10
            Character v79; // r5
            Projectile v81; // r7
            bool v82; // r9
            bool v84; // zf
            int v86; // r0
            int v88; // r0
            int v89; // r0
            bool v90; // r0
            Character v91; // r2
            bool v92; // r5
            bool v93; // r4
            int v94; // r0
            int IgnoreTargetCounts; // r1
            int v96; // r3
            int v97; // r5
            int v98; // r7
            int v99; // r8
            int v100; // r5
            int v101; // r0
            int v102; // r0
            int v103; // r1
            int v104; // r5
            int v105; // r4
            int v106; // r7
            int v107; // r5
            int i; // r6
            int v109; // r0
            int v110; // r5
            int v111; // r0
            Character v112; // r8
            int v113; // r5
            int v114; // r6
            int v115; // r6
            int v116; // r8
            int v117; // r0
            int v118; // r6
            int v119; // r0
            int v121; // r0
            int v122; // r5
            int v123; // r6
            int v124; // r0
            int v125; // r3
            int v126; // r0
            int v127; // r0
            int v128; // r1
            int v129; // r7
            int v130; // r2
            Character v131; // r5
            int v132; // r8
            Projectile v133; // r4
            int v134; // r9
            int v136; // r0
            int v137; // r0
            int v138; // r2
            int v140; // r2
            int v141; // r5
            int v142; // r0
            int j; // r6
            int v145; // r4
            int v146; // r8
            int v147; // r5
            Projectile v149; // r5
            int v150; // r0
            int v151; // r0
            int v152; // r0
            int v153; // r5
            int v156; // r0
            int v157; // r0
            int v158; // r0
            int v159; // r0
            int v160; // r0
            int v161; // r0
            int v162; // r0
            int v163; // r0
            int v164; // r4
            int v165; // r0
            int v166; // r0
            int v167; // r0
            int v168; // r1
            int v169; // r0
            int v170 = 1; // r6
            int v171; // r1
            int v172; // r0
            int v173; // r0
            int v174; // r4
            int v175; // r0
            int v176; // r0
            int v177; // r5
            int v178; // r0
            int v179; // r4
            int v180; // r1
            Projectile v181; // r0
            int v182; // r2
            int v183; // r3
            bool v184; // r0
            int v185; // r0
            bool v187; // zf
            int v188; // r0
            int v189; // r4
            int v190; // r0
            int v191; // r0
            int v193; // r0
            int v194; // r1
            int v195; // r2
            int v196; // r0
            int v197; // r5
            int v198; // r0
            int v199; // r8
            int v200; // r5
            int v201; // r0
            int v202; // r10
            int v203; // r0
            int v204; // r1
            int v205; // r0
            Character v206; // r0
            Character v207; // r5
            int v208; // r1
            int v209; // r2
            int v210; // r3
            int v212; // r7
            int v213; // r0
            int v214; // r0
            bool v215; // zf
            int v216; // r7
            int v217; // r5
            int v218; // r4
            int v219; // r7
            int v220; // r0
            int v221; // r0
            int v222; // r5
            int v223; // r0
            AreaEffect v224; // r5
            int v225; // r6
            int v226; // r0
            int v228; // r1
            int v229; // r0
            int v230; // r0
            int v232; // r6
            int v233; // r0
            int v234; // r7
            int v235; // r1
            int v236; // r0
            bool v237; // zf
            int v238; // r5
            int v239; // r0
            int v240; // r0
            int v241; // r2
            bool v242; // zf
            int v243; // r0
            int v244; // r0
            int v245; // r5
            int v246; // r0
            Projectile v247; // r7
            int v248; // r0
            int v249; // r6
            int v250; // r0
            int v251; // r5
            int v252; // r6
            int v253; // r0
            int v256; // [sp+3Ch] [bp-DCh]
            int v257; // [sp+40h] [bp-D8h]
            int v258; // [sp+44h] [bp-D4h]
            int v259; // [sp+48h] [bp-D0h]
            int v260; // [sp+4Ch] [bp-CCh]
            int v263; // [sp+54h] [bp-C4h]
            int v265; // [sp+58h] [bp-C0h]
            bool v267; // [sp+5Ch] [bp-BCh]
            int v268; // [sp+60h] [bp-B8h]
            bool v270; // [sp+6Ch] [bp-ACh]
            int v271; // [sp+70h] [bp-A8h]
            int v272; // [sp+70h] [bp-A8h]
            int v273; // [sp+74h] [bp-A4h]
            int v276; // [sp+80h] [bp-98h]
            int v277; // [sp+84h] [bp-94h]
            int v279; // [sp+8Ch] [bp-8Ch]
            int v280; // [sp+8Ch] [bp-8Ch]
            int v282; // [sp+94h] [bp-84h]
            int v283; // [sp+98h] [bp-80h]
            int v284; // [sp+9Ch] [bp-7Ch]
            bool v286; // [sp+A0h] [bp-78h]
            int v287; // [sp+A0h] [bp-78h]
            bool v288; // [sp+A4h] [bp-74h]
            Character v289; // [sp+A4h] [bp-74h]
            bool v290; // [sp+A8h] [bp-70h]
            int v291; // [sp+A8h] [bp-70h]
            int v292; // [sp+ACh] [bp-6Ch]
            int v293; // [sp+B0h] [bp-68h]
            int v295; // [sp+B4h] [bp-64h]
            int v296; // [sp+B8h] [bp-60h]
            int v297; // [sp+B8h] [bp-60h]
            int v298; // [sp+B8h] [bp-60h] MAPDST
            int v299; // [sp+BCh] [bp-5Ch]
            Character v300; // [sp+BCh] [bp-5Ch]
            int v301; // [sp+C0h] [bp-58h]
            int v302; // [sp+C4h] [bp-54h]
            int v303; // [sp+C4h] [bp-54h]
            int v304; // [sp+C4h] [bp-54h]
            int v305; // [sp+C4h] [bp-54h]
            int v306; // [sp+C8h] [bp-50h]
            int v307; // [sp+CCh] [bp-4Ch]
            int v308; // [sp+CCh] [bp-4Ch]
            int v311; // [sp+D0h] [bp-48h]
            int v313; // [sp+D0h] [bp-48h]
            int v315; // [sp+D4h] [bp-44h]
            int v317; // [sp+D4h] [bp-44h]
            int v319; // [sp+E8h] [bp-30h] BYREF
            int v320; // [sp+ECh] [bp-2Ch] BYREF
            int v321; // [sp+F0h] [bp-28h] BYREF

            //Debugger.Print(Pos.X);
            v5 = GetX();
            v6 = GetY();
            v7 = ProjectileData.Radius;
            v276 = GetModifiedScale(TotalDelta) * v7 / 100;
            v302 = NowX;
            v10 = v5 - NowX;
            v307 = NowY;
            v11 = v6 - NowY;
            v315 = v5;
            v311 = (v276 + 150) / 300 + 1;
            int v13 = LogicMath.Sqrt(v10 * v10 + v11 * v11);
            v296 = (v276 + 150) / 300;
            if (v13 > 0)
            {
                v10 = 20 * v10 / v13;
                v11 = 20 * v11 / v13;
            }
            v15 = GameObjectManager.GetBattle().GetTileMap();
            v16 = LogicMath.Clamp(v302, 1, MapWidth - 2);
            v292 = LogicMath.Clamp(v307, 1, MapHeight - 2);
            v17 = LogicMath.Clamp(v10 + v315, 1, MapWidth - 2);
            v18 = LogicMath.Clamp(v11 + v6, 1, MapHeight - 2);
            Pos.X = 0;
            Pos.Y = 0;
            v295 = v16;
            v19 = (v16) / 100;
            v20 = (v292) / 100;
            v282 = v17;
            v21 = v19 / 3;
            v22 = v17 / 100;
            v283 = v18;
            v23 = (v18) / 100;
            v24 = LogicMath.Min(v21, v22 / 3) - v311;
            v25 = LogicMath.Max(v21, v22 / 3) + v311;
            int v26 = v20 / 3;
            v27 = v24;
            v28 = v26 + (v26 >> 31);
            v29 = v23 / 3;
            v30 = LogicMath.Min(v28, v29);
            v31 = LogicMath.Max(v28, v29);
            LogicVector2 v269 = new LogicVector2();
            v269.X = -1;
            v277 = 999999999;
            v263 = v25;
            int v274, v275;
            if (v27 > v25)
            {
                v286 = false;
                v269.Y = -1;
                v275 = -1;
                v274 = -1;
                v271 = -1;
                v268 = -1;
                v270 = false;
                goto LABEL_74;
            }
            v33 = v30 - v311;
            v34 = v31 + v311;
            v260 = v30 - v311;
            v279 = v31 + v311;
            v259 = 300 * v30 - 300 * v296;
            v258 = 300 * v296 + 150 - 300 * v30;
            v35 = v30 - 2;
            v257 = v35 - v296;
            v36 = v292;
            v267 = false;
            v269.X = -1;
            v269.Y = -1;
            v275 = -1;
            v274 = -1;
            v271 = -1;
            v268 = -1;
            v270 = false;
            do
            {
                if (v33 > v34)
                {
                    v37 = v27 + 1;
                    goto LABEL_65;
                }
                v38 = v258;
                v312 = v257;
                v303 = v27;
                v287 = 300 * v27;
                v265 = 300 * v27 + 150;
                v293 = v27 + 1;
                v284 = v27 - 1;
                v308 = v259;
                do
                {
                    v40 = v312 + 1;

                    Tile v41 = v15.GetTile(v27, v312 + 1, true);
                    if (v41 == null || v41.IsDestructed())
                        goto LABEL_63;
                    if ((!v41.Data.BlocksProjectiles || ProjectileData.PassesEnvironment)
        && (!ProjectileData.PiercesEnvironment
         || !v41.Data.IsDestructible)
        && (!v41.Data.IsDestructibleNormalWeapon
         || v41.Data.IsDestructibleNormalWeapon
         && !v41.Data.BlocksProjectiles
         && !v41.Data.BlocksMovement))
                    {
                        goto LABEL_63;
                    }
                    v299 = v38;
                    v43 = v41.Data.CollisionMargin;
                    Tile v44 = v15.GetTile(v284, v40, true);
                    v45 = v43 + 1;
                    v46 = v43 + v287;
                    v301 = 0;
                    if (v44 != null)
                    {
                        v301 = v43 + v287;
                        v46 = v43 + v287;
                    }
                    if (v44 != null && !v41.Data.BlocksProjectiles)
                    {
                        v48 = true;
                    }
                    else
                    {
                        v48 = false;
                        v301 = v46 - v45;
                    }
                    v288 = v48;
                    Tile v49 = v15.GetTile(v293, v40, true);
                    if (v49 != null && !v49.Data.BlocksProjectiles)
                    {
                        v297 = v43 + v287 + 300 - 2 * v43;
                        v50 = false;
                    }
                    else
                    {
                        v50 = true;
                        v297 = v43 + v287 + 300 - 2 * v43 + v45;
                    }
                    v290 = v50;
                    v51 = v43 + 300 * v33;
                    Tile v52 = v15.GetTile(v303, v40, true);
                    if (v52 != null && !v52.Data.BlocksProjectiles)
                        v53 = v43 + v308 - 300;
                    else
                        v53 = v51 - v45;
                    Tile v54 = v15.GetTile(v303, v40 + 2, true);
                    if (v54 != null && !v54.Data.BlocksProjectiles)
                        v55 = v308 - v43;
                    else
                        v55 = v51 + 300 - 2 * v43 + v45;
                    v56 = true;
                    if (!ProjectileData.PiercesEnvironment || v41.Data.BlocksMovement
        && v41.Data.BlocksProjectiles
        && !v41.Data.IsDestructible)
                    {
                        v56 = false;
                        if (v41.Data.IsDestructibleNormalWeapon)
                        {
                            v56 = true;
                            if (!v41.Data.BlocksProjectiles)
                                v56 = v41.Data.BlocksMovement;
                        }
                    }

                    v57 = GamePlayUtil.LineSegmentIntersectsRectangle(
                            v295,
                            v292,
                            v282,
                            v283,
                            v301,
                            v53,
                            v297,
                            v55,
                            Pos);
                    if (v57 > 0)
                    {
                        v58 = v57;
                        //Debugger.Print(v56.ToString());
                        if (v56 && (!v41.Data.BlocksProjectiles
         ||
         //v41.Data.IsDestructibleNormalWeapon&&
         (!v41.Data.IsDestructibleNormalWeapon || v41.Data.BlocksProjectiles || v41.Data.BlocksMovement)
         || ProjectileData.PiercesEnvironmentLikeButter))
                        {
                            //v59 = ZNK19LogicProjectileData18piercesEnvironmentEv(v306);
                            v27 = v303;
                            //ZN12LogicTileMap11destroyTileEiibP21LogicBattleModeServer(v316, v303, v312, v59 ^ 1);
                            if (!ProjectileData.PiercesEnvironmentLikeButter && v41.Data.BlocksMovement && v41.Data.BlocksProjectiles && !v41.IsDestructed()) v267 = true;

                            v41.Destruct();
                            v34 = v279;
                            v38 = v299;
                            goto LABEL_63;
                        }

                        if ((Pos.X - v295) * (Pos.X - v295) + (Pos.Y - v292) * (Pos.Y - v292) < v277)
                        {
                            v268 = v303 + v15.Width * (v312 + 1);
                            v271 = v58;
                            v269.Y = Pos.Y;
                            v269.X = Pos.X;
                            v267 = true;
                            v277 = (Pos.X - v295) * (Pos.X - v295) + (Pos.Y - v292) * (Pos.Y - v292);
                            //goto LABEL_62;
                            v34 = v279;
                            v38 = v299;
                            goto LABEL_63;
                        }
                    LABEL_59:
                        v27 = v303;
                        v34 = v279;
                        v38 = v299;
                        goto LABEL_63;
                    }
                    if (v276 < 101)
                    {
                        v27 = v303;
                        v34 = v279;
                        v38 = v299;
                        goto LABEL_63;
                    }
                    if (!v56)
                    {
                        v27 = v303;
                    LABEL_62:
                        v34 = v279;
                        v38 = v299;
                        goto LABEL_63;
                    }
                    v27 = v303;
                    v34 = v279;
                    v38 = v299;
                LABEL_63:

                    v38 -= 300;
                    ++v33;
                    v308 += 300;
                    v312 = v40;
                }
                while (v312 < v34);
                v33 = v260;
                v37 = v293;
                v36 = v292;
            LABEL_65:
                v66 = v27 < v263;
                v27 = v37;
            }
            while (v66);
            if (v267)
            {
                v67 = v282;
                if (!v269.Equals(new LogicVector2(v295, v36)))
                    v67 = v269.X;
                v282 = v67;
                v68 = v283;
                if (!v269.Equals(new LogicVector2(v295, v36)))
                    v68 = v269.Y;
                v283 = v68;
                v69 = true;
            }
            else
            {
                v69 = false;
            }
            v286 = v69;
        LABEL_74:
            bool v70 = false;
            if (ProjectileData.GrapplesEnemy)
                v70 = ProjectileData.ChainBullet != null;
            bool v71 = v70 || ProjectileData.ConsumableShield > 0 || ProjectileData.UniqueProperty == 2;
            bool v72 = Damage != 0;
            v298 = ProjectileData.HealOwnPercent;
            v73 = v71 || v72;
            bool v285 = v71;
            bool v75;
            if (SkillType == 2)
            {
                v74 = GetCardValueForPassiveFromPlayer("super_heals", 1);
                v291 = v74 & ~(v74 >> 31);
                v75 = false;
            }
            else
            {
                v75 = GetCardValueForPassiveFromPlayer("cure_debuffs", 1) > -1;
                v291 = 0;
            }
            bool v294 = v75;
            v289 = null;
            if (v73 && GameObjectManager.GetGameObjects().Count() >= 1)
            {
                v76 = v291 != 0;
                if (v291 != 0)
                    v76 = true;
                bool v262 = v294 | v76;
                v289 = null;

                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    v79 = (Character)gameObject;
                    int tmp = GetIndex() / 16;
                    v82 = false;
                    int v309 = tmp;
                    v304 = v79.GetIndex() / 16;
                    bool v83 = v304 != tmp;
                    v84 = v304 == tmp;
                    if (v304 != tmp)
                        v84 = v298 == 0;
                    if (!v84)
                        tmp = Healing;
                    v313 = v271;
                    bool v85 = false;
                    if (!v84)
                        v83 = (tmp >> 31) > 0;
                    int Index = GetIndex();
                    bool v87 = false;
                    if (v298 > 0)
                        v82 = true;
                    if (v79.GetIndex() == Index)
                    {
                        v87 = false;
                    }
                    if (v304 == v309)
                    {
                        v85 = false;
                        if (v294 || v291 != 0 || Healing > 0 || ProjectileData.ConsumableShield > 0 || ProjectileData.UniqueProperty == 2)
                        {
                            if (v79.CharacterData.IsHero())
                                v85 = Owner != v79;
                        }
                    }
                    v90 = v79.IsImmuneAndBulletsGoThrough(IsInRealm);
                    v91 = v79;
                    v92 = v90;
                    v66 = v298 <= 0;
                    v93 = false;
                    v300 = v91;
                    if (!v66)
                        v93 = true;
                    if (v91.CharacterData.IsLootBox() && LogicMath.Abs(v91.GetX() - v282) < 600 && LogicMath.Abs(v91.GetY() - v283) < 600)
                    if ((ProjectileData.ConsumableShield > 0 && v304 != v309) || !v91.IsAlive() || !(v82 & (v304 == v309) | v83 | v87 | v85) || !(v93 | !v92))
                        continue;
                    v94 = v300.GetGlobalID();
                    if (IgnoredTargets.Count >= 1)
                    {
                        foreach (Ignoring ignoring in IgnoredTargets)
                        {
                            if (ignoring.GID == v94) goto LABEL_99;
                        }
                    }
                    goto LABEL_100;
                LABEL_99:
                    //Debugger.Print("this:" + GetGlobalID() + "ignored!");
                    continue;
                LABEL_100:

                    v97 = v300.CharacterData.CollisionRadius;
                    //v98 = *(ZNK20LogicCharacterServer16getCharacterDataEv(v300) + 260);
                    v99 = v97 + v276;
                    v100 = v300.GetX();
                    v101 = v300.GetY();
                    v113 = v300.GetX() - v282;
                    v114 = v300.GetY();
                    if (LogicMath.Abs(v113) > v99)
                        continue;
                    v118 = v114 - v283;
                    if (LogicMath.Abs(v118) <= v99 && v113 * v113 + v118 * v118 <= (v99 * v99))
                    {
                        v103 = 0;
                        Pos.X = v282;
                        Pos.Y = v283;
                    }
                    else continue;
                    if (true)
                    {
                    LABEL_117:
                        if (ProjectileData.PiercesCharacters || CanPierceCharacter)
                        {
                            //v63 = v98 == 13;
                            v63 = false;
                            if (!v63)
                                goto LABEL_119;
                            v289 = v300;
                        }
                        //v63 = v98 == 13;
                        v63 = false;
                        v105 = v292;
                        v106 = v313;
                        v286 = true;

                        if (v63 || !(v262 & (v304 == v309)))
                        {
                            v115 = Pos.X;
                            v116 = Pos.Y;
                            v117 = (v115 - v295) * (v115 - v295) + (v116 - v105) * (v116 - v105);
                            if (v117 >= v277)
                            {
                                v271 = v106;
                                continue;
                            }
                            v277 = v117;
                            v286 = true;
                            v289 = v300;
                            v269.X = v115;
                            v269.Y = v116;
                        }
                    LABEL_119:
                        // В csv пустая колонка читается как "", а не null: с проверкой на null
                        // выстрел без зоны (Шелли и др.) выходил тут, не нанеся урона ящику.
                        if (!string.IsNullOrEmpty(ProjectileData.SpawnAreaEffectObject))
                        {
                            return 2;
                        }
                        if (ProjectileData.DamagesConstantly)
                        {
                            v320 = v300.GetGlobalID();
                            v319 = ProjectileData.DamagesConstantlyTickDelay;
                            IgnoredTargets.Add(new Ignoring(v320, v319));
                            IncreaseTargetHitCount(v320);
                            if (LinkedProjectileGIDs.Count >= 1)
                            {
                                foreach (int id in LinkedProjectileGIDs)
                                {
                                    Projectile result = (Projectile)GameObjectManager.GetGameObjectByID(id);
                                    if (result != null)
                                    {
                                        result.IncreaseTargetHitCount(v320);
                                    }
                                }
                            }
                            if (ProjectileData.DamageOnlyWithOneProj)
                            {
                                AddIgnoredTarget(v300.GetGlobalID(), ProjectileData.DamagesConstantlyTickDelay);
                            }
                            v112 = v300;
                        }
                        else
                        {
                            v112 = v300;
                            IgnoredTargets.Add(new Ignoring(v300.GetGlobalID(), 9999999));
                            if (ProjectileData.DamageOnlyWithOneProj)
                            {
                                AddIgnoredTarget(v300.GetGlobalID(), 9999999);
                            }
                        }
                        v121 = v112.GetGlobalID();
                        v122 = GetModifiedDamage(Damage, false, v121);
                        v125 = GetModifiedDamage(NormalDMG, true, v121);
                        if (v304 == v309)
                        {
                            if (ProjectileData.ConsumableShield > 0) v300.AddConsumableShield(ProjectileData.ConsumableShield, ProjectileData.ConsumableShieldTicks);
                            if (v112.CharacterData.IsHero())
                            {
                                if (v294)
                                {
                                    //ZN20LogicCharacterServer11giveCleanseEi(v112);
                                }
                                else if (ProjectileData.UniqueProperty == 2)
                                {
                                    v300.AddExtraHealthRegen(v122, 61, GetIndex());
                                }
                                else
                                {
                                    if (Healing <= 0)
                                        Healing = v122 * v298 / 100;
                                    if (v291 > 0)
                                        Healing = v291;
                                    v300.Heal(Index, Healing, true, ProjectileData);
                                }
                            }
                            goto LABEL_165;
                        }
                        v127 = Healing;
                        v128 = Index;
                        if (v127 > -1)
                        {
                            v129 = v122;
                            v130 = v122;
                            v131 = v300;
                            v132 = v313;
                            if (PercentDamage > 0)
                            {
                                v130 = AttackSpecialParams_StaticPercentDamage + GamePlayUtil.CalculatePercentDamage(PercentDamage, v130, false, AttackSpecialParams_DamageBuff, AttackSpecialParams_DamageDebuff, AttackSpecialParams_MinPercentDamage, v300);
                                v125 = 1000;
                            }
                            if (PreyOnTheWeakDamage >= 1 && (!v300.IsObject() || v300.GetIndex() != -16))
                            {
                                if (v300.GetHitpointPercentage() <= PreyOnTheWeakPercent) v130 += PreyOnTheWeakDamage;
                            }
                            if (ProjectileData.Name == "SilencerUltiProjectile")
                            {
                                v300.IsInRealm = true;
                                v300.RealmTimer = 140;
                                Owner.IsInRealm = true;
                                Owner.RealmTimer = 140;
                                AreaEffect effect = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("MummyUltiArea"));
                                effect.SetIndex(-1);
                                effect.SetPosition(Position.X, Position.Y, 0);
                                effect.IsInRealm = true;
                                GameObjectManager.AddGameObject(effect);
                                if (!(ProjectileData.PiercesCharacters || CanPierceCharacter)) break;

                                continue;
                            }
                            if (ProjectileData.TravelType == 10)
                            {
                                v130 /= 2;
                                v125 /= 2;
                            }
                            if (v300.CauseDamage(
                                   Owner,
                                   v130,
                                   v125,
                                   SkillType == 2, ProjectileData, true, false, GetX(), GetY()))
                            {
                                if (AttackSpecialParams_StealBulletPercent > 0)
                                {
                                    int charge = 3000 * AttackSpecialParams_StealBulletPercent / 100;
                                    if (v300.m_skills.Count >= 1 && v300.m_skills[0].MaxCharge >= 1)
                                    {
                                        charge = v300.m_skills[0].MaxCharge * AttackSpecialParams_StealBulletPercent / 100;
                                        v300.m_skills[0].AddCharge(v300, -charge / 10);
                                        Owner?.m_skills[0].AddCharge(Owner, charge * AttackSpecialParams_StealBulletPercentSelf / 1000);
                                    }
                                }
                                if (ProjectileData.PoisonDamagePercent >= 1)
                                {
                                    int d = v130 * ProjectileData.PoisonDamagePercent / 100;
                                    if (Owner?.GetGearBoost(9) >= 1)
                                    {
                                        d += d * Owner.GetGearBoost(9) / 100;
                                    }
                                    v300.ApplyPoison(this.GetIndex() % 16, d, v125 * ProjectileData.PoisonDamagePercent / 100, SkillType == 2, Owner, SkinProjectileData.PoisonType);
                                }
                                if (CripplePercent >= 1) v300.Cripple(CripplePercent);
                                if (ProjectileData.LifeStealPercent >= 1 && Owner != null && !v300.IsObject())
                                {
                                    if (GetCardValueForPassiveFromPlayer("self_heal_increase", 1) >= 1)
                                    {
                                        v129 += v129 * GetCardValueForPassiveFromPlayer("self_heal_increase", 1) / 100;
                                    }
                                    Owner.Heal(Index, ProjectileData.LifeStealPercent * v129 / 100, true, ProjectileData);
                                }
                                if (ProjectileData.FreezeStrength >= 1)
                                {
                                    v141 = ProjectileData.FreezeStrength;
                                    v142 = ProjectileData.GetFreezeDuration();
                                    int v139 = -v141;
                                    v131 = v300;
                                    v138 = v142;
                                LABEL_168:
                                    v131.GiveSpeedSlowerBuff(v139, v138);
                                }
                                else if (FreezeStrength >= 1)
                                {
                                    v131.GiveSpeedSlowerBuff(-FreezeStrength, FreezeDuration);
                                }
                                else if (Owner?.GetGearBoost(20) >= 1) v131.GiveSpeedSlowerBuff(-Owner.GetGearBoost(20) * v131.CharacterData.Speed / 100, 20);
                                if (SkillType != 2 && Owner != null)
                                {
                                    if (v131.CharacterData.IsHero() || v131.CharacterData.IsTrainingDummy())
                                    {
                                        if (Owner.GetMaxChargedShots() >= 1)
                                        {
                                            if (ProjectileData.Name != Owner.GetWeaponSkill().SkillData.SecondaryProjectile)
                                            {
                                                Owner.ChargedShotCount = LogicMath.Clamp(Owner.ChargedShotCount + 1, 0, Owner.GetMaxChargedShots());
                                                ChargedShotHit = true;
                                            }
                                        }
                                    }
                                }
                                if (ProjectileData.UniqueProperty == 3 && BelleWeaponBounces < 3)
                                {
                                    v131.GiveElectrocution(Damage, NormalDMG, SkinProjectileData.AppliedEffectVisualType, BelleWeaponBounces);
                                }
                                if (ProjectileData.UniqueProperty == 4)
                                {
                                    v131.m_buffs.Add(new Buff(15, 16383, 35, 0));
                                    v131.m_buffs.Last().BelleWeaponBounces = Owner.GetCardValueForPassive("ulti_buff_buff", 1);
                                }
                            }
                            v152 = GetModifiedPushback();
                            if (v152 != 0)
                            {
                                v153 = v152;
                                int X = StartX;
                                int Y = StartY;
                                if (ProjectileData.PushbackType == 1 || AttackSpecialParams_Pushback != 0)
                                {
                                    if (v152 > 0)
                                    {
                                        X = GetX() - TargetX + StartX;
                                        Y = GetY() - TargetY + StartY;
                                        v153 = v153 * (1000 - TotalDelta) / 1000;
                                    }
                                    else
                                    {
                                        X = TargetX * 2 - StartX;
                                        Y = TargetY * 2 - StartY;
                                        v153 = -v153;
                                    }

                                }
                                if (AttackSpecialParams_Pushback != 0) v300.TriggerPushback(X, Y, v153, false, 0, false, false, false, false, false, false, false, 0, StartX, StartY);
                                else v300.TriggerPushback(X, Y, v153, false, 0, false, false, false, false, false, false, false, 0);
                            }
                            v300.TriggerStun(ProjectileData.GetStunLengthTicks(), false);
                            if (AttackSpecialParams_WeaklyStun >= 1)
                            {
                                v300.TriggerStun(AttackSpecialParams_WeaklyStun, false);
                                v300.IsWeaklyStunned = true;
                            }
                            //v271 = v132;
                            if (!(ProjectileData.PiercesCharacters || CanPierceCharacter)) break;
                            continue;
                        }
                    }
                    else
                    {
                    }
                LABEL_165:
                    v271 = v313;
                    if (!(ProjectileData.PiercesCharacters || CanPierceCharacter)) break;
                    continue;
                }
            }
        LABEL_185:
            if (!v286)
            {
                v166 = 2;
                if (v282 >= 2)
                {
                    v168 = MapWidth - 2;
                    v166 = 3;
                    if (v282 < v168)
                    {
                        v166 = 4;
                        if (v283 >= 2)
                        {
                            v170 = 0;
                            v171 = MapHeight - 2;
                            v166 = 1;
                            if (v283 < v171)
                                return v170;
                        }
                    }
                }
                BounceDirection = v166;
                //dwordB0 = -1;
                v170 = 1;
                v174 = LogicMath.Clamp(v282, 1, MapWidth - 2);
                v177 = LogicMath.Clamp(v283, 1, MapHeight - 2);
                v178 = GetZ();
                SetPosition(v174, v177, v178);
                //v179 = ZNK21LogicGameObjectServer32getCardValueForPassiveFromPlayerEii(a1, 90, 1);
                v179 = -1;
                if (!ProjectileData.ExecuteChainAlways())
                {
                    v184 = ProjectileData.ExecuteChainOnAnyHit();
                    if (v179 <= -1 && !v184)
                        return v170;
                }
                return v170;
            }
            v163 = GetZ();
            SetPosition(v269.X, v269.Y, v163);
            //Debugger.Print(v269.ToString());
            if (v289 != null)
            {
                if (ProjectileData.ExecuteChainOnObjectHit())
                {
                    ExecuteChainBullet(v269.X, v269.Y, v289);
                    goto LABEL_8888;
                }
            }
            else
            {
                //gap10C[3] = v270 & 1;
                BounceDirection = v271;
                BounceTileIndex = v268;
            };
            if (ProjectileData.ExecuteChainAlways()
              || ProjectileData.ExecuteChainOnAnyHit()
              || GetCardValueForPassiveFromPlayer("split_also_wall_hit", 1) > -1)
            {
                ExecuteChainBullet(v269.X, v269.Y, v289);
            }
        LABEL_8888:
            if (v289 == null || !string.IsNullOrEmpty(ProjectileData.SpawnAreaEffectObject))
            {
            LABEL_213:
                if (v275 != -1)
                {
                    v15.GetTile(v275, v274, true).Destruct(ProjectileData.PiercesEnvironment);
                }
                v170 = 1;
                if (v289 != null)
                    return 2;
                return v170;
            }
            if (Damage < 1)
            {
                if (!ProjectileData.GrapplesEnemy || ProjectileData.ChainBullet == null)
                {
                    if (v275 != -1)
                    {
                        v15.GetTile(v275, v274, true).Destruct(ProjectileData.PiercesEnvironment);
                    }
                    v170 = 1;
                    if (v289 != null)
                        return 2;
                    return v170;
                }
            }
            else
            {
                if (HealOthersAreaEffect >= 1 && Owner != null && (!v289.IsObject() || v289.GetIndex() != -16))
                {
                    v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("HealWithAutoAttack"));
                    v224.SetPosition(Owner.GetX(), Owner.GetY(), 0);
                    v224.SetIndex(Owner.GetIndex());
                    v224.SetSource(Owner, SkillType);
                    v224.Damage = -HealOthersAreaEffect;
                    v224.NormalDMG = 0;
                    v224.CanHealOwner = true;
                    GameObjectManager.AddGameObject(v224);
                    v224.Trigger();
                }
                if (Owner != null && (!v289.IsObject() || v289.GetIndex() != -16))
                {
                    v229 = Owner.GetCardValueForPassive("reload_on_hit", 1);
                    if (v229 >= 0) Owner.m_skills[0].AddCharge(Owner, v229);
                }
                v170 = 2;
            }
            return v170;
        }
        public void RunEarlyTicks()
        {
            //Debugger.Error("ETR!");
            if (ProjectileData.EarlyTicks >= 1)
            {
                for (int i = 0; i < ProjectileData.EarlyTicks; i++)
                {
                    EarlyTicks++;
                    Tick();
                    OnDeploy = true;
                }
            }
        }
        public void UpdateTrailAreaEffect()
        {
            if (ProjectileData.SpawnAreaEffectTrail != null)
            {
                // Имя может быть прописано в projectiles.csv, а строки в area_effects.csv не быть —
                // без проверки это роняет весь бой NRE в конструкторе GameObject.
                AreaEffectData trailData = DataTables.GetAreaEffectByName(ProjectileData.SpawnAreaEffectTrail);
                if (trailData == null) return;
                AreaEffect v5 = GameObjectFactory.CreateGameObjectByData(trailData);
                v5.SetPosition(GetX(), GetY(), 0);
                v5.SetIndex(GetIndex());
                v5.SetSource(Owner, SkillType);
                v5.Damage = GetModifiedDamage(Damage, false, -1);
                v5.NormalDMG = NormalDMG;
                GameObjectManager.AddGameObject(v5);
                v5.Trigger();
            }
            if (AttackSpecialParams_TrailArea != null)
            {
                if (GamePlayUtil.GetDistanceBetween(TrailX, TrailY, GetX(), GetY()) < AttackSpecialParams_TrailAreaInterval) return;
                AreaEffect v5 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(AttackSpecialParams_TrailArea));
                v5.SetPosition(GetX(), GetY(), 0);
                v5.SetIndex(GetIndex());
                v5.SetSource(Owner, 4);
                v5.NormalDMG = 0;
                GameObjectManager.AddGameObject(v5);
                v5.Trigger();
                TrailX = GetX();
                TrailY = GetY();
            }
        }
        public int GetCirclingPosAt(int a2, LogicVector2 a3)
        {
            int v11 = ProjectileData.Speed * (EarlyTicks + TicksGone - DeployedTick) / 20;
            SteerAngle += 15;
            a3.X = Owner.GetX() + ((LogicMath.Cos(SteerAngle) * v11) >> 10);
            a3.Y = Owner.GetY() + ((LogicMath.Sin(SteerAngle) * v11) >> 10);
            return DefaultZ;
        }
        public bool TickMovement()
        {
            int v2; // r0
            int v3; // r0
            int v6; // r5
            int v7; // r7
            int v8; // r6
            int v9; // r0
            int v10; // r0
            int v11; // r8
            int v13; // r0
            int v14; // r5
            int v15; // r0
            int v16; // r0
            int v17; // r9
            int v18; // r10
            int v20; // r6
            int v21; // r0
            int v22; // r0
            int v23; // r6
            int v25; // r0
            int v26; // r0
            int v27; // r0
            int v28; // [sp+0h] [bp-20h]

            if (ProjectileData.TravelType == 8)
            {
                if (Owner == null || !Owner.IsAlive()) return true;
            }
            v3 = TicksGone;
            v6 = EarlyTicks + v3;
            v7 = v6 - DeployedTick;
            v8 = EndingTick - DeployedTick;
            if (v6 - DeployedTick > EndingTick - DeployedTick)
                return true;
            if (ProjectileData.TravelType == 8)
                v10 = GetCirclingPosAt(0, Pos);
            else if (ProjectileData.SteerStrength < 1)
                v10 = GetPosAtTick(v6, Pos);
            else
                v10 = GetNextSteeredPos(Pos);
            v11 = v10;
            UpdateTrailAreaEffect();
            if (ProjectileData.Indirect)
            {
                if (Pos.X < 2 || Pos.X >= MapWidth - 2 || Pos.Y < 2 || Pos.Y >= MapHeight - 2)
                {
                    v20 = Pos.X;
                    v23 = LogicMath.Clamp(v20, 1, MapWidth - 2);
                    v27 = LogicMath.Clamp(Pos.Y, 1, MapHeight - 2);
                    SetPosition(v23, v27, v11);
                    return true;
                }
            }
            if (ProjectileData.TravelType != 1 || (OrginalEndX - StartX) * (Pos.X - GetX()) + (OrginalEndY - StartY) * (Pos.Y - GetY()) >= 1)
            {
                SetPosition(Pos.X, Pos.Y, v11);
            }
            return v7 == v8;
        }

        public int GetPosAtTick(int a2, LogicVector2 a3)
        {
            int v15 = 0;
            int v70 = TargetX - StartX;
            int v71 = TargetY - StartY;
            int v10 = LogicMath.Sqrt(v70 * v70 + v71 * v71);
            int v73 = LogicMath.Max(1, v10);
            int v11 = a2 - DeployedTick;
            int v12 = EndingTick - DeployedTick;
            int v77 = TargetX;
            if (v12 >= v11)
            {
                int v72 = TargetY;
                LogicVector2 v69 = a3;
                if (ProjectileData.BouncePercent < 1)
                {
                    if (ProjectileData.TravelType == 11)
                    {
                        v11 = LogicMath.Max(0, v11 - ProjectileData.TravelTypeVariable);
                        v12 = LogicMath.Max(1, v12 - ProjectileData.TravelTypeVariable);
                    }
                    int v29 = v12 - v11;
                    int v30 = v11 * v72 / v12;
                    a3.Y = (v12 - v11) * StartY / v12 + v30;
                    int v31 = v11 * v77 / v12;
                    a3.X = v29 * StartX / v12 + v31;
                    int v32 = ProjectileData.Gravity;
                    v15 = DefaultZ;
                    if (v32 > 0)
                        v15 = v15 * v29 / v12;
                }
                else
                {
                    int v16 = ProjectileData.BouncePercent;
                    int v17 = 5 * v16;
                    int v18 = 10 * v16;
                    int v19 = 10 * v16 * v73 + 1499;
                    int v67 = 1000 * v11;
                    int v20 = 1000 * v11 / v12;
                    int v21 = 1000 - 2 * v17;
                    int v22 = v20;
                    int v23 = 0;
                    int v24 = 0;
                    if (v19 >= 0xBB7)
                    {
                        int v65 = v22;
                        int v64 = (v18 * v73) / 1500;
                        v24 = v64 * v70 / v73;
                        v23 = v64 * v71 / v73;
                        v22 = v65;
                    }
                    int v25 = v23 + v72;
                    int v26 = v24 + v77;
                    if (v22 <= v21)
                    {
                        int v33 = v21 * v12;
                        int v34 = v22;
                        int v35 = LogicMath.Max(1, v33 / 1000);
                        int v36 = v67 / v35;
                        int v37 = (1000 - v34) * StartX + v26 * v34;
                        a3 = v69;
                        long v38 = (274877907L * (1000 - v36) * DefaultZ) >> 32;
                        v69.X = v37 / 1000;
                        v69.Y = ((1000 - v34) * StartY + v25 * v34) / 1000;
                        v15 = (int)((v38 >> 6) + (v38 >> 31));
                        //Debugger.Print(v15.ToString());
                    }
                    else
                    {
                        int v66 = v26;
                        int v68 = (v26 - StartX) * v21 / 1000 + StartX + ((v26 - StartX) * v18) / 2000;
                        int v27 = v22 - v21;
                        int v28 = v18 - (v22 - v21);
                        a3 = v69;
                        v69.Y = ((v18 * StartY + v25 * v21) / 1000 * v28
                                 + ((v25 - StartY) * v21 / 1000 + StartY + ((v25 - StartY) * v18) / 2000) * (v22 - v21)) /
                                   v18;
                        v69.X = ((v18 * StartX + v66 * v21) / 1000 * v28 + v68 * v27) / v18;
                        v15 = 0;
                        //Debugger.Print(v69.ToString());
                    }
                }
                TargetY = v72;
            }
            else
            {
                a3.X = TargetX;
                a3.Y = TargetY;
                v15 = 0;
                if (ProjectileData.Gravity < 1)
                    v15 = DefaultZ;
            }
            if (ProjectileData.Gravity > 0)
            {
                bool v39 = ProjectileData.ConstantFlyTime;
                float v40 = -ProjectileData.Gravity;
                float v41;
                if (v39)
                    v41 = v40 * 0.001F;
                else
                    v41 = v40 / (float)(LogicMath.Sqrt(v73) * 40.0F);
                int v42 = ProjectileData.BouncePercent;
                int v43 = ProjectileData.Speed;
                float v44 = 0.1F;
                float v45 = 0.1F;
                if ((v43 * 0.1F) > 0.1F)
                    v45 = v43 * 0.1F;
                float v60;
                if (v42 < 1)
                {
                    float v58 = GamePlayUtil.GetDistanceBetween(StartX, StartY, v77, TargetY);
                    float v59 = GamePlayUtil.GetDistanceBetween(StartX, StartY, a3.X, a3.Y) / v45;
                    if ((v58 / v45) > 0.1)
                        v44 = v58 / v45;
                    if (ProjectileData.ConstantFlyTime)
                        v60 = (float)(v59 * (float)((float)(v41 * 2000000.0) / (float)(v58 * v58))) * (float)(v59 - v44);
                    else
                        v60 = (float)(v59 * v41) * (float)(v59 - v44);
                }
                else
                {
                    int v46 = LogicMath.Sqrt(v70 * v70 + v71 * v71);
                    int v47 = v46 * v42;
                    int v48 = v46;
                    int v49 = 0;
                    bool v50 = (v46 * v42 + 149) >= 0x12B;
                    int v51 = 0;
                    if (v50)
                    {
                        int v52 = v47 / 150;
                        v49 = v47 / 150 * v70 / v48;
                        v51 = v52 * v71 / v48;
                    }
                    LogicVector2 v80 = new LogicVector2(v77 - v49, TargetY - v51);
                    LogicVector2 v79 = new LogicVector2(v77 - v80.X, TargetY - v80.Y);
                    LogicVector2 v78 = new LogicVector2(a3.X - v80.X, a3.Y - v80.Y);
                    float v53;
                    int v54, v55, v56, v57;
                    if (v79.Dot(v78) <= -1)
                    {
                        int v61 = GamePlayUtil.GetDistanceBetween(StartX, StartY, v80.X, v80.Y);
                        v56 = StartX;
                        v57 = StartY;
                        v53 = v61;
                        v54 = a3.X;
                        v55 = a3.Y;
                    }
                    else
                    {
                        v53 = GamePlayUtil.GetDistanceBetween(v80.X, v80.Y, v77, TargetY);
                        v54 = a3.X;
                        v55 = a3.Y;
                        v56 = v80.X;
                        v57 = v80.Y;
                    }
                    float v62 = GamePlayUtil.GetDistanceBetween(v56, v57, v54, v55) / v45;
                    if ((float)(v53 / v45) > 0.1F)
                        v44 = v53 / v45;
                    v60 = (float)(v62 * v41) * (float)(v62 - v44);
                }
                v15 += (int)(float)(v60 * 300.0F);
            }
            return LogicMath.Clamp(v15, 0, 3000);
        }
        private void HandleEnragers()
        {
            foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
            {
                if (gameObject == null) continue;
                if (gameObject.GetObjectType() != 0) continue;
                if (!gameObject.IsAlive()) continue;
                if (PassedEnrager.Any())
                {
                    if (PassedEnrager.Contains((Character)gameObject)) continue;
                }
                int teamIndex = gameObject.GetIndex() / 16;
                if (teamIndex == this.GetIndex() / 16) continue;

                Character character = (Character)gameObject;
                if (character.CharacterData.Name != "Luchador") continue;

                if (Position.GetDistance(gameObject.GetPosition()) <= 1000)
                {
                    // Collision!
                    PassedEnrager.Add((Character)gameObject);

                    return;
                }
            }
            foreach (Character gameObject in PassedEnrager)
            {
                if (gameObject == null) continue;
                if (!gameObject.IsAlive()) continue;

                if (Position.GetDistance(gameObject.GetPosition()) >= 1100)
                {
                    // Collision!
                    BattleMode battle = GameObjectManager.GetBattle();
                    BattlePlayer enemy = battle.GetPlayerWithObject(gameObject.GetGlobalID());
                    enemy.AddUltiCharge(1000);

                    return;
                }
            }
        }
        private void ReturnBoomerang()
        {
            int StartX; // r9
            int v4; // r0
            Character v5; // r8
            int StartY; // r10
            Projectile v12; // r0

            if (Owner != null)
            {
                StartX = Owner.GetX();
                v4 = Owner.GetY();
                v5 = Owner;
                StartY = v4;
            }
            else
            {
                StartX = this.StartX;
                v5 = null;
                StartY = this.StartY;
            }
            v12 = ShootProjectile(
                    -1,
                    -1,
                    v5,
                    this,
                    DataTables.GetProjectileByName(ProjectileData.ChainBullet),
                    StartX,
                    StartY,
                    Damage,
                    NormalDMG,
                    0,
                    false,
                    State == 4 ? -Owner?.GetGearBoost(17) * ProjectileData.Speed / 100 ?? 0 : 0,
                    GameObjectManager.GetBattle(),
                    0,
                    SkillType);
            v12.HomingTarget = Owner;
            //return ZN24LogicAttackSpecialParams12assignValuesERKS_(&v12->field_154, &a1->field_154);
        }
        public void TargetReached(int a2)
        {
            int v2; // r6
            int v6; // r9
            int v7; // r0
            AreaEffectData v8; // r0 MAPDST
            AreaEffect v10; // r7
            int v11; // r10
            int v12; // r3
            ProjectileData v13; // r0
            bool v14; // zf
            int v15; // r0
            int v16; // r0
            int v17; // r1
            int v18; // r0
            int v19; // r5
            int v21; // r4
            CharacterData v22; // r5
            int v23; // r10
            int v25; // r9
            int v26; // r7
            int v27; // r0

            if (State == 0)
            {
                State = a2;
                if (a2 != 5)
                {
                    //v5 = &a1->field_154;
                    v6 = 2;
                    do
                    {
                        if (v6 == 2)
                        {
                            v8 = DataTables.GetAreaEffectByName(SkinProjectileData.SpawnAreaEffectObject);
                            if (AttackSpecialParams_AreaEffectData != null && v8 != null) v8 = AttackSpecialParams_AreaEffectData;
                        }
                        else
                            v8 = DataTables.GetAreaEffectByName(SkinProjectileData.SpawnAreaEffectObject2);
                        if (v8 != null)
                        {
                            v10 = new AreaEffect(v8);
                            v11 = GetX();
                            v2 = GetY();
                            v12 = 0;
                            if (v8.Type == "BulletExplosion")
                            {
                                v13 = DataTables.GetProjectileByName(v8.BulletExplosionBullet);
                                v14 = v13 == null;
                                if (v13 != null)
                                    v14 = !v13.Indirect;
                                if (!v14 || v8.BulletExplosionItem != null)
                                    v12 = GetZ();
                            }
                            v10.SetPosition(v11, v2, v12);
                            v10.SetIndex(GetIndex());
                            v10.SetSource(Owner, SkillType);
                            v16 = GetModifiedDamage(Damage, false, -1);
                            v17 = NormalDMG;
                            v10.Damage = v16;
                            v10.NormalDMG = v17;
                            if (ProjectileData.AreaEffect2DamagePercent >= 1 && v6 == 1)
                            {
                                v10.Damage = v10.Damage * ProjectileData.AreaEffect2DamagePercent / 100;
                                v10.NormalDMG = v10.NormalDMG * ProjectileData.AreaEffect2DamagePercent / 100;
                            }
                            if (AttackSpecialParams_Stun >= 1) v10.Stun = AttackSpecialParams_Stun;
                            if (AreaEffectCantPushback) v10.CantPushback = true;
                            if (aoe_dot != 0) v10.Damage = aoe_dot;
                            GameObjectManager.AddGameObject(v10);
                            v10.Trigger();
                        }
                        --v6;
                    }
                    while (v6 > 0);
                    if (Owner?.GetGearBoost(21) >= 1 && SkillType == 2)
                    {
                        v10 = new AreaEffect(DataTables.GetAreaEffectByName("BlackHoleVisionArea"));
                        v11 = GetX();
                        v2 = GetY();
                        v12 = 0;
                        v10.SetPosition(v11, v2, v12);
                        v10.SetIndex(GetIndex());
                        v10.SetSource(Owner, SkillType);
                        v10.Damage = 0;
                        //v10.DisplayScale = 10000;
                        v10.NormalDMG = 0;
                        GameObjectManager.AddGameObject(v10);
                        v10.Trigger();
                    }
                    v22 = SummonedCharacter;
                    if (v22 != null)
                    {
                        if (Owner != null)
                            v27 = Owner.GetGlobalID();
                        else
                            v27 = 0;
                        Character.SummonMinion(
                          v22,
                          GetX(),
                          GetY(),
                          0,
                          NumSpawns,
                          MaxSpawns,
                          SpawnDamage,
                          SpawnHealth,
                          GameObjectManager.GetBattle(),
                          GetIndex() % 16,
                          GetIndex() / 16,
                          v27,
                          true,
                          true,
                          0,
                          SpawnStarPowerIC,
                          aoe_dot);
                    }
                    if (SpawnedItem != null&&(SpawnedItem.Name!= "SoulCollectorSoul"||ChargedShotHit))
                    {
                        Item item = GameObjectFactory.CreateGameObjectByData(SpawnedItem);
                        item.SetPosition(GetX(), GetY(), 0);
                        item.SetIndex(GetIndex());
                        item.Damage = Damage;
                        GameObjectManager.AddGameObject(item);
                    }
                    if (Owner != null && Owner.GetMaxChargedShots() >= 1)
                    {
                        if (SkillType != 2)
                        {
                            if (Owner.CharacterData.UniqueProperty == 10)
                            {
                                if (ChargedShotHit && SpawnedItem != null) Owner.ChargedShotCount = 0;
                            }
                            else if (!ChargedShotHit)
                            {
                                if (Owner.ChargedShotMisses < 1
                                  || a2 == 3
                                  || Owner.ChargedShotMisses >= GetCardValueForPassiveFromPlayer("instant_charged_shot", 1))
                                {
                                    Owner.ChargedShotCount = LogicMath.Clamp(0, 0, Owner.GetMaxChargedShots());
                                }
                                else
                                {
                                    Owner.ChargedShotCount = LogicMath.Clamp(1, 0, Owner.GetMaxChargedShots());
                                }

                            }
                        }
                    }
                    if (ProjectileData.IsBoomerang)
                    {
                        if (ProjectileData.ChainBullet != null)
                            ReturnBoomerang();
                    }
                    if (ProjectileData.IsBoomerang)
                    {
                        if (ProjectileData.ChainBullet == null)
                        {
                            if (Owner != null)
                                Owner.m_skills[0].AddCharge(Owner, 100);
                        }
                    }
                    if (ProjectileData.IsFriendlyHomingMissile && HomingTarget != null)
                    {
                        if (SpeedBuffModifier >= 1)
                        {
                            HomingTarget.GiveSpeedFasterBuff(SpeedBuffModifier, SpeedBuffLength, true);
                        }
                        else if (StealSouls2Buff >= 1)
                        {
                            HomingTarget.GiveDamageBuff(StealSouls2Buff, StealSouls2BuffTicks);
                        }
                        else HomingTarget.Heal(GetIndex(), Damage, true, ProjectileData);
                    }
                    if (ProjectileData.IsHomingMissile && HomingTarget != null)
                    {
                        HomingTarget.CauseDamage(Owner, GetModifiedDamage(Damage, false, HomingTarget.GetGlobalID()), GetModifiedDamage(NormalDMG, true, HomingTarget.GetGlobalID()), SkillType == 2, ProjectileData, true);
                    }
                }
            }
        }

        public override void OnDestruct()
        {
            ;
        }

        private bool IsDestroyed()
        {
            return ((TotalDelta > MaxRange && !ProjectileData.Indirect) || ShouldDestructImmediately);
        }

        public override bool ShouldDestruct()
        {
            return State != 0 && DestroyedTicks > 5;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int OwnObjectIndex, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);
            int TriggerDelay;
            int encState = Environment.GetEnvironmentVariable("PROJ_STATE0") == "1" ? 0 : State; // ТЕСТ
            bitStream.WritePositiveIntMax7(encState); // next effect 1 unhit 2 hitcharacter 
            //if (State != 0) Debugger.Print(Position.ToString());
            // v64 (LogicProjectileClient::decode 0x100476798): бит читается ВСЕГДА,
            // и только потом клиент решает, читать ли индекс — причём всегда Max4095,
            // без зависимости от ширины карты (это было в v53).
            bool hasBounceTile = ProjectileData.IsBouncing && BounceTileIndex >= 0;
            bitStream.WriteBoolean(hasBounceTile);
            if (hasBounceTile || encState == 4)
            {
                bitStream.WritePositiveIntMax4095(BounceTileIndex);
            }
            if (ProjectileData.SpecialVisualState)
            {
                bitStream.WritePositiveIntMax3(SpecialEffect ? 1 : 0);
            }
            if (!string.IsNullOrEmpty(ProjectileData.SpecialTrailEffect)) // пустая строка = следа нет, иначе лишние 2 бита
            {
                bitStream.WritePositiveIntMax3(SpecialEffect ? 1 : 0);
            }
            // v64: безусловный бит (клиент кладёт его в +388), в v53 его не было.
            bitStream.WriteBoolean(false);

            if (ProjectileData.UniqueProperty == 7) bitStream.WriteBoolean(true);
            // v64: новая ветка.
            if (ProjectileData.UniqueProperty == 18) bitStream.WriteIntMax127(0);
            if (ProjectileData.TriggerWithDelayMs > 0)
            {
                TriggerDelay = this.TriggerDelay;
            }
            else
            {
                if (!(ProjectileData.PreExplosionTimeMs > 0))
                    goto LABEL_14;
                TriggerDelay = EndingTick;
            }
            bitStream.WritePositiveVIntMax65535(TriggerDelay);
        LABEL_14:
            if (ProjectileData.PreExplosionTimeMs > 0)
            {
                bitStream.WritePositiveVIntMax65535(TargetX);
                bitStream.WritePositiveVIntMax65535(TargetY);

            }

            bitStream.WritePositiveIntMax1023(DisplayScale != 0 ? DisplayScale : TotalDelta); // Total path

            // v64: после Total path идёт флаг с парой Max1023 — в v53 его не было.
            bitStream.WriteBoolean(false);

            if (SkinProjectileData.Rendering != "DoNotRotateClip")
                bitStream.WritePositiveIntMax511(Angle);

            bitStream.WriteBoolean(OnDeploy);
            // v64: кодировку координат выбирает ОТДЕЛЬНЫЙ бит, который пишется всегда,
            // а не свойство IgnoreLevelBoarder на стороне сервера.
            bitStream.WriteBoolean(ProjectileData.IgnoreLevelBorder);
            if (OnDeploy)
            {
                if (ProjectileData.IgnoreLevelBorder)
                {
                    bitStream.WriteIntMax65535(NowX);
                    bitStream.WriteIntMax65535(NowY);
                }
                else
                {
                    bitStream.WritePositiveVIntMax65535(NowX);
                    bitStream.WritePositiveVIntMax65535(NowY);
                }
            }

            // v64: в самом конце клиент вызывает LogicStatusEffectControllerClient::decode,
            // который при пустом списке читает один бит.
            bitStream.WriteBoolean(false);
        }

        public override int GetObjectType()
        {
            return 1;
        }
    }
}
