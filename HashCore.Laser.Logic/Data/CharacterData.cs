using HashCore.Laser.Logic.Data.Helper;
using HashCore.Laser.Logic.Data.Reader;

namespace HashCore.Laser.Logic.Data
{
    public class CharacterData : LogicData
    {
        public CharacterData(Row row, DataTable datatable) : base(row, datatable)
        {
            // Используем родную загрузку
            LoadData(this, GetType(), row);
        }

        /// <summary>
        /// Номер типа, как его хранит клиент в LogicCharacterData +548. Таблица
        /// снята с разбора колонки Type (LogicCharacterData::createReferences):
        /// на неизвестной строке клиент падает «Character has invalid type!».
        /// </summary>
        public int TypeId => Type switch
        {
            "Hero" => 0,
            "Npc_Boss" => 1,
            "Minion_FollowOwner" => 2,
            "Minion_FindEnemies" => 3,
            "Minion_Building" => 4,
            "Pvp_Base" => 6,
            "Minion_Building_charges_ulti" => 7,
            "Minion_Dog" => 8,
            "LootBox" => 10,
            "Minion_FindEnemies2" => 11,
            "RoboWars" => 12,
            "Train" => 13,
            "Minion_Mirage" => 15,
            "Npc_Boss_TownCrush" => 16,
            "Carryable" => 17,
            "Minion_Duplicate" => 18,
            "Payload" => 19,
            "Minion_Invasion" => 20,
            "Minion_LastStand" => 21,
            "Minion_Percenter" => 22,
            "Minion_Twin" => 23,
            "Minion_Critter" => 24,
            "Minion_Orbiting" => 25,
            "Her0" => 26,
            "ArenaMinion" => 27,
            "ArenaJungle" => 28,
            "Decoration" => 29,
            "Mega_Boss" => 30,
            _ => -1
        };

        /// <summary>LogicCharacterData::isHero клиента: Hero и Her0.</summary>
        public bool IsHeroType => TypeId == 0 || TypeId == 26;

        // --- ЛОГИЧЕСКИЕ МЕТОДЫ ---
        public bool IsDecoy() => Type == "Minion_Mirage";
        public bool IsBoss() => Type == "Npc_Boss" || Type == "Npc_Boss_TownCrush";
        public bool IsTrain() => Type == "Train";
        public bool IsBase() => Type == "Pvp_Base";
        public bool IsLootBox() => Type == "LootBox";
        public bool IsTrainingDummy() => Type == "Minion_Building_charges_ulti";
        public bool IsTownCrushBoss() => Type == "Npc_Boss_TownCrush";
        public bool IsCarryable() => Type == "Carryable";
        public bool IsHero() => Type == "Hero";
        public bool IsPet() => Pet != null;
        public bool IsDuplicate() => Type == "Minion_Duplicate";
        public bool IsPayload() => Type == "Payload";
        public bool IsMirrage() => Type == "Minion_Mirage";
        public bool IsTwin() => Type == "Minion_Twin";
        public bool IsMinionLastStand() => Type == "Minion_LastStand";
        public bool HasAutoAttack() => AutoAttackDamage > 0;
        public bool HasVeryMuchHitPoints() => Hitpoints > 5799;

