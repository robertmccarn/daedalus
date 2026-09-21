using Systemic.Engine.State;

public static class VisibilitySystem
{
    public static bool IsVisible(
        GameWorld world,
        GridPosition origin,
        GridPosition target,
        int radius = 7)
    {
        if (origin.X < 0 || origin.X >= GameWorld.Width ||
            origin.Y < 0 || origin.Y >= GameWorld.Height ||
            target.X < 0 || target.X >= GameWorld.Width ||
            target.Y < 0 || target.Y >= GameWorld.Height)
            return false;

        int dx = target.X - origin.X;
        int dy = target.Y - origin.Y;

        if (dx * dx + dy * dy > radius * radius)
            return false;

        int x = origin.X;
        int y = origin.Y;
        int sx = Math.Sign(dx);
        int sy = Math.Sign(dy);
        int ax = Math.Abs(dx);
        int ay = Math.Abs(dy);
        int err = ax - ay;

        while (true)
        {
            if (x == target.X && y == target.Y)
                return true;

            if (!(x == origin.X && y == origin.Y) &&
                world.Dungeon[y, x].BlocksVision)
                return false;

            int e2 = 2 * err;
            if (e2 > -ay)
            {
                err -= ay;
                x += sx;
            }
            if (e2 < ax)
            {
                err += ax;
                y += sy;
            }

            if (x < 0 || x >= GameWorld.Width || y < 0 || y >= GameWorld.Height)
                return false;
        }
    }
}
