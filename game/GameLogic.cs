public enum MoveResult
{
    Blocked,
    Moved,
    Encounter
}


public class GameLogic
{
    public MoveResult MovePlayer(
        GameWorld world,
        int x,
        int y)
    {
        if (!world.IsWalkable(x, y))
        {
            return MoveResult.Blocked;
        }

        Character? enemy =
            world.GetEnemyAt(
                x,
                y);

        if (enemy != null)
        {
            return MoveResult.Encounter;
        }

        world.Player.MoveTo(
            x,
            y);

        return MoveResult.Moved;
    }


    public Tile GetPlayerTile(
        GameWorld world)
    {
        return world.GetTile(
            world.Player.X,
            world.Player.Y);
    }
}