        // --- ВСЕ ПЕРЕМЕННЫЕ ---
        public string Name { get; set; }
        public bool LockedForChronos { get; set; }
        public bool Disabled { get; set; }
        public string ItemName { get; set; }
        public string WeaponSkill { get; set; }
        public string UltimateSkill { get; set; }
        public string OverchargedUltimateSkill { get; set; }
        public string Pet { get; set; }
        public string AltPet { get; set; }
        public string MovementType { get; set; }
        public int Speed { get; set; }
        public int Hitpoints { get; set; }
        public string Traits { get; set; }
        public string Components { get; set; }
        public int HealthPromilleLostPerSecond { get; set; }
        public int HealPercentChange { get; set; }
        public int LifeTimeTicks { get; set; }
        public bool SkipDeploy { get; set; }
        public bool MeleeAutoAttackSplashDamage { get; set; }
        public int DelayTicksFirstAttack { get; set; }
        public int AutoAttackSpeedMs { get; set; }
        public int AutoAttackDamage { get; set; }
        public int AutoAttackBulletsPerShot { get; set; }
        public string AutoAttackMode { get; set; }
        public int AutoAttackTicksBetweenBurstShots { get; set; }
        public int AutoAttackProjectileSpread { get; set; }
        public string AutoAttackProjectile { get; set; }
        public string OverchargedAutoAttackProjectile { get; set; }
        public int AutoAttackRange { get; set; }
        public int RegeneratePerSecond { get; set; }
        public bool LowPriorityTarget { get; set; }
        public int UltiUses { get; set; }
        public int UltiChargeInitial { get; set; }
        public int UltiChargeMul { get; set; }
        public int UltiChargeUltiMul { get; set; }
        public int OverchargeChargeMul { get; set; }
        public int OverchargePassiveChargeMul { get; set; }
        public int OverchargeChargeActiveMul { get; set; }
        public int OverchargeDamagePercent { get; set; }
        public int OverchargeSpeedPercent { get; set; }
        public int OverchargeShieldPercent { get; set; }
        public string Type { get; set; }
        public bool IsNeutral { get; set; }
        public bool IsBossPet { get; set; }
        public string CarryableType { get; set; }
        public string DefaultSkin { get; set; }
        public string Mastery { get; set; }
        public bool ManualRotations { get; set; }
        public string FileName { get; set; }
        public string BlueExportName { get; set; }
        public string RedExportName { get; set; }
        public string ShadowExportName { get; set; }
        public string OverchargedEffect { get; set; }
        public bool OverchargedUseDefaultSkin { get; set; }
        public string StatusEffect { get; set; }
        public string AreaEffect { get; set; }
        public string DeathAreaEffect { get; set; }
        public string DeathOverchargedAreaEffect { get; set; }
        public string TakeDamageEffect { get; set; }
        public string KillEffect { get; set; }
        public string DeathEffect { get; set; }
        public string DeathEffectOvercharged { get; set; }
        public bool AllowDeathEffectOverride { get; set; }
        public string MoveEffect { get; set; }
        public string ReloadEffect { get; set; }
        public string OutOfAmmoEffect { get; set; }
        public string DryFireEffect { get; set; }
        public string SpawnEffect { get; set; }
        public string OverchargedSpawnEffect { get; set; }
        public string MeleeHitEffect { get; set; }
        public string AutoAttackStartEffect { get; set; }

        // ВОТ ЗДЕСЬ ИСПРАВЛЕНИЕ: BoneEffectUse теперь string!
        public string BoneEffect1 { get; set; }
        public string BoneEffectUse1 { get; set; }
        public string BoneEffect2 { get; set; }
        public string BoneEffectUse2 { get; set; }
        public string BoneEffect3 { get; set; }
        public string BoneEffectUse3 { get; set; }
        public string BoneEffect4 { get; set; }
        public string BoneEffectUse4 { get; set; }
        public string BoneEffect5 { get; set; }
        public string BoneEffectUse5 { get; set; }
        public string BoneEffect6 { get; set; }
        public string BoneEffectUse6 { get; set; }

