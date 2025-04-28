using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] public Tileset tileset;
    [SerializeField] public List<Tile> allowedAboveList;
    [SerializeField] public List<Tile> allowedBelowList; //Since we go from top left to bottom right, we technically don't need to check the below tile
    [SerializeField] public List<Tile> allowedNorthList; //Since we go from top left to bottom right, we technically don't need to check the North tile
    [SerializeField] public List<Tile> allowedEastList;
    [SerializeField] public List<Tile> allowedSouthList;
    [SerializeField] public List<Tile> allowedWestList; //Since we go from top left to bottom right, we technically don't need to check the West tile
    public bool allowRotation;
    public bool sameRotationWhenStacked; //If true, the tile will always be rotated to the same rotation as the tile below it
    
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
    
    public HashSet<Tile> GetAllowed(Direction dir, Rotation rot = Rotation.zero)
    {
        if (dir == Direction.ABOVE)
        {
            return allowedAbove;
        } 
        if (dir == Direction.BELOW)
        {
            return allowedBelow;
        }
        
        return (((int) dir + (int) rot) % 4) switch //Use enum casts to rotate 90 degrees per angle
        {
            0 => allowedNorth,
            1 => allowedEast,
            2 => allowedSouth,
            3 => allowedWest,
            _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null)
        };
    }
}
