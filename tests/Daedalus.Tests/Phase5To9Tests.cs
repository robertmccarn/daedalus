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
    public void BattleSystem_UsesFourPersonPartyAndMultipleEnemies()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember { Id = "arden", Name = "Arden", HP = 40, MaxHP = 40, MP = 10, MaxMP = 10, Stats = new StatsData { Strength = 8, Agility = 8 } },
                new PartyMember { Id = "lyra", Name = "Lyra", HP = 35, MaxHP = 35, MP = 10, MaxMP = 10, Stats = new StatsData { Strength = 5, Agility = 12 } },
                new PartyMember { Id = "marek", Name = "Marek", HP = 50, MaxHP = 50, MP = 10, MaxMP = 10, Stats = new StatsData { Strength = 11, Agility = 3 } },
                new PartyMember { Id = "sera", Name = "Sera", HP = 36, MaxHP = 36, MP = 10, MaxMP = 10, SpriteId = "sera", Stats = new StatsData { Strength = 6, Agility = 10 } }
            },
            CarriedInventory = new()
            {
                new InventoryItem { Id = "healing-tonic", Name = "Healing Tonic", Quantity = 1 }
            }
        };

        Character enemyA = new("Hollow Guard", 40, 1, new Stats(5, 2, 4, 2), 10, 10);
        Character enemyB = new("Hollow Stalker", 34, 1, new Stats(4, 3, 6, 4), 12, 10);

        BattleSystem battle = new(expedition, new[] { enemyA, enemyB });

        Assert.Equal(4, battle.Party.Count);
        Assert.Equal(2, battle.Enemies.Count);
        Assert.Equal("lyra", battle.SelectedActor?.Id);
        Assert.Same(enemyA, battle.SelectedTarget);

        battle.SelectNextTarget();
        Assert.Same(enemyB, battle.SelectedTarget);

        battle.SelectPreviousCommand();
        Assert.Equal(BattleCommand.Run, battle.SelectedCommand);

        battle.SelectNextCommand();
        Assert.Equal(BattleCommand.Attack, battle.SelectedCommand);

        BattleResult result = battle.PerformPlayerTurn(out int damage, out _);
        Assert.Equal(BattleResult.Continue, result);
        Assert.True(damage > 0);
        Assert.NotEqual("lyra", battle.SelectedActor?.Id);

        battle.SelectNextCommand(); // Skill
        battle.PerformPlayerTurn(out _, out _);
        Assert.Equal("arden", battle.SelectedActor?.Id);
        Assert.True(battle.HasStatus(enemyB, "Exposed"));
    }

    [Fact]
    public void VisibilitySystem_BlocksLineOfSightThroughWalls()
    {
        GameWorld world = new(1, 12345);
        GridPosition origin = new(world.SpawnX, world.SpawnY);
        GridPosition wall = new(world.SpawnX + 1, world.SpawnY);
        GridPosition target = new(world.SpawnX + 2, world.SpawnY);

        world.Dungeon[wall.Y, wall.X] = new Tile(TileType.Wall, false, true);

        Assert.True(VisibilitySystem.IsVisible(world, origin, wall));
        Assert.False(VisibilitySystem.IsVisible(world, origin, target));
    }

    [Fact]
    public void GameSession_ExtractionReturnsToCampaignAndCanRestart()
    {
        GameSession session = new(new GameWorld(1, 12345));

        session.Party.Initialize(
            session.StateManager.ActiveExpedition,
            new GridPosition(session.World.ExitX, session.World.ExitY));

        Assert.True(session.ExtractExpedition());
        Assert.Equal(GameState.ExtractionResults, session.State);
        Assert.NotNull(session.LastExtraction);
        Assert.True(session.LastExtraction!.CampaignRunsAfter >= 1);

        session.ReturnToCampaign();
        Assert.Equal(GameState.Campaign, session.State);

        session.StartNewExpeditionFromCampaign();
        Assert.Equal(GameState.Exploration, session.State);
        Assert.Equal("Active", session.StateManager.ActiveExpedition.ExtractionState);
    }

    [Fact]
    public void BattleSystem_UsesInterleavedInitiativeDuringRuntime()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember { Id = "fast", Name = "Fast", HP = 100, MaxHP = 100, MP = 10, MaxMP = 10, Stats = new StatsData { Strength = 5, Agility = 12 } },
                new PartyMember { Id = "slow", Name = "Slow", HP = 100, MaxHP = 100, MP = 10, MaxMP = 10, Stats = new StatsData { Strength = 5, Agility = 8 } }
            }
        };

        Character enemyFast = new("Hollow Stalker", 60, 1, new Stats(3, 1, 10, 1), 10, 10);
        Character enemySlow = new("Hollow Guard", 60, 1, new Stats(3, 1, 6, 1), 12, 10);

        BattleSystem battle = new(expedition, new[] { enemyFast, enemySlow });

        Assert.Equal("fast", battle.State.TurnOrder[0]);

        battle.PerformPlayerTurn(out _, out int enemyDamage);

        Assert.True(enemyDamage > 0);
        Assert.Equal("slow", battle.SelectedActor?.Id);
    }

    [Fact]
    public void BattleSystem_GearAndMoraleAffectPlayerDamage()
    {
        CampaignState campaign = new();
        Gear weapon = new() { Id = "test-blade", Name = "Test Blade", Slot = "Weapon", Power = 7 };
        campaign.Gear.Add(weapon);

        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember
                {
                    Id = "arden",
                    Name = "Arden",
                    HP = 50,
                    MaxHP = 50,
                    MP = 10,
                    MaxMP = 10,
                    Morale = 100,
                    EquippedGearIds = new() { weapon.Id },
                    SpriteId = "arden",
                    Stats = new StatsData { Strength = 8, Magic = 2, Agility = 12 }
                }
            }
        };

        Character enemy = new("Hollow Guard", 100, 1, new Stats(2, 1, 1, 1), 10, 10);
        BattleSystem battle = new(expedition, new[] { enemy }, campaign);

        battle.PerformPlayerTurn(out int damage, out _);

        Assert.Equal(21, damage);
        Assert.Equal(79, enemy.HP);
    }

    [Fact]
    public void BattleSystem_PoisonDeathRegistersDefeat()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember
                {
                    Id = "lyra",
                    Name = "Lyra",
                    HP = 100,
                    MaxHP = 100,
                    MP = 10,
                    MaxMP = 10,
                    SpriteId = "lyra",
                    Stats = new StatsData { Strength = 1, Magic = 1, Agility = 12 }
                }
            }
        };

        Character enemy = new("Hollow Guard", 10, 1, new Stats(1, 1, 1, 1), 10, 10);
        BattleSystem battle = new(expedition, new[] { enemy });

        battle.SelectNextCommand();
        Assert.Equal(BattleCommand.Skill, battle.SelectedCommand);

        BattleResult result = battle.PerformPlayerTurn(out _, out _);

        Assert.Equal(BattleResult.EnemyDefeated, result);
        Assert.Contains(enemy, battle.DefeatedEnemies);
        Assert.True(enemy.HP <= 0);
    }

    [Fact]
    public void BattleSystem_RetargetsAfterSelectedEnemyDies()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember
                {
                    Id = "arden",
                    Name = "Arden",
                    HP = 100,
                    MaxHP = 100,
                    MP = 10,
                    MaxMP = 10,
                    SpriteId = "arden",
                    Stats = new StatsData { Strength = 20, Agility = 12 }
                }
            }
        };

        Character first = new("Hollow Guard", 15, 1, new Stats(1, 1, 1, 1), 10, 10);
        Character second = new("Hollow Stalker", 100, 1, new Stats(1, 1, 1, 1), 12, 10);

        BattleSystem battle = new(expedition, new[] { first, second });
        BattleResult result = battle.PerformPlayerTurn(out _, out _);

        Assert.Equal(BattleResult.Continue, result);
        Assert.Same(second, battle.SelectedTarget);
    }

    [Fact]
    public void BattleSystem_SkillAppliesPoisonStatus()
    {
        ExpeditionState expedition = new()
        {
            Party = new()
            {
                new PartyMember
                {
                    Id = "lyra",
                    Name = "Lyra",
                    HP = 40,
                    MaxHP = 40,
                    MP = 10,
                    MaxMP = 10,
                    SpriteId = "lyra",
                    Stats = new StatsData { Strength = 1, Magic = 6, Agility = 12 }
                }
            }
        };

        Character enemy = new(
            "Hollow Guard",
            60,
            1,
            new Stats(4, 2, 3, 2),
            10,
            10);

        BattleSystem battle = new(expedition, new[] { enemy });
        battle.SelectNextCommand();

        Assert.Equal(BattleCommand.Skill, battle.SelectedCommand);

        battle.PerformPlayerTurn(out _, out _);

        Assert.True(battle.HasStatus("Hollow Guard:10:10:0", "Poisoned"));
        Assert.True(enemy.HP < 60);
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
            (_, _) => true,
            "Reach the extraction point.",
            "Supplies recovered.",
            new FeedbackEffect(
                FeedbackEffectType.Loot,
                world.SpawnX,
                world.SpawnY,
                Environment.TickCount64,
                900));

        Assert.Equal("Reach the extraction point.", context.CurrentObjective);
        Assert.Equal("Supplies recovered.", context.Message);
        Assert.NotNull(context.Feedback);
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
