using System.Text.Json;
using Systemic.Engine.State;
using Xunit;

public class Phase4PartyTests
{
    [Fact]
    public void PartySelection_ChoosesExactlyRequestedMembersAndLeader()
    {
        GameStateManager manager = new();
        string[] selected = ["arden", "lyra", "marek", "sera"];

        manager.StartNewExpedition(
            10,
            10,
            30,
            30,
            selectedMemberIds: selected,
            leaderId: "sera",
            formation: PartyFormationType.Wedge);

        Assert.Equal(4, manager.ActiveExpedition.Party.Count);
        Assert.Equal(selected, manager.ActiveExpedition.Party.Select(member => member.Id));
        Assert.Equal("sera", manager.ActiveExpedition.LeaderId);
        Assert.Equal(PartyFormationType.Wedge, manager.ActiveExpedition.Formation);
        Assert.DoesNotContain(manager.ActiveExpedition.Party, member => member.Id == "voss");
    }

    [Fact]
    public void PartySelection_RejectsInvalidSelection()
    {
        GameStateManager manager = new();

        Assert.Throws<ArgumentException>(() => manager.StartNewExpedition(
            10, 10, 30, 30,
            selectedMemberIds: ["arden", "lyra", "marek", "sera", "voss"],
            leaderId: "arden"));

        Assert.Throws<ArgumentException>(() => manager.StartNewExpedition(
            10, 10, 30, 30,
            selectedMemberIds: ["arden", "arden"],
            leaderId: "arden"));

        Assert.Throws<ArgumentException>(() => manager.StartNewExpedition(
            10, 10, 30, 30,
            selectedMemberIds: ["arden", "lyra"],
            leaderId: "voss"));
    }

    [Fact]
    public void PartyController_MovesLeaderAndFollowersWithoutOverlap()
    {
        GameWorld world = new(seed: 1234);
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "arden",
            formation: PartyFormationType.Column);

        PartyController party = new(world);
        party.Initialize(
            manager.ActiveExpedition,
            new GridPosition(world.SpawnX, world.SpawnY));

        (int dx, int dy, CharacterDirection direction) = FindWalkableDirection(world, party.LeaderPosition);
        Assert.NotEqual((0, 0), (dx, dy));

        MoveResult result = party.TryMoveLeader(direction, manager.ActiveExpedition);

        Assert.Equal(MoveResult.Moved, result);
        Assert.Equal(new GridPosition(world.SpawnX + dx, world.SpawnY + dy), party.LeaderPosition);

