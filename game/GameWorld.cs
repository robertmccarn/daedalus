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
    public List<WorldVisualFeature> VisualFeatures { get; private set; } = new();

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
        VisualFeatures = new List<WorldVisualFeature>();

        CreateVisualFeatures();
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

    private void CreateVisualFeatures()
    {
        // The first room is a deterministic visual reference chamber. These features
        // are render-only metadata: they intentionally do not modify collision.
        AddFeature(WorldVisualFeatureType.Doorway, SpawnX, SpawnY - 4, 3, 1, 3, 0);

        AddFeature(WorldVisualFeatureType.Pillar, SpawnX - 5, SpawnY - 4, 1, 1, 2, 0);
        AddFeature(WorldVisualFeatureType.Pillar, SpawnX + 5, SpawnY - 4, 1, 1, 2, 1);
        AddFeature(WorldVisualFeatureType.Pillar, SpawnX - 5, SpawnY + 2, 1, 1, 1, 2);
        AddFeature(WorldVisualFeatureType.Pillar, SpawnX + 5, SpawnY + 2, 1, 1, 1, 3);

        AddFeature(WorldVisualFeatureType.Abyss, SpawnX - 6, SpawnY + 3, 13, 3, 0, Floor % 2);
        AddFeature(WorldVisualFeatureType.Bridge, SpawnX - 3, SpawnY + 2, 7, 2, 2, 0);

        AddFeature(WorldVisualFeatureType.BrokenWall, SpawnX - 6, SpawnY - 3, 2, 1, 2, 0);
        AddFeature(WorldVisualFeatureType.BrokenWall, SpawnX + 4, SpawnY - 3, 2, 1, 2, 1);

        AddFeature(WorldVisualFeatureType.Rubble, SpawnX - 4, SpawnY, 2, 2, 1, 0);
        AddFeature(WorldVisualFeatureType.Rubble, SpawnX + 3, SpawnY + 1, 2, 2, 1, 1);
        AddFeature(WorldVisualFeatureType.Rubble, SpawnX - 3, SpawnY - 2, 2, 1, 0, 2);

        AddFeature(WorldVisualFeatureType.Landmark, SpawnX + 5, SpawnY - 1, 2, 3, 4, Floor % 3);
    }

    private void AddFeature(
        WorldVisualFeatureType type,
        int x,
        int y,
        int width,
        int height,
        int elevation,
        int variant)
    {
        if (x < -width || x >= Width || y < -height || y >= Height)
            return;

        VisualFeatures.Add(new WorldVisualFeature(
            type,
            Math.Clamp(x, 0, Width - 1),
            Math.Clamp(y, 0, Height - 1),
            width,
            height,
            elevation,
            variant));
    }

    private void CreateEnemies()
    {
        BiomeType biome = BiomeCatalog.ForFloor(Floor);

        AddEnemy(
            EnemyName(biome, 0),
            20 + (Floor - 1) * 3,
            1 + Floor - 1,
            new Stats(5 + Floor - 1, 2 + Floor / 2, 4 + Floor / 2, 3 + Floor / 2),
            FindOpenCellNear(ExitX - 2, ExitY));

        if (Floor >= 2)
        {
            AddEnemy(
                EnemyName(biome, 1),
                15 + Floor * 2,
                1 + Floor,
                new Stats(3 + Floor / 2, 6 + Floor / 3, 7 + Floor / 2, 5),
                FindOpenCellNear(ExitX - 4, ExitY + 1));
        }

        if (Floor >= 3)
        {
            AddEnemy(
                EnemyName(biome, 2),
                28 + Floor * 2,
                2 + Floor,
                new Stats(8 + Floor / 2, 3 + Floor / 3, 2 + Floor / 2, 4),
                FindOpenCellNear(ExitX - 5, ExitY - 1));
        }
    }

    private void AddEnemy(string name, int hp, int level, Stats stats, GridPosition position)
    {
        Enemies.Add(new Character(name, hp, level, stats, position.X, position.Y));
    }

    private string EnemyName(BiomeType biome, int variant) => biome switch
    {
        BiomeType.AshenHalls => variant switch
        {
            1 => "Ash Hound",
            2 => "Cinder Brute",
            _ => "Ash Warden"
        },
        BiomeType.VerdantBelow => variant switch
        {
            1 => "Root Stalker",
            2 => "Verdant Husk",
            _ => "Mossbound"
        },
        BiomeType.CrystalWastes => variant switch
        {
            1 => "Shard Moth",
            2 => "Prism Sentinel",
            _ => "Crystal Husk"
        },
        _ => variant switch
        {
            1 => "Hollow Stalker",
            2 => "Ruined Brute",
            _ => "Hollow Guard"
        }
    };

    private GridPosition FindOpenCellNear(int centerX, int centerY)
    {
        for (int radius = 0; radius <= 5; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || x >= Width || y < 0 || y >= Height)
                        continue;
                    if (!Dungeon[y, x].IsWalkable || GetEnemyAt(x, y) != null)
                        continue;
                    return new GridPosition(x, y);
                }
            }
        }

        return new GridPosition(Math.Clamp(centerX, 0, Width - 1), Math.Clamp(centerY, 0, Height - 1));
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
