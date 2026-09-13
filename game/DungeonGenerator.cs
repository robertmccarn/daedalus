public class DungeonGenerator
{
    private readonly Random random = new();

    public (
        Tile[,] Map,
        (int X, int Y) Spawn,
        (int X, int Y) Exit)
        Generate(
            int width,
            int height,
            int roomCount)
    {
        Tile[,] map = new Tile[height, width];

        FillWithWalls(map);

        List<Room> rooms = GenerateRooms(map, width, height, roomCount);
        ConnectRooms(map, rooms);

        Room startingRoom = rooms[0];
        Room finalRoom = rooms[rooms.Count - 1];

        (int X, int Y) spawn = startingRoom.Center;
        (int X, int Y) exit = finalRoom.Center;

        map[exit.Y, exit.X] = CreateStairsDown();

        return (map, spawn, exit);
    }

    private List<Room> GenerateRooms(
        Tile[,] map,
        int width,
        int height,
        int roomCount)
    {
        List<Room> rooms = new();
        int attempts = 0;
        int maxAttempts = roomCount * 30;

        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;

            int roomWidth = random.Next(5, 10);
            int roomHeight = random.Next(4, 7);
            int roomX = random.Next(1, width - roomWidth - 1);
            int roomY = random.Next(1, height - roomHeight - 1);

            Room room = new(roomX, roomY, roomWidth, roomHeight);

            if (OverlapsExistingRoom(room, rooms))
                continue;

            CarveRoom(map, room);
            rooms.Add(room);
        }

        if (rooms.Count < roomCount)
            throw new Exception("Could not generate enough non-overlapping rooms.");

        return rooms;
    }

    private static bool OverlapsExistingRoom(Room candidate, List<Room> rooms)
    {
        foreach (Room existingRoom in rooms)
        {
            if (candidate.Overlaps(existingRoom))
                return true;
        }

        return false;
    }

    private void ConnectRooms(Tile[,] map, List<Room> rooms)
    {
        for (int i = 1; i < rooms.Count; i++)
            ConnectRooms(map, rooms[i - 1], rooms[i]);
    }

    private void ConnectRooms(Tile[,] map, Room first, Room second)
    {
        int startX = first.CenterX;
        int startY = first.CenterY;
        int endX = second.CenterX;
        int endY = second.CenterY;

        if (random.Next(2) == 0)
        {
            CarveHorizontalCorridor(map, startX, endX, startY);
            CarveVerticalCorridor(map, startY, endY, endX);
        }
        else
        {
            CarveVerticalCorridor(map, startY, endY, startX);
            CarveHorizontalCorridor(map, startX, endX, endY);
        }
    }

    private static void CarveRoom(Tile[,] map, Room room)
    {
        for (int y = room.Y; y < room.Y + room.Height; y++)
        for (int x = room.X; x < room.X + room.Width; x++)
            map[y, x] = CreateFloor();
    }

    private static void CarveHorizontalCorridor(
        Tile[,] map,
        int startX,
        int endX,
        int y)
    {
        int minX = Math.Min(startX, endX);
        int maxX = Math.Max(startX, endX);

        for (int x = minX; x <= maxX; x++)
            map[y, x] = CreateFloor();
    }

    private static void CarveVerticalCorridor(
        Tile[,] map,
        int startY,
        int endY,
        int x)
    {
        int minY = Math.Min(startY, endY);
        int maxY = Math.Max(startY, endY);

        for (int y = minY; y <= maxY; y++)
            map[y, x] = CreateFloor();
    }

    private static void FillWithWalls(Tile[,] map)
    {
        for (int y = 0; y < map.GetLength(0); y++)
        for (int x = 0; x < map.GetLength(1); x++)
            map[y, x] = CreateWall();
    }

    private static Tile CreateWall() =>
        new(TileType.Wall, false, true);

    private static Tile CreateFloor() =>
        new(TileType.Floor, true, false);

    private static Tile CreateStairsDown() =>
        new(TileType.StairsDown, true, false);

    private class Room
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public int CenterX => X + Width / 2;
        public int CenterY => Y + Height / 2;
        public (int X, int Y) Center => (CenterX, CenterY);

        public Room(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Overlaps(Room other) =>
            X - 1 < other.X + other.Width &&
            X + Width + 1 > other.X &&
            Y - 1 < other.Y + other.Height &&
            Y + Height + 1 > other.Y;
    }
}
