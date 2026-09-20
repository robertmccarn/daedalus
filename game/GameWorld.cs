public class GameWorld
{
    public const int Width = 60;
    public const int Height = 25;
    public const int RoomCount = 10;

    public Tile[,] Dungeon { get; private set; } = null!;
    public Character Player { get; private set; } = null!;
    public List<Character> Enemies { get; private set; } = new();
    public List<Character> DefeatedEnemies { get; private set; } = new();
    public List<InteractiveProp> Props { get; private set; } = new();

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

        CreateEnemies();
        CreateProps();
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

    public void RemoveEnemy(Character enemy) =>
        Enemies.Remove(enemy);

    public void BeginEnemyDeath(Character enemy)
    {
        Enemies.Remove(enemy);
        DefeatedEnemies.Add(enemy);
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
        Props.Add(new Chest(SpawnX + 1, SpawnY));
        Props.Add(new Terminal(SpawnX + 2, SpawnY));
    }
}
