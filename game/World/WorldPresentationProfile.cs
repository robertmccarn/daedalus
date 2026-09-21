using System.Drawing;

public sealed record WorldPresentationProfile(
    Color Floor,
    Color FloorAlternate,
    Color Wall,
    Color WallHighlight,
    Color Void,
    Color Accent,
    Color WarmLight,
    Color Hazard,
    Color Foreground,
    Color Mist)
{
    public static WorldPresentationProfile ForBiome(BiomeType biome) => biome switch
    {
        BiomeType.AshenHalls => new(
            Color.FromArgb(62, 54, 49), Color.FromArgb(72, 60, 52),
            Color.FromArgb(76, 69, 66), Color.FromArgb(112, 93, 76),
            Color.FromArgb(24, 21, 22), Color.FromArgb(196, 102, 70),
            Color.FromArgb(218, 156, 84), Color.FromArgb(174, 68, 62),
            Color.FromArgb(20, 19, 22), Color.FromArgb(68, 55, 57)),
        BiomeType.VerdantBelow => new(
            Color.FromArgb(49, 65, 55), Color.FromArgb(57, 76, 62),
            Color.FromArgb(67, 75, 68), Color.FromArgb(91, 111, 84),
            Color.FromArgb(20, 29, 25), Color.FromArgb(83, 184, 135),
            Color.FromArgb(202, 164, 89), Color.FromArgb(181, 76, 75),
            Color.FromArgb(17, 26, 22), Color.FromArgb(56, 85, 67)),
        BiomeType.CrystalWastes => new(
            Color.FromArgb(48, 51, 70), Color.FromArgb(57, 59, 82),
            Color.FromArgb(73, 75, 92), Color.FromArgb(101, 108, 135),
            Color.FromArgb(18, 20, 34), Color.FromArgb(104, 215, 226),
            Color.FromArgb(203, 169, 103), Color.FromArgb(187, 77, 112),
            Color.FromArgb(17, 18, 31), Color.FromArgb(58, 63, 96)),
        _ => new(
            Color.FromArgb(53, 56, 59), Color.FromArgb(61, 64, 66),
            Color.FromArgb(78, 77, 73), Color.FromArgb(112, 107, 94),
            Color.FromArgb(19, 22, 26), Color.FromArgb(77, 190, 181),
            Color.FromArgb(218, 166, 82), Color.FromArgb(190, 82, 86),
            Color.FromArgb(18, 20, 24), Color.FromArgb(50, 69, 72))
    };
}
