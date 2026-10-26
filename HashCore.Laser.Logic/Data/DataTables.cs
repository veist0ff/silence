global using HashCore.Laser.Logic.Data.Helper;
global using HashCore.Laser.Logic.Data.Reader;

namespace HashCore.Laser.Logic.Data
{
    using HashCore.Laser.Logic.Data.Helper;
    using HashCore.Laser.Logic.Data.Reader;
    using HashCore.Laser.Server;

    public partial class DataTables
    {
        public static readonly Dictionary<DataType, string> Gamefiles = new Dictionary<DataType, string>();
        private static Gamefiles Tables;

        private static void AddFilesToLoad()
        {
            Gamefiles.Add(DataType.Projectile, "Assets/csv_logic/projectiles.csv");
            Gamefiles.Add(DataType.AllianceBadge, "Assets/csv_logic/alliance_badges.csv");
            Gamefiles.Add(DataType.Location, "Assets/csv_logic/locations.csv");
            Gamefiles.Add(DataType.GameModeVariation, "Assets/csv_logic/game_mode_variations.csv");
            Gamefiles.Add(DataType.Character, "Assets/csv_logic/characters.csv");
            Gamefiles.Add(DataType.AreaEffect, "Assets/csv_logic/area_effects.csv");
            Gamefiles.Add(DataType.StatusEffect, "Assets/csv_logic/status_effects_logic.csv");
            Gamefiles.Add(DataType.Item, "Assets/csv_logic/items.csv");
            Gamefiles.Add(DataType.Map, "Assets/csv_logic/maps.csv");
            Gamefiles.Add(DataType.Skill, "Assets/csv_logic/skills.csv");
            Gamefiles.Add(DataType.Card, "Assets/csv_logic/cards.csv");
            Gamefiles.Add(DataType.Tile, "Assets/csv_logic/tiles.csv");
            Gamefiles.Add(DataType.PlayerThumbnail, "Assets/csv_logic/player_thumbnails.csv");
            Gamefiles.Add(DataType.Skin, "Assets/csv_logic/skins.csv");
            Gamefiles.Add(DataType.Milestone, "Assets/csv_logic/milestones.csv");
            Gamefiles.Add(DataType.SkinConf, "Assets/csv_logic/skin_confs.csv");
            Gamefiles.Add(DataType.Accessory, "Assets/csv_logic/accessories.csv");
            Gamefiles.Add(DataType.Emote, "Assets/csv_logic/emotes.csv");
            Gamefiles.Add(DataType.Gear, "Assets/csv_logic/gear_boosts.csv");
            Gamefiles.Add(DataType.NameColor, "Assets/csv_logic/name_colors.csv");
            Gamefiles.Add(DataType.Titul, "Assets/csv_logic/player_titles.csv");
            Gamefiles.Add(DataType.MasteryVanity, "Assets/csv_logic/mastery_hero_confs.csv");
            Gamefiles.Add(DataType.Mastery, "Assets/csv_logic/mastery_levels.csv");
            Gamefiles.Add(DataType.Spray, "Assets/csv_logic/sprays.csv");
            Gamefiles.Add(DataType.RankedMilestones, "Assets/csv_logic/competitive_pass_tiers.csv");
            Gamefiles.Add(DataType.TrophyWorldMilestone, "Assets/csv_logic/trophy_world_milestones.csv");
            Gamefiles.Add(DataType.RandomReward, "Assets/csv_logic/random_rewards.csv");
            Gamefiles.Add(DataType.RandomRewardContainer, "Assets/csv_logic/random_reward_containers.csv");
            Gamefiles.Add(DataType.BrawlerBoxReward, "Assets/csv_logic/brawler_box_rewards.csv");
            Gamefiles.Add(DataType.Region, "Assets/csv_logic/regions.csv");
            
        }

        public static void Load()
        {
            DataTables.AddFilesToLoad();

            Tables = new Gamefiles();

            foreach (var file in Gamefiles)
            {
                var path = Path.Combine(AppContext.BaseDirectory, file.Value);
                Tables.Initialize(new Table(path), file.Key);
                //Debugger.Print(file.Value);
            }

            Logger.Print($"[LogicDataTable::loadTable] load {Gamefiles.Count} table");
        }

        public static bool TableExists(int t)
        {
            return Tables.ContainsTable(t);
        }

        public static DataTable Get(DataType classId)
        {
            return Tables.Get(classId);
        }

        public static DataTable Get(int classId)
        {
            return Tables.Get(classId);
        }
        public static CharacterData GetCharacterByName(string name)
        {
            return Get(16).GetData<CharacterData>(name);
        }
        public static ProjectileData GetProjectileByName(string name)
        {
            return Get(6).GetData<ProjectileData>(name);
        }
        public static AreaEffectData GetAreaEffectByName(string name)
        {
            AreaEffectData data = Get(DataType.AreaEffect).GetData<AreaEffectData>(name);
            if (data == null && !string.IsNullOrEmpty(name))
            {
                Logger.Warning($"[DataTables] area effect '{name}' not found in area_effects.csv");
            }
            return data;
        }
        public static ItemData GetItemByName(string name)
        {
            return Get(DataType.Item).GetData<ItemData>(name);
        }
        public static CardData GetUnlockCardFor(CharacterData character)
        {
            foreach(CardData i in Get(DataType.Card).GetDatas())
            {
                if (i.Target == character.Name && i.Type == "unlock") return i;
            }
            return null;
        }
    }
}
