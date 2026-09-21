using Systemic.Engine.State;
using Xunit;

public class Phase5To9Tests
{
    [Fact]
    public void Tile_DefaultMetadata_DistinguishesRaisedStructures()
    {
        Tile wall = new(TileType.Wall, false, true);
        Tile floor = new(TileType.Floor, true, false);

        Assert.Equal(TileOcclusionBehavior.Raised, wall.OcclusionBehavior);
        Assert.Equal(TileOcclusionBehavior.None, floor.OcclusionBehavior);
        Assert.Equal(0, floor.Elevation);
    }

    [Fact]
    public void BiomeCatalog_CyclesDeterministically()
    {
        Assert.Equal(BiomeType.RuinedDepths, BiomeCatalog.ForFloor(1));
        Assert.Equal(BiomeType.AshenHalls, BiomeCatalog.ForFloor(2));
        Assert.Equal(BiomeType.VerdantBelow, BiomeCatalog.ForFloor(3));
        Assert.Equal(BiomeType.CrystalWastes, BiomeCatalog.ForFloor(4));
        Assert.Equal(BiomeType.RuinedDepths, BiomeCatalog.ForFloor(5));
    }

    [Fact]
    public void ProgressionSystem_LevelsAndImprovesStats()
    {
        PartyMember member = new()
        {
            Level = 1,
            HP = 20,
            MaxHP = 20,
            MP = 5,
            MaxMP = 5,
            Stats = new StatsData { Strength = 4, Agility = 4 }
        };

        bool leveled = ProgressionSystem.ApplyExperience(member, 20);

        Assert.True(leveled);
        Assert.Equal(2, member.Level);
        Assert.Equal(24, member.MaxHP);
        Assert.Equal(6, member.MaxMP);
        Assert.Equal(5, member.Stats.Strength);
        Assert.Equal(5, member.Stats.Agility);
    }

    [Fact]
    public void EventSystem_AppliesMoraleAndCarriedGold()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember { Id = "a", Morale = 50 },
                new PartyMember { Id = "b", Morale = 90 }
            }
        };
        CampaignState campaign = new();

        EventSystem.Apply(
            campaign,
            expedition,
            new ExpeditionEvent("test", "Test", "Test", 5, 7, "test-flag"));

        Assert.Equal(55, expedition.Party[0].Morale);
        Assert.Equal(95, expedition.Party[1].Morale);
        Assert.Equal(7, expedition.CarriedGold);
        Assert.Contains("test-flag", campaign.Flags);
    }

    [Fact]
    public void BattlePartyController_SortsByAgilityDeterministically()
    {
        BattleState state = new();
        state.Party.Add(new PartyMember { Id = "slow", Stats = new StatsData { Agility = 2 } });
        state.Party.Add(new PartyMember { Id = "fast", Stats = new StatsData { Agility = 9 } });
        state.Enemies.Add(new BattleEnemyState { Id = "enemy", Agility = 5 });

        new BattlePartyController(state);

        Assert.Equal(new[] { "fast", "enemy", "slow" }, state.TurnOrder);
        Assert.Equal("fast", state.SelectedActorId);
    }

    [Fact]
    public void ContentCatalog_ProvidesProductionRecipes()
    {
        CampaignState campaign = new();
        ContentCatalog.InitializeCampaign(campaign);

        Assert.Contains(campaign.Gear, gear => gear.Id == "iron-blade");
        Assert.Contains(campaign.Recipes, recipe => recipe.Id == "field-coat-reinforcement");
    }

    [Fact]
    public void ExplorationRenderContext_CarriesPlayerFacingFeedback()
    {
        GameWorld world = new(1, 12345);
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember
                {
                    Id = "leader",
                    Name = "Arden",
                    HP = 30,
                    MaxHP = 30
                }
            },
            LeaderId = "leader",
            Formation = PartyFormationType.Column,
            PlayerGridPosition = (world.SpawnX, world.SpawnY)
        };

        PartyController party = new(world);
        party.Initialize(
            expedition,
            new GridPosition(world.SpawnX, world.SpawnY));

        ExplorationRenderContext context = new(
            world,
            party,
            expedition,
            (_, _) => true,
            "Reach the extraction point.",
            "Supplies recovered.");

        Assert.Equal("Reach the extraction point.", context.CurrentObjective);
        Assert.Equal("Supplies recovered.", context.Message);
    }

    [Fact]
    public void GameWorld_AuthoredRoomFeatures_AreDeterministicAndRenderOnly()
    {
        GameWorld first = new(1, 12345);
        GameWorld second = new(1, 12345);

        Assert.Equal(first.VisualFeatures, second.VisualFeatures);
        Assert.Contains(first.VisualFeatures, feature => feature.Type == WorldVisualFeatureType.Abyss);
        Assert.Contains(first.VisualFeatures, feature => feature.Type == WorldVisualFeatureType.Bridge);
        Assert.Contains(first.VisualFeatures, feature => feature.Type == WorldVisualFeatureType.Pillar);
        Assert.Contains(first.VisualFeatures, feature => feature.Type == WorldVisualFeatureType.Landmark);

        WorldVisualFeature bridge = first.VisualFeatures.First(feature => feature.Type == WorldVisualFeatureType.Bridge);
        Assert.True(first.IsWalkable(bridge.X, bridge.Y));
    }

    [Fact]
    public void NewExpedition_RevealsStartingChamberButNotTheWholeFloor()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(
            20,
            10,
            30,
            30,
            floor: 1,
            floorSeed: 12345);

        Assert.True(manager.IsDiscovered(20, 10));
        Assert.True(manager.IsDiscovered(15, 5));
        Assert.False(manager.IsDiscovered(10, 0));
    }
}
