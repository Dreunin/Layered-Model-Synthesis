using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private HashSet<Tile> allowedAbove;
    [SerializeField] private HashSet<Tile> allowedBelow; //Since we go from top left to bottom right, we technically don't need to check the below tile
    [SerializeField] private HashSet<Tile> allowedNorth; //Since we go from top left to bottom right, we technically don't need to check the North tile
    [SerializeField] private HashSet<Tile> allowedEast; 
    [SerializeField] private HashSet<Tile> allowedSouth;
    [SerializeField] private HashSet<Tile> allowedWest; //Since we go from top left to bottom right, we technically don't need to check the West tile

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
