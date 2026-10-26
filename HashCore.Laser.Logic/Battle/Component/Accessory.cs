namespace HashCore.Laser.Logic.Battle.Structures
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http.Headers;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Intrinsics;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using HashCore.Laser.Logic.Battle.Component;
    using HashCore.Laser.Logic.Battle.Level;
    using HashCore.Laser.Logic.Battle.Objects;
    using HashCore.Laser.Logic.Battle.Structures;
    using HashCore.Laser.Logic.Data;
    using HashCore.Laser.Logic.Data.Helper;

    using HashCore.Laser.Logic.Util;

    using HashCore.Laser.Titan.Math;

    public class Accessory
    {
        public AccessoryData AccessoryData;
        public int Uses;
        public int CoolDown;
        public int State;
        public string Type => AccessoryData.Type;
        public int ActivationDelay;
        public int X;
        public int Y;
        public int StartUsingTick;
        public int Angle;
        public bool IsActive;
        public int TicksActive;

        public int RewindX;
        public int RewindY;
        public int RewindHP;
        public int TrailX;
        public int TrailY;
        public Accessory(AccessoryData accessoryData)
        {
            X = 0;
            Y = 0;
            State = 0;
            ActivationDelay = 0;
            StartUsingTick = 0;
            CoolDown = 0;
            Uses = 3;
            Angle = 0;
            IsActive = false;
            TicksActive = 0;
            AccessoryData = accessoryData;
        }
        public void UpdateAccessory(Character character)
        {
            State = CheckCurrentAccessoryAvailability(character);
            CoolDown = LogicMath.Max(0, CoolDown - 1);
            if (IsActive)
            {
                if (ActivationDelay < 1)
                {
                    if (TicksActive >= AccessoryData.ActiveTicks && AccessoryData.Type != "ulti_change")
                    {
                        IsActive = false;
                        CoolDown = AccessoryData.Cooldown;
                    }
                    else
                    {
                        TickAccessory(character);
                        TicksActive++;
                    }
                }
                else
                {
                    ActivationDelay--;
                    if (ActivationDelay == 0) ActivateAccessory(character);
                }
            }
            if (character.m_isBot == 2 || (character.GetPlayer()?.IsAdmin ?? false))
            {
                if (character.GetPlayer()?.IsAdmin ?? false) CoolDown = 0;
                Uses = 3;
                State = 0;
            }
        }
        public void TickAccessory(Character character)
        {
            switch (Type)
            {
                case "spin_shoot":
                    character.SetForcedAngle(LogicMath.NormalizeAngle360(character.MoveAngle + 90));
                    if (TicksActive % AccessoryData.CustomValue1 == 0)
                    {
                        ProjectileData v17 = DataTables.GetProjectileByName(AccessoryData.CustomObject);
                        if (v17?.Name == character.m_skills[0].SkillData.Projectile)
                        {
                            if (character.GetPlayer()?.SkinConfData?.MainAttackProjectile != null) v17 = DataTables.GetProjectileByName(character.GetPlayer()?.SkinConfData?.MainAttackProjectile);
                        }
                        if (v17 != null)
                        {
                            StartUsingTick = character.TicksGone;
                            int v25 = (TicksActive / AccessoryData.CustomValue1) * AccessoryData.CustomValue2;
                            int v27 = AccessoryData.CustomValue4 + AccessoryData.CustomValue5 * (TicksActive / AccessoryData.CustomValue1);
                            TileMap v29 = character.GetBattle().GetTileMap();
                            int v33 = 1;//todo:Play Field Mirrored for player
                            int v34 = LogicMath.GetRotatedX(v27, 0, v25) * v33;
                            int v35 = LogicMath.GetRotatedY(v27, 0, v25) * v33;
                            int v36 = character.GetX() + v34;
                            int v37 = character.GetY() + v35;
                            int v38 = LogicMath.Clamp(v36, 1, v29.LogicWidth - 2);
                            int v39 = LogicMath.Clamp(v37, 1, v29.LogicHeight - 2);
                            int v40 = 300 * v34 / v27 + character.GetX();
                            int v43 = 300 * v35 / v27 + character.GetY();
                            Projectile v46 = Projectile.ShootProjectile(v40, v43, character, character, v17, v38, v39, AccessoryData.CustomValue3, AccessoryData.CustomValue6, 0, false, 0, character.GetBattle(), 0, 4);
                            if (character.GetCardValueForPassive("pushback_self", 1) >= 0) v46.AreaEffectCantPushback = true;
                            v46.RunEarlyTicks();
                        }
                    }
                    break;
                case "repeat_shot":
                    if (TicksActive % AccessoryData.CustomValue1 != 0) return;
                    int v79, v80, v83, v85, v87;
                    if (AccessoryData.SubType != 1)
                    {
                        if (AccessoryData.SubType != 2) return;
                        v79 = 0;
                        v80 = 0;
                        Projectile v103 = Projectile.ShootProjectile(
                                character.GetX(),
                                character.GetY(),
                                character,
                                character,
                                DataTables.GetProjectileByName(AccessoryData.CustomObject),
                                v79,
                                v80,
                                AccessoryData.CustomValue2,
                                0,
                                0,
                                false,
                                0,
                                character.GetBattle(),
                                0,
                                4);
                        v103.RunEarlyTicks();
                        return;
                    }
                    if (AccessoryData.TargetFriends)
                    {
                        return;
                    }
                    else
                    {
                        Character v61 = GetClosestEnemyForAccessory(character);
                        if (v61 != null)
                        {
                            v79 = v61.GetX();
                            v80 = v61.GetY();
                            if (!AccessoryData.TargetIndirect)
                            {
                                v83 = v79 - character.GetX();
                                v85 = v80 - character.GetY();
                                v87 = LogicMath.Sqrt(v85 * v85 + v83 * v83);
                                v79 = v83 * AccessoryData.Range * 300 / v87 + character.GetX();
                                v80 = v85 * AccessoryData.Range * 300 / v87 + character.GetY();
                            }
                            Angle = LogicMath.GetAngle(v61.GetX() - character.GetX(), v61.GetY() - character.GetY());
                            //LABEL_85:
                            Projectile v103 = Projectile.ShootProjectile(
                                character.GetX(),
                                character.GetY(),
                                character,
                                character,
                                DataTables.GetProjectileByName(AccessoryData.CustomObject),
                                v79,
                                v80,
                                AccessoryData.CustomValue2,
                                0,
                                0,
                                false,
                                0,
                                character.GetBattle(),
                                0,
                                4);
                            v103.RunEarlyTicks();

                        }
                    }
                    break;
                case "rewind":
                    State = 0;
                    CoolDown = AccessoryData.Cooldown;
                    if (TicksActive == AccessoryData.ActiveTicks - 1)
                    {
                        AreaEffectData v12 = DataTables.GetAreaEffectByName(AccessoryData.CustomObject);
                        character.TriggerBlink(RewindX, RewindY, v12, v12, 0, 0);
                        character.m_hitpoints = RewindHP;
                        foreach (GameObject gameObject in character.GetGameObjectManager().GetGameObjects())
                        {
                            if (gameObject.GetObjectType() != 2) continue;
                            AreaEffect areaEffect = (AreaEffect)gameObject;
                            if (areaEffect.AreaEffectData.Name != AccessoryData.AreaEffect || areaEffect.GetIndex() != character.GetIndex()) continue;
                            character.GetGameObjectManager().RemoveGameObject(areaEffect);
                        }
                    }
                    break;
                case "trail":
                    if (GamePlayUtil.GetDistanceSquaredBetween(character.GetX(), character.GetY(), TrailX, TrailY) <= AccessoryData.CustomValue1 * AccessoryData.CustomValue1)
                    {
                        break;
                    }
                    character.TriggerAreaEffect(DataTables.GetAreaEffectByName(AccessoryData.CustomObject), character.GetX(), character.GetY(), 0, 4);
                    TrailX = character.GetX();
                    TrailY = character.GetY();
                    StartUsingTick = character.TicksGone;
                    break;
                case "repeat_area":
                    if (TicksActive % AccessoryData.CustomValue1 == 0)
                    {
                        AreaEffectData v7 = DataTables.GetAreaEffectByName(AccessoryData.AreaEffect);
                        if (v7 != null)
                        {
                            character.TriggerAreaEffect(v7, character.GetX(), character.GetY(), 0, 4);
                            StartUsingTick = character.TicksGone;
                        }
                    }
                    break;
            }
        }
        public int GetActivationAngle(Character character)
        {
            if (X == 0 && Y == 0)
                return character.MoveAngle;
            return LogicMath.GetAngle(X, Y);

        }
        public void ActivateAccessory(Character character)
        {
            RewindHP = 0;
            switch (Type)
            {
                case "jump":
                    character.TriggerPushback(character.GetX() - LogicMath.GetRotatedX(1000, 0, GetActivationAngle(character)), character.GetY() - LogicMath.GetRotatedY(1000, 0, GetActivationAngle(character)), AccessoryData.CustomValue1, true, 0, true, true, false, true, true, false, false, 0);
                    break;
                case "vision":

                    //} эта шлюха не работает так что пока
                    Console.WriteLine("Active");
                    break;
                case "dash":
                    int Angle = GetActivationAngle(character);
                    int Dis = AccessoryData.CustomValue1;
                    character.TriggerCharge(character.GetX() + LogicMath.GetRotatedX(Dis, 0, Angle), character.GetY() + LogicMath.GetRotatedY(Dis, 0, Angle), AccessoryData.CustomValue2, 5, AccessoryData.CustomValue4, AccessoryData.CustomValue3, false);
                    break;
                case "teleport_to_pet":
                    Character character1 = character.GetActivePet(false);
                    if (character1 == null) break;
                    AreaEffectData v14 = DataTables.GetAreaEffectByName(AccessoryData.CustomObject);
                    character.TriggerBlink(character1.GetX(), character1.GetY(), v14, v14, 0, 0);
                    break;
                case "cc_immunity":
                    break;
                case "heal":
                    if (AccessoryData.SubType == 1) character.Heal(character.GetIndex(), AccessoryData.CustomValue1 == 0 ? (character.m_maxHitpoints - character.m_hitpoints) : AccessoryData.CustomValue1, true, null);
                    else if (AccessoryData.SubType == 2) character.AddExtraHealthRegen(AccessoryData.CustomValue1, AccessoryData.ActiveTicks, character.GetIndex());
                    break;
                case "spawn":
                    switch (AccessoryData.SubType)
                    {
                        case 1:
                            Character v136 = Character.SummonMinion(
                            DataTables.GetCharacterByName(AccessoryData.CustomObject),
                            character.GetX(),
                            character.GetY() + 150,
                            0,
                            1,
                            1,
                            0,
                            0,
                            character.GetBattle(),
                            character.GetIndex() % 16,
                            character.GetIndex() / 16,
                            character.GetGlobalID(),
                            true, false, 0, false, 0
                            );
                            break;
                        case 4:
                            Character v137 = GetClosestEnemyForAccessory(character);
                            if (v137 != null)
                            {
                                AreaEffect v141 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(AccessoryData.CustomObject));
                                v141.SetPosition(v137.GetX(), v137.GetY(), 0);
                                v141.SetIndex(character.GetIndex());
                                v141.SetSource(character, 4);
                                v141.SetDamage(AccessoryData.CustomValue1);
                                v141.NormalDMG = 0;
                                character.GetGameObjectManager().AddGameObject(v141);
                                v141.Trigger();
                            }
                            break;
                        case 5:
                            for (int i = 0; i < AccessoryData.CustomValue1; i++)
                            {
                                int x = character.GetX() + LogicMath.GetRotatedX(AccessoryData.CustomValue2, 0, i * AccessoryData.CustomValue3 - AccessoryData.CustomValue3 / 4);
                                int y = character.GetY() + LogicMath.GetRotatedY(AccessoryData.CustomValue2, 0, i * AccessoryData.CustomValue3 - AccessoryData.CustomValue3 / 4);
                                Character.SummonMinion(
                            DataTables.GetCharacterByName(AccessoryData.CustomObject),
                            x,
                            y,
                            0,
                            1,
                            AccessoryData.CustomValue1,
                            0,
                            0,
                            character.GetBattle(),
                            character.GetIndex() % 16,
                            character.GetIndex() / 16,
                            character.GetGlobalID(),
                            true, false, 0, false, 0
                            );
                            }
                            break;
                    }
                    break;
                case "throw_opponent":
                    Character v60 = GetClosestEnemyForAccessory(character);
                    if (v60 != null)
                    {
                        int v63 = v60.GetX() - character.GetX();
                        int v65 = v60.GetY() - character.GetY();
                        if (v65 == 0 && v63 == 0) ;
                        character.SetForcedAngle(LogicMath.GetAngle(v63, v65));
                        v60.TriggerPushback(v60.GetX() + v63, v60.GetY() + v65, AccessoryData.CustomValue1, true, 0, true, false, false, true, false, false, false, 0);

                    }
                    break;
                case "consume_bush":
                    Tile tile = GetClosestBush(character.GetX(), character.GetY(), character.GetBattle().GetTileMap());
                    if (tile == null) break;
                    tile.Destruct();
                    character.Heal(character.GetIndex(), AccessoryData.CustomValue1, true, null);
                    break;
                case "promote_minion":
                    break;
                case "reload":
                    character.GetSkill("Attack").AddCharge(character, AccessoryData.CustomValue1);
                    break;
                case "kill_projectile":
                    Projectile v11 = character.GetControlledProjectile();
                    if (v11 != null)
                    {
                        AreaEffectData v141 = DataTables.GetAreaEffectByName(AccessoryData.CustomObject);
                        character.TriggerBlink(v11.GetX(), v11.GetY(), v141, v141, 0, 0);
                        v11.TargetReached(5);
                    }
                    break;
                case "teleport_forward":
                    break;
                case "poison_trigger":
                    foreach (GameObject gameObject in character.GetGameObjectManager().GetGameObjects())
                    {
                        if (gameObject.GetObjectType() != 0) continue;
                        Character v100 = (Character)gameObject;
                        if (v100.IsAlive() && v100.GetIndex() / 16 != character.GetIndex() / 16)
                        {
                            if (v100.Poisons.Count >= 1)
                            {
                                foreach (Poison poison in v100.Poisons)
                                {
                                    if (poison.SourceIndex == character.GetIndex() % 16)
                                    {
                                        v100.GiveSpeedSlowerBuff(-AccessoryData.CustomValue1, AccessoryData.CustomValue2);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                case "reload_speed":
                    character.GiveReloadBuff(AccessoryData.CustomValue1);
                    break;
                case "rewind":
                    RewindX = character.GetX();
                    RewindY = character.GetY();
                    RewindHP = character.m_hitpoints;
                    break;
                case "shield_pet":
                    Character v105 = character.GetActivePet(false);
                    if (v105 != null)
                    {
                        v105.AddShield(AccessoryData.ActiveTicks, AccessoryData.CustomValue1);
                    }
                    break;
                case "following_area":
                    AreaEffectData v110 = DataTables.GetAreaEffectByName(AccessoryData.CustomObject);
                    if (v110 != null)
                    {
                        character.AddAreaEffect(0, 0, v110, 4, false);
                    }
                    break;
                case "reposition_wall":
                    TileMap v112 = character.GetBattle().GetTileMap();
                    if (v112.NewTiles.Count >= 1)
                    {
                        foreach (Tile tile1 in v112.NewTiles)
                        {
                            if (tile1.PlacerIndex == character.GetIndex())
                            {
                                tile1.Destruct();
                            }
                        }
                    }
                    character.GetPlayer()?.ChargeUlti(4000, false, true);
                    break;
                case "consumable_shield":
                    character.AddConsumableShield(AccessoryData.CustomValue1, AccessoryData.CustomValue2);
                    break;
                case "movement_speed":
                    character.GiveSpeedFasterBuff(AccessoryData.CustomValue1 * character.CharacterData.Speed / 100, AccessoryData.CustomValue2 * 20, false);
                    character.GiveReloadBuff(AccessoryData.CustomValue2 * 20);
                    break;
                case "random_ulti":
                    string skill = null;
                    SkillData skillData = character.m_skills[1].SkillData;
                    switch (character.GetBattle().GetRandomInt(0, 4))
                    {
                        case 0:
                            skill = skillData.SecondarySkill;
                            break;
                        case 1:
                            skill = skillData.SecondarySkill2;
                            break;
                        case 2:
                            skill = skillData.SecondarySkill3;
                            break;
                        case 3:
                            skill = skillData.SecondarySkill4;
                            break;
                    }
                    character.m_skills[1] = new Skill(DataTables.Get(DataType.Skill).GetData<SkillData>(skill).GetGlobalId(), true);
                    break;
                case "random_buff":
                    switch (character.GetBattle().GetRandomInt(0, 4))
                    {
                        case 0:
                            character.GiveDamageBuff(AccessoryData.CustomValue2, AccessoryData.CustomValue1);
                            break;
                        case 1:
                            character.AddExtraHealthRegen(AccessoryData.CustomValue3, AccessoryData.CustomValue1, character.GetIndex());
                            break;
                        case 2:
                            character.GiveSpeedFasterBuff(AccessoryData.CustomValue4, AccessoryData.CustomValue1, true);
                            break;
                        case 3:
                            character.GiveReloadBuff(AccessoryData.CustomValue1);
                            break;
                    }
                    break;
                case "explode_souls":
                    if (AccessoryData.SubType == 0)
                    {
                        Character c = character.GetClosestVisibleEnemy();
                        if (c == null) break;
                        foreach (GameObject gameObject in character.GetGameObjectManager().GetGameObjects())
                        {
                            if (gameObject.GetIndex() == character.GetIndex() && gameObject.GetObjectType() == 3)
                            {
                                Projectile.ShootProjectile(gameObject.GetX(), gameObject.GetY(), character, gameObject, DataTables.GetProjectileByName("SoulCollectorProjectile"), c.GetX(), c.GetY(), 1410, 0, 0, false, 0, character.GetBattle(), 0, 4).RunEarlyTicks();
                                character.GetGameObjectManager().RemoveGameObject(gameObject);
                            }
                        }
                    }
                    break;
            }
            if (Type != "repeat_area")
            {
                AreaEffectData v150 = DataTables.GetAreaEffectByName(AccessoryData.AreaEffect);
                if (v150 != null)
                {
                    character.TriggerAreaEffect(v150, character.GetX(), character.GetY(), 0, 4);
                }
            }
            AreaEffectData v155 = DataTables.GetAreaEffectByName(AccessoryData.PetAreaEffect);
            if (v155 != null)
            {
                Character v157 = character.GetActivePet(false);
                if (v157 != null)
                {
                    v157.TriggerAreaEffect(v155, v157.GetX(), v157.GetY(), 0, 4);
                }
            }
            if (AccessoryData.ShieldPercent >= 1)
            {
                character.AddShield(AccessoryData.ShieldTicks, AccessoryData.ShieldPercent);
            }
            if (AccessoryData.SpeedBoost >= 1)
            {
                character.GiveSpeedFasterBuff(AccessoryData.SpeedBoost, AccessoryData.SpeedBoostTicks, false);
            }
            if (AccessoryData.ActiveTicks < 1 && AccessoryData.Type != "ulti_change")
            {
                IsActive = false;
                CoolDown = AccessoryData.Cooldown;
            }
            else
            {
                TicksActive = 0;
                TickAccessory(character);
                ++TicksActive;
            }
            if ((character.GetPlayer()?.IsAdmin ?? false))
            {
                CoolDown = 0;
                State = 0;
            }
        }
        public Character GetClosestEnemyForAccessory(Character character)
        {
            Character e = character.GetClosestEnemy();
            if (e == null || e.GetPosition().GetDistance(character.GetPosition()) > AccessoryData.Range * 300) return null;
            return e;
        }
        public static Tile GetClosestBush(int a2, int a3, TileMap a4)
        {
            int X = a2 / 300;
            int Y = a3 / 300;
            TileMap tileMap = a4;
            int XMin = LogicMath.Clamp(X - 1, 0, tileMap.Width - 1);
            int YMin = LogicMath.Clamp(Y - 1, 0, tileMap.Height - 1);
            int XMax = LogicMath.Clamp(X + 1, 0, tileMap.Width - 1);
            int YMax = LogicMath.Clamp(Y + 1, 0, tileMap.Height - 1);
            int v14 = 999999999;
            Tile result = null;
            for (int i = XMin; i <= XMax; i++)
            {
                for (int j = YMin; j <= YMax; j++)
                {
                    Tile tile = tileMap.GetTile(i, j, true);
                    if (tile == null || !tile.Data.HidesHero || tile.IsDestructed()) continue;
                    if (GamePlayUtil.GetDistanceSquaredBetween(tile.X + 150, tile.Y + 150, X * 300 + 150, Y * 300 + 150) < v14)
                    {
                        result = tile;
                        v14 = GamePlayUtil.GetDistanceSquaredBetween(tile.X + 150, tile.Y + 150, X * 300 + 150, Y * 300 + 150);
                    }
                }
            }
            return result;
        }
        public int CheckCurrentAccessoryAvailability(Character character)
        {
            int result; // x0
            int v5; // w22
            Character v6; // x0
            int v7; // x21
            int v8; // w22
            int v9; // w23
            int v10; // w24
            int v11; // w0
            int v12; // w22
            int v13; // w0
            int v14; // w8
            int v15; // x0
            int v16; // x20
            int v17; // w21
            int v18; // x0
            Skill v19; // x0
            Projectile v20; // x0
            int v21; // x0
            int v22; // x0
            int v23; // x20
            int v24; // x22
            char v25; // w23
            int v26; // x21
            int v27; // x8
            int v28; // x10
            bool v29; // w11
            int v30; // x0
            int v31; // x0
            int v32; // x22
            //_QWORD* v33; // x20
            int v34; // x24
            char v35; // w23
            char v36; // w25
            int v37; // x21
            int v38; // w8
            bool v39; // zf
            int v40; // w8
            SkillData v41; // x0
            SkillData v42; // x19
            int active; // w20
            int v44; // w20
            int v45; // x0
            int v46; // x0
            int v47; // x8
            int v48; // x9
            int v49; // x10
            int v50; // x11

            if (character == null || !character.IsAlive())
                return 3;
            if (IsActive)
                return 1;
            if (Uses < 1)
                return 4;
            if (CoolDown > 0)
                return 5;
            if (character.IsPlayerControlRemoved()
              && (!character.IsCharging || AccessoryData.UsableDuringCharge == 0)
              && (Type != "cc_immunity" || character.IsCharging))
            {
                return 2;
            }
            if (AccessoryData.RequirePetDistance >= 1)
            {
                v6 = character.GetActivePet(Type == "promote_minion");
                if (v6 == null)
                    return 7;
                v12 = GamePlayUtil.GetDistanceSquaredBetween(character.GetX(), character.GetY(), v6.GetX(), v6.GetY());
                v13 = AccessoryData.RequirePetDistance;
                if (v12 > 300 * v13 * 300 * v13)
                    return 8;
                if (v6.GetZ() > 100)
                    return 2;
            }
            if (AccessoryData.ConsumesAmmo && character.GetWeaponSkill().Charge < 1000)
                return 9;
            if (AccessoryData.RequireEnemyInRange
              && GetClosestEnemyForAccessory(character) == null)
            {
                return 8;
            }
            if (AccessoryData.UsableDuringCharge == 2 && !character.IsCharging)
                return 14;
            if (AccessoryData.SkipTypeCondition)
                return 0;
            switch (Type)
            {
                case "heal":
                    if (character.m_hitpoints != character.m_maxHitpoints)
                        return 0;
                    return 10;
                case "reload":
                    v19 = character.GetSkill("Attack");
                    if (!v19.HasFullCharge())
                    {
                        goto LABEL_48;
                    }
                    return 12;
                case "consume_bush":
                    if (GetClosestBush(character.GetX(), character.GetY(), character.GetBattle().GetTileMap()) != null) return 0;
                    return 8;
                default:
                LABEL_48:
                    switch (Type)
                    {
                        case "movement_speed":
                            if (AccessoryData.SubType == 1)
                            {
                                if (character.CharacterData.UniqueProperty == 7) return 7;
                            }
                            return 0;
                        case "dash":
                            if (AccessoryData.SubType == 1)
                            {
                                if (character.CharacterData.UniqueProperty != 7) return 7;
                            }
                            return 0;
                        case "kill_projectile":
                            v20 = character.GetControlledProjectile();
                            if (v20 != null && v20.State == 0)
                                return 0;
                            return 7;
                        case "teleport_forward":
                        case "turret_barrage":
                        case "pet_attack_speed":
                        case "reload_speed":
                        case "rewind":
                        case "shield_pet":
                        case "following_area":
                            return 0;
                        case "poison_trigger":
                            foreach (GameObject gameObject in character.GetGameObjectManager().GetGameObjects())
                            {
                                if (gameObject.GetObjectType() != 0) continue;
                                Character v100 = (Character)gameObject;
                                if (v100.IsAlive() && v100.GetIndex() / 16 != character.GetIndex() / 16)
                                {
                                    if (v100.Poisons.Count >= 1)
                                    {
                                        foreach (Poison poison in v100.Poisons)
                                        {
                                            if (poison.SourceIndex == character.GetIndex() % 16)
                                            {
                                                return 0;
                                            }
                                        }
                                    }
                                }
                            }
                            return 7;
                        case "mine_trigger":
                            return 7;
                        case "next_attack_change":
                            v41 = character.GetCurrentActiveOrCastingSkill();
                            if (v41 == null)
                                return 0;
                            v42 = v41;
                            active = v41.ActiveTime;
                            if (active < (2 * v42.MsBetweenAttacks))
                                return 0;
                            return 2;
                        case "reposition_wall":
                            TileMap v112 = character.GetBattle().GetTileMap();
                            if (v112.NewTiles.Count >= 1)
                            {
                                foreach (Tile tile in v112.NewTiles)
                                {
                                    if (tile.PlacerIndex == character.GetIndex())
                                    {
                                        return 0;
                                    }
                                }
                            }
                            return 7;
                        default:
                            if (Type != "spawn" || AccessoryData.SubType != 3 || false)
                                return 0;
                            return 13;
                    }
                    return 0;
            }

        }
        public void TriggeredAgain(Character character)
        {
            if (Type == "rewind")
            {
                TicksActive = AccessoryData.ActiveTicks - 1;
            }
        }
        public void TriggerAccessory(Character character,
        int X,
        int Y)
        {
            if (IsActive && Type == "rewind")
            {
                TriggeredAgain(character);
                return;
            }
            State = CheckCurrentAccessoryAvailability(character);
            bool result = false;
            if (character != null)
            {
                if (State == 0 || (character.GetPlayer()?.IsAdmin ?? false))
                {
                    result |= CoolDown > 0;
                    if (!result || (character.GetPlayer()?.IsAdmin ?? false))
                    {                                         // 来真的了
                        if (AccessoryData.InterruptsAction)
                            character.InterruptAllSkills();
                        if (AccessoryData.StopMovement)
                            character.StopMovement();
                        IsActive = true;
                        this.X = X;
                        this.Y = Y;
                        //a1->field_44 = 0;
                        StartUsingTick = character.TicksGone;
                        int v13 = AccessoryData.ActivationDelay;
                        if (v13 <= 0)
                        {
                            ActivateAccessory(character);
                        }
                        else
                        {
                            ActivationDelay = v13;
                        }
                        character.BlockHealthRegen();
                        Uses--;
                    }
                }
            }
        }
        public void EndAccessoryActivation()
        {
            if (IsActive)
            {
                IsActive = false;
                CoolDown = AccessoryData.Cooldown;
            }
        }
        public void Interrupt(bool a2, Character a3)
        {
            bool result; // r0

            result = false;
            if (Type == "next_attack_change" || Type == "ulti_change")
                result = a3.m_hitpoints > 0;
            if (!result)
            {
                result = IsActive;
                if (IsActive)
                {
                    if (!a2)
                    {
                        IsActive = false;
                        CoolDown = AccessoryData.Cooldown;
                    }
                }
            }
        }
        public void Encode(BitStream stream, bool IsOwn)
        {
            if (IsOwn)
            {

                stream.WritePositiveVIntMax255OftenZero(CoolDown);
                stream.WritePositiveVIntMax255OftenZero(State);
            }
            else
            {
                stream.WriteBoolean(State == 1);
            }
            if (State == 1)
            {
                stream.WritePositiveInt(StartUsingTick, 14);
                stream.WritePositiveInt(Angle, 9);
            }
            stream.WritePositiveIntMax7(Uses);
        }
    }
}
