using System.Drawing;

public class TileRenderer : IDisposable
{
    public const int TileSize = 32;

    private readonly AtlasSlicer atlas = new();

    public void DrawTile(Graphics graphics, Tile tile, int screenX, int screenY)
    {
        Rectangle destinationRectangle = tile.Type is TileType.Wall or TileType.Pillar
            ? new Rectangle(
                screenX - TileSize / 2,
                screenY - TileSize + TileSize / 4,
                TileSize,
                TileSize)
            : new Rectangle(
                screenX - TileSize / 2,
                screenY - TileSize / 4,
                TileSize,
                TileSize / 2);

        atlas.Draw(graphics, atlas.GetTileSourceRect(tile.Type), destinationRectangle);
    }

    public void DrawUnit(
        Graphics graphics,
        AtlasUnit unit,
        AtlasDirection direction,
        int screenX,
        int screenY)
    {
        AtlasRegion region = atlas.GetUnitRegion(unit, direction);
        int spriteSize = TileSize * 2;
        int bobbingOffset = unit == AtlasUnit.ReconDrone
            ? (int)MathF.Round(MathF.Sin(Environment.TickCount / 150f) * 3f)
            : 0;

        Rectangle destinationRectangle = new(
            screenX - spriteSize / 2,
            screenY - (int)(spriteSize * region.PivotY) + bobbingOffset,
            spriteSize,
            spriteSize);

        atlas.Draw(graphics, region, destinationRectangle);
    }

    public void DrawUnit(
        Graphics graphics,
        Character character,
        AtlasDirection direction,
        int screenX,
        int screenY)
    {
        if (character.EnemyAnimator == null)
        {
            DrawUnit(graphics, character.VisualUnit, direction, screenX, screenY);
            return;
        }

        character.EnemyAnimator.Update(80);
        AtlasRegion region = character.EnemyAnimator.GetFrame(atlas, character.VisualUnit, direction);
        int spriteSize = TileSize * 2;
        Rectangle destination = new(
            screenX - spriteSize / 2,
            screenY - (int)(spriteSize * region.PivotY),
            spriteSize,
            spriteSize);

        atlas.Draw(graphics, region, destination);
    }

    public void DrawProp(Graphics graphics, AtlasProp prop, int screenX, int screenY)
    {
        AtlasRegion region = atlas.GetPropRegion(prop);
        int spriteSize = TileSize * 2;
        Rectangle destinationRectangle = new(
            screenX - spriteSize / 2,
            screenY - (int)(spriteSize * region.PivotY),
            spriteSize,
            spriteSize);

        atlas.Draw(graphics, region, destinationRectangle);
    }

    public void Dispose()
    {
        atlas.Dispose();
    }
}
