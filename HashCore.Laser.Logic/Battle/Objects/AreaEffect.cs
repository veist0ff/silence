namespace HashCore.Laser.Logic.Battle.Objects
{
    using System.Numerics;
    using HashCore.Laser.Logic.Battle.Component;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Util;
     
    using HashCore.Laser.Titan.Math;

    public class AreaEffect : GameObject
    {
        private Character Source;

        private int m_ticksElapsed;
        public int Damage;
        public int NormalDMG;
        private List<Character> m_alreadyDamagedList;
        public int SkillType;
        public int DeployedTick;
        public int EndingTick;
        public int BulletAngle;
        public int BulletDelta;
        public int TicksGone => GameObjectManager.GetBattle().GetTicksGone();
        public int MapHeight => GameObjectManager.GetBattle().GetTileMap().LogicHeight;
        public int MapWidth => GameObjectManager.GetBattle().GetTileMap().LogicWidth;
        public int Stun;
        public int EffectTimer;
        public int LifeTimeConsumedPercent;
        public bool ShouldBeDestructed;
        public bool CanHealOwner;
        public AreaEffectData SkinAreaEffectData;
        public bool CantPushback;
        public int aoe_dot;
        public bool IsActive;
        public int DisplayScale;

        public int PreyOnTheWeakDamage;
        public int PreyOnTheWeakPercent;
        public AreaEffect(int classId, int instanceId) : base(classId, instanceId)
        {
            m_alreadyDamagedList = new List<Character>();
            AreaEffectData = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(instanceId);
        }
        public AreaEffect(AreaEffectData areaEffect) : base(areaEffect)
        {
            m_alreadyDamagedList = new List<Character>();
            AreaEffectData = areaEffect;
            SkinAreaEffectData = areaEffect;
            if (AreaEffectData.ParentAreaEffectForSkin != null)
            {
                // Родителя может не быть в таблице — тогда остаёмся на исходных данных,
                // иначе следующая же строка роняет весь бой NRE.
                AreaEffectData parent = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>(AreaEffectData.ParentAreaEffectForSkin);
                if (parent != null)
                {
                    AreaEffectData = parent;
                }
            }
            if (!AreaEffectData.DelayFirstTick) EffectTimer = 20;
            Damage = AreaEffectData.Damage;
            //NormalDMG = AreaEffectData.Damage;
        }

        public AreaEffectData EffectData => DataTables.Get(DataType.AreaEffect).GetDataByGlobalId<AreaEffectData>(DataId);
        public AreaEffectData AreaEffectData;
        public void SetPreyOnTheWeak(int Percent, int Damage)
        {
            PreyOnTheWeakPercent = Percent;
            PreyOnTheWeakDamage = Damage;
        }
        public int GetDamage(int a2)
        {
            if (Source != null && SkillType <= 2)
            {
                int d = Damage;
                d += d * Source.GetDamageBuffTemporary() / 100;
                d -= d * Source.CripplePercent / 100;
                return d + a2;
            }
            return Damage;
        }
        public void Trigger()
        {
            int v2; // r0
            int v3; // r9
            int v4; // r4 MAPDST
            string v5; // r6 MAPDST
            int v6; // r0
            //_DWORD* v8; // r0
            int v9; // r7
            Character v10; // r6
            int v11; // r0
            int v12; // r0
            int v13; // r0
            int v14; // r4
            int v15; // r8
            int v16; // r10
            int v17; // r5
            int v18; // r5
            int v19; // r9
            int v20; // r4
            int v21; // r5
            int v22; // r9
            int v23; // r8
            int v24; // r4
            int v25; // r0
            int v28; // r1
            int v29; // r0
            int v30; // r10
            //CharacterServer* v31; // r4
            int v32; // r9
            int v33; // r0
            int v35; // r0
            int v36; // r0
            int v37; // r4
            int v38; // r0
            int v39; // r0
            int v40; // r9
            int v41; // r5
            int v42; // r0
            int v43; // r5
            int v44; // r5
            int v45; // r0
            int v46; // r8
            int v47; // r5
            int v48; // r4
            int v49; // r5
            int v50; // r2
            int v51; // r4
            int v52; // r5
            int v53; // r0
            int v54; // r6
            int v55; // r0
            bool v56; // r7
            int v60; // r10
            int v62; // r0
            int v63; // r0
            int v64; // r9
            int v65; // r8
            int v66; // r9
            int v67; // r4
            int v68; // r8
            int v69; // r8
            int v70; // r7
            int v71; // r0
            int v72; // r2
            int v73; // r0
            int v74; // r7
            int v75; // r0
            int v76; // r0
            int v77; // r0
            //_DWORD* v78; // r5
            int v79; // r0
            int v80; // r0
            int v81; // r8
            int v82; // r4
            int v83; // r0
            int v84; // r6
            int v85; // r0
            int v86; // r7
            int v87; // r8
            int v88; // r10
            int v89; // r5
            int v90; // r9
            int v91; // r6
            int v92; // r4
            int v93; // r0
            int v94; // r1
            int v95; // r6
            int v96; // r4
            int v97; // r0
            bool v98; // cc
            int i; // r7
            int v100; // r6
            int v101; // r0
            //unsigned int v102; // r4
            int v103; // r9
            int v104; // r0
            int v105; // r0
            TileMap v106; // r5
            int v107; // r0
            int v108; // r9
            int v110; // r10
            int v111; // r0
            int v112; // r7
            int v113; // r6
            int v114; // r0
            int v115; // r4
            int v116; // r8
            int v117; // r9
            int v118; // r7
            Tile v119; // r10
            int v120; // r4
            int v121; // r0
            int v122; // r4
            int v123; // r0
            int v124; // r0
            bool v125; // cc
            int v126; // r0
            int v127; // r8 MAPDST
            BattlePlayer v128; // r0
            bool v129; // zf
            int v130; // r0
            int v131; // r0 MAPDST
            int v134; // r4
            int v135; // r4
            int v136; // r0 MAPDST
            int v137; // r0
            int v138; // r0
            int v140; // r4
            int v141; // r6
            int v142; // r5 MAPDST
            int v143; // r6
            int v144; // r7
            int v145; // r4 MAPDST
            int v146; // r6
            int v147; // r5
            int v148; // r0
            int v149; // r4
            int v150; // r4
            int v151; // r8
            int v152; // r0
            int v153; // r0
            int v154; // r4
            int v155; // r0
            int v156; // r0
            int v157; // r6
            int v158; // r4
            int v159; // r6
            int v160; // r0
            //_DWORD* v161; // r0
            Tile v162; // r0
            int v163; // r10
            int v164; // r5
            int v165; // r6
            int v166; // r7
            int v167; // r0
            int v168; // r8
            //int* v170; // r8
            int v171; // r7
            int v172; // r4
            //int** v173; // r0
            Projectile v174; // r4
            //_DWORD* v175; // r6
            int v176; // r0
            int v177; // r0
            int v178; // r0
            int v179; // r0
            int v180; // r0
            //signed int v181; // r7
            int v182; // r8
            int v183; // r4
            int v184; // r5
            int v185; // r0
            int v186; // r0
            int v187; // r1
            int v188; // [sp+4h] [bp-94h]
            ItemData v189; // [sp+2Ch] [bp-6Ch]
            int v190; // [sp+30h] [bp-68h]
            int v191; // [sp+34h] [bp-64h]
            int v192; // [sp+3Ch] [bp-5Ch]
            int v193; // [sp+40h] [bp-58h]
            int Index; // [sp+44h] [bp-54h]
            int v195; // [sp+44h] [bp-54h]
            int v196; // [sp+44h] [bp-54h]
            bool v197; // [sp+48h] [bp-50h]
            int v200; // [sp+4Ch] [bp-4Ch]
            int v203; // [sp+4Ch] [bp-4Ch]
            int v204; // [sp+50h] [bp-48h]
            int v206; // [sp+50h] [bp-48h]
            int v207; // [sp+50h] [bp-48h]
            int v208; // [sp+50h] [bp-48h]
            int v210; // [sp+54h] [bp-44h]
            int v211; // [sp+54h] [bp-44h]
            int v212; // [sp+54h] [bp-44h]
            int v213; // [sp+54h] [bp-44h]
            TileMap v214; // [sp+58h] [bp-40h]
            int v215; // [sp+58h] [bp-40h]
            int v216; // [sp+58h] [bp-40h]
            int v217; // [sp+58h] [bp-40h]
            ProjectileData v218; // [sp+58h] [bp-40h]
            int v219; // [sp+5Ch] [bp-3Ch]
            int v220; // [sp+5Ch] [bp-3Ch] MAPDST
            int v221; // [sp+5Ch] [bp-3Ch]
            int v222; // [sp+5Ch] [bp-3Ch]
            int v223; // [sp+5Ch] [bp-3Ch] MAPDST
            int v226; // [sp+60h] [bp-38h]
            int v228; // [sp+64h] [bp-34h]
            int v229; // [sp+64h] [bp-34h]
            int v230; // [sp+64h] [bp-34h]
            int v231; // [sp+64h] [bp-34h]
            //TileMap v233; // [sp+68h] [bp-30h]
            int v234; // [sp+68h] [bp-30h]
            int v235; // [sp+68h] [bp-30h]
            int v236; // [sp+68h] [bp-30h]
            int v237; // [sp+68h] [bp-30h]

            v3 = TicksGone;
            DeployedTick = v3;
            EndingTick = AreaEffectData.TimeMs / 50 + v3;
            v5 = AreaEffectData.Type;
            //if (v5 <= 0x10 && ((&stru_14080.r_offset + 1) & (1 << v5)) != 0)
            if (v5 == "Damage" || v5 == "Stun" || v5 == "HealAndDamage" || v5 == "SetOnFire" || v5 == "SetOnFireIce" || v5 == "Pushback")
            {
                v214 = GameObjectManager.GetBattle().GetTileMap();
                v219 = -1;
                if (Source != null && Source.GetCardValueForPassive("pushback_self", 1) >= 0)
                    v219 = AreaEffectData.PushbackStrengthSelf;
                if (true)
                {
                    v9 = 0;
                    v197 = v5 == "Stun" || v5 == "SetOnFire" || v5 == "Pushback" || v5 == "SetOnFireIce";
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0)
                            continue;
                        v10 = (Character)gameObject;
                        if (!v10.IsAlive())
                            continue;
                        if (v10.IsImmuneAndBulletsGoThrough(IsInRealm)
                          || v10.GetIndex() != -16 && v10.GetIndex() / 16 == GetIndex() / 16)
                        {
                            if (v219 < 1 || Source != v10 || v10.IsImmuneAndBulletsGoThrough(IsInRealm))
                                continue;
                        }
                        v14 = AreaEffectData.Radius;
                        v15 = v10.CharacterData.CollisionRadius;
                        v16 = v10.GetX();
                        v17 = GetX();
                        v204 = v10.GetY();
                        v18 = v16 - v17;
                        v19 = GetY();
                        v20 = v14 + v15 - 50;
                        if (LogicMath.Abs(v18) > v20
                          || LogicMath.Abs(v204 - v19) > v20
                          || v18 * v18 + (v204 - v19) * (v204 - v19) > (v20 * v20))
                        {
                            continue;
                        }
                        v28 = 0;
                        v30 = GetDamage(v28);
                        if (!v197 && Source != v10)
                        {
                            Index = GetIndex();
                            v32 = GetX();
                            v33 = GetY();
                            if (v10.CauseDamage(Source, v30, NormalDMG, SkillType == 2, AreaEffectData, true))
                            {
                                if (Stun >= 1)
                                    v10.TriggerStun(Stun, false);
                                if (GetCardValueForPassiveFromPlayer("ulti_area_slow", 1) >= 1)
                                {
                                    v10.GiveSpeedSlowerBuff(-GetCardValueForPassiveFromPlayer("ulti_area_slow", 1), GetCardValueForPassiveFromPlayer("ulti_area_slow", 0));
                                }
                            }
                        }
                        if (v5 == "Stun")
                        {
                            v10.TriggerStun(AreaEffectData.CustomValue, false);
                        }
                        if (SkinAreaEffectData.Type == "SetOnFireIce")
                        {
                            v10.ApplyPoison(GetIndex() % 16, v30, NormalDMG, true, Source, 8);
                        }
                        else if (v5 == "SetOnFire")
                        {
                            v10.ApplyPoison(GetIndex() % 16, v30, NormalDMG, true, Source, 2);
                        }
                        if (Source == v10)
                        {
                            v37 = v219;
                            if (v219 <= 0)
                            {
                            LABEL_38:
                                if (v10.KnockBacked)
                                    continue;
                                v37 = 0;
                            }
                        }
                        else
                        {
                            v37 = AreaEffectData.PushbackStrength;
                            if (v37 == 0) continue;
                        }
                        //if (Index == -1 || SkillType != 2)
                        {
                        LABEL_45:
                            if (!CantPushback)
                            {
                                if (AreaEffectData.PushbackDeadzone >= 1)
                                {
                                    v206 = Character.TransformPushBackStrengthToLength(v37);
                                    v195 = GetX();
                                    v40 = GetY();
                                    v41 = v10.GetX();
                                    v42 = v10.GetY();
                                    v43 = GamePlayUtil.GetDistanceBetween(v195, v40, v41, v42);
                                    v44 = v43 - AreaEffectData.PushbackDeadzone;
                                    if (v44 < v206)
                                    {
                                        v45 = LogicMath.Abs(v44);
                                        v46 = LogicMath.Max(150, v45);
                                        v47 = LogicMath.Sign(v44) * v46;
                                        v48 = LogicMath.Sign(v37);
                                        v37 = Character.TransformPushBackLengthToStrength(v47) * v48;
                                    }
                                }
                                v49 = GetX();
                                v50 = GetY();
                                v10.TriggerPushback(
                                  v49,
                                  v50,
                                  v37,
                                  v37 > 0,
                                  1,
                                  Source == v10,
                                  false,
                                  false,
                                  false,
                                  false,
                                  false,
                                  AreaEffectData.CanStopGrapple,
                                  0);
                                if (Source == v10)
                                {
                                    v10.ViusalChargeType = 3;
                                }
                            }
                            continue;
                        }
                    }
                }
                v51 = GetX();
                v52 = GetY();
                v54 = AreaEffectData.Radius;
                v56 = AreaEffectData.DestroysEnvironment;
                v214.DestroyEnvironment(v51, v52, v54, !v56);
                if (v5 != "HealAndDamage") return;
            }
            if (AreaEffectData.Type == "Heal" || AreaEffectData.Type == "HealRegen" || AreaEffectData.Type == "HealAndDamage")
            {

                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    Character v61 = (Character)gameObject;
                    if (v61.GetIndex() / 16 == GetIndex() / 16)
                    {
                        if (v61.IsAlive() && v61.CharacterData.IsHero() && (CanHealOwner || v5 == "HealAndDamage" || Source != v61))
                        {
                            v229 = v61.GetX();
                            v65 = GetX();
                            v215 = v61.GetY();
                            v210 = GetY();
                            v66 = AreaEffectData.Radius + v61.CharacterData.CollisionRadius - 50;
                            v67 = v229 - v65;
                            if (LogicMath.Abs(v229 - v65) <= v66)
                            {
                                v68 = v215 - v210;
                                if (LogicMath.Abs(v215 - v210) <= v66 && v67 * v67 + v68 * v68 <= (v66 * v66))
                                {
                                    if (AreaEffectData.Type == "Heal")
                                    {
                                        v61.Heal(GetIndex(), -GetDamage(0), true, AreaEffectData);
                                    }
                                    else if (v5 == "HealAndDamage") v61.Heal(GetIndex(), GetDamage(0), true, AreaEffectData);
                                    else if (AreaEffectData.Type == "HealRegen")
                                    {
                                        v61.AddExtraHealthRegen(-GetDamage(0), AreaEffectData.CustomValue, Source?.GetIndex() ?? -1);
                                    }
                                }
                            }
                        }
                    }

                }
                return;
            }

            if (AreaEffectData.Type == "WallSpawn")
            {
                int X = GetX() / 300;
                int Y = GetY() / 300;
                TileMap tileMap = GetBattle().GetTileMap();
                int Radius = AreaEffectData.Radius / 300;
                int XMin = LogicMath.Clamp(X - Radius, 0, tileMap.Width - 1);
                int YMin = LogicMath.Clamp(Y - Radius, 0, tileMap.Height - 1);
                int XMax = LogicMath.Clamp(X + Radius, 0, tileMap.Width - 1);
                int YMax = LogicMath.Clamp(Y + Radius, 0, tileMap.Height - 1);
                for (i = XMin; i <= XMax; i++)
                {
                    for (int j = YMin; j <= YMax; j++)
                    {
                        Tile tile = tileMap.GetTile(i, j, true);
                        if (tile == null) continue;
                        if (GamePlayUtil.GetDistanceSquaredBetween(tile.X + 150, tile.Y + 150, X * 300 + 150, Y * 300 + 150) < AreaEffectData.Radius * AreaEffectData.Radius)
                        {
                            if (!tile.Data.BlocksMovement)
                            {
                                tile.Destruct();
                                Tile tile1 = new Tile(SkinAreaEffectData.SkinnedCustomValue, i * 300, j * 300);
                                tile1.PlacerIndex = GetIndex();
                                tileMap.NewTiles.Add(tile1);
                            }
                        }
                    }
                }
            }

            if (AreaEffectData.Type == "PetrolDistribution")
            {
                v106 = GetBattle().GetTileMap();
                v108 = AreaEffectData.Radius;
                v110 = v108 / 300;
                v111 = GetX();
                v112 = v111 / 300;
                v113 = v112 - v110;
                v114 = GetY();
                v115 = v114 / 300;
                v236 = v112 * 300 + 150;
                v231 = v115 * 300 + 150;
                v222 = v112 + v110;
                if (v112 - v110 <= v112 + v110)
                {
                    v116 = v108 * v108;
                    v117 = v115 + v110;
                    v217 = v115 - v110;
                    v212 = v115 - 1 - v110;
                    do
                    {
                        v118 = v212;
                        if (v217 <= v117)
                        {
                            do
                            {
                                if ((++v118 | v113) >= 0)
                                {
                                    v124 = v106.Width;
                                    v125 = v113 < v124;
                                    if (v113 < v124)
                                        v125 = v118 < v106.Height;
                                    if (v125)
                                    {
                                        v119 = v106.GetTile(v113, v118, true);
                                        v120 = v113 * 300 + 150;
                                        v121 = v118 * 300 + 150;
                                        if (GamePlayUtil.GetDistanceSquaredBetween(v236, v231, v120, v121) < v116 && !(v119.Data.BlocksMovement && !v119.Data.BlocksProjectiles && !v119.Data.IsDestructibleNormalWeapon))
                                        {
                                            Petrol petrol = GetBattle().AddPetrol(v113, v118, 150, GetIndex(), GetIndex() / 16, 0);
                                            petrol.Slowing = Source.GetGearBoost(13);
                                        }
                                    }
                                }
                            }
                            while (v118 < v117);
                        }
                        v98 = v113++ < v222;
                    }
                    while (v98);
                }
                return;
            }
            if (v5 == "BulletExplosion")
            {
                v127 = AreaEffectData.CustomValue;
                v218 = DataTables.GetProjectileByName(AreaEffectData.BulletExplosionBullet);
                v189 = DataTables.GetItemByName(AreaEffectData.BulletExplosionItem);
                if (SkillType == 2 && AreaEffectData.Name == "CrowUltiKnifes")
                {
                    v128 = GetPlayer();
                    v129 = v128 == null;
                    if (v128 != null)
                    {
                        v129 = v128.SkinId == 0;
                    }
                    if (!v129 && DataTables.Get(DataType.Projectile).GetData<ProjectileData>(DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(v128.SkinId).Conf).UltiProjectile) != null)
                        v218 = DataTables.Get(DataType.Projectile).GetData<ProjectileData>(DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(v128.SkinId).Conf).UltiProjectile);
                }
                v131 = AreaEffectData.BulletExplosionBulletDistance;
                if (Source != null && SkillType != 2) v131 += Source.GetGearBoost(18);
                if (BulletAngle < 0)
                {
                    v208 = 0;
                    v213 = 0;
                }
                else
                {
                    v134 = 26 * BulletDelta;
                    v213 = LogicMath.GetRotatedX(v134, 0, BulletAngle);
                    v208 = LogicMath.GetRotatedY(v134, 0, BulletAngle);
                }
                if (v218 != null && !v218.Indirect)
                {
                    v136 = 0;
                    v135 = 0;
                }
                else
                {
                    v135 = 15;
                    v136 = 1;
                }
                v190 = -1;
                if (GetIndex() == -16 || SkillType != 1)
                {
                    v191 = -1;
                }
                else
                {
                    v191 = GetPlayer().GetCardValueForPassive("curve_ball", 1);
                    if (v191 >= 0)
                    {
                        v190 = GetPlayer().GetCardValueForPassive("curve_ball", 0);
                    }
                }
                if (v127 >= 1)
                {
                    List<Projectile> L = new List<Projectile>();
                    v193 = 100 * v131;
                    v192 = 0x168 / v127;
                    v223 = 0;
                    v237 = 200000;
                    v196 = v135;
                    do
                    {
                        v140 = LogicMath.NormalizeAngle360(v135 + v192 * v223);
                        v141 = GetX();
                        v142 = LogicMath.GetRotatedX(v193, 0, v140) + v141;
                        v143 = GetY();
                        v144 = LogicMath.GetRotatedY(v193, 0, v140) + v143;
                        if (v136 > 0)
                        {
                            v145 = LogicMath.Sqrt(v237);
                        }
                        else
                        {
                            v146 = v142 + v213 - GetX();
                            v147 = v144 + v208 - GetY();
                            v148 = LogicMath.Sqrt(v146 * v146 + v147 * v147);
                            if (v148 > 0)
                            {
                                v149 = v148;
                                v146 = 200 * v146 / v148;
                                v147 = 200 * v147 / v149;
                            }
                            v150 = GetX();
                            v151 = GetY();
                            v154 = LogicMath.Clamp(v150 + v146, 1, MapWidth - 2);
                            v157 = LogicMath.Clamp(v151 + v147, 1, MapHeight - 2);
                            v158 = v154 / 300;
                            v159 = v157 / 300;

                            //v162 = ZNK12LogicTileMap7getTileEii(v161, v158, v159);
                            v145 = 0;
                            v162 = GameObjectManager.GetBattle().GetTileMap().GetTile(v158, v159, true);
                            if (!v162.IsDestructed() && v162.Data.BlocksProjectiles)
                                goto LABEL_157;
                        }
                        if (v218 != null)
                        {
                            v163 = v144 + v208;
                            v164 = v142 + v213;
                            if (v190 >= 0)
                            {
                                v165 = v164 - GetX();
                                v166 = v163 - GetY();
                                v167 = LogicMath.Sqrt(v165 * v165 + v166 * v166);
                                if (v167 > 0)
                                {
                                    v168 = v167;
                                    v165 = v165 * v190 / v167;
                                    v166 = v166 * v190 / v168;
                                }
                                v163 += v166;
                                v164 += v165;
                            }
                            v171 = GetDamage(0);
                            v172 = NormalDMG;
                            v174 = Projectile.ShootProjectile(
                                     -1,
                                     -1,
                                     Source,
                                     this,
                                     v218,
                                     v164,
                                     v163,
                                     v171,
                                     v172,
                                     0,
                                     false,
                                     v145,
                                     GameObjectManager.GetBattle(),
                                     0,
                                     SkillType);
                            //*v174->gap84 = dword64;
                            L.Add(v174);
                            v174.SetPreyOnTheWeak(PreyOnTheWeakPercent, PreyOnTheWeakDamage);
                            if (v191 >= 0)
                                v174.Curving = v191;
                        }
                        else
                        {
                        }
                    LABEL_157:
                        v135 = v196;
                        //v237 = (&stru_30D40 + v237);
                        v237 = (0 + v237);

                        v3 += 2;
                        --v127;
                        ++v223;
                    }
                    while (v127 > 0);
                    if (AreaEffectData.Name == "CrossBomberClusterExplosion" || AreaEffectData.Name == "CrossBomberUltiClusterExplosion")
                    {
                        foreach (Projectile projectile in L)
                        {
                            foreach (Projectile projectile1 in L)
                            {
                                if (projectile == projectile1) continue;
                                projectile.LinkedProjectileGIDs.Add(projectile1.GetGlobalID());
                            }
                        }
                    }
                }
            }

        }
        public static int GetRangeAgainst(AreaEffectData a1, Character a2)
        {
            return a1.Radius + a2.CharacterData.CollisionRadius - 50;
        }
        public override void Tick()
        {
            int v2; // r0
            int v3; // r5
            int v5; // r0 MAPDST
            int v6; // r4
            int v8; // r6 MAPDST
            int v9; // r0
            int v10; // r7
            int v11; // r0
            int v12; // r0 MAPDST
            int v13; // r0
            int v14; // r7
            int v16; // r8
            int v17; // r10
            int v18; // r7
            int v19; // r0
            int v20; // r0
            AreaEffect v21; // r5
            int v22; // r4
            int v23; // r6
            int v24; // r0
            int v27; // r5
            int v28; // r6
            int v29; // r0
            int v31; // r8
            int v32; // r6
            int v34; // r5
            int v35; // r6
            int v36; // r7
            int v37; // r9 MAPDST
            int v38; // r8
            int v39; // r10
            int i; // r10
            int v52; // r0
            bool v53; // zf
            int v54; // r7
            int v55; // r4
            Character v56; // r9
            int v57; // r8
            bool v58; // r0
            bool v59; // r7
            bool v60; // r6
            bool v61; // r7
            int v62; // r8
            bool v63; // r6
            int v64; // r4
            int v65; // r0
            int v66; // r7
            int v67; // r1
            int v68; // r6
            int v69; // r0
            int v70; // zf
            int v71; // r4
            int v72; // r0
            int v73; // r0
            int v74; // r8
            //int* v76; // r9
            int v77; // r4
            int v78; // r0
            //unsigned int v79; // r1
            int v80; // r4
            int v81; // r0
            int v82; // r4
            int v83; // r0
            int v84; // r7
            int v85; // r4
            int v86; // r2
            int v87; // r0
            int v88; // r4
            int v89; // r0
            int v90; // r4
            int v91; // r0
            bool v92; // zf
            int v93; // r0
            int v94; // r8
            int v95; // r5
            int v96; // r6
            int v97; // r7
            int v98; // r4
            int v99; // r1
            int v100; // r0
            int v101; // r0 MAPDST
            Character v112; // r0
            bool v113; // r5
            int v114; // r7
            int v115; // r4
            int v116; // r5
            int v117; // r6
            int v118; // r6
            int v119; // r0
            int v120; // r0
            int v121; // r7
            int v122; // r8
            //int* v123; // r4
            int v124; // r6
            int v125; // r0
            int v126; // r2
            int v131; // r9
            int v132; // r6
            int v133; // r0
            //_BOOL4 v134; // r4
            int v135; // r0
            int v136; // r4
            int v137; // r0
            int v138; // r7
            int v139; // r4
            int v140; // r0
            //unsigned int v141; // r0
            int v142; // r4
            int v143; // r0
            int v144; // r4
            int v145; // r0
            int v146; // r0
            int v147; // r0
            int v148; // r6
            int v149; // r0

            v3 = TicksGone;
            LifeTimeConsumedPercent = (int)(float)((float)(1000.0 / (float)(EndingTick - DeployedTick))
                                           * (float)(v3 - DeployedTick));
            //v5 = ZNK21LogicGameObjectServer7getDataEv(a1);
            v6 = EndingTick;
            if (v3 >= v6)
            {
                //v13 = a1->gap74[13];
                ShouldBeDestructed = true;
                AreaEffectData chainData = AreaEffectData.ChainAreaEffect == null
                    ? null
                    : DataTables.GetAreaEffectByName(AreaEffectData.ChainAreaEffect);
                if (chainData != null)
                {
                    v21 = GameObjectFactory.CreateGameObjectByData(chainData);
                    v21.SetPosition(GetX(), GetY(), GetZ());
                    v21.SetIndex(GetIndex());
                    v21.SetSource(this.Source, this.SkillType);
                    if (Damage > 0)
                    {
                        v21.Damage = Damage;
                        v21.NormalDMG = NormalDMG;
                    }
                    GameObjectManager.AddGameObject(v21);
                    v21.Trigger();
                }
                return;
            }
            if (AreaEffectData.Type=="Vision"|| AreaEffectData.Type == "Dot" || AreaEffectData.Type == "Hot" || AreaEffectData.Type == "DelayedDamage" || AreaEffectData.Type == "DamageBoost" || AreaEffectData.Type == "Slow")  // 4 5 6 7 12 13 14 15 16 17 18 19
            {
                if (AreaEffectData.Type == "Vision")
                    v10 = 1;
                else if (AreaEffectData.Type == "Slow"||AreaEffectData.Type=="Vision")
                    v10 = 10;
                else
                    v10 = 20;
                EffectTimer++;
                if (AreaEffectData.Type != "DelayedDamage" && EffectTimer >= v10 || AreaEffectData.Type == "DelayedDamage" && (TicksGone == EndingTick - AreaEffectData.TimeMs / 50 + AreaEffectData.CustomValue / 50))
                {
                    v148 = GetDamage(0);
                    //v13 = *(a1 + 128);
                    IsActive = true;
                    //v149 = v8;
                    int v151;
                    if (Source != null)
                        v151 = Source.GetCardValueForPassive("cactus_heal", 1);
                    else
                        v151 = -1;
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        v56 = (Character)gameObject;
                        v57 = GetIndex() / 16;
                        if (v56.GetIndex() / 16 == v57)
                        {
                            v58 = false;
                        }
                        else
                        {
                            v58 = AreaEffectData.Type == "Dot" || AreaEffectData.Type == "DelayedDamage" || AreaEffectData.Type == "Slow" || aoe_dot > 0;
                            if (v57 != -1)
                            {
                                v60 = false;
                                goto LABEL_53;
                            }
                        }
                        if (AreaEffectData.Type != "DamageBoost" && AreaEffectData.Type != "Hot")
                        {
                            v60 = false;
                            goto LABEL_53;
                        }
                        v60 = v56.CharacterData.IsHero() && v56.GetIndex() / 16 == v57;
                    LABEL_53:
                        v61 = v151 != -1 && AreaEffectData.Type == "Dot" && GetIndex() == v56.GetIndex();
                        v63 = v56.CharacterData.IsTrain();
                        if (v56.IsAlive() && !v56.IsImmuneAndBulletsGoThrough(IsInRealm) && !(v63 | (!v58 && !v60 && !v61)))
                        {
                            v64 = AreaEffectData.Radius;
                            v65 = v64 + v56.CharacterData.CollisionRadius - 50;
                            v66 = v56.GetX();
                            v67 = v66 - GetX();
                            v68 = v56.GetY();
                            v69 = GetY();
                            if (LogicMath.Abs(v67) <= v65)
                            {
                                v70 = v68 - v69;
                                if (LogicMath.Abs(v70) <= v65 && v67 * v67 + v70 * v70 <= v65 * v65)
                                {
                                    if (AreaEffectData.Type == "Dot" || AreaEffectData.Type == "DelayedDamage")
                                    {
                                        v71 = v56.GetIndex();
                                        v72 = GetIndex();
                                        if (v71 == v72)
                                        {
                                            v56.Heal(v71, v151, true, AreaEffectData);
                                        }
                                        else
                                        {
                                            if (v56.CauseDamage(
                                                    Source,
                                                    v148,
                                                    NormalDMG,
                                                    SkillType == 2,
                                                    AreaEffectData,
                                                    true))
                                            {
                                                if (AreaEffectData.SlowStrength >= 1)
                                                {
                                                    int FS = -AreaEffectData.SlowStrength;
                                                    if (Source?.GetGearBoost(10) >= 1)
                                                    {
                                                        FS += FS * Source.GetGearBoost(10) / 100;
                                                    }
                                                    v56.GiveSpeedSlowerBuff(FS, AreaEffectData.FreezeTicks);
                                                }
                                                if (AreaEffectData.SameAreaEffectCanNotDamageMs >= 1)
                                                {
                                                    v56.SetImmunity(GetIndex(), AreaEffectData.SameAreaEffectCanNotDamageMs, AreaEffectData);
                                                }
                                            }
                                        }
                                        if (AreaEffectData.PushbackStrength >= 1)
                                        {
                                            v56.TriggerPushback(GetX(), GetY(), AreaEffectData.PushbackStrength, true, 1, false, false, false, false, false, false, false, 0);
                                        }
                                    }
                                    else if (AreaEffectData.Type == "Slow")
                                    {
                                        if (!v56.IsImmuneAndBulletsGoThrough(IsInRealm))
                                            v56.GiveSpeedSlowerBuff(-AreaEffectData.SlowStrength, AreaEffectData.FreezeTicks);
                                    }
                                    else if (AreaEffectData.Type == "Hot")
                                    {
                                        if (v56.GetIndex() / 16 == GetIndex() || aoe_dot < 1)
                                        {
                                            if (v56.Heal(GetIndex(), -GetDamage(0), true, AreaEffectData)
                                              && AreaEffectData.SameAreaEffectCanNotDamageMs >= 1)
                                            {
                                                v56.SetImmunity(GetIndex(), AreaEffectData.SameAreaEffectCanNotDamageMs, AreaEffectData);
                                            }
                                        }

                                        else
                                        {
                                            v56.CauseDamage(Source, aoe_dot, 0, SkillType == 2, AreaEffectData, true);
                                        }
                                    }
                                    else if (AreaEffectData.Type == "Vision")
                                    {
                                        v56.SetForcedVisible();
                                    }
                                    else
                                    {
                                        v56.GiveDamageBuff2(GetDamage(0), 21);
                                    }

                                }
                            }
                        }
                    }
                    if (AreaEffectData.Type == "Dot" || AreaEffectData.Type == "DelayedDamage")
                    {
                        GameObjectManager.GetBattle().GetTileMap().DestroyEnvironment(GetX(), GetY(), AreaEffectData.Radius, !AreaEffectData.DestroysEnvironment);
                    }
                    EffectTimer = 0;
                }
                return;
            }
            if (AreaEffectData.Type == "SmokeScreen")
            {
                EffectTimer++;
                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    if (gameObject.GetIndex() / 16 != GetIndex() / 16) continue;
                    Character c = (Character)gameObject;
                    if (!c.CharacterData.IsHero()) continue;
                    if (Position.GetDistance(c.GetPosition()) > AreaEffectData.Radius) continue;
                    c.SetInvisible();
                    if (Damage < 0 && EffectTimer >= 20) c.Heal(GetIndex(), -Damage, true, AreaEffectData);
                }
                if (Damage > 0 || Source?.GetGearBoost(12) >= 1)
                {
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        if (gameObject.GetIndex() / 16 == GetIndex() / 16) continue;
                        Character c = (Character)gameObject;
                        if (c.IsImmuneAndBulletsGoThrough(IsInRealm)) continue;
                        if (Position.GetDistance(c.GetPosition()) > AreaEffectData.Radius) continue;
                        if (EffectTimer >= 20 && Damage > 0) c.CauseDamage(Source, Damage, 0, false, AreaEffectData, true);
                        if (Source?.GetGearBoost(12) >= 1) c.Cripple(Source.GetGearBoost(12), 2);
                    }
                }
                if (EffectTimer >= 20) EffectTimer = 0;
            }
            if (AreaEffectData.Type == "SpeedBuff")
            {
                foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    v112 = (Character)gameObject;
                    if (v112.IsAlive())
                    {
                        v113 = AreaEffectData.IsPersonal ? v112.GetIndex() == GetIndex() : v112.GetIndex() / 16 == GetIndex() / 16;
                        if (v113 && v112.CharacterData.IsHero() && v112.IsAlive())
                        {
                            if ((v112.GetX() - GetX()) * (v112.GetX() - GetX()) + (v112.GetY() - GetY()) * (v112.GetY() - GetY()) <= AreaEffectData.Radius * AreaEffectData.Radius)
                            {
                                switch (AreaEffectData.Type)
                                {
                                    case "SpeedBuff":
                                        v112.GiveSpeedFasterBuff(AreaEffectData.Damage, AreaEffectData.CustomValue, false);
                                        break;
                                }
                            }

                        }
                    }
                }
            }
            if (AreaEffectData.Type == "AllyChargeSuper" || AreaEffectData.Type == "EnemyChargeSuper")
            {
                EffectTimer++;
                if (EffectTimer % 20 == 0)
                {
                    foreach (GameObject gameObject in GameObjectManager.GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        v112 = (Character)gameObject;
                        if (v112.IsAlive() && v112 != Source)
                        {
                            v113 = v112.GetIndex() / 16 == GetIndex() / 16;
                            if (AreaEffectData.Type == "EnemyChargeSuper") v113 = !v113;
                            if (v113 && v112.CharacterData.IsHero() && v112.IsAlive())
                            {
                                if ((v112.GetX() - GetX()) * (v112.GetX() - GetX()) + (v112.GetY() - GetY()) * (v112.GetY() - GetY()) <= AreaEffectData.Radius * AreaEffectData.Radius)
                                {
                                    Source?.ChargeUlti(AreaEffectData.CustomValue, false, true, GetPlayer(), Source);
                                }

                            }
                        }
                    }
                    EffectTimer = 0;
                }
            }
        LABEL_96:;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int OwnObjectIndex, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);

            // Сверено с LogicAreaEffectClient::decode (0x100444d10), v64.
            if (AreaEffectData.Type == "DelayedDamage") bitStream.WriteBoolean(IsActive);
            bitStream.WritePositiveIntMax1023(LifeTimeConsumedPercent);

            // v64: при PlaybackType == 4 клиент читает ещё Max127.
            if (AreaEffectData.PlaybackType == 4) bitStream.WritePositiveIntMax127(0);

            bitStream.WriteBoolean(false);
            bitStream.WritePositiveVIntMax65535OftenZero(DisplayScale);
            bitStream.WritePositiveVIntMax65535OftenZero(0);

            // v64: финальный флаг клиент читает ТОЛЬКО при RotationType 1 или 3
            // (условие (RotationType & ~2) == 1). Безусловная запись давала лишний бит
            // на каждом area effect и разъезжала весь дальнейший поток зрения.
            if (AreaEffectData.RotationType == 1 || AreaEffectData.RotationType == 3)
            {
                if (bitStream.WriteBoolean(false)) bitStream.WritePositiveIntMax511(0);
            }
        }

        public void SetSource(Character source, int SkillType)
        {
            Source = source;
            this.SkillType = SkillType;
        }

        public void SetDamage(int damage)
        {
            Damage = damage;
        }

        public override int GetRadius()
        {
            return EffectData.Radius;
        }

        public override bool ShouldDestruct()
        {
            return ShouldBeDestructed;
        }

        public override int GetObjectType()
        {
            return 2;
        }
    }
}
