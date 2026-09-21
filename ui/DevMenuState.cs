public static class DevMenuState
{
    public const int MinFloor = 1;
    public const int MaxFloor = 99;

    public static int NormalizeFloor(int floor) =>
        Math.Clamp(floor, MinFloor, MaxFloor);

    public static int PartyLevelForFloor(int floor) =>
        NormalizeFloor(floor);
}
