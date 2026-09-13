using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public enum AtlasDirection
{
    SouthWest = 0,
    SouthEast = 1,
    NorthWest = 2,
    NorthEast = 3
}

public enum AtlasUnit
{
    Cyrus = 0,
    SteelGolem = 1,
    CorruptedAutomaton = 2,
    Technomancer = 3,
    Paladin = 4,
    VoidHound = 5,
    ReconDrone = 6
}

public enum AtlasSheet
{
    Master,
    Heroes,
    Enemies,
    Props,
    Environment,
    Portraits,
    CommandIcons,
    StatusIcons,
    BossExpressions,
    EnemyBattleCards,
    EnemyAnimations,
    TacticalUi
}

public enum AtlasProp
{
    ChestClosed = 0,
    ChestOpen = 1,
    EnergyTerminal = 2,
    RubblePile = 3
}

public enum AtlasEnvironment
{
    Water0 = 0,
    Water1 = 1,
    Water2 = 2,
    Water3 = 3,
    MossyStone0 = 4,
    MossyStone1 = 5,
    MossyStone2 = 6,
    MossyStone3 = 7,
    RustGrate0 = 8,
    RustGrate1 = 9,
    RustGrate2 = 10,
    RustGrate3 = 11,
    Staircase0 = 12,
    Staircase1 = 13,
    Staircase2 = 14,
    Staircase3 = 15,
    Pillar0 = 16,
    Pillar1 = 17,
    Pillar2 = 18,
    Pillar3 = 19
}

public enum AtlasPortrait { Cyrus = 0, Technomancer = 1, Paladin = 2 }
public enum AtlasCommandIcon { Attack = 0, Defend = 1, Tech = 2, Item = 3, Interact = 4, Retreat = 5 }
public enum AtlasStatusIcon { AttackBuff = 0, DefenseBuff = 1, Haste = 2, Poison = 3, Bleed = 4, CoreCharge = 5 }
public enum BossExpression { Threatening = 0, Taunting = 1, Damaged = 2, Enraged = 3 }
public enum TacticalHighlight { MoveRange = 0, AttackRange = 1, Selected = 2, EnemyTile = 3, Invalid = 4 }

public readonly record struct AtlasRegion(
    AtlasSheet Sheet,
    Rectangle Source,
    float PivotY);

public class AtlasSlicer : IDisposable
{
    public const int ColumnCount = 4;
    public const int RowCount = 5;

    private readonly Bitmap texture;
    private readonly Dictionary<AtlasSheet, Bitmap> expansionTextures = new();

    public int CellWidth { get; }
    public int CellHeight { get; }

    public AtlasSlicer()
    {
        string assetPath = Path.Combine(AppContext.BaseDirectory, "assets", "master_atlas.png");

        texture = ChromaKeyTextureLoader.Load(assetPath);
        expansionTextures.Add(AtlasSheet.Heroes, LoadTexture("atlas_heroes.png"));
        expansionTextures.Add(AtlasSheet.Enemies, LoadTexture("atlas_enemies.png"));
        expansionTextures.Add(AtlasSheet.Props, LoadTexture("atlas_props.png"));
        expansionTextures.Add(AtlasSheet.Environment, LoadTexture("atlas_environment.png"));
        expansionTextures.Add(AtlasSheet.Portraits, LoadTexture("portraits.png"));
        expansionTextures.Add(AtlasSheet.CommandIcons, LoadTexture("command_icons.png"));
        expansionTextures.Add(AtlasSheet.StatusIcons, LoadTexture("status_icons.png"));
        expansionTextures.Add(AtlasSheet.BossExpressions, LoadTexture("enemy_boss_expressions.png"));
        expansionTextures.Add(AtlasSheet.EnemyBattleCards, LoadTexture("enemy_battle_cards.png"));
        expansionTextures.Add(AtlasSheet.EnemyAnimations, LoadTexture("enemy_spritesheets.png"));
        expansionTextures.Add(AtlasSheet.TacticalUi, LoadTexture("tactical_ui_suite.png"));
        CellWidth = texture.Width / ColumnCount;
        CellHeight = texture.Height / RowCount;
    }

