using System;
/// <summary>
/// Extension of the Direction enum. Used to get offsets accounting for rotation or to get the opposite direction.
/// </summary>
public static class DirectionExtensions
{
    public static (int, int, int) ToOffset(this Direction dir)
    {
        return dir switch
        {
            Direction.ABOVE => (0, 1, 0),
            Direction.BELOW => (0, -1, 0),
            Direction.NORTH => (0, 0, 1),
            Direction.EAST => (1, 0, 0),
            Direction.SOUTH => (0, 0, -1),
            Direction.WEST => (-1, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }
    
    public static Direction[] GetDirections() => new Direction[] {Direction.ABOVE, Direction.BELOW, Direction.NORTH, Direction.EAST, Direction.SOUTH, Direction.WEST};

    public static Direction[] GetCardinalDirections() => new Direction[] {Direction.NORTH, Direction.EAST, Direction.SOUTH, Direction.WEST};
    
    public static Direction GetOpposite(this Direction dir)
    {
        return dir switch
        {
            Direction.ABOVE => Direction.BELOW,
            Direction.BELOW => Direction.ABOVE,
            Direction.NORTH => Direction.SOUTH,
            Direction.EAST => Direction.WEST,
            Direction.SOUTH => Direction.NORTH,
            Direction.WEST => Direction.EAST,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }

    public static string GetName(this Direction dir)
    {
        return dir switch
        {
            Direction.ABOVE => "Above",
            Direction.BELOW => "Below",
            Direction.NORTH => "North",
            Direction.EAST => "East",
            Direction.SOUTH => "South",
            Direction.WEST => "West",
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }
}