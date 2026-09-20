using Systemic.Engine.State;

public class GameWorld
{
    public const int Width = 60;
    public const int Height = 25;
    public const int RoomCount = 10;

    public Tile[,] Dungeon { get; private set; } = null!;
    // Compatibility bridge for the current battle system. PartyController is the authoritative party position.
    public Character Player { get; private set; } = null!;
    public List<Character> Enemies { get; private set; } = new();
    public List<Character> DefeatedEnemies { get; private set; } = new();
    public List<InteractiveProp> Props { get; private set; } = new();
    public List<StaticUnit> StaticUnits { get; private set; } = new();
    public List<DungeonNode> Nodes { get; private set; } = new();

    public int Floor { get; private set; } = 1;
    public int FloorSeed { get; private set; }

    public int SpawnX { get; private set; }
    public int SpawnY { get; private set; }
    public int ExitX { get; private set; }
    public int ExitY { get; private set; }

    public GameWorld(int floor = 1, int? seed = null)
    {
        Player = new Character(
            "Arden",
            30,
            1,
            new Stats(8, 3, 6, 5),
            0,
            0);

        RebuildFloor(seed ?? Random.Shared.Next(), floor);
    }

    public void RebuildFloor(int seed, int floor)
    {
        Floor = floor;
        FloorSeed = seed;

        DungeonGenerator generator = new(seed);
        var dungeon = generator.Generate(Width, Height, RoomCount);

        Dungeon = dungeon.Map;
        SpawnX = dungeon.Spawn.X;
        SpawnY = dungeon.Spawn.Y;
        ExitX = dungeon.Exit.X;
        ExitY = dungeon.Exit.Y;

        Player.MoveTo(SpawnX, SpawnY);

        Enemies = new List<Character>();
        DefeatedEnemies = new List<Character>();
        Props = new List<InteractiveProp>();
        StaticUnits = new List<StaticUnit>();
        Nodes = new List<DungeonNode>();

        CreateEnemies();
        CreateProps();
        CreateNodes();
    }

    public void SyncPlayerFromPartyMember(PartyMember member, GridPosition position)
    {
        Player.SyncFromState(member, position);
    }

    public bool IsWalkable(int x, int y)
    {
        if (y < 0 || y >= Dungeon.GetLength(0))
            return false;

        if (x < 0 || x >= Dungeon.GetLength(1))
            return false;

        InteractiveProp? prop = GetPropAt(x, y);
        return Dungeon[y, x].IsWalkable &&
               (prop == null || !prop.IsBlocking);
    }

    public Tile GetTile(int x, int y) => Dungeon[y, x];

    public Character? GetEnemyAt(int x, int y) =>
        Enemies.FirstOrDefault(enemy => enemy.X == x && enemy.Y == y);

    public InteractiveProp? GetPropAt(int x, int y) =>
        Props.FirstOrDefault(prop => prop.X == x && prop.Y == y);

    public StaticUnit? GetStaticUnitAt(int x, int y) =>
        StaticUnits.FirstOrDefault(unit => unit.X == x && unit.Y == y);

    public DungeonNode? GetNodeAt(int x, int y) =>
        Nodes.FirstOrDefault(node => node.X == x && node.Y == y);

    public void CompleteNodeAt(int x, int y)
    {
        DungeonNode? node = GetNodeAt(x, y);
        if (node != null)
            node.IsCompleted = true;
    }

    public void RemoveEnemy(Character enemy) =>
        Enemies.Remove(enemy);

    public void BeginEnemyDeath(Character enemy)
    {
        Enemies.Remove(enemy);
        DefeatedEnemies.Add(enemy);
        CompleteNodeAt(enemy.X, enemy.Y);
    }