        public string LoopedEffect { get; set; }
        public string LoopedEffect2 { get; set; }
        public string KillCelebrationSoundVO { get; set; }
        public string InLeadCelebrationSoundVO { get; set; }
        public string StartSoundVO { get; set; }
        public string UseUltiSoundVO { get; set; }
        public string TakeDamageSoundVO { get; set; }
        public string DeathSoundVO { get; set; }
        public string AttackSoundVO { get; set; }
        public int AttackStartEffectOffset { get; set; }
        public int TwoWeaponAttackEffectOffset { get; set; }
        public int ShadowScaleX { get; set; }
        public int ShadowScaleY { get; set; }
        public int ShadowX { get; set; }
        public int ShadowY { get; set; }
        public int ShadowSkew { get; set; }
        public int Scale { get; set; }
        public int HeroScreenScale { get; set; }
        public int FitToBoxScale { get; set; }
        public int EndScreenScale { get; set; }
        public int GatchaScreenScale { get; set; }
        public int HomeScreenScale { get; set; }
        public int HeroScreenXOffset { get; set; }
        public int HeroScreenZOffset { get; set; }
        public int CollisionRadius { get; set; }
        public string HealthBar { get; set; }
        public int HealthBarOffsetY { get; set; }
        public int FlyingHeight { get; set; }
        public int ProjectileStartZ { get; set; }
        public int StopMovementAfterMS { get; set; }
        public int WaitMS { get; set; }
        public string TID { get; set; }
        public bool ForceAttackAnimationToEnd { get; set; }
        public string IconSWF { get; set; }
        public string IconExportName { get; set; }
        public int RecoilAmount { get; set; }
        public string FootstepClip { get; set; }
        public int DifferentFootstepOffset { get; set; }
        public int FootstepIntervalMS { get; set; }
        public int AttackingWeaponScale { get; set; }
        public bool UseThrowingLeftWeaponBoneScaling { get; set; }
        public bool UseThrowingRightWeaponBoneScaling { get; set; }
        public int CommonSetUpgradeBonus { get; set; }
        public int RareSetUpgradeBonus { get; set; }
        public int SuperRareSetUpgradeBonus { get; set; }
        public bool CanWalkOverWater { get; set; }
        public bool UseColorMod { get; set; }
        public int RedAdd { get; set; }
        public int GreenAdd { get; set; }
        public int BlueAdd { get; set; }
        public int RedMul { get; set; }
        public int GreenMul { get; set; }
        public int BlueMul { get; set; }
        public int ChargeUltiAutomatically { get; set; }
        public string VideoLink { get; set; }
        public bool ShouldEncodePetStatus { get; set; }
        public bool SecondaryPet { get; set; }
        public int ExtraMinions { get; set; }
        public int PetAutoSpawnDelay { get; set; }
        /// <summary>
        /// Колонки `HasPowerLevels` в characters.csv НЕТ — есть `PowerLevelsType`.
        /// Свойство с несуществующим именем `Row` молча оставлял пустым, поэтому
        /// уровни силы не работали в бою вообще. Считаем его по настоящей колонке.
        ///
        /// Тип стоит ровно у двоих: PowerLeveler (1) и Crab (2).
        /// </summary>
        public bool HasPowerLevels => PowerLevelsType > 0;

        public int PowerLevelsType { get; set; }

        /// <summary>
        /// Прибавка к скорости за уровень силы. У PowerLeveler 170, у Crab 100 —
        /// в бою стояло жёстко 170 для обоих.
        /// </summary>
        public int PowerLevelsSpeedBuff { get; set; }

        public int OffenseRating { get; set; }
        public int DefenseRating { get; set; }
        public int UtilityRating { get; set; }
        public int UniqueProperty { get; set; }
        public int UniquePropertyValue1 { get; set; }
        public int UniquePropertyValue2 { get; set; }

        // ------------------------------------------------------------------
        // Колонки, которые читает клиент (LogicCharacterData::resetStatics,
        // 0x1003f1d64), а сервер не загружал вовсе. Имена сверены с шапкой
        // characters.csv один в один: расхождение имени не даёт ни ошибки, ни
        // предупреждения — только пустое поле.
        // ------------------------------------------------------------------

        // Интро боя и экран бойца
        public int BattleIntroScale { get; set; }
        public int BattleIntroXOffset { get; set; }
        public int BattleIntroZOffset { get; set; }
        public bool ShowPetInHeroScreen { get; set; }
        public bool AlwaysShowWithPet { get; set; }
        public int HeroScreenEmoteX { get; set; }
        public int HeroScreenEmoteY { get; set; }
        public string EmblemExportName { get; set; }
        public int FloorIndicatorScale { get; set; }
        public int HealthBarScale { get; set; }

        // Движение и эффекты
        public string HyperMoveEffect { get; set; }
        public bool HumanoidSpecies { get; set; }
        public string WaterFootstepEffect { get; set; }

        // Ульта
        public bool ChargeUltiFromDamage { get; set; }
        public bool ChargeUltiFromHealing { get; set; }

        // Питомцы
        public bool PetRotatable { get; set; }
        public bool IsExternalPet { get; set; }
        public int PetSpawnCount { get; set; }

        // Озвучка разблокировки
        public string UnlockVO { get; set; }
        public int UnlockVOIndex { get; set; }

        // Прочее
        public bool ShowHealInBattleEnd { get; set; }
        public string ClassArchetype { get; set; }
        public int OrbitingRadius { get; set; }
        public int OrbitingAngularSpeed { get; set; }

        // Переопределения для арены
        public int ArenaOverrideMaxHpBuff { get; set; }
        public int ArenaOverrideDmgBuff { get; set; }
        public int ArenaOverrideSpeedBuff { get; set; }
        public int ArenaOverrideReloadBuff { get; set; }
    }
}