        List<GridPosition> positions = party.Members.Select(member => member.Position).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
        Assert.All(positions, position => Assert.True(world.IsWalkable(position.X, position.Y)));
    }

    [Fact]
    public void PartyController_IsDeterministicForIdenticalInputs()
    {
        GameWorld worldA = new(seed: 4321);
        GameWorld worldB = new(seed: 4321);
        GameStateManager managerA = CreatePartyManager(worldA);
        GameStateManager managerB = CreatePartyManager(worldB);

        PartyController partyA = CreatePartyController(worldA, managerA);
        PartyController partyB = CreatePartyController(worldB, managerB);

        CharacterDirection[] inputs = FindWalkableDirections(worldA, partyA.LeaderPosition, 6);
        foreach (CharacterDirection input in inputs)
        {
            partyA.TryMoveLeader(input, managerA.ActiveExpedition);
            partyB.TryMoveLeader(input, managerB.ActiveExpedition);
        }

        Assert.Equal(
            partyA.Members.Select(member => member.Position),
            partyB.Members.Select(member => member.Position));
    }

    [Fact]
    public void Extraction_CommitsPartyMoraleStatsAndSpecializations()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30);

        PartyMember expeditionMember = manager.ActiveExpedition.Party.First();
        expeditionMember.Morale = 61;
        expeditionMember.Stats.Strength = 17;
        expeditionMember.Specializations.Add("Scout");

        Assert.True(ExtractionSystem.Extract(manager));

        PartyMember campaignMember = manager.Campaign.PartyRoster
            .Single(member => member.Id == expeditionMember.Id);

        Assert.Equal(61, campaignMember.Morale);
        Assert.Equal(17, campaignMember.Stats.Strength);
        Assert.Contains("Scout", campaignMember.Specializations);
    }

    [Fact]
    public void Schema3Save_MigratesToPhase4Defaults()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"daedalus-schema3-{Guid.NewGuid():N}.json");

        string json = JsonSerializer.Serialize(new
        {
            SchemaVersion = 3,
            Campaign = new
            {
                PartyRoster = new[]
                {
                    new
                    {
                        Id = "arden",
                        Name = "Arden",
                        Level = 2,
                        Experience = 50,
                        HP = 20,
                        MaxHP = 30,
                        Stats = new { Strength = 8, Magic = 3, Agility = 6, Luck = 5 },
                        EquippedGearIds = Array.Empty<string>()
                    }
                }
            },
            Expedition = new
            {
                CurrentFloor = 2,
                Health = 20,
                MaxHealth = 30,
                Party = new[]
                {
                    new
                    {
                        Id = "arden",
                        Name = "Arden",
                        Level = 2,
                        Experience = 50,
                        HP = 20,
                        MaxHP = 30,
                        Stats = new { Strength = 8, Magic = 3, Agility = 6, Luck = 5 },
                        EquippedGearIds = Array.Empty<string>()
                    }
                },
                PlayerGridPosition = new { X = 10, Y = 10 }
            }
        });

        try
        {
            File.WriteAllText(path, json);
            Assert.True(SaveSystem.TryLoad(path, out _, out ExpeditionState expedition));
            Assert.Equal("arden", expedition.LeaderId);
            Assert.Equal(PartyFormationType.Column, expedition.Formation);
            Assert.Equal(100, expedition.Party[0].Morale);
            Assert.Equal(10, expedition.Party[0].MaxMP);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void SaveRoundTrip_PreservesLeaderFormationAndPartyComposition()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(
            10,
            10,
            30,
            30,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "sera",
            formation: PartyFormationType.Defensive);

        string path = Path.Combine(Path.GetTempPath(), $"daedalus-phase4-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(manager.Save(path));

            GameStateManager loaded = new();
            Assert.True(loaded.Load(path));
            Assert.Equal("sera", loaded.ActiveExpedition.LeaderId);
            Assert.Equal(PartyFormationType.Defensive, loaded.ActiveExpedition.Formation);
            Assert.Equal(
                ["arden", "lyra", "marek", "sera"],
                loaded.ActiveExpedition.Party.Select(member => member.Id));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void FloorReform_PreservesPartyProgressionAndResetsRuntimeHistory()
    {
        GameWorld world = new(seed: 9876);
        GameStateManager manager = CreatePartyManager(world);
        PartyController party = CreatePartyController(world, manager);

        (int dx, int dy, CharacterDirection direction) = FindWalkableDirection(world, party.LeaderPosition);
        Assert.Equal(MoveResult.Moved, party.TryMoveLeader(direction, manager.ActiveExpedition));
        manager.ActiveExpedition.Party[0].Experience = 75;
        manager.ActiveExpedition.Party[0].Morale = 70;
        manager.ActiveExpedition.Upkeep = 9;
        manager.ActiveExpedition.NodeHistory.Add("floor-1-chest");

        manager.AdvanceFloor(30, 30);
        world.RebuildFloor(manager.ActiveExpedition.FloorSeed, manager.ActiveExpedition.CurrentFloor);
        party.ReformForFloor(manager.ActiveExpedition, new GridPosition(world.SpawnX, world.SpawnY));

        Assert.Equal(75, manager.ActiveExpedition.Party[0].Experience);
        Assert.Equal(70, manager.ActiveExpedition.Party[0].Morale);
        Assert.Equal(9, manager.ActiveExpedition.Upkeep);
        Assert.Contains("floor-1-chest", manager.ActiveExpedition.NodeHistory);
        Assert.Equal(new GridPosition(world.SpawnX, world.SpawnY), party.LeaderPosition);
        Assert.Equal(party.Members.Count, party.Members.Select(member => member.Position).Distinct().Count());
    }


    [Fact]
    public void PartyController_NonFirstLeader_RemainsLeaderAndFollowersRemainUnique()
    {
        GameWorld world = new(seed: 2468);
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "sera",
            formation: PartyFormationType.Column);

        PartyController party = new(world);
        party.Initialize(
            manager.ActiveExpedition,
            new GridPosition(world.SpawnX, world.SpawnY));

        AssertValidParty(party, world, 4, "sera");

        (int dx, int dy, CharacterDirection direction) = FindWalkableDirection(world, party.LeaderPosition);
        Assert.Equal(MoveResult.Moved, party.TryMoveLeader(direction, manager.ActiveExpedition));
        AssertValidParty(party, world, 4, "sera");
        Assert.Equal("sera", party.GetLeaderRuntime().MemberId);
    }

    [Fact]
    public void PartyController_AllFormations_SupportNonFirstLeader()
    {
        foreach (PartyFormationType formation in Enum.GetValues<PartyFormationType>())
        {
            GameWorld world = new(seed: 1357);
            GameStateManager manager = new();
            manager.StartNewExpedition(
                world.SpawnX,
                world.SpawnY,
                30,
                30,
                floorSeed: world.FloorSeed,
                selectedMemberIds: ["arden", "lyra", "marek", "sera"],
                leaderId: "sera",
                formation: formation);

            PartyController party = new(world);
            party.Initialize(
                manager.ActiveExpedition,
                new GridPosition(world.SpawnX, world.SpawnY));

            Assert.Equal(formation, party.Formation);
            AssertValidParty(party, world, 4, "sera");

            (int dx, int dy, CharacterDirection direction) = FindWalkableDirection(world, party.LeaderPosition);
            Assert.Equal(MoveResult.Moved, party.TryMoveLeader(direction, manager.ActiveExpedition));
            AssertValidParty(party, world, 4, "sera");
        }
    }

    [Fact]
    public void FloorReform_NonFirstLeader_RemainsLeader()
    {
        GameWorld world = new(seed: 9753);
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "sera",
            formation: PartyFormationType.Defensive);

        PartyController party = new(world);
        party.Initialize(manager.ActiveExpedition, new GridPosition(world.SpawnX, world.SpawnY));

        manager.AdvanceFloor(30, 30);
        world.RebuildFloor(manager.ActiveExpedition.FloorSeed, manager.ActiveExpedition.CurrentFloor);
        party.ReformForFloor(manager.ActiveExpedition, new GridPosition(world.SpawnX, world.SpawnY));

        AssertValidParty(party, world, 4, "sera");
        Assert.Equal(new GridPosition(world.SpawnX, world.SpawnY), party.LeaderPosition);
        Assert.Equal(PartyFormationType.Defensive, party.Formation);
    }

    [Fact]
    public void SaveLoad_ReconstructsRuntimePartyForNonFirstLeader()
    {
        GameWorld world = new(seed: 1122);
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "sera",
            formation: PartyFormationType.Wedge);

        PartyController party = new(world);
        party.Initialize(manager.ActiveExpedition, new GridPosition(world.SpawnX, world.SpawnY));

        (int dx, int dy, CharacterDirection direction) = FindWalkableDirection(world, party.LeaderPosition);
        Assert.Equal(MoveResult.Moved, party.TryMoveLeader(direction, manager.ActiveExpedition));

        string path = Path.Combine(Path.GetTempPath(), $"daedalus-phase4-runtime-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(manager.Save(path));

            GameStateManager loaded = new();
            Assert.True(loaded.Load(path));

            GameWorld loadedWorld = new(seed: loaded.ActiveExpedition.FloorSeed);
            loadedWorld.RebuildFloor(
                loaded.ActiveExpedition.FloorSeed,
                loaded.ActiveExpedition.CurrentFloor);
            PartyController loadedParty = new(loadedWorld);
            loadedParty.Load(
                loaded.ActiveExpedition,
                new GridPosition(
                    loaded.ActiveExpedition.PlayerGridPosition.X,
                    loaded.ActiveExpedition.PlayerGridPosition.Y));

            AssertValidParty(loadedParty, loadedWorld, 4, "sera");
            Assert.Equal(PartyFormationType.Wedge, loadedParty.Formation);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Phase4PartyLifecycle_PreservesPartyThroughSaveAndExtraction()
    {
        GameWorld world = new(seed: 3344);
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "sera",
            formation: PartyFormationType.Defensive);

        PartyController party = new(world);
        party.Initialize(manager.ActiveExpedition, new GridPosition(world.SpawnX, world.SpawnY));

        foreach (CharacterDirection direction in FindWalkableDirections(world, party.LeaderPosition, 4))
        {
            MoveResult result = party.TryMoveLeader(direction, manager.ActiveExpedition);
            if (result == MoveResult.Moved)
                AssertValidParty(party, world, 4, "sera");
        }

        PartyMember expeditionLeader = manager.ActiveExpedition.Party
            .Single(member => member.Id == "sera");
        expeditionLeader.Morale = 80;
        MoraleSystem.ApplyEvent(manager.ActiveExpedition, MoraleEventType.LootFound);
        ExtractionSystem.ApplyReward(
            manager.Campaign,
            manager.ActiveExpedition,
            new RewardBundle
            {
                Gold = 25,
                Materials =
                {
                    new Material { Id = "monster-residue", Name = "Monster Residue", Quantity = 1 }
                }
            });

        string path = Path.Combine(Path.GetTempPath(), $"daedalus-phase4-e2e-{Guid.NewGuid():N}.json");
        try
        {
            Assert.True(manager.Save(path));

            GameStateManager loaded = new();
            Assert.True(loaded.Load(path));

            PartyMember loadedLeader = loaded.ActiveExpedition.Party
                .Single(member => member.Id == "sera");

            Assert.Equal(82, loadedLeader.Morale);
            Assert.Equal(25, loaded.ActiveExpedition.CarriedGold);
            Assert.Contains(
                loaded.ActiveExpedition.CarriedMaterials,
                material => material.Id == "monster-residue");

            Assert.True(ExtractionSystem.Extract(loaded));
            Assert.Equal("Extracted", loaded.ActiveExpedition.ExtractionState);
            Assert.Equal(25, loaded.Campaign.Gold);
            Assert.Equal(
                82,
                loaded.Campaign.PartyRoster.Single(member => member.Id == "sera").Morale);
            Assert.Contains(
                loaded.Campaign.Materials,
                material => material.Id == "monster-residue");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static void AssertValidParty(
        PartyController party,
        GameWorld world,
        int expectedCount,
        string expectedLeaderId)
    {
        Assert.Equal(expectedCount, party.Members.Count);
        Assert.Equal(expectedLeaderId, party.LeaderId);
        Assert.Single(party.Members, member => member.MemberId == expectedLeaderId);

        List<GridPosition> positions = party.Members.Select(member => member.Position).ToList();
        Assert.Equal(expectedCount, positions.Distinct().Count());
        Assert.All(positions, position => Assert.True(world.IsWalkable(position.X, position.Y)));
    }

    private static GameStateManager CreatePartyManager(GameWorld world)
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(
            world.SpawnX,
            world.SpawnY,
            30,
            30,
            floorSeed: world.FloorSeed,
            selectedMemberIds: ["arden", "lyra", "marek", "sera"],
            leaderId: "arden");
        return manager;
    }

    private static PartyController CreatePartyController(GameWorld world, GameStateManager manager)
    {
        PartyController party = new(world);
        party.Initialize(
            manager.ActiveExpedition,
            new GridPosition(world.SpawnX, world.SpawnY));
        return party;
    }

    private static (int Dx, int Dy, CharacterDirection Direction) FindWalkableDirection(
        GameWorld world,
        GridPosition position)
    {
        foreach ((int dx, int dy, CharacterDirection direction) candidate in new[]
        {
            (0, -1, CharacterDirection.Up),
            (0, 1, CharacterDirection.Down),
            (-1, 0, CharacterDirection.Left),
            (1, 0, CharacterDirection.Right)
        })
        {
            if (world.IsWalkable(position.X + candidate.dx, position.Y + candidate.dy))
                return candidate;
        }

        throw new InvalidOperationException("No walkable test direction exists.");
    }

    private static CharacterDirection[] FindWalkableDirections(
        GameWorld world,
        GridPosition start,
        int count)
    {
        List<CharacterDirection> result = new();
        GridPosition current = start;

        for (int i = 0; i < count; i++)
        {
            (int dx, int dy, CharacterDirection direction) candidate = FindWalkableDirection(world, current);
            result.Add(candidate.direction);
            current = new GridPosition(current.X + candidate.dx, current.Y + candidate.dy);
        }

        return result.ToArray();
    }
}
