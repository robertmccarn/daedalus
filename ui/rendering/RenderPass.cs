public enum RenderPass
{
    Backdrop = 0,
    Terrain = 10,
    Structures = 20,
    Entities = 30,
    Foreground = 40,
    Effects = 50,
    Hud = 60
}

public readonly record struct RenderItem(
    RenderPass Pass,
    int Depth,
    int StableOrder,
    Action<System.Drawing.Graphics> Draw);
