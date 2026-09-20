public enum MoveResult
{
    Blocked,
    Moved,
    Encounter
}

public class GameLogic
{
    public Tile GetTile(GameWorld world, GridPosition position) =>
        world.GetTile(position.X, position.Y);
}