    public Rectangle GetSourceRect(int row, int column)
    {
        if (row < 0 || row >= RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }

        if (column < 0 || column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        return new Rectangle(column * CellWidth, row * CellHeight, CellWidth, CellHeight);
    }

    public Rectangle GetTileSourceRect(TileType tileType)
    {
        if (tileType == TileType.Wall)
        {
            return GetWallSourceRect();
        }

        int column = tileType switch
        {
            TileType.Floor => 0,
            TileType.Water => 2,
            TileType.Trap => 1,
            TileType.Pillar => 3,
            _ => 0
        };

        return GetArtRegion(0.712f, 0.155f, column);
    }

    private Rectangle GetWallSourceRect()
    {
        return GetArtRegion(0.712f, 0.155f, 3);
    }

    public Rectangle GetUnitSourceRect(AtlasUnit unit, AtlasDirection direction)
    {
        return GetArtRegion((int)unit * 0.25f, 0.25f, (int)direction);
    }

    public AtlasRegion GetUnitRegion(AtlasUnit unit, AtlasDirection direction)
    {
        return unit switch
        {
            AtlasUnit.Technomancer => GetExpansionRegion(AtlasSheet.Heroes, 2, 0, (int)direction, 0.88f),
            AtlasUnit.Paladin => GetExpansionRegion(AtlasSheet.Heroes, 2, 1, (int)direction, 0.88f),
            AtlasUnit.VoidHound => GetExpansionRegion(AtlasSheet.Enemies, 2, 0, (int)direction, 0.85f),
            AtlasUnit.ReconDrone => GetExpansionRegion(AtlasSheet.Enemies, 2, 1, (int)direction, 0.75f),
            _ => new AtlasRegion(AtlasSheet.Master, GetUnitSourceRect(unit, direction), 0.92f)
        };
    }

    public AtlasRegion GetPropRegion(AtlasProp prop)
    {
        return GetExpansionRegion(AtlasSheet.Props, 1, 0, (int)prop, 0.90f);
    }

    public AtlasRegion GetEnvironmentRegion(AtlasEnvironment feature)
    {
        int value = (int)feature;

        if (value < 12)
        {
            return GetExpansionRegion(AtlasSheet.Environment, 6, 4, value / 4, value % 4, 0f);
        }

        int variant = value % 4;
        int column = value < 16 ? 4 : 5;
        return GetExpansionRegion(AtlasSheet.Environment, 6, 4, variant, column, 0.90f);
    }

    public AtlasRegion GetPortraitRegion(AtlasPortrait portrait) =>
        GetExpansionRegion(AtlasSheet.Portraits, 1, 3, 0, (int)portrait, 0f);

    public AtlasRegion GetCommandIconRegion(AtlasCommandIcon icon) =>
        GetExpansionRegion(AtlasSheet.CommandIcons, 1, 6, 0, (int)icon, 0f);

    public AtlasRegion GetStatusIconRegion(AtlasStatusIcon icon) =>
        GetExpansionRegion(AtlasSheet.StatusIcons, 1, 6, 0, (int)icon, 0f);

    public AtlasRegion GetBossExpressionRegion(BossExpression expression) =>
        new(AtlasSheet.BossExpressions, new Rectangle((int)expression * 543, 0, 543, 724), 0f);

    public AtlasRegion GetEnemyAnimationRegion(AtlasUnit unit, AtlasDirection direction, int frame)
    {
        int row = unit switch
        {
            AtlasUnit.VoidHound => (int)direction,
            AtlasUnit.ReconDrone => 4 + (int)direction,
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

        if (frame < 0 || frame > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }

        return new(AtlasSheet.EnemyAnimations, new Rectangle(frame * 221, row * 110, 221, 110), 0.85f);
    }

    public AtlasRegion GetTacticalHighlightRegion(TacticalHighlight highlight)
    {
        return new(AtlasSheet.TacticalUi, new Rectangle(820 + (int)highlight * 128, 896, 112, 112), 0f);
    }

    public AtlasRegion GetEnemyCardPortraitRegion(AtlasUnit unit)
    {
        int y = unit == AtlasUnit.VoidHound ? 26 : 537;
        return new(AtlasSheet.EnemyBattleCards, new Rectangle(31, y, 321, 461), 0f);
    }

    public AtlasRegion GetEnemyCardActionRegion(AtlasUnit unit, int actionIndex, bool isActive)
    {
        if (actionIndex < 0 || actionIndex > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(actionIndex));
        }

        int baseY = unit == AtlasUnit.VoidHound ? 26 : 537;
        int y = baseY + (isActive ? 0 : 238);
        return new(AtlasSheet.EnemyBattleCards, new Rectangle(367 + actionIndex * 193, y, 178, 223), 0f);
    }

    public Rectangle GetIconSourceRect(int column)
    {
        return GetArtRegion(0.862f, 0.138f, column);
    }

    public void Draw(Graphics graphics, Rectangle sourceRect, Rectangle destinationRect)
    {
        InterpolationMode originalInterpolation = graphics.InterpolationMode;
        PixelOffsetMode originalPixelOffset = graphics.PixelOffsetMode;
        CompositingQuality originalCompositing = graphics.CompositingQuality;

        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.DrawImage(texture, destinationRect, sourceRect, GraphicsUnit.Pixel);

        graphics.InterpolationMode = originalInterpolation;
        graphics.PixelOffsetMode = originalPixelOffset;
        graphics.CompositingQuality = originalCompositing;
    }

    public void Draw(Graphics graphics, AtlasRegion region, Rectangle destinationRect)
    {
        Bitmap sourceTexture = region.Sheet == AtlasSheet.Master
            ? texture
            : expansionTextures[region.Sheet];

        DrawTexture(graphics, sourceTexture, region.Source, destinationRect);
    }

    public void Dispose()
    {
        texture.Dispose();

        foreach (Bitmap expansionTexture in expansionTextures.Values)
        {
            expansionTexture.Dispose();
        }
    }

    private Rectangle GetArtRegion(float normalizedY, float normalizedHeight, int column)
    {
        if (column < 0 || column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        int y = (int)MathF.Round(texture.Height * normalizedY);
        int height = (int)MathF.Round(texture.Height * normalizedHeight);

        return new Rectangle(column * CellWidth, y, CellWidth, height);
    }

    private AtlasRegion GetExpansionRegion(
        AtlasSheet sheet,
        int rowCount,
        int row,
        int column,
        float pivotY)
    {
        return GetExpansionRegion(sheet, rowCount, ColumnCount, row, column, pivotY);
    }

    private AtlasRegion GetExpansionRegion(
        AtlasSheet sheet,
        int rowCount,
        int columnCount,
        int row,
        int column,
        float pivotY)
    {
        Bitmap sourceTexture = expansionTextures[sheet];
        int cellWidth = sourceTexture.Width / columnCount;
        int cellHeight = sourceTexture.Height / rowCount;

        return new AtlasRegion(sheet, new Rectangle(
            column * cellWidth,
            row * cellHeight,
            cellWidth,
            cellHeight), pivotY);
    }

    private static Bitmap LoadTexture(string fileName)
    {
        string assetPath = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
        return ChromaKeyTextureLoader.Load(assetPath);
    }

    private static void DrawTexture(
        Graphics graphics,
        Bitmap sourceTexture,
        Rectangle sourceRect,
        Rectangle destinationRect)
    {
        InterpolationMode originalInterpolation = graphics.InterpolationMode;
        PixelOffsetMode originalPixelOffset = graphics.PixelOffsetMode;
        CompositingQuality originalCompositing = graphics.CompositingQuality;

        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.CompositingQuality = CompositingQuality.HighSpeed;
        graphics.DrawImage(sourceTexture, destinationRect, sourceRect, GraphicsUnit.Pixel);

        graphics.InterpolationMode = originalInterpolation;
        graphics.PixelOffsetMode = originalPixelOffset;
        graphics.CompositingQuality = originalCompositing;
    }
}
