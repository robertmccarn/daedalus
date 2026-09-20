namespace Systemic.Engine.State;

public class GameStateManager
{
    public CampaignState Campaign { get; private set; }
    public ExpeditionState ActiveExpedition { get; private set; }

    public GameStateManager()
    {
        Campaign = CreateDefaultCampaign();
        ActiveExpedition = new ExpeditionState();
    }

    public void StartNewExpedition(
        int startX,
        int startY,
        int health,
        int maxHealth,
        int floor = 1,
        int? floorSeed = null)
    {
        ActiveExpedition = new ExpeditionState
        {
            CurrentFloor = floor,
            Health = health,
            MaxHealth = maxHealth,
            PlayerGridPosition = (startX, startY),
            FloorSeed = floorSeed ?? Random.Shared.Next(),
            Party = Campaign.PartyRoster.Select(ClonePartyMember).ToList()
        };

        DiscoverArea(startX, startY);
    }

    public void StartNewExpedition()
    {
        StartNewExpedition(26, 5, 30, 30);
    }

    public void SynchronizeExpedition(int x, int y, int health, int maxHealth)
    {
        ActiveExpedition.PlayerGridPosition = (x, y);
        ActiveExpedition.Health = health;
        ActiveExpedition.MaxHealth = maxHealth;
        DiscoverArea(x, y);
    }

    public void AdvanceFloor(int startX, int startY, int health, int maxHealth)
    {
        ActiveExpedition.CurrentFloor++;
        ActiveExpedition.CurrentNode = string.Empty;
        ActiveExpedition.PlayerGridPosition = (startX, startY);
        ActiveExpedition.Health = health;
        ActiveExpedition.MaxHealth = maxHealth;
        ActiveExpedition.FloorSeed = Random.Shared.Next();
        ActiveExpedition.DiscoveredCells.Clear();
        MarkDiscovered(startX, startY);

        Campaign.HighestDepth = Math.Max(
            Campaign.HighestDepth,
            ActiveExpedition.CurrentFloor);
    }

    public void MarkDiscovered(int x, int y)
    {
        string key = $"{x},{y}";
        if (!ActiveExpedition.DiscoveredCells.Contains(key))
            ActiveExpedition.DiscoveredCells.Add(key);
    }

    public void DiscoverArea(int centerX, int centerY, int radius = 2)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
                MarkDiscovered(x, y);
    }

    public bool IsDiscovered(int x, int y) =>
        ActiveExpedition.DiscoveredCells.Contains($"{x},{y}");

    public void CompleteExpedition()
    {
        Campaign.RunsCompleted++;
        ActiveExpedition.ExtractionState = "Extracted";
    }

    public bool Save(string path) =>
        TrySave(path, out _);

    public bool TrySave(string path, out string? error)
    {
        try
        {
            SaveSystem.Save(path, Campaign, ActiveExpedition);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool Load(string path)
    {
        if (!SaveSystem.TryLoad(path, out CampaignState campaign, out ExpeditionState expedition))
            return false;

        Campaign = campaign;
        ActiveExpedition = expedition;
        return true;
    }

    private static CampaignState CreateDefaultCampaign()
    {
        CampaignState campaign = new()
        {
            PartyRoster = new()
            {
                new PartyMember
                {
                    Id = "arden",
                    Name = "Arden",
                    Level = 1,
                    HP = 30,
                    MaxHP = 30,
                    Stats = new StatsData
                    {
                        Strength = 8,
                        Magic = 3,
                        Agility = 6,
                        Luck = 5
                    }
                }
            }
        };

        campaign.Materials.Add(new Material
        {
            Id = "rusted-catalyst",
            Name = "Rusted Catalyst",
            Quantity = 2
        });

        campaign.Recipes.Add(new Recipe
        {
            Id = "reinforced-blade",
            Name = "Reinforced Blade",
            Ingredients = new() { "rusted-catalyst", "monster-residue" },
            IngredientQuantities = new()
            {
                ["rusted-catalyst"] = 2,
                ["monster-residue"] = 1
            },
            ResultKind = "Gear",
            ResultSlot = "Weapon",
            ResultPower = 5
        });

        return campaign;
    }

    private static PartyMember ClonePartyMember(PartyMember source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            Level = source.Level,
            Experience = source.Experience,
            HP = source.HP,
            MaxHP = source.MaxHP,
            Stats = new StatsData
            {
                Strength = source.Stats.Strength,
                Magic = source.Stats.Magic,
                Agility = source.Stats.Agility,
                Luck = source.Stats.Luck
            },
            EquippedGearIds = new List<string>(source.EquippedGearIds)
        };
}
