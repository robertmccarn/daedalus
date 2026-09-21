public enum WorldVisualFeatureType
{
    Abyss,
    Bridge,
    Pillar,
    Rubble,
    BrokenWall,
    Doorway,
    Landmark
}

public sealed record WorldVisualFeature(
    WorldVisualFeatureType Type,
    int X,
    int Y,
    int Width = 1,
    int Height = 1,
    int Elevation = 0,
    int Variant = 0);
