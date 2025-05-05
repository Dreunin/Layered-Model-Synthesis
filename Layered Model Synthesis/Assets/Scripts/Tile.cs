using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// A tile is any object that can be placed in the grid.
/// Attached to a prefab and dictates the rules of placement.
/// A tile is part of a tileset and references other tiles in that tileset.
/// </summary>
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
    public bool allowFreeRotation; //If true, the tile will be randomly rotated upon placement (can't be used with allowRotation)
    public bool dontInstantiate; //If true, the tile will not be instantiated when placed (i.e. only used for neighbour logic)
    [Range(0.05f, 1f)]
    public float weight = 0.5f; //The weight of the tile, used for random selection
    public Vector3Int customSize = new Vector3Int(1, 1, 1);

    public bool IsCustomSize => customSize != Vector3Int.one;
    
    public HashSet<Tile> allowedAbove { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedBelow { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedNorth { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedEast  { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedSouth { get; private set; } = new HashSet<Tile>();
    public HashSet<Tile> allowedWest  { get; private set; } = new HashSet<Tile>();

    private void OnValidate()
    {
        // Update hashsets
        allowedAbove = new HashSet<Tile>(allowedAboveList.Where(t => t != null));
        allowedBelow = new HashSet<Tile>(allowedBelowList.Where(t => t != null));
        allowedNorth = new HashSet<Tile>(allowedNorthList.Where(t => t != null));
        allowedEast  = new HashSet<Tile>(allowedEastList.Where(t => t != null));
        allowedSouth = new HashSet<Tile>(allowedSouthList.Where(t => t != null));
        allowedWest  = new HashSet<Tile>(allowedWestList.Where(t => t != null));
        
        if(allowFreeRotation && allowRotation)
        {
            Debug.LogWarning($"Tile {name} has both allowFreeRotation and allowRotation set to true. This is not allowed.");
            allowFreeRotation = false;
        }
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
    
    public Vector3Int GetRotatedSize(Rotation rot = Rotation.zero)
    {
        return rot switch
        {
            Rotation.zero => customSize,
            Rotation.ninety => new Vector3Int(customSize.z, customSize.y, customSize.x),
            Rotation.oneEighty => new Vector3Int(customSize.x, customSize.y, customSize.z),
            Rotation.twoSeventy => new Vector3Int(customSize.z, customSize.y, customSize.x),
            _ => throw new ArgumentOutOfRangeException(nameof(rot), rot, null)
        };
    }
}
