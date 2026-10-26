namespace HashCore.Laser.Logic.Data
{
    public class ProjectileData : LogicData
    {
        public ProjectileData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }
        public int GetScaleStart()
        {
            return ScaleStart == 0 ? 100 : ScaleStart;
        }
        public int GetScaleEnd()
        {
            return ScaleEnd == 0 ? 100 : ScaleEnd;
        }
        public int GetDamagePercentStart()
        {
            return DamagePercentStart == 0 ? 100 : DamagePercentStart;
        }
        public int GetDamagePercentEnd()
        {
            return DamagePercentEnd == 0 ? 100 : DamagePercentEnd;
        }
        public int GetFreezeDuration()
        {
            return FreezeDurationMS / 50;
        }
        public int GetStunLengthTicks()
        {
            return StunLengthMS / 50;
        }
        public bool ExecuteChainOnNoHit()
        {
            return ExecuteChainSpecialCase == 1;
        }
        public bool ExecuteChainAlways()
        {
            return ExecuteChainSpecialCase == 2;
        }
        public bool ExecuteChainOnObjectHit()
        {
            return ExecuteChainSpecialCase == 0 && ChainBullet != null;
        }
        public bool ExecuteChainOnAnyHit()
        {
            return ExecuteChainSpecialCase == 3;
        }
        public bool DamagesConstantly => DamagesConstantlyTickDelay != 0;
        public string Name { get; set; }

        public string ParentProjectileForSkin { get; set; }

        public int Speed { get; set; }

        public string FileName { get; set; }

        public string BlueSCW { get; set; }

        public string RedSCW { get; set; }

        public string BlueExportName { get; set; }

        public string RedExportName { get; set; }

        public string ShadowExportName { get; set; }

        public string BlueGroundGlowExportName { get; set; }

        public string RedGroundGlowExportName { get; set; }

        public string PreExplosionBlueExportName { get; set; }

        public string PreExplosionRedExportName { get; set; }

        public int PreExplosionTimeMs { get; set; }

        public string HitEffectEnv { get; set; }

        public string HitEffectChar { get; set; }

        public string MaxRangeReachedEffect { get; set; }

        public string CancelEffect { get; set; }

        public int Radius { get; set; }

        public bool Indirect { get; set; }

        public bool ConstantFlyTime { get; set; }

        public int TriggerWithDelayMs { get; set; }

        public int BouncePercent { get; set; }

        public int Gravity { get; set; }

        public int EarlyTicks { get; set; }

        public int HideTime { get; set; }

        public int Scale { get; set; }

        public int RandomStartFrame { get; set; }

        public string SpawnAreaEffectObject { get; set; }

        public string SpawnAreaEffectObject2 { get; set; }

        public int AreaEffect2DamagePercent { get; set; }

        public string SpawnCharacter { get; set; }

        public string SpawnItem { get; set; }

        public string SpawnAreaEffectTrail { get; set; }

        public string TrailEffect { get; set; }

        public string SpecialTrailEffect { get; set; }

        public bool ShotByHero { get; set; }

        public bool IsBeam { get; set; }

        public bool IsBouncing { get; set; }

        public int DistanceAddFromBounce { get; set; }

        public string Rendering { get; set; }

        public bool PiercesCharacters { get; set; }

        public bool PiercesEnvironment { get; set; }

        public bool PiercesEnvironmentLikeButter { get; set; }

        public int PushbackStrength { get; set; }
        public int PushbackType { get; set; }

        public int ChainsToEnemies { get; set; }

        public int ChainBullets { get; set; }

        public int ChainSpread { get; set; }

        public int ChainTravelDistance { get; set; }

        public string ChainBullet { get; set; }

        public int ExecuteChainSpecialCase { get; set; }

        public int DamagePercentStart { get; set; }

        public int DamagePercentEnd { get; set; }

        public int DamagesConstantlyTickDelay { get; set; }

        public int FreezeStrength { get; set; }

        public int FreezeDurationMS { get; set; }

        public int PartialStunPromille { get; set; }

        public int StunLengthMS { get; set; }

        public int ScaleStart { get; set; }

        public int ScaleEnd { get; set; }

        public bool AttractsPet { get; set; }

        public int LifeStealPercent { get; set; }

        public bool PassesEnvironment { get; set; }

        public int PoisonDamagePercent { get; set; }
        public int SuppressHealing { get; set; }
        public int SuppressHealingTicks { get; set; }
        public int ConsumableShield { get; set; }
        public int ConsumableShieldTicks { get; set; }

        public bool DamageOnlyWithOneProj { get; set; }

        public int HealOwnPercent { get; set; }

        //public bool CanGrowStronger { get; set; }

        public bool SpecialVisualState { get; set; }

        public bool HideFaster { get; set; }

        public bool GrapplesEnemy { get; set; }

        public int KickBack { get; set; }

        public bool UseColorMod { get; set; }

        public int RedAdd { get; set; }

        public int GreenAdd { get; set; }

        public int BlueAdd { get; set; }

        public int RedMul { get; set; }

        public int GreenMul { get; set; }

        public int BlueMul { get; set; }

        public bool GroundBasis { get; set; }

        public int MinDistanceForSpread { get; set; }

        public bool IsFriendlyHomingMissile { get; set; }

        public bool IsBoomerang { get; set; }

        public bool CanHitAgainAfterBounce { get; set; }

        public bool IsHomingMissile { get; set; }

        public int UltiChargeChangePercent { get; set; }

        public int PoisonType { get; set; }

        public int AppliedEffectVisualType { get; set; }

        public int TravelType { get; set; }

        public int TravelTypeVariable { get; set; }

        public bool IgnoreLevelBoarder { get; set; }
        /// <summary>Колонка IgnoreLevelBorder (старое свойство выше названо с опечаткой и не грузится). Нужна кадру: клиент по ней читает X,Y 16-битными числами.</summary>
        public bool IgnoreLevelBorder { get; set; }

        public int SteerStrength { get; set; }

        public int SteerIgnoreTicks { get; set; }

        public int HomeDistance { get; set; }

        public int SteerLifeTime { get; set; }

        public int UniqueProperty { get; set; }
    }
}
