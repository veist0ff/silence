using Newtonsoft.Json;
using System.IO;
using System;

public class Configuration
{
    public static Configuration Instance;

    [JsonProperty("udp_host")] public readonly string UdpHost;
    [JsonProperty("udp_port")] public readonly int UdpPort;
    [JsonProperty("tcp_port")] public readonly int TcpPort;    
    [JsonProperty("database_username")] public readonly string DatabaseUsername;
    [JsonProperty("database_password")] public readonly string DatabasePassword;
    [JsonProperty("database_name")] public readonly string DatabaseName;
    [JsonProperty("update_sha")] public readonly string UpdateSha;
    [JsonProperty("ContentUrl")] public readonly string ContentUrl;
    [JsonProperty("Fingerprint")] public readonly string Fingerprint;
    [JsonProperty("ThemeID")] public int ThemeId { get; set; }
    [JsonProperty("Maintenance")] public bool Maintenance { get; set; }
    [JsonProperty("MaintenanceText")] public string MaintenanceText { get; set; }

    /// <summary>
    /// Слоты событий, бои которых идут на новом движке. Пустой список или его
    /// отсутствие означает, что новый движок не включён нигде и всё идёт как
    /// раньше.
    /// </summary>
    [JsonProperty("new_engine_slots")] public int[] NewEngineSlots { get; set; }
    
    private static DateTime _lastFileUpdate = DateTime.MinValue;

    public static Configuration LoadFromFile(string filename)
    {
        Instance = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        _lastFileUpdate = File.GetLastWriteTime(filename);
        return Instance;
    }
    
    public static void ReloadIfNeeded()
    {
        try
        {
            string path = "Json/config.json";
            DateTime currentWriteTime = File.GetLastWriteTime(path);
            
            if (currentWriteTime > _lastFileUpdate)
            {
                var newConfig = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(path));
                if (newConfig != null)
                {
                    Instance.ThemeId = newConfig.ThemeId;
                    Instance.Maintenance = newConfig.Maintenance;
                    Instance.MaintenanceText = newConfig.MaintenanceText;
                    Instance.NewEngineSlots = newConfig.NewEngineSlots;
                    
                    _lastFileUpdate = currentWriteTime;
                    Console.WriteLine($"[Config] Конфиг обновлен! Тех. перерыв: {Instance.Maintenance}");
                }
            }
        }
        catch 
        {
        }
    }

    public static int GetThemeId()
    {
        ReloadIfNeeded();
        return Instance != null ? Instance.ThemeId : 152;
    }

    /// <summary>
    /// Идут ли бои этого слота на новом движке.
    ///
    /// Читается это перед каждым боем и переживает правку конфига без перезапуска:
    /// так слот можно включить и выключить прямо на живом сервере.
    /// </summary>
    public static bool IsNewEngineSlot(int slot)
    {
        ReloadIfNeeded();

        int[] slots = Instance?.NewEngineSlots;
        if (slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == slot) return true;
        }

        return false;
    }
}
