using Systemic.Engine.State;
using Xunit;

public class Phase3HardeningTests
{
    [Fact]
    public void AdvanceFloor_PreservesExpeditionWideProgress()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30, floor: 1, floorSeed: 123);
        manager.ActiveExpedition.Upkeep = 7;
        manager.ActiveExpedition.NodeHistory.Add("floor-1-chest");

        manager.AdvanceFloor(25, 30);
        manager.SetExpeditionPosition(20, 20);

        Assert.Equal(2, manager.ActiveExpedition.CurrentFloor);
        Assert.Equal(7, manager.ActiveExpedition.Upkeep);
        Assert.Contains("floor-1-chest", manager.ActiveExpedition.NodeHistory);
        Assert.Empty(manager.ActiveExpedition.CompletedNodeIds);
        Assert.Empty(manager.ActiveExpedition.DefeatedNodeIds);
        Assert.Equal((20, 20), manager.ActiveExpedition.PlayerGridPosition);
    }

    [Fact]
    public void Extraction_CommitsPartyProgressionButNotCurrentHealth()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30);

        PartyMember expeditionMember = manager.ActiveExpedition.Party
            .Single(member => member.Id == "arden");
        expeditionMember.Experience = 125;
        expeditionMember.Level = 2;
        expeditionMember.HP = 7;

        PartyMember campaignMember = manager.Campaign.PartyRoster
            .Single(member => member.Id == "arden");
        campaignMember.HP = 30;

        Assert.True(ExtractionSystem.Extract(manager));

        Assert.Equal(125, campaignMember.Experience);
        Assert.Equal(2, campaignMember.Level);
        Assert.Equal(30, campaignMember.HP);
    }

    [Fact]
    public void SaveRoundTrip_PreservesPartyProgression()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30);
        PartyMember expeditionMember = manager.ActiveExpedition.Party
            .Single(member => member.Id == "arden");
        expeditionMember.Experience = 75;
        expeditionMember.Level = 2;

        string path = Path.Combine(
            Path.GetTempPath(),
            $"daedalus-party-test-{Guid.NewGuid():N}.json");

        try
        {
            Assert.True(manager.Save(path));

            GameStateManager loaded = new();
            Assert.True(loaded.Load(path));

            PartyMember loadedMember = loaded.ActiveExpedition.Party
                .Single(member => member.Id == "arden");

            Assert.Equal(75, loadedMember.Experience);
            Assert.Equal(2, loadedMember.Level);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ApplyReward_KeepsLootInExpeditionUntilTransfer()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30);

        RewardBundle reward = new()
        {
            Gold = 25
        };
        reward.Cores.Add(new CoreReward { Type = "Standard", Charge = 10 });

        ExtractionSystem.ApplyReward(
            manager.Campaign,
            manager.ActiveExpedition,
            reward);

        Assert.Equal(0, manager.Campaign.Gold);
        Assert.Empty(manager.Campaign.Cores);
        Assert.Equal(25, manager.ActiveExpedition.CarriedGold);
        Assert.Single(manager.ActiveExpedition.CarriedCores);

        InventorySystem.TransferCarriedInventoryToStash(
            manager.Campaign,
            manager.ActiveExpedition);

        Assert.Equal(25, manager.Campaign.Gold);
        Assert.Single(manager.Campaign.Cores);
        Assert.Equal(0, manager.ActiveExpedition.CarriedGold);
    }

    [Fact]
    public void SaveRoundTrip_PreservesCompletedAndDefeatedNodes()
    {
        GameStateManager manager = new();
        manager.StartNewExpedition(10, 10, 30, 30, floor: 1, floorSeed: 456);
        manager.ActiveExpedition.CompletedNodeIds.Add("floor-1-chest");
        manager.ActiveExpedition.DefeatedNodeIds.Add("floor-1-combat");
        manager.ActiveExpedition.CarriedGold = 12;

        string path = Path.Combine(
            Path.GetTempPath(),
            $"daedalus-test-{Guid.NewGuid():N}.json");

        try
        {
            Assert.True(manager.Save(path));

            GameStateManager loaded = new();
            Assert.True(loaded.Load(path));
            Assert.Contains("floor-1-chest", loaded.ActiveExpedition.CompletedNodeIds);
            Assert.Contains("floor-1-combat", loaded.ActiveExpedition.DefeatedNodeIds);
            Assert.Equal(12, loaded.ActiveExpedition.CarriedGold);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void WorldRestore_ReopensCompletedChestAndRemovesDefeatedEnemy()
    {
        GameWorld world = new(seed: 789);
        ExpeditionState expedition = new()
        {
            CurrentFloor = world.Floor,
            FloorSeed = world.FloorSeed
        };

        DungeonNode? chestNode = world.Nodes
            .FirstOrDefault(node => node.Type == DungeonNodeType.Chest);
        DungeonNode? combatNode = world.Nodes
            .FirstOrDefault(node => node.Type == DungeonNodeType.Combat);

        Assert.NotNull(chestNode);
        Assert.NotNull(combatNode);

        expedition.CompletedNodeIds.Add(chestNode!.Id);
        expedition.DefeatedNodeIds.Add(combatNode!.Id);

        world.RestoreExpeditionState(expedition);

        Chest chest = Assert.IsType<Chest>(world.GetPropAt(chestNode.X, chestNode.Y));
        Assert.True(chest.IsOpen);
        Assert.Null(world.GetEnemyAt(combatNode.X, combatNode.Y));
    }
}
