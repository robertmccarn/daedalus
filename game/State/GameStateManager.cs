namespace Systemic.Engine.State;

public class GameStateManager
{
    public CampaignState Campaign { get; private set; }
    public ExpeditionState ActiveExpedition { get; private set; }

    public GameStateManager()
    {
        Campaign = CreateDefaultCampaign();
        ContentCatalog.InitializeCampaign(Campaign);
        ActiveExpedition = new ExpeditionState();
    }

    public void StartNewExpedition(
        int startX, int startY, int health, int maxHealth, int floor = 1, int? floorSeed = null,
        IReadOnlyCollection<string>? selectedMemberIds = null, string? leaderId = null,
        PartyFormationType formation = PartyFormationType.Column)
    {
        List<PartyMember> selected = SelectPartyMembers(selectedMemberIds);
        string selectedLeaderId = leaderId ?? selected[0].Id;
        if (selected.All(member => member.Id != selectedLeaderId))
            throw new ArgumentException("Leader must be one of the selected party members.", nameof(leaderId));

        ActiveExpedition = new ExpeditionState
        {
            CurrentFloor = floor, Health = health, MaxHealth = maxHealth,
            PlayerGridPosition = (startX, startY), FloorSeed = floorSeed ?? Random.Shared.Next(),
            Party = selected, LeaderId = selectedLeaderId, Formation = formation
        };

        PartyMember leader = ActiveExpedition.Party.First(member => member.Id == selectedLeaderId);
        leader.HP = Math.Clamp(health, 0, Math.Max(health, leader.MaxHP));
        leader.MaxHP = Math.Max(leader.MaxHP, maxHealth);
        ActiveExpedition.Health = leader.HP;
        ActiveExpedition.MaxHealth = leader.MaxHP;
        DiscoverArea(startX, startY);
    }

    public void StartNewExpedition()
    {
        IReadOnlyCollection<string> selected = Campaign.PartyRoster.Take(Math.Min(4, Campaign.PartyRoster.Count)).Select(member => member.Id).ToArray();
        StartNewExpedition(26, 5, 30, 30, selectedMemberIds: selected, leaderId: Campaign.PartyRoster.FirstOrDefault()?.Id);
    }

    public void SynchronizeExpedition(int x, int y, int health, int maxHealth)
    {
        ActiveExpedition.PlayerGridPosition = (x, y);
        ActiveExpedition.Health = health;
        ActiveExpedition.MaxHealth = maxHealth;
        PartyMember? leader = ActiveExpedition.Party.FirstOrDefault(member => member.Id == ActiveExpedition.LeaderId);
        if (leader != null) { leader.HP = health; leader.MaxHP = maxHealth; }
        DiscoverArea(x, y);
    }

    public void AdvanceFloor(int health, int maxHealth)
    {
        ActiveExpedition.CurrentFloor++;
        ActiveExpedition.CurrentNode = string.Empty;
        ActiveExpedition.Health = health;
        ActiveExpedition.MaxHealth = maxHealth;
        ActiveExpedition.FloorSeed = Random.Shared.Next();
        ActiveExpedition.DiscoveredCells.Clear();
        ActiveExpedition.CompletedNodeIds.Clear();
        ActiveExpedition.DefeatedNodeIds.Clear();
        Campaign.HighestDepth = Math.Max(Campaign.HighestDepth, ActiveExpedition.CurrentFloor);
    }

    public void SetExpeditionPosition(int x, int y)
    {
        ActiveExpedition.PlayerGridPosition = (x, y);
        DiscoverArea(x, y);
    }

    public void CommitExpeditionProgress()
    {
        foreach (PartyMember expeditionMember in ActiveExpedition.Party)
        {
            PartyMember? campaignMember = Campaign.PartyRoster.FirstOrDefault(member => member.Id == expeditionMember.Id);
            if (campaignMember == null) continue;
            campaignMember.Experience = expeditionMember.Experience;
            campaignMember.Level = expeditionMember.Level;
            campaignMember.HP = expeditionMember.HP;
            campaignMember.MaxHP = expeditionMember.MaxHP;
            campaignMember.MP = expeditionMember.MP;
            campaignMember.MaxMP = expeditionMember.MaxMP;
            campaignMember.Stats = CloneStats(expeditionMember.Stats);
            campaignMember.Morale = expeditionMember.Morale;
            campaignMember.Specializations = new List<string>(expeditionMember.Specializations);
            campaignMember.EquippedGearIds = new List<string>(expeditionMember.EquippedGearIds);
        }
    }

    public void MarkDiscovered(int x, int y)
    {
        string key = $"{x},{y}";
        if (!ActiveExpedition.DiscoveredCells.Contains(key)) ActiveExpedition.DiscoveredCells.Add(key);
    }

