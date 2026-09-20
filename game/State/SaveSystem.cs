using System.Text.Json;

namespace Systemic.Engine.State;

public static class SaveSystem
{
    private const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public static void Save(
        string path,
        CampaignState campaign,
        ExpeditionState expedition)
    {
        SaveData data = new()
        {
            SchemaVersion = CurrentSchemaVersion,
            Campaign = campaign,
            Expedition = expedition
        };

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static bool TryLoad(
        string path,
        out CampaignState campaign,
        out ExpeditionState expedition)
    {
        campaign = new CampaignState();
        expedition = new ExpeditionState();

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            SaveData? data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);

            if (data == null || data.SchemaVersion > CurrentSchemaVersion)
                return false;

            campaign = data.Campaign ?? new CampaignState();
            expedition = data.Expedition ?? new ExpeditionState();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private sealed class SaveData
    {
        public int SchemaVersion { get; set; }
        public CampaignState? Campaign { get; set; }
        public ExpeditionState? Expedition { get; set; }
    }
}