    public void RestoreExpeditionState(ExpeditionState expedition)
    {
        foreach (string nodeId in expedition.CompletedNodeIds)
        {
            DungeonNode? node = Nodes.FirstOrDefault(candidate => candidate.Id == nodeId);
            if (node == null)
                continue;

            node.IsCompleted = true;

            InteractiveProp? prop = GetPropAt(node.X, node.Y);
            switch (prop)
            {
                case Chest chest:
                    chest.RestoreOpen();
                    break;
                case Terminal terminal:
                    terminal.RestoreActivated();
                    break;
            }

            StaticUnit? unit = GetStaticUnitAt(node.X, node.Y);
            unit?.Activate();
        }

        foreach (string nodeId in expedition.DefeatedNodeIds)
        {
            DungeonNode? node = Nodes.FirstOrDefault(candidate => candidate.Id == nodeId);
            if (node == null || node.Type != DungeonNodeType.Combat)
                continue;

            Character? enemy = GetEnemyAt(node.X, node.Y);
            if (enemy != null)
            {
                Enemies.Remove(enemy);
                DefeatedEnemies.Add(enemy);
                node.IsCompleted = true;
            }
        }
    }

    private void CreateEnemies()
    {
        Character goblin = new(
            Floor == 1 ? "Goblin" : $"Goblin Depth {Floor}",
            20 + (Floor - 1) * 3,
            1 + (Floor - 1),
            new Stats(
                5 + Floor - 1,
                2 + Floor / 2,
                4 + Floor / 2,
                3 + Floor / 2),
            ExitX - 2,
            ExitY);

        Enemies.Add(goblin);
    }

    private void CreateProps()
    {
        RewardBundle chestReward = new()
        {
            Gold = 5 * Floor
        };

        chestReward.Items.Add(new InventoryItem
        {
            Id = "healing-tonic",
            Name = "Healing Tonic",
            Quantity = 1
        });

        chestReward.Materials.Add(new Material
        {
            Id = "rusted-catalyst",
            Name = "Rusted Catalyst",
            Quantity = 2
        });

        Props.Add(new Chest(SpawnX + 1, SpawnY, chestReward));

        Props.Add(new Terminal(
            SpawnX + 2,
            SpawnY,
            new CoreReward
            {
                Type = Floor >= 3 ? "Refined" : "Standard",
                Charge = 8 + Floor,
                Quantity = 1
            },
            healAmount: 8));
    }

    private void CreateNodes()
    {
        Nodes.Add(new DungeonNode
        {
            Id = $"floor-{Floor}-start",
            Name = "Expedition Start",
            Type = DungeonNodeType.Start,
            X = SpawnX,
            Y = SpawnY,
            IsCompleted = true
        });

        InteractiveProp? chest = GetPropAt(SpawnX + 1, SpawnY);
        if (chest != null)
        {
            Nodes.Add(new DungeonNode
            {
                Id = $"floor-{Floor}-chest",
                Name = "Supply Cache",
                Type = DungeonNodeType.Chest,
                X = chest.X,
                Y = chest.Y
            });

            StaticUnits.Add(new StaticUnit(
                "Supply Cache",
                chest.X,
                chest.Y,
                false,
                DungeonNodeType.Chest,
                $"floor-{Floor}-chest"));
        }

        InteractiveProp? terminal = GetPropAt(SpawnX + 2, SpawnY);
        if (terminal != null)
        {
            Nodes.Add(new DungeonNode
            {
                Id = $"floor-{Floor}-terminal",
                Name = "Energy Terminal",
                Type = DungeonNodeType.Terminal,
                X = terminal.X,
                Y = terminal.Y
            });

            StaticUnits.Add(new StaticUnit(
                "Energy Terminal",
                terminal.X,
                terminal.Y,
                false,
                DungeonNodeType.Terminal,
                $"floor-{Floor}-terminal"));
        }

        Nodes.Add(new DungeonNode
        {
            Id = $"floor-{Floor}-combat",
            Name = "Hostile Contact",
            Type = DungeonNodeType.Combat,
            X = ExitX - 2,
            Y = ExitY
        });

        Nodes.Add(new DungeonNode
        {
            Id = $"floor-{Floor}-extraction",
            Name = "Descent",
            Type = DungeonNodeType.Extraction,
            X = ExitX,
            Y = ExitY
        });
    }
}