    public void DiscoverArea(int centerX, int centerY, int radius = 2)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
                MarkDiscovered(x, y);
    }

    public bool IsDiscovered(int x, int y) => ActiveExpedition.DiscoveredCells.Contains($"{x},{y}");

    public void CompleteExpedition()
    {
        Campaign.RunsCompleted++;
        ActiveExpedition.ExtractionState = "Extracted";
    }

    public bool Save(string path) => TrySave(path, out _);

    public bool TrySave(string path, out string? error)
    {
        try { SaveSystem.Save(path, Campaign, ActiveExpedition); error = null; return true; }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        { error = ex.Message; return false; }
    }

    public bool Load(string path)
    {
        if (!SaveSystem.TryLoad(path, out CampaignState campaign, out ExpeditionState expedition)) return false;
        Campaign = campaign;
        ActiveExpedition = expedition;
        ContentCatalog.InitializeCampaign(Campaign);

        foreach (PartyMember campaignMember in Campaign.PartyRoster)
        {
            PartyMember? expeditionMember = ActiveExpedition.Party.FirstOrDefault(member => member.Id == campaignMember.Id);
            if (expeditionMember != null)
                expeditionMember.EquippedGearIds = new List<string>(campaignMember.EquippedGearIds);
        }

        if (ActiveExpedition.Party.Count > 0 && ActiveExpedition.Party.All(member => member.Id != ActiveExpedition.LeaderId))
            ActiveExpedition.LeaderId = ActiveExpedition.Party[0].Id;
        return true;
    }

    private List<PartyMember> SelectPartyMembers(IReadOnlyCollection<string>? selectedMemberIds)
    {
        IReadOnlyCollection<string> ids = selectedMemberIds ??
            Campaign.PartyRoster.Take(Math.Min(4, Campaign.PartyRoster.Count)).Select(member => member.Id).ToArray();

        if (ids.Count is < 1 or > 4)
            throw new ArgumentException("An expedition party must contain between one and four members.", nameof(selectedMemberIds));
        if (ids.Count != ids.Distinct(StringComparer.Ordinal).Count())
            throw new ArgumentException("Expedition party member IDs must be unique.", nameof(selectedMemberIds));

        List<PartyMember> selected = new();
        foreach (string id in ids)
        {
            PartyMember? source = Campaign.PartyRoster.FirstOrDefault(member => member.Id == id);
            if (source == null) throw new ArgumentException($"Party member '{id}' does not exist in the campaign roster.", nameof(selectedMemberIds));
            selected.Add(ClonePartyMember(source));
        }
        return selected;
    }

    private static CampaignState CreateDefaultCampaign()
    {
        CampaignState campaign = new()
        {
            PartyRoster = new()
            {
                CreatePartyMember("arden", "Arden", 30, 8, 3, 6, 5, "arden"),
                CreatePartyMember("lyra", "Lyra", 24, 4, 9, 7, 6, "lyra"),
                CreatePartyMember("marek", "Marek", 36, 10, 2, 3, 4, "marek"),
                CreatePartyMember("sera", "Sera", 26, 6, 7, 9, 8, "sera"),
                CreatePartyMember("voss", "Voss", 28, 7, 5, 5, 10, "voss")
            }
        };

        campaign.Materials.Add(new Material { Id = "rusted-catalyst", Name = "Rusted Catalyst", Quantity = 2 });
        campaign.Recipes.Add(new Recipe
        {
            Id = "reinforced-blade", Name = "Reinforced Blade",
            Ingredients = new() { "rusted-catalyst", "monster-residue" },
            IngredientQuantities = new() { ["rusted-catalyst"] = 2, ["monster-residue"] = 1 },
            ResultKind = "Gear", ResultSlot = "Weapon", ResultPower = 5
        });
        return campaign;
    }

    private static PartyMember CreatePartyMember(string id, string name, int hp, int strength, int magic, int agility, int luck, string spriteId) =>
        new()
        {
            Id = id, Name = name, Level = 1, HP = hp, MaxHP = hp, MP = 10, MaxMP = 10,
            Morale = 100, SpriteId = spriteId, PortraitId = spriteId,
            Stats = new StatsData { Strength = strength, Magic = magic, Agility = agility, Luck = luck }
        };

    private static PartyMember ClonePartyMember(PartyMember source) =>
        new()
        {
            Id = source.Id, Name = source.Name, Level = source.Level, Experience = source.Experience,
            HP = source.HP, MaxHP = source.MaxHP, MP = source.MP, MaxMP = source.MaxMP,
            Morale = source.Morale, Stats = CloneStats(source.Stats),
            EquippedGearIds = new List<string>(source.EquippedGearIds),
            Specializations = new List<string>(source.Specializations),
            SpriteId = source.SpriteId, PortraitId = source.PortraitId
        };

    private static StatsData CloneStats(StatsData source) =>
        new() { Strength = source.Strength, Magic = source.Magic, Agility = source.Agility, Luck = source.Luck };
}
