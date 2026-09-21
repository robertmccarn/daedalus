public enum BiomeType
{
    RuinedDepths,
    AshenHalls,
    VerdantBelow,
    CrystalWastes
}

public static class BiomeCatalog
{
    public static BiomeType ForFloor(int floor) =>
        (BiomeType)Math.Clamp((Math.Max(1, floor) - 1) % 4, 0, 3);

    public static string Name(BiomeType biome) => biome switch
    {
        BiomeType.AshenHalls => "THE ASHEN HALLS",
        BiomeType.VerdantBelow => "THE VERDANT BELOW",
        BiomeType.CrystalWastes => "THE CRYSTAL WASTES",
        _ => "THE RUINED DEPTHS"
    };

    public static string Description(BiomeType biome) => biome switch
    {
        BiomeType.AshenHalls => "Heat-scarred masonry and dormant industrial chambers.",
        BiomeType.VerdantBelow => "Root-choked ruins where old stone has begun to breathe again.",
        BiomeType.CrystalWastes => "Cold caverns threaded with impossible mineral light.",
        _ => "A drowned civilization of broken bridges and forgotten machines."
    };
}
