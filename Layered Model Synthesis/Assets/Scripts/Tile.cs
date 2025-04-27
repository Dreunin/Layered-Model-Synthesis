using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private Tileset tileset;
    [SerializeField] private List<Tile> allowedAboveList;
    [SerializeField] private List<Tile> allowedBelowList; //Since we go from top left to bottom right, we technically don't need to check the below tile
    [SerializeField] private List<Tile> allowedNorthList; //Since we go from top left to bottom right, we technically don't need to check the North tile
    [SerializeField] private List<Tile> allowedEastList;
    [SerializeField] private List<Tile> allowedSouthList;
    [SerializeField] private List<Tile> allowedWestList; //Since we go from top left to bottom right, we technically don't need to check the West tile

    public HashSet<Tile> allowedAbove { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedBelow { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedNorth { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedEast  { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedSouth { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedWest  { get; private set; } = new HashSet<Tile>();

    private void OnValidate()
    {
        allowedAbove = new HashSet<Tile>(allowedAboveList);
        allowedBelow = new HashSet<Tile>(allowedBelowList);
        allowedNorth = new HashSet<Tile>(allowedNorthList);
        allowedEast  = new HashSet<Tile>(allowedEastList);
        allowedSouth = new HashSet<Tile>(allowedSouthList);
        allowedWest  = new HashSet<Tile>(allowedWestList);
    }
    
    public HashSet<Tile> GetAllowed(Direction dir)
    {
        return dir switch
        {
            Direction.ABOVE => allowedAbove,
            Direction.BELOW => allowedBelow,
            Direction.NORTH => allowedNorth,
            Direction.EAST => allowedEast,
            Direction.SOUTH => allowedSouth,
            Direction.WEST => allowedWest,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }
}
