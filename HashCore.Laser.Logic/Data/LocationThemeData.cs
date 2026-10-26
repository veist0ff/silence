namespace HashCore.Laser.Logic.Data
{
    public class LocationThemeData : LogicData
    {
        public LocationThemeData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
        public bool Disabled { get; set; }
        public string TileSetPrefix { get; set; }
        public string BannerSWF { get; set; }
        public string BannerExportName { get; set; }
        public bool Island { get; set; }
        public string Music { get; set; }
        public string GroundModel { get; set; }
        public string EnvironmentModel { get; set; }
        public string MaskedEnvironmentModel { get; set; }
        public string CampaignGroundModel { get; set; }
        public string Wall1Model { get; set; }
        public string Wall1Submesh { get; set; }
        public int Wall1AngleStep { get; set; }
        public string Wall2Model { get; set; }
        public string Wall2Submesh { get; set; }
        public int Wall2AngleStep { get; set; }
        public string CrateModel { get; set; }
        public string CrateSubmesh { get; set; }
        public int CrateAngleStep { get; set; }
        public string BarrelModel { get; set; }
        public string BarrelSubmesh { get; set; }
        public int BarrelAngleStep { get; set; }
        public string ThemedModel { get; set; }
        public string ThemedSubmesh { get; set; }
        public int ThemedAngleStep { get; set; }
        public string Grass { get; set; }
        public string ClutterModel { get; set; }
        public string ClutterSubmesh { get; set; }
        public int ClutterAngleStep { get; set; }
        public string ClutterModel_CN { get; set; }
        public string ClutterSubmesh_CN { get; set; }
        public int ClutterAngleStep_CN { get; set; }
        public string FragileModel { get; set; }
        public string FragileSubmesh { get; set; }
        public int FragileAngleStep { get; set; }
        public string FragileModel_CN { get; set; }
        public string FragileSubmesh_CN { get; set; }
        public int FragileAngleStep_CN { get; set; }
        public string WaterTileModel { get; set; }
        public string FenceModel { get; set; }
        public string RopeFenceModel { get; set; }
        public string TrainTrackModel { get; set; }
        public string IndestructibleFenceModel { get; set; }
        public string IndestructibleModel { get; set; }
        public string IndestructibleSubmesh { get; set; }
        public string Damageable1Model { get; set; }
        public string Damageable1Submesh { get; set; }
        public int Damageable1AngleStep { get; set; }
        public string Damageable2Model { get; set; }
        public string Damageable2Submesh { get; set; }
        public int Damageable2AngleStep { get; set; }
        public string Damageable3Model { get; set; }
        public string Damageable3Submesh { get; set; }
        public int Damageable3AngleStep { get; set; }
        public string Damageable4Model { get; set; }
        public string Damageable4Submesh { get; set; }
        public int Damageable4AngleStep { get; set; }
        public string IndestructibleDeco1Model { get; set; }
        public string IndestructibleDeco1Submesh { get; set; }
        public int IndestructibleDeco1AngleStep { get; set; }
        public string IndestructibleDeco2Model { get; set; }
        public string IndestructibleDeco2Submesh { get; set; }
        public int IndestructibleDeco2AngleStep { get; set; }
        public string IndestructibleDeco3Model { get; set; }
        public string IndestructibleDeco3Submesh { get; set; }
        public int IndestructibleDeco3AngleStep { get; set; }
        public string IndestructibleDeco4Model { get; set; }
        public string IndestructibleDeco4Submesh { get; set; }
        public int IndestructibleDeco4AngleStep { get; set; }
        public string GroundFillerModel { get; set; }
        public string LaserBallSkinOverride { get; set; }
        public string MineGemSpawnModelOverride { get; set; }
        public string LootBoxSkinOverride { get; set; }
        public string SafeSkinOverride { get; set; }
        public string SafeCharacterOverride { get; set; }
        public string ShowdownBoostModelOverride { get; set; }
        public int MapPreviewBGColorRed { get; set; }
        public int MapPreviewBGColorGreen { get; set; }
        public int MapPreviewBGColorBlue { get; set; }
        public int MapPreviewSkyColorRed { get; set; }
        public int MapPreviewSkyColorGreen { get; set; }
        public int MapPreviewSkyColorBlue { get; set; }
        public string MapPreviewGemGrabSpawnHoleExportName { get; set; }
        public string MapPreviewBallExportName { get; set; }
        public string MapPreviewGoal1ExportName { get; set; }
        public string MapPreviewGoal2ExportName { get; set; }
        public string MapPreviewSafeExportName { get; set; }
        public string MapPreviewCNOverrides { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public string StageFrameName { get; set; }
        public string BgFrameName { get; set; }
        public string MatchmakingFrameName { get; set; }
        public string FogVFXExportName { get; set; }
        public string SkullsVFXExportName { get; set; }
    }
}
