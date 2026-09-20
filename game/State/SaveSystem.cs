using System.Text.Json;

namespace Systemic.Engine.State;

public static class SaveSystem
{
    private const int CurrentSchemaVersion = 4;

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
            MigrateToCurrent(data.SchemaVersion, campaign, expedition);
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

    private static void MigrateToCurrent(
        int schemaVersion,
        CampaignState campaign,
        ExpeditionState expedition)
    {
        if (schemaVersion >= CurrentSchemaVersion)
            return;

        if (schemaVersion <= 3)
            MigrateSchema3To4(campaign, expedition);
    }

    private static void MigrateSchema3To4(
        CampaignState campaign,
        ExpeditionState expedition)
    {
        foreach (PartyMember member in campaign.PartyRoster)
        {
            member.Morale = 100;
            member.MP = 10;
            member.MaxMP = 10;
            member.Specializations ??= new List<string>();
            member.EquippedGearIds ??= new List<string>();
            member.Stats ??= new StatsData();
        }

        foreach (PartyMember member in expedition.Party)
        {
            member.Morale = 100;
            member.MP = 10;
            member.MaxMP = 10;
            member.Specializations ??= new List<string>();
            member.EquippedGearIds ??= new List<string>();
            member.Stats ??= new StatsData();
        }

        if (expedition.Party.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(expedition.LeaderId) ||
                expedition.Party.All(member => member.Id != expedition.LeaderId))
            {
                expedition.LeaderId = expedition.Party[0].Id;
            }
        }

        expedition.Formation = Enum.IsDefined(expedition.Formation)
            ? expedition.Formation
            : PartyFormationType.Column;
    }

    private sealed class SaveData
    {
        public int SchemaVersion { get; set; }
        public CampaignState? Campaign { get; set; }
        public ExpeditionState? Expedition { get; set; }
    }
}
