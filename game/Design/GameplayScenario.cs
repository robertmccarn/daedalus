using Systemic.Engine.State;

public readonly record struct GameplayScenarioSnapshot(
    int Floor,
    int TurnCount,
    int CarriedGold,
    int CarriedCores,
    int CarriedMaterials,
    int CarriedItems,
    int CarriedGear,
    int CampaignGold,
    int CampaignRuns,
    int PartyMembersAlive,
    int PartyMoraleAverage,
    string ExtractionState);

public readonly record struct GameplayScenarioResult(
    int Seed,
    bool Completed,
    GameplayScenarioSnapshot Snapshot,
    IReadOnlyList<GameplayEvent> Events);

public static class GameplayScenario
{
    public static GameplayScenarioResult RunBenchmark(int seed)
    {
        GameSession session = new(new GameWorld(1, seed));

        MoveTo(session, session.World.SpawnX + 1, session.World.SpawnY);
        session.Interact();

        MoveTo(session, session.World.SpawnX + 2, session.World.SpawnY);
        session.Interact();

        Character? enemy = session.World.Enemies.FirstOrDefault();
        if (enemy != null)
        {
            MoveTo(session, enemy.X, enemy.Y);
            ResolveBattle(session);
        }

        MoveTo(session, session.World.ExitX, session.World.ExitY);
        bool extracted = session.ExtractExpedition();

        return new GameplayScenarioResult(
            seed,
            extracted && session.State == GameState.ExtractionResults,
            Capture(session),
            session.Recorder.Events.ToArray());
    }

    private static void ResolveBattle(GameSession session)
    {
        const int maxCommands = 100;

        for (int command = 0;
             command < maxCommands && session.State == GameState.Battle;
             command++)
        {
            session.PerformBattleCommand();
        }

        if (session.State == GameState.Battle)
            throw new InvalidOperationException("Deterministic benchmark battle exceeded its command limit.");
    }

    private static void MoveTo(GameSession session, int targetX, int targetY)
    {
        const int maxSteps = GameWorld.Width * GameWorld.Height * 4;

        for (int step = 0; step < maxSteps; step++)
        {
            GridPosition current = session.Party.LeaderPosition;
            if (current.X == targetX && current.Y == targetY)
                return;

            CharacterDirection direction = FindNextDirection(
                session.World,
                current,
                new GridPosition(targetX, targetY));

            MoveResult result = session.MoveLeader(direction);

            if (session.State == GameState.Battle)
                ResolveBattle(session);

            if (result == MoveResult.Blocked)
                throw new InvalidOperationException(
                    $"Benchmark path was blocked at {current.X},{current.Y}.");
        }

        throw new InvalidOperationException(
            $"Benchmark path could not reach {targetX},{targetY}.");
    }

    private static CharacterDirection FindNextDirection(
        GameWorld world,
        GridPosition start,
        GridPosition target)
    {
        Dictionary<GridPosition, (GridPosition Previous, CharacterDirection Direction)> parents = new();
        Queue<GridPosition> queue = new();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            GridPosition current = queue.Dequeue();

            if (current == target)
                break;

            foreach ((GridPosition next, CharacterDirection direction) in Neighbors(current))
            {
                if (parents.ContainsKey(next) || next == start)
                    continue;

                if (!IsPathCell(world, next, target))
                    continue;

                parents[next] = (current, direction);
                queue.Enqueue(next);
            }
        }

        if (target != start && !parents.ContainsKey(target))
            throw new InvalidOperationException(
                $"No benchmark path exists from {start.X},{start.Y} to {target.X},{target.Y}.");

        GridPosition cursor = target;

        while (parents[cursor].Previous != start)
            cursor = parents[cursor].Previous;

        return parents[cursor].Direction;
    }

    private static bool IsPathCell(
        GameWorld world,
        GridPosition position,
        GridPosition target) =>
        position == target || world.IsWalkable(position.X, position.Y);

    private static IEnumerable<(GridPosition Position, CharacterDirection Direction)> Neighbors(
        GridPosition position)
    {
        yield return (new GridPosition(position.X, position.Y - 1), CharacterDirection.Up);
        yield return (new GridPosition(position.X + 1, position.Y), CharacterDirection.Right);
        yield return (new GridPosition(position.X, position.Y + 1), CharacterDirection.Down);
        yield return (new GridPosition(position.X - 1, position.Y), CharacterDirection.Left);
    }

    private static GameplayScenarioSnapshot Capture(GameSession session)
    {
        ExpeditionState expedition = session.StateManager.ActiveExpedition;
        CampaignState campaign = session.StateManager.Campaign;

        return new GameplayScenarioSnapshot(
            expedition.CurrentFloor,
            expedition.TurnCount,
            expedition.CarriedGold,
            expedition.CarriedCores.Count,
            expedition.CarriedMaterials.Sum(material => material.Quantity),
            expedition.CarriedInventory.Sum(item => item.Quantity),
            expedition.CarriedGear.Count,
            campaign.Gold,
            campaign.RunsCompleted,
            expedition.Party.Count(member => member.HP > 0),
            expedition.Party.Count == 0
                ? 0
                : (int)Math.Round(expedition.Party.Average(member => member.Morale)),
            expedition.ExtractionState);
    }
}
