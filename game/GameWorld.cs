public class GameWorld
{
    public Tile[,] Dungeon { get; private set; }

    public Character Player { get; private set; }

    public List<Character> Enemies { get; private set; }
    public List<Character> DefeatedEnemies { get; private set; }
    public List<InteractiveProp> Props { get; private set; }

    public int SpawnX { get; private set; }

    public int SpawnY { get; private set; }

    public int ExitX { get; private set; }

    public int ExitY { get; private set; }

    public GameWorld()
    {
        DungeonGenerator generator =
            new DungeonGenerator();

        var dungeon =
            generator.Generate(
                60,
                25,
                10);

        Dungeon =
            dungeon.Map;

        SpawnX =
            dungeon.Spawn.X;

        SpawnY =
            dungeon.Spawn.Y;

        ExitX =
            dungeon.Exit.X;

        ExitY =
            dungeon.Exit.Y;

        Stats playerStats =
            new Stats(
                8,
                3,
                6,
                5);

        Player =
            new Character(
                "Arden",
                30,
                1,
                playerStats,
                SpawnX,
                SpawnY,
                AtlasUnit.Technomancer);

        Enemies =
            new List<Character>();

        DefeatedEnemies =
            new List<Character>();

        Props =
            new List<InteractiveProp>();

        CreateEnemies();
        CreateProps();
    }

    private void CreateEnemies()
    {
        Character goblin =
            new Character(
                "Goblin",
                20,
                1,
                new Stats(
                    5,
                    2,
                    4,
                    3),
                ExitX - 2,
                ExitY,
                AtlasUnit.VoidHound);

        Enemies.Add(goblin);
    }

    private void CreateProps()
    {
        Props.Add(new Chest(SpawnX + 1, SpawnY));
        Props.Add(new Terminal(SpawnX + 2, SpawnY));
    }

    public bool IsWalkable(
        int x,
        int y)
    {
        if (y < 0 ||
            y >= Dungeon.GetLength(0))
        {
            return false;
        }

        if (x < 0 ||
            x >= Dungeon.GetLength(1))
        {
            return false;
        }

        InteractiveProp? prop = GetPropAt(x, y);
        return Dungeon[y, x].IsWalkable && (prop == null || !prop.IsBlocking);
    }

    public Tile GetTile(
        int x,
        int y)
    {
        return Dungeon[y, x];
    }

    public Character? GetEnemyAt(
        int x,
        int y)
    {
        foreach (Character enemy in Enemies)
        {
            if (enemy.X == x &&
                enemy.Y == y)
            {
                return enemy;
            }
        }

        return null;
    }

    public InteractiveProp? GetPropAt(int x, int y)
    {
        return Props.FirstOrDefault(prop => prop.X == x && prop.Y == y);
    }


    public void RemoveEnemy(
        Character enemy)
    {
        Enemies.Remove(enemy);
    }

    public void BeginEnemyDeath(Character enemy)
    {
        enemy.EnemyAnimator?.SetState(EnemyAnimationState.Death);
        Enemies.Remove(enemy);
        DefeatedEnemies.Add(enemy);
    }

    public void RemoveFinishedDeathAnimations()
    {
        DefeatedEnemies.RemoveAll(enemy => enemy.EnemyAnimator?.IsFinished == true);
    }
}
